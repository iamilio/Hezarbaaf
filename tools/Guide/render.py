import json, math
from PIL import Image, ImageDraw, ImageFont
D=json.load(open('/tmp/tut/data.json'))
OUT='/tmp/tut/img/'
FP='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'; FB='/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'
BG=(255,255,255); MORTAR=(212,203,190)
COL={0:(200,116,78),1:(228,170,130),2:(160,90,60),3:(222,140,96),4:(156,82,54),5:(132,96,160),6:(110,90,76),7:(138,67,44),8:(150,205,228),9:(224,50,43)}
TONES={1:(226,176,132),2:(170,100,64),3:(84,40,30)}
def colour(b,mode):
    st=b[14]
    if mode==1:
        if st==8: return COL[8]
        if b[16]>0: return TONES.get(b[16],(120,60,40))
        if b[17]>0: return [(200,116,78),(212,172,60),(64,180,176),(36,52,120)][min(3,b[17])]
        return COL[0]
    return COL[st]
def corners(b):
    cx,cy,cz,xx,xy,xz,yx,yy,hx,hy,hz,L,D_,H,st,taper=b[:16]
    pts=[]
    for sz in (-1,1):
        for sx,sy in [(-1,-1),(1,-1),(1,1),(-1,1)]:
            l=(L-taper) if sy<0 else L
            ts=b[20] if len(b)>21 else 0; te=b[21] if len(b)>21 else 0
            ex=l/2*sx-(te*sz/2 if sx>0 else -ts*sz/2)
            pts.append((cx+xx*ex+yx*D_/2*sy+hx*H/2*sz, cy+xy*ex+yy*D_/2*sy+hy*H/2*sz, cz+xz*ex+hz*H/2*sz))
    return pts
def hull(pts):
    pts=sorted(set(pts))
    if len(pts)<3: return pts
    def cross(o,a,b): return (a[0]-o[0])*(b[1]-o[1])-(a[1]-o[1])*(b[0]-o[0])
    lo=[];up=[]
    for p in pts:
        while len(lo)>=2 and cross(lo[-2],lo[-1],p)<=0: lo.pop()
        lo.append(p)
    for p in reversed(pts):
        while len(up)>=2 and cross(up[-2],up[-1],p)<=0: up.pop()
        up.append(p)
    return lo[:-1]+up[:-1]
def elevation(key,name,W=1400,mode=0,pad=20):
    bs=D[key]
    xs=[p[0] for b in bs for p in corners(b)]; zs=[p[2] for b in bs for p in corners(b)]
    x0,x1,z0,z1=min(xs),max(xs),min(zs),max(zs)
    k=(W-2*pad)/(x1-x0); H=int((z1-z0)*k+2*pad)
    im=Image.new('RGB',(W,H),BG); d=ImageDraw.Draw(im)
    P=lambda x,z:(pad+(x-x0)*k, pad+(z1-z)*k)
    # back to front: smaller y first is front? front=+Y side → draw higher y last
    order=sorted(bs,key=lambda b:b[1])
    for b in order:
        pts=hull([(p[0],p[2]) for p in corners(b)])
        g=[P(*q) for q in pts]
        d.polygon(g,fill=MORTAR)
    for b in order:
        pts=hull([(p[0],p[2]) for p in corners(b)])
        g=[P(*q) for q in pts]
        cx=sum(q[0] for q in g)/len(g); cy=sum(q[1] for q in g)/len(g)
        g=[(cx+(x-cx)*0.94-0.0, cy+(y-cy)*0.86) for x,y in g] if k*b[13]>6 else g
        d.polygon(g,fill=colour(b,mode))
    im.save(OUT+name+'.png'); return name
