import re,glob,json
comps={}
for p in sorted(glob.glob('src/MasonryGH/*.cs')):
    s=open(p).read()
    for m in re.finditer(r'public class (\w+) : (?:GH_Component|HzbComponent|FieldComponentBase)', s):
        name=m.group(1)
        nxt=re.search(r'\n    public (?:abstract )?class ', s[m.end():])
        body=s[m.end(): m.end()+nxt.start() if nxt else len(s)]
        c={'cls':name}
        bm=re.search(r': base\((.*?)\)\s*\{\s*\}', body, re.S)
        if bm:
            args=re.findall(r'"((?:[^"\\]|\\.)*)"', bm.group(1))
            c['raw']=args
        ins=re.findall(r'p\.Add(\w+)Parameter\("([^"]+)", "([^"]+)", "((?:[^"\\]|\\.)*)", GH_ParamAccess\.(\w+)(?:, ([^)]*))?\)', body.split('RegisterOutputParams')[0])
        outs=re.findall(r'p\.Add(\w+)Parameter\("([^"]+)", "([^"]+)", "((?:[^"\\]|\\.)*)", GH_ParamAccess\.(\w+)', body.split('RegisterOutputParams')[1] if 'RegisterOutputParams' in body else '')
        c['in']=[dict(type=t,name=n,nick=k,desc=d,access=a,default=(df or '').strip()) for t,n,k,d,a,df in ins]
        c['out']=[dict(type=t,name=n,nick=k,desc=d,access=a) for t,n,k,d,a in outs]
        comps[name]=c
# field subclasses: inputs come from base? print
for k,v in comps.items():
    print(k, len(v['in']), len(v['out']), v.get('raw','')[:3])
json.dump(comps,open('/tmp/tut/comps.json','w'),ensure_ascii=False,indent=1)
