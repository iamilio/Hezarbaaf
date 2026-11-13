import json
from PIL import Image, ImageDraw, ImageFont
C=json.load(open('/tmp/tut/comps.json'))
ICON='/home/claude/hezarbaaf/src/MasonryGH/Icons/'
FP='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'; FB='/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
S=2
f=ImageFont.truetype(FP,13*S); fb=ImageFont.truetype(FB,14*S)
FIELD_OUT=[dict(nick='Fd',name='Field')]
def nick_of(c):
    raw=c.get('raw',[])
    # base(name, nick, desc,...) — for "X ? fa : en" pattern raw = [fa, en, nick, desc...]
    if c['cls'] in ('BrickWallComponent',): return 'BWall'
    if c['cls']=='HezarbaafWallComponent': return 'Hezar'
    if c['cls']=='BrickFormatsComponent': return 'BFormats'
    if c['cls']=='BondsComponent': return 'Bonds'
    if len(raw)>=3 and any('؀'<=ch<='ۿ' for ch in raw[0]) and not '·' in raw[0]: return raw[2]
    return raw[1] if len(raw)>1 else c['cls']
for cls,c in C.items():
    ins=c['in']; outs=c['out'] or FIELD_OUT
    n=max(len(ins),len(outs),2)
    rowh=24*S; top=12*S
    lw=max([f.getlength(i['nick']) for i in ins]+[0])+22*S
    rw=max([f.getlength(o['nick']) for o in outs]+[0])+22*S
    bar=46*S
    W=int(lw+bar+rw); H=int(top*2+n*rowh)
    im=Image.new('RGBA',(W+20*S,H+10*S),(0,0,0,0)); d=ImageDraw.Draw(im)
    ox=10*S; oy=5*S
    d.rounded_rectangle([ox,oy,ox+W,oy+H],radius=8*S,fill=(232,232,232),outline=(40,40,40),width=2*S)
    d.rounded_rectangle([ox+lw,oy+4*S,ox+lw+bar,oy+H-4*S],radius=6*S,fill=(45,45,45))
    nick=nick_of(c)
    try:
        ic=Image.open(ICON+cls+'.png').convert('RGBA').resize((28*S,28*S),Image.NEAREST)
        im.paste(ic,(int(ox+lw+bar/2-14*S),int(oy+10*S)),ic)
    except Exception: pass
    # vertical nickname
    tw=int(fb.getlength(nick))+4; th=18*S
    t=Image.new('RGBA',(tw,th),(0,0,0,0)); ImageDraw.Draw(t).text((2,0),nick,fill='white',font=fb)
    t=t.rotate(90,expand=True)
    avail=H-46*S
    if t.height>avail: t=t.resize((int(t.width*avail/t.height),int(avail)))
    im.paste(t,(int(ox+lw+bar/2-t.width/2),int(oy+42*S+(avail-t.height)/2)),t)
    def rows(items,left):
        k=len(items); y0=oy+H/2-k*rowh/2
        for i,p in enumerate(items):
            y=y0+i*rowh+rowh/2
            if left:
                d.ellipse([ox-6*S,y-6*S,ox+6*S,y+6*S],fill=(255,255,255),outline=(40,40,40),width=2*S)
                d.text((ox+10*S,y-8*S),p['nick'],fill=(20,20,20),font=f)
            else:
                d.ellipse([ox+W-6*S,y-6*S,ox+W+6*S,y+6*S],fill=(255,255,255),outline=(40,40,40),width=2*S)
                d.text((ox+W-10*S-f.getlength(p['nick']),y-8*S),p['nick'],fill=(20,20,20),font=f)
    rows(ins,True); rows(outs,False)
    im.save(f'/tmp/tut/comp/{cls}.png')
print(len(C))