def axon(key,name,W=1300,H=900,mode=0,az=35,el=30):
    bs=D[key]
    a=math.radians(az); e=math.radians(el)
    def T(x,y,z):
        X=x*math.cos(a)-y*math.sin(a); Y=x*math.sin(a)+y*math.cos(a)
        return (X, -(Y*math.sin(e)+z*math.cos(e)), Y*math.cos(e)-z*math.sin(e))
    allp=[T(*p) for b in bs for p in corners(b)]
    xs=[p[0] for p in allp]; ys=[p[1] for p in allp]
    pad=30; k=min((W-2*pad)/(max(xs)-min(xs)),(H-2*pad)/(max(ys)-min(ys)))
    W2=int((max(xs)-min(xs))*k+2*pad); H2=int((max(ys)-min(ys))*k+2*pad)
    im=Image.new('RGB',(W2,H2),BG); d=ImageDraw.Draw(im)
    S=lambda p:(pad+(p[0]-min(xs))*k, pad+(p[1]-min(ys))*k)
    faces=[]
    for b in bs:
        c=corners(b); base=colour(b,mode)
        quads=[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]
        shade=[0.8,0.92,1.0,0.86,1.12]
        for q,sh in zip(quads,shade):
            tp=[T(*c[i]) for i in q]
            depth=sum(p[2] for p in tp)/4
            faces.append((depth,[S(p) for p in tp],tuple(min(255,int(v*sh)) for v in base)))
    faces.sort(key=lambda f:-f[0])
    for dep,poly,col in faces: d.polygon(poly,fill=col,outline=tuple(int(v*0.72) for v in col))
    im.save(OUT+name+'.png'); return name
def heat(key,name,W=900):
    g=D[key]; R=len(g); C=len(g[0])
    im=Image.new('RGB',(C,R))
    for r in range(R):
        for c in range(C):
            t=max(0,min(1,g[r][c])); im.putpixel((c,R-1-r),(int(240+(92-240)*t),int(214+(38-214)*t),int(170+(26-170)*t)))
    im=im.resize((W,int(W*R/C)),Image.BICUBIC); im.save(OUT+name+'.png')
def motif(key,name,cell=22):
    rows=D[key].split('\n'); R=len(rows); C=max(len(r) for r in rows)
    pal={'A':(200,116,78),'B':(230,150,100),'C':(130,62,40),'G':(212,172,60),'T':(64,180,176),'N':(36,52,120),'.':(250,250,250)}
    im=Image.new('RGB',(C*cell+2,R*cell+2),(255,255,255)); d=ImageDraw.Draw(im)
    f=ImageFont.truetype(FB,int(cell*0.5))
    for r,row in enumerate(rows):
        for c,ch in enumerate(row):
            col=pal.get(ch.upper(),(200,200,200))
            d.rectangle([c*cell+1,r*cell+1,c*cell+cell-1,r*cell+cell-1],fill=col)
            if ch!='.': d.text((c*cell+cell*0.3,r*cell+cell*0.2),ch,fill=(255,255,255) if ch.upper() in 'CNT' else (60,30,20),font=f)
    im.save(OUT+name+'.png')
