"""Write Grasshopper .ghx files by hand (format learned from a file saved by Grasshopper 8.36)."""
import uuid
from xml.sax.saxutils import escape
LIB = '6f1d2a8e-3b4c-4f2e-9a71-5c0d8e2b7a10'   # Hezarbaaf assembly id
def it(name, tn, code, val, idx=None):
    i = '' if idx is None else f' index="{idx}"'
    return f'<item name="{name}"{i} type_name="{tn}" type_code="{code}">{val}</item>'
S = lambda n, v, idx=None: it(n, 'gh_string', 10, escape(str(v)), idx)
G = lambda n, v, idx=None: it(n, 'gh_guid', 9, v, idx)
B = lambda n, v: it(n, 'gh_bool', 1, 'true' if v else 'false')
I = lambda n, v: it(n, 'gh_int32', 3, v)
D = lambda n, v: it(n, 'gh_double', 6, v)
def chunk(name, items=(), chunks=(), index=None):
    i = '' if index is None else f' index="{index}"'
    s = f'<chunk name="{name}"{i}>'
    if items: s += f'<items count="{len(items)}">' + ''.join(items) + '</items>'
    if chunks: s += f'<chunks count="{len(chunks)}">' + ''.join(chunks) + '</chunks>'
    return s + '</chunk>'
def rectf(n, x, y, w, h): return it(n, 'gh_drawing_rectanglef', 35, f'<X>{x}</X><Y>{y}</Y><W>{w}</W><H>{h}</H>')
def ptf(n, x, y): return it(n, 'gh_drawing_pointf', 31, f'<X>{x}</X><Y>{y}</Y>')
def attrs(x, y, w, h, extra=()): return chunk('Attributes', [rectf('Bounds', x, y, w, h), *extra, ptf('Pivot', x + w / 2, y + h / 2)])
def value_items(kind, v):
    if kind == 'num': return [D('number', v)]
    if kind == 'int': return [I('number', v)]
    if kind == 'bool': return [B('boolean', v)]
    if kind == 'str': return [B('null_string', False), S('string', v)]
    if kind == 'pt': return [it('Coordinate', 'gh_point3d', 51, '<X>%s</X><Y>%s</Y><Z>%s</Z>' % tuple(v))]
    raise ValueError(kind)
def pdata(kind, values):
    items = [chunk('Item', value_items(kind, v), index=k) for k, v in enumerate(values)]
    return chunk('PersistentData', [I('Count', 1)], [chunk('Branch', [I('Count', len(values)), S('Path', '{0}')], items, index=0)])
def param(tag, idx, p, x, y, h):
    """p: dict nick,name,desc,optional,access(0 item/1 list),hidden,source(list of guids),value(kind,[values]),id"""
    items = []
    if p.get('access'): items.append(I('Access', 1))
    items.append(S('Description', p.get('desc', p['name'])))
    if p.get('hidden'): items.append(B('Hidden', True))
    items += [G('InstanceGuid', p['id']), S('Name', p['name']), S('NickName', p['nick']), B('Optional', p.get('optional', False))]
    src = p.get('source') or []
    items += [G('Source', g, k) for k, g in enumerate(src)]
    items.append(I('SourceCount', len(src)))
    ch = [attrs(x, y, 17, h)]
    if p.get('value') and not src: ch.append(pdata(*p['value']))
    return chunk(tag, items, ch, index=idx)
def component(idx, guid, name, nick, desc, ins, outs, x, y, lib=LIB):
    for p in ins + outs: p.setdefault('id', str(uuid.uuid4()))
    n = max(len(ins), len(outs), 1); H = 20 * n + 4
    ch = [attrs(x, y, 80, H)]
    ch += [param('param_input', k, p, x + 2, y + 2 + k * (H - 4) / max(1, len(ins)), (H - 4) / max(1, len(ins))) for k, p in enumerate(ins)]
    ch += [param('param_output', k, p, x + 60, y + 2 + k * (H - 4) / max(1, len(outs)), (H - 4) / max(1, len(outs))) for k, p in enumerate(outs)]
    cont = chunk('Container', [S('Description', desc), G('InstanceGuid', str(uuid.uuid4())), S('Name', name), S('NickName', nick)], ch)
    head = [G('GUID', guid)] + ([G('Lib', lib)] if lib else []) + [S('Name', name)]
    return chunk('Object', head, [cont], index=idx)
