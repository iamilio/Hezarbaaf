import json, math, random
from PIL import Image, ImageDraw, ImageFilter
src=open('/home/claude/hezarbaaf/tools/Guide/render.py').read().split('# --- render everything')[0]
src=src.replace("D=json.load(open('/tmp/tut/data.json'))","D={}").replace("OUT='/tmp/tut/img/'","OUT='/tmp/fa/img/'")
exec(src)
P=json.load(open('/tmp/fa/proj.json'))
BRICK=(186,98,64); TONE3={1:(214,160,116),2:(176,94,60),3:(98,46,32)}; TONE2={1:(206,150,104),2:(150,72,46)}
GLAZE={1:(212,172,60),2:(46,160,170),3:(36,52,120)}
def mat(b,ntones,i):
    st=b[14]
    if st==8: return (170,214,232)
    if b[17]>0: return GLAZE.get(b[17],(60,160,170))
    if st==7: base=(150,70,44)
    elif st==6: base=(150,96,72)
    elif b[16]>0: base=(TONE3 if ntones==3 else TONE2).get(b[16],BRICK)
    else: base=BRICK
    r=random.Random(i*7919+13); t=1+r.uniform(-0.08,0.08)
    return tuple(max(0,min(255,int(v*t))) for v in base)
def stat(b):
    st=b[14]
    if st==9: return (226,46,40)
    return COL[st]
def axon_p(bs,name,W=1600,H=1000,az=25,el=18,colfn=None,bg=(255,255,255),outline=0.72,ground=True):
    a=math.radians(az); e=math.radians(el)
    def T(x,y,z):
        X=x*math.cos(a)-y*math.sin(a); Y=x*math.sin(a)+y*math.cos(a)
        return (X, -(Y*math.sin(e)+z*math.cos(e)), Y*math.cos(e)-z*math.sin(e))
    allp=[T(*p) for b in bs for p in corners(b)]
    xs=[p[0] for p in allp]; ys=[p[1] for p in allp]
    pad=30; k=min((W-2*pad)/(max(xs)-min(xs)),(H-2*pad)/(max(ys)-min(ys)))
    W2=int((max(xs)-min(xs))*k+2*pad); H2=int((max(ys)-min(ys))*k+2*pad)
    S=4 if W2*H2<1.2e6 else 2
    im=Image.new('RGB',(W2*S,H2*S),bg); d=ImageDraw.Draw(im)
    SS=lambda p:((pad+(p[0]-min(xs))*k)*S,(pad+(p[1]-min(ys))*k)*S)
    faces=[]
    quads=[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7)]
    shade=[0.78,0.9,1.0,0.84,1.1]
    for i,b in enumerate(bs):
        c=corners(b); base=colfn(b,i)
        for q,sh in zip(quads,shade):
            tp=[T(*c[j]) for j in q]
            area=sum(tp[j][0]*tp[(j+1)%4][1]-tp[(j+1)%4][0]*tp[j][1] for j in range(4))
            if area>0: continue
            faces.append((sum(p[2] for p in tp)/4,[SS(p) for p in tp],tuple(min(255,int(v*sh)) for v in base)))
    faces.sort(key=lambda f:-f[0])
    for dep,poly,col in faces: d.polygon(poly,fill=col,outline=tuple(int(v*outline) for v in col))
    im=im.resize((W2,H2),Image.LANCZOS); im.save(OUT+name+'.png'); return name
def elev_p(bs,name,W=1500,colfn=None,bg=(255,255,255),back=False,wires=None,mortar=None):
    pts=[p for b in bs for p in corners(b)]
    xs=[p[0] for p in pts]; zs=[p[2] for p in pts]
    x0,x1,z0,z1=min(xs),max(xs),min(zs),max(zs); pad=24
    k=(W-2*pad)/(x1-x0); H=int((z1-z0)*k+2*pad); S=3
    im=Image.new('RGB',(W*S,H*S),bg); d=ImageDraw.Draw(im)
    Pp=lambda x,z:((pad+((x1-x) if back else (x-x0))*k)*S,(pad+(z1-z)*k)*S)
    order=sorted(bs,key=lambda b:(-b[1] if back else b[1]))
    if mortar:
        for b in order: d.polygon([Pp(*q) for q in hull([(p[0],p[2]) for p in corners(b)])],fill=mortar)
    for i,b in enumerate(order):
        g=[Pp(*q) for q in hull([(p[0],p[2]) for p in corners(b)])]
        if mortar and k*b[13]>4:
            cx=sum(q[0] for q in g)/len(g); cy=sum(q[1] for q in g)/len(g)
            g=[(cx+(x-cx)*0.95,cy+(y-cy)*0.84) for x,y in g]
        d.polygon(g,fill=colfn(b,bs.index(b) if False else i))
    if wires:
        for u0,u1,v in wires: d.line([Pp(u0,v),Pp(u1,v)],fill=(30,110,200),width=3*S)
    im=im.resize((W,H),Image.LANCZOS); im.save(OUT+name+'.png'); return name
M=lambda n: (lambda b,i: mat(b,n,i))
SAT=lambda b,i: stat(b)
# 1 residential façade
b=P['p1']['b']; axon_p(b,'p1_axon',W=1500,H=1500,az=22,el=10,colfn=M(3)); elev_p(b,'p1_elev',W=900,colfn=M(3),mortar=(222,212,198))
axon_p(b,'p1_status',W=900,H=900,az=22,el=10,colfn=SAT)
# 2 screen
b=P['p2']['b']; axon_p(b,'p2_axon',W=1600,H=900,az=18,el=10,colfn=M(0))
elev_p(b,'p2_inside',W=1500,colfn=lambda b,i:(58,40,34),bg=(250,236,200),back=True)
# 3 perforated
b=P['p3']['b']; elev_p(b,'p3_elev',W=1500,colfn=M(0),mortar=(222,212,198),wires=[(w[0],w[1],w[2]) for w in P['p3']['wire']])
axon_p(b,'p3_axon',W=1500,H=800,az=28,el=14,colfn=M(0))
# 4 kufic
b=P['p4']['b']; elev_p(b,'p4_elev',W=1100,colfn=M(0),mortar=(232,224,210)); axon_p(b,'p4_axon',W=1000,H=1000,az=35,el=14,colfn=M(0))
# 5 arcade
b=P['p5']['b']; axon_p(b,'p5_axon',W=1700,H=900,az=20,el=12,colfn=M(2)); elev_p(b,'p5_elev',W=1500,colfn=M(2),mortar=(226,216,200))
# 6 relief
axon_p(P['p6']['b'],'p6_good',W=1500,H=700,az=-25,el=20,colfn=M(0)); axon_p(P['p6']['b'],'p6_status',W=900,H=500,az=-25,el=20,colfn=SAT); axon_p(P['p6bad']['b'],'p6_bad',W=900,H=500,az=-25,el=20,colfn=SAT)
# 7 tower
axon_p(P['p7']['b'],'p7_axon',W=900,H=1400,az=30,el=14,colfn=M(3)); axon_p(P['p7']['b'],'p7_status',W=600,H=900,az=30,el=14,colfn=SAT)
# 8 café
b=P['p8']['b']; axon_p(b,'p8_axon',W=1500,H=1000,az=24,el=10,colfn=M(0)); elev_p(b,'p8_elev',W=1200,colfn=M(0),mortar=(222,212,198))
print('ok')
