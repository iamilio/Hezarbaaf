"""Hezarbaaf component metadata (read from the C# source) + a tiny builder on top of ghx.py."""
import re, glob, os, uuid
from ghx import *
SRC = os.path.join(os.path.dirname(__file__), '..', '..', 'src', 'MasonryGH', '*.cs')
KIND = {'Number': 'num', 'Integer': 'int', 'Text': 'str', 'Boolean': 'bool', 'Point': 'pt'}
PREVIEW = {'Mesh', 'Box', 'Plane', 'Curve', 'Point', 'Generic', 'Brep', 'Surface', 'Line'}
def load():
    comps = {}
    for path in sorted(glob.glob(SRC)):
        s = open(path, encoding='utf-8').read()
        for m in re.finditer(r'public class (\w+) : (GH_Component|HzbComponent|FieldComponentBase|BridgeComponent)', s):
            nxt = re.search(r'\n    public (?:abstract )?class ', s[m.end():])
            body = s[m.end(): m.end() + nxt.start() if nxt else len(s)]
            g = re.search(r'ComponentGuid => new Guid\("([^"]+)"\)', body)
            b = re.search(r': base\((.*?)\)\s*\{', body, re.S)
            strs = re.findall(r'"((?:[^"\\]|\\.)*)"', b.group(1)) if b else []
            c = dict(cls=m.group(1), guid=g.group(1) if g else None, base=m.group(2), strs=strs, ins=[], outs=[])
            parts = body.split('RegisterOutputParams')
            def params(text):
                out = []
                for line in text.split('\n'):
                    pm = re.search(r'p\.Add(\w+)Parameter\("([^"]+)", "([^"]+)", "((?:[^"\\]|\\.)*)", GH_ParamAccess\.(\w+)(?:, (.*))?\);', line)
                    if pm: out.append(dict(type=pm.group(1), name=pm.group(2), nick=pm.group(3), desc=pm.group(4), access=1 if pm.group(5) == 'list' else 0, default=(pm.group(6) or '').strip()))
                return out
            c['ins'] = params(parts[0]); c['outs'] = params(parts[1].split('SolveInstance')[0]) if len(parts) > 1 else []
            for om in re.finditer(r'(?:p|Params\.Input)\[(\d+)\]\.Optional = true', parts[0]):
                i = int(om.group(1))
                if i < len(c['ins']): c['ins'][i]['optional'] = True
            if c['base'] == 'FieldComponentBase' and not c['outs']:
                c['outs'] = [dict(type='Generic', name='Field', nick='Fd', desc='Field (0..1 on the façade) · میدان', access=0, default='')]
            comps[c['cls']] = c
    return comps
META = load()
def default_value(p):
    d = p['default']; k = KIND.get(p['type'])
    if not d or not k: return None
    if k == 'str': mm = re.match(r'"(.*)"$', d); return ('str', [mm.group(1)]) if mm else None
    if k == 'bool': return ('bool', [d == 'true'])
    if k == 'pt': return ('pt', [(0, 0, 0)]) if 'Origin' in d else None
    try: return (k, [int(float(d)) if k == 'int' else float(d)])
    except ValueError: return None

class Doc:
    def __init__(self): self.objs = []; self.out = {}
    def _add(self, make):   # make(index) -> xml
        self.objs.append(make)
    def comp(self, id, cls, x, y, **values):
        """values: nick=number/str/bool/(x,y,z)/list, or nick='@id.Out' / ['@a.W', …] for wires."""
        m = META[cls]; ins = []; outs = []
        en_name = m['strs'][-3] if len(m['strs']) >= 3 else cls   # last three strings are name, nick, description
        if not en_name[:1].isupper(): en_name = re.sub(r'(?<!^)([A-Z])', r' \1', cls.replace('Component', ''))
        for p in m['ins']:
            q = dict(nick=p['nick'], name=p['name'], desc=p['desc'], access=p['access'], optional=p.get('optional', False), value=default_value(p))
            if p['nick'] in values:
                v = values[p['nick']]; vs = v if isinstance(v, list) else [v]
                if isinstance(vs[0], str) and vs[0].startswith('@'): q['source'] = [self.out[r[1:]] for r in vs]
                else: q['value'] = (KIND.get(p['type'], 'str'), vs)
            ins.append(q)
        unknown = set(values) - {p['nick'] for p in m['ins']}
        if unknown: raise KeyError(f'{cls} has no input {unknown}')
        for p in m['outs']:
            q = dict(nick=p['nick'], name=p['name'], desc=p['desc'], access=p['access'], hidden=p['type'] in ('Mesh', 'Box', 'Plane') or (p['type'] == 'Generic' and m['base'] == 'HzbComponent'), id=str(uuid.uuid4()))
            self.out[f"{id}.{p['nick']}"] = q['id']; outs.append(q)
        desc = m['strs'][-1] if m['strs'] else ''
        nick = m['strs'][-2] if len(m['strs']) >= 2 else cls
        self._add(lambda i: component(i, m['guid'], en_name, nick, desc, ins, outs, x, y))
    def native(self, id, guid, name, nick, ins, outs, x, y):
        for q in ins:
            if 'source' in q: q['source'] = [self.out[r[1:]] for r in q['source']]
        for q in outs: q['id'] = str(uuid.uuid4()); self.out[f"{id}.{q['nick']}"] = q['id']
        self._add(lambda i: component(i, guid, name, nick, name, ins, outs, x, y, lib=None))
    def slider(self, id, value, lo, hi, x, y, digits=3):
        gid = str(uuid.uuid4()); self.out[id] = gid
        self._add(lambda i: slider(i, value, lo, hi, x, y, digits, gid))
    def panel(self, id, text, x, y, w=160, h=60, multiline=True, source=None):
        gid = str(uuid.uuid4()); self.out[id] = gid
        src = [self.out[source[1:]]] if source else []
        self._add(lambda i: panel(i, text, x, y, w, h, gid, multiline, src))
    def points(self, id, pts, x, y):
        gid = str(uuid.uuid4()); self.out[id] = gid
        def make(i):
            cont = chunk('Container', [S('Description', 'Contains a collection of three-dimensional points'), G('InstanceGuid', gid), S('Name', 'Point'), S('NickName', 'Pt'), B('Optional', False), I('SourceCount', 0)],
                         [attrs(x, y, 50, 20), pdata('pt', pts)])
            return chunk('Object', [G('GUID', 'fbac3e32-f100-4292-8692-77240a42fd1a'), S('Name', 'Point')], [cont], index=i)
        self._add(make)
    def save(self, path, description=''):
        xml = archive([mk(i) for i, mk in enumerate(self.objs)], os.path.basename(path), description)
        import xml.dom.minidom as md; md.parseString(xml.encode('utf-8'))
        open(path, 'w', encoding='utf-8').write(xml); return len(xml)