def slider(idx, value, lo, hi, x, y, digits=3, gid=None):
    gid = gid or str(uuid.uuid4())
    cont = chunk('Container', [S('Description', 'Numeric slider for single values'), G('InstanceGuid', gid), S('Name', 'Number Slider'), S('NickName', ''), B('Optional', False), I('SourceCount', 0)],
                 [attrs(x, y, 166, 20), chunk('Slider', [I('Digits', digits), I('GripDisplay', 1), I('Interval', 0 if digits else 1), D('Max', hi), D('Min', lo), I('SnapCount', 0), D('Value', value)])])
    return chunk('Object', [G('GUID', '57da07bd-ecab-415d-9d86-af36d7073abc'), S('Name', 'Number Slider')], [cont], index=idx)
def panel(idx, text, x, y, w=160, h=60, gid=None, multiline=True, source=()):
    gid = gid or str(uuid.uuid4())
    props = chunk('PanelProperties', [it('Colour', 'gh_drawing_color', 36, '<ARGB>255;255;250;90</ARGB>'), B('DrawIndices', True), B('DrawPaths', True), B('Multiline', multiline), B('SpecialCodes', False), B('Stream', False), B('Wrap', True)])
    cont = chunk('Container', [S('Description', 'A panel for custom notes and text values'), G('InstanceGuid', gid), S('Name', 'Panel'), S('NickName', ''), B('Optional', False), D('ScrollRatio', 0)] + [G('Source', g, k) for k, g in enumerate(source)] + [I('SourceCount', len(source)), S('UserText', text)],
                 [attrs(x, y, w, h, [I('MarginLeft', 0), I('MarginRight', 0), I('MarginTop', 0)]), props])
    return chunk('Object', [G('GUID', '59e0b89a-e487-49f8-bab8-b5bab16be14c'), S('Name', 'Panel')], [cont], index=idx)
def archive(objects, name='hezarbaaf.ghx', description=''):
    ver = lambda n, a, b, c: it(n, 'gh_version', 80, f'<Major>{a}</Major><Minor>{b}</Minor><Revision>{c}</Revision>')
    col = lambda n, a, r, g, b: it(n, 'gh_drawing_color', 36, f'<ARGB>{a};{r};{g};{b}</ARGB>')
    header = chunk('DocumentHeader', [G('DocumentID', str(uuid.uuid4())), S('Preview', 'Shaded'), I('PreviewMeshType', 1), col('PreviewNormal', 100, 150, 0, 0), col('PreviewSelected', 100, 0, 150, 0)])
    props = chunk('DefinitionProperties', [it('Date', 'gh_date', 8, '639301979656891760'), S('Description', description), B('KeepOpen', False), S('Name', name)],
                  [chunk('Revisions', [I('RevisionCount', 0)]), chunk('Projection', [it('Target', 'gh_drawing_point', 30, '<X>0</X><Y>0</Y>'), it('Zoom', 'gh_single', 5, '1')]), chunk('Views', [I('ViewCount', 0)])])
    objs = chunk('DefinitionObjects', [I('ObjectCount', len(objects))], objects)
    defn = chunk('Definition', [ver('plugin_version', 1, 0, 8)], [header, props, chunk('RcpLayout', [I('GroupCount', 0)]), objs])
    return ('<?xml version="1.0" encoding="utf-8" standalone="yes"?>\n<Archive name="Root">\n<!--Grasshopper archive-->\n<items count="1">'
            + ver('ArchiveVersion', 0, 2, 2) + '</items>\n<chunks count="1">' + defn + '</chunks>\n</Archive>\n')
