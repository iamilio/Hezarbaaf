import json, math
from PIL import Image, ImageDraw, ImageFont
exec(open('/tmp/render_v07.py').read().split('# 1 arches')[0])   # reuse corners/front helpers + fonts
d=json.load(open('/tmp/arches.json'))
k=0.17; W=int(8400*k)+80; top=3200
BG=(246,241,232); MORT=(214,206,192)
img=Image.new('RGB',(W,int(top*k)+190),BG); dr=ImageDraw.Draw(img)
x0,y0=40,110
P=lambda u,v:(x0+u*k, y0+(top-v)*k)
wall_top=max(b[2]+b[13]/2 for b in d['b'] if not b[14])
# 1. mortar fills the whole wall outline
dr.polygon([P(0,0),P(8400,0),P(8400,wall_top),P(0,wall_top)],fill=MORT)
# 2. cut the openings out of the mortar (rectangle below the springing + intrados above it)
def arc_poly(u0,u1,vs,shape):
    span=u1-u0; uc=(u0+u1)/2; pts=[(u0,0 if shape!='seg' else None)]
    out=[]
    N=80
    for i in range(N+1):
        u=u0+span*i/N
        if shape=='semi': v=vs+math.sqrt(max(0,(span/2)**2-(u-uc)**2))
        elif shape=='pointed':
            e=0.3*span; R=span/2+e
            v=vs+min(math.sqrt(max(0,R*R-(u-(uc+e))**2)), math.sqrt(max(0,R*R-(u-(uc-e))**2)))
        elif shape=='seg':
            rise=span/6; R=(span*span/4+rise*rise)/(2*rise); cv=vs+rise-R
            v=cv+math.sqrt(max(0,R*R-(u-uc)**2))
        else: v=vs
        out.append((u,v))
    return out
for (u0,u1,s0,vs,shape) in [(500,1700,0,1700,'semi'),(2500,3700,0,1600,'pointed'),(4600,5900,600,1800,'seg'),(6800,7900,600,2000,'flat')]:
    poly=[(u0,s0)]+arc_poly(u0,u1,vs,shape)+[(u1,s0)]
    dr.polygon([P(u,v) for u,v in poly],fill=BG)
# 3. bricks (slightly inset so joints show)
for b in d['b']:
    pts=front(b)
    cx=sum(p[0] for p in pts)/4; cz=sum(p[1] for p in pts)/4
    g=[P(cx+(x-cx)*1.0,cz+(z-cz)*1.0) for x,z in pts]
    dr.polygon(g,fill=archc if b[14] else clay)
dr.text((40,26),"Arched Openings with mortar · طاق‌ها با ملات",fill=(60,40,30),font=F)
dr.text((40,60),"Wall bricks are cut on the slant to follow each arch; every joint, including the one around the arch, is filled with mortar",fill=(110,95,85),font=Fs)
for u,lab in zip([1100,3100,5250,7350],["semicircular · 2 header rings","pointed","segmental","soldier lintel"]):
    tw=dr.textlength(lab,font=Fs); dr.text((x0+u*k-tw/2, y0+top*k+12),lab,fill=(70,55,45),font=Fs)
img.save('/home/claude/hezarbaaf/docs/arches-mortar.png')
# zoom on the pointed arch
z=img.crop((int(P(2050,0)[0]),int(P(0,3100)[1]),int(P(4150,0)[0]),int(P(0,1300)[1]))).resize((1260,int(1260*((3100-1300)/(4150-2050)))),Image.LANCZOS)
z.save('/home/claude/hezarbaaf/docs/arches-mortar-zoom.png')
print('ok')