# --- render everything
elevation('presets','presets'); elevation('brickwall','brickwall')
for k in ['stretcher','stack','english','flemish']: elevation('bond_'+k,'bond_'+k,W=520)
elevation('hezarbaaf','hezarbaaf'); motif('motif_ali','motif_ali')
for k in ['attractor','wave','ripple','noise','bitmap','combine']: heat('f_'+k,'f_'+k)
for k in ['rotate','protrude','remove','perforate']: axon('op_'+k,'op_'+k,W=900,H=600,az=25,el=25)
axon('output','output',W=900,H=600,az=25,el=25); axon('quickfacade','quickfacade',az=20,el=18)
axon('loft','loft',az=28,el=22); axon('section','section',az=60,el=12)
axon('corner','corner',az=35,el=30); elevation('stencilwall','stencilwall'); axon('stencilwall','stencilwall_axon',az=20,el=30)
motif('imagemotif','imagemotif',cell=16); elevation('imagemotif_wall','imagemotif_wall')
elevation('tones','tones',mode=1); elevation('arched','arched'); axon('surface','surface',az=25,el=22)
axon('tower','tower',W=700,H=900,az=30,el=20); elevation('glass','glass',mode=1); elevation('details','details'); axon('details','details_axon',az=25,el=20)
axon('lbwall','lbwall',W=900,H=500,az=20,el=20)
# stencil sheet
st=D['stencils']; cols=4; cell=250; im=Image.new('RGB',(cols*cell,((len(st)+cols-1)//cols)*cell),BG); d=ImageDraw.Draw(im); f=ImageFont.truetype(FP,18)
for i,s in enumerate(st):
    cx=(i%cols)*cell+cell/2; cy=(i//cols)*cell+cell/2-10; kk=0.75
    d.rectangle([cx-cell/2+6,cy-cell/2+16,cx+cell/2-6,cy+cell/2+4],outline=(200,190,175),width=2)
    d.line([cx-110,cy-50*kk,cx+110,cy-50*kk],fill=(40,110,160),width=2)
    d.rectangle([cx-105*kk,cy-50*kk,cx+105*kk,cy+50*kk],outline=(170,160,150))
    d.polygon([(cx+q[0]*kk,cy-q[1]*kk) for q in s['o']],fill=(214,140,100),outline=(120,55,35))
    d.text((cx-cell/2+14,cy+cell/2-36),f"{s['c']}  {abs(s['a'])}°  ×{s['n']}",fill=(60,40,30),font=f)
im.save(OUT+'stencils.png')
# assembly: serpentine numbering
bs=D['assembly']; k=0.6; pad=20
xs=[b[18] for b in bs]; W=int(2000*k+2*pad); H=int(520*k+2*pad)
im=Image.new('RGB',(W,H),BG); d=ImageDraw.Draw(im); f=ImageFont.truetype(FB,11)
courses={}
for b in bs: courses.setdefault(round(b[19]),[]).append(b)
order=[]
for ci,(v,lst) in enumerate(sorted(courses.items())):
    lst=sorted(lst,key=lambda b:b[18]); order+= lst if ci%2==0 else lst[::-1]
pts=[]
for i,b in enumerate(order):
    u,v,L,Hh=b[18],b[19],b[11],b[13]
    x0=pad+(u-L/2)*k; x1=pad+(u+L/2)*k; y1=H-pad-(v-Hh/2)*k; y0=H-pad-(v+Hh/2)*k
    d.rectangle([x0,y0,x1,y1],fill=COL[1] if b[14]==1 else COL[0],outline=MORTAR)
    pts.append(((x0+x1)/2,(y0+y1)/2))
d.line(pts,fill=(40,90,150),width=2)
for i,(x,y) in enumerate(pts): d.text((x-6,y-7),str(i+1),fill='white',font=f)
im.save(OUT+'assembly.png')
# ladybug sensors
faces=D['lbgrid']; a=math.radians(20); e=math.radians(20)
def T(x,y,z):
    X=x*math.cos(a)-y*math.sin(a); Y=x*math.sin(a)+y*math.cos(a); return (X,-(Y*math.sin(e)+z*math.cos(e)))
ps=[T(*v) for q in faces for v in q]; mx=min(p[0] for p in ps); my=min(p[1] for p in ps)
k=800/(max(p[0] for p in ps)-mx); im=Image.new('RGB',(840,int((max(p[1] for p in ps)-my)*k)+40),BG); d=ImageDraw.Draw(im)
for q in faces: d.polygon([(20+(T(*v)[0]-mx)*k,20+(T(*v)[1]-my)*k) for v in q],fill=(250,220,90),outline=(150,110,40))
im.save(OUT+'lbgrid.png')
print('done')
# showcase hero renders
axon('showcase','showcase_hero',W=1800,H=900,mode=1,az=-28,el=18)
axon('showcase','showcase_status',W=1400,H=700,mode=0,az=-28,el=18)
axon('showcase','showcase_back',W=1400,H=700,mode=1,az=150,el=22)
