import json, math
exec(open('/tmp/tut/render.py').read().split('# --- render everything')[0])
from PIL import Image, ImageDraw
def axon2(groups,name,W=1500,H=1000,az=-30,el=22,zoom=None):
    a=math.radians(az); e=math.radians(el)
    def T(x,y,z):
        X=x*math.cos(a)-y*math.sin(a); Y=x*math.sin(a)+y*math.cos(a)
        return (X, -(Y*math.sin(e)+z*math.cos(e)), Y*math.cos(e)-z*math.sin(e))
    boxes=[]
    for gi,(bs,col_fn) in enumerate(groups):
        for b in bs:
            c=corners(b); tp=[T(*p) for p in c]
            depth=sum(p[2] for p in tp)/8 + (40 if gi==0 else 0)   # mortar sits a little behind its brick
            boxes.append((depth,tp,col_fn(b)))
    xs=[p[0] for bx in boxes for p in bx[1]]; ys=[p[1] for bx in boxes for p in bx[1]]
    if zoom: xs=[min(xs)+(max(xs)-min(xs))*zoom[0], min(xs)+(max(xs)-min(xs))*zoom[1]]; ys=[min(ys)+(max(ys)-min(ys))*zoom[2], min(ys)+(max(ys)-min(ys))*zoom[3]]
    pad=20; k=min((W-2*pad)/(max(xs)-min(xs)),(H-2*pad)/(max(ys)-min(ys)))
    W2=int((max(xs)-min(xs))*k+2*pad); H2=int((max(ys)-min(ys))*k+2*pad)
    im=Image.new('RGB',(W2,H2),(255,255,255)); d=ImageDraw.Draw(im)
    S=lambda p:(pad+(p[0]-min(xs))*k,pad+(p[1]-min(ys))*k)
    boxes.sort(key=lambda bx:-bx[0])
    quads=[(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7),(4,5,6,7),(0,3,2,1)]
    shade=[0.8,0.92,1.0,0.86,1.12,0.7]
    for dep,tp,base in boxes:
        fs=[]
        for q,sh in zip(quads,shade):
            pts=[tp[i] for i in q]
            # back-face culling via 2D winding
            area=sum(pts[i][0]*pts[(i+1)%4][1]-pts[(i+1)%4][0]*pts[i][1] for i in range(4))
            fs.append((sum(p[2] for p in pts)/4,area,pts,sh))
        fs.sort(key=lambda f:-f[0])
        for dz,area,pts,sh in fs[3:]:   # the three faces nearest the viewer
            col=tuple(min(255,int(v*sh)) for v in base)
            d.polygon([S(p) for p in pts],fill=col,outline=tuple(int(v*0.85) for v in col))
    im.save('/home/claude/hezarbaaf/docs/'+name+'.png')
brick=lambda b:(138,67,44) if b[14]==7 else (196,104,66)
grey=lambda b:(206,198,184)
axon2([(D['mortar'],grey),(D['mortar_bricks'],brick)],'mortar-preview',az=-32,el=20)
axon2([(D['mortar'],grey)],'mortar-only',az=-32,el=20)
axon2([(D['mortar'],grey),(D['mortar_bricks'],brick)],'mortar-zoom',W=1400,H=1000,az=-32,el=20,zoom=(0.3,0.75,0.0,0.55))
print('ok')
# front elevation from the real geometry: mortar front faces first, then brick front faces
def front_el(groups,name,W=1400,pad=20):
    polys=[]
    for gi,(bs,col_fn) in enumerate(groups):
        for b in bs:
            c=corners(b)
            # front face = the 4 corners with +Y (indices 2,3 bottom; 6,7 top) — project to (x,z)
            f=[c[3],c[2],c[6],c[7]]
            polys.append((gi,[(p[0],p[2]) for p in f],col_fn(b)))
    xs=[p[0] for _,ps,_ in polys for p in ps]; zs=[p[1] for _,ps,_ in polys for p in ps]
    k=(W-2*pad)/(max(xs)-min(xs)); H=int((max(zs)-min(zs))*k+2*pad)
    im=Image.new('RGB',(W,H),(255,255,255)); d=ImageDraw.Draw(im)
    for gi,ps,col in sorted(polys,key=lambda p:p[0]):
        d.polygon([(pad+(x-min(xs))*k,pad+(max(zs)-z)*k) for x,z in ps],fill=col)
    im.save('/home/claude/hezarbaaf/docs/'+name+'.png')
front_el([(D['mortar'],grey),(D['mortar_bricks'],brick)],'mortar-front')
print('front ok')
