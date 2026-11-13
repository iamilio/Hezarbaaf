import json, sys
sys.path.insert(0,'/tmp/tut')
from content import C, ORDER, NAMES
import arabic_reshaper
from bidi.algorithm import get_display
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.lib import colors
from reportlab.lib.styles import ParagraphStyle
from reportlab.platypus import (BaseDocTemplate, PageTemplate, Frame, Paragraph, Spacer, Image, Table, TableStyle,
                                PageBreak, KeepTogether, Preformatted, NextPageTemplate, CondPageBreak)
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from PIL import Image as PI

pdfmetrics.registerFont(TTFont('DV','/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'))
pdfmetrics.registerFont(TTFont('DVB','/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf'))
pdfmetrics.registerFont(TTFont('DVI','/usr/share/fonts/truetype/dejavu/DejaVuSans-Oblique.ttf'))
pdfmetrics.registerFont(TTFont('DVM','/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf'))
from reportlab.lib.fonts import addMapping
addMapping('DV',0,0,'DV'); addMapping('DV',1,0,'DVB'); addMapping('DV',0,1,'DVI'); addMapping('DV',1,1,'DVB')

def fa(s): return get_display(arabic_reshaper.reshape(s))
CLAY=colors.HexColor('#B5562F'); INK=colors.HexColor('#2B1D16'); SOFT=colors.HexColor('#7A6558'); SAND=colors.HexColor('#F6EFE6'); LINE=colors.HexColor('#E3D6C8')
def S(**k):
    base=dict(fontName='DV',fontSize=10,leading=14.5,textColor=INK); base.update(k)
    return ParagraphStyle('x',**base)
body=S(); small=S(fontSize=8.6,leading=11.5,textColor=SOFT); cap=S(fontSize=8.6,leading=11.5,textColor=SOFT,alignment=1)
h1=S(fontName='DVB',fontSize=22,leading=27,textColor=CLAY,spaceAfter=6)
h2=S(fontName='DVB',fontSize=17,leading=21,textColor=INK)
h3=S(fontName='DVB',fontSize=11,leading=15,textColor=CLAY,spaceBefore=8,spaceAfter=3)
tab=S(fontSize=9,textColor=SOFT)
cell=S(fontSize=8.4,leading=10.8)
cellb=S(fontName='DVB',fontSize=8.4,leading=10.8)
step=S(leftIndent=14,firstLineIndent=-14,spaceAfter=3)
IMG='/tmp/tut/img/'; COMP='/tmp/tut/comp/'; ICON='/home/claude/hezarbaaf/src/MasonryGH/Icons/'
COMPS=json.load(open('/tmp/tut/comps.json')); DATA=json.load(open('/tmp/tut/data.json'))
W=A4[0]-3.6*cm

def img(path, maxw=W, maxh=8*cm):
    im=PI.open(path); w,h=im.size; s=min(maxw/w, maxh/h)
    return Image(path, w*s, h*s)

import re as _re
def _ar(t): return any('\u0600'<=ch<='\u06ff' for ch in t)
def en(desc):
    segs=[x for x in desc.split(' · ') if not _ar(x) or _re.sub(r'\s*\([^)]*\)','',x).strip() and not _ar(_re.sub(r'\s*\([^)]*\)','',x))]
    segs=[_re.sub(r'\s*\([^)]*[\u0600-\u06ff][^)]*\)','',x) for x in segs]
    out=' · '.join(segs) or desc
    return out.replace('&','&amp;').replace('<','&lt;').replace('>','&gt;')

def param_table(c):
    rows=[[Paragraph('<b>Input</b>',cell),Paragraph('<b>What it is</b>',cell),Paragraph('<b>Default</b>',cell)]]
    for p in c['in']:
        d=p['default'].replace('"','').replace('Plane.WorldXY','World XY').replace('Point3d.Origin','origin')
        if 'new Plane' in d: d='XZ (façade)'
        if 'L10n' in d: d='current language'
        if d.startswith('Waal (NL'): d='Waal (NL)'
        if d.startswith('Ali (square'): d='Ali (square Kufic)'
        rows.append([Paragraph(f"<b>{p['nick']}</b> {p['name']}",cell),Paragraph(en(p['desc']),cell),Paragraph(d or '—',cell)])
    outs=c['out'] or [dict(nick='Fd',name='Field',desc='Field (0..1 on the façade)')]
    rows.append([Paragraph('<b>Output</b>',cell),Paragraph('<b>What it is</b>',cell),Paragraph('',cell)])
    for p in outs:
        rows.append([Paragraph(f"<b>{p['nick']}</b> {p['name']}",cell),Paragraph(en(p['desc']),cell),Paragraph('',cell)])
    t=Table(rows,colWidths=[3.1*cm,6.3*cm,2.1*cm],repeatRows=0)
    oi=len(c['in'])+1
    st=[('GRID',(0,0),(-1,-1),0.4,LINE),('VALIGN',(0,0),(-1,-1),'TOP'),('BACKGROUND',(0,0),(-1,0),SAND),('BACKGROUND',(0,oi),(-1,oi),SAND),
        ('TOPPADDING',(0,0),(-1,-1),2),('BOTTOMPADDING',(0,0),(-1,-1),2),('LEFTPADDING',(0,0),(-1,-1),4)]
    if len(c['in'])==0: st.append(('BACKGROUND',(0,0),(-1,0),colors.white))
    t.setStyle(TableStyle(st)); return t

def header(cls):
    e,f=NAMES[cls]
    ic=Image(ICON+cls+'.png',0.95*cm,0.95*cm)
    t=Table([[ic,Paragraph(f'{e}',h2),Paragraph(f'<font name="DVB" size="14" color="#B5562F">{fa(f)}</font>',S(alignment=2))]],
            colWidths=[1.25*cm,W-6.25*cm,5*cm])
    t.setStyle(TableStyle([('VALIGN',(0,0),(-1,-1),'MIDDLE'),('LEFTPADDING',(0,0),(-1,-1),0),('LINEBELOW',(0,0),(-1,0),1.2,CLAY),('BOTTOMPADDING',(0,0),(-1,-1),6)]))
    return t

story=[]
# ---------- cover
story.append(Spacer(1,1.2*cm))
story.append(img(IMG+'showcase_hero.png',W,10*cm))
story.append(Spacer(1,0.6*cm))
story.append(Paragraph('Hezarbaaf',S(fontName='DVB',fontSize=40,leading=46,textColor=CLAY)))
story.append(Paragraph(f'<font size="26" color="#B5562F">{fa("هزارباف")}</font>',S(fontName='DVB',leading=34)))
story.append(Spacer(1,0.3*cm))
story.append(Paragraph('Parametric brickwork for Grasshopper · component guide and tutorials',S(fontSize=14,leading=19)))
story.append(Spacer(1,0.4*cm))
story.append(Paragraph('Version 0.8 · Rhino 8 · English guide with Persian component names',small))
story.append(Paragraph('Every picture in this guide was produced by Hezarbaaf\'s own geometry engine, the same code the plugin runs.',small))
story.append(PageBreak())

# ---------- contents
story.append(Paragraph('Contents',h1))
toc=[('Getting started','install, your first wall in one minute, key ideas'),('Showcase','a curved parametric wall with arched openings, step by step')]
for part,cl in ORDER: toc.append((part,', '.join(NAMES[c][0] for c in cl)))
toc.append(('Troubleshooting','the common mistakes and what the messages mean'))
for a,b in toc:
    story.append(Paragraph(f'<b>{a}</b>',S(spaceBefore=5))); story.append(Paragraph(b,small))
story.append(PageBreak())

# ---------- getting started
story.append(Paragraph('Getting started',h1))
story.append(Paragraph('Install',h3))
for i,t in enumerate(['Copy <b>Hezarbaaf.gha</b> and <b>Masonry.Core.dll</b> into Grasshopper\'s Components folder (Grasshopper: <i>File › Special Folders › Components Folder</i>).',
    'On Windows, right-click each file › Properties › tick <b>Unblock</b>.',
    'Restart Rhino 8. Two new tabs appear: <b>Hezarbaaf</b> (the essentials) and <b>Hezarbaaf Advanced</b> (everything else).']):
    story.append(Paragraph(f'{i+1}.&nbsp;&nbsp;{t}',step))
story.append(Paragraph('Your first wall in one minute',h3))
for i,t in enumerate(['Work in a Rhino file in <b>metres</b>. Draw a line about 4 m long.',
    'In Grasshopper, add a <b>Curve</b> parameter, right-click › <i>Set one Curve</i>, and pick the line.',
    'From the Hezarbaaf tab add <b>Presets</b> and <b>Brick Output</b>. Wire Curve → Presets · C, and Presets · W → Brick Output · W.',
    'Change Presets · P from 0 to 13 to see 14 different walls.']):
    story.append(Paragraph(f'{i+1}.&nbsp;&nbsp;{t}',step))
story.append(Spacer(1,4))
story.append(img(IMG+'presets-gallery.png',W,7.5*cm))
story.append(Paragraph('The 14 presets on a 3 m wall.',cap))
story.append(Paragraph('Key ideas',h3))
for t in ['<b>Wall data (W).</b> Most components pass a <i>wall</i> to each other through the W input and output. Start with Brick Wall, Presets or another wall component; operations change the wall; Brick Output, Bake and the site components read it.',
          '<b>Fields (Fd).</b> A field is a number from 0 to 1 over the wall\'s face. It tells an operation where to act and how strongly. Combine fields to make complex designs from simple ones.',
          '<b>Steps.</b> Operations snap to a few steps (e.g. 4 angles), so a mason only needs a few templates and the wall stays buildable.',
          '<b>Colours.</b> Red = buildability problem · light = cut brick · dark = turned · orange = projecting · purple = tapered · deep brown = arch · grey-brown = sill/coping · light blue = glass.',
          '<b>Units.</b> Inputs are in your Rhino model units (metres in this guide). Brick sizes and joints are always in millimetres.',
          '<b>Language.</b> Use the Language component (fa / en) and restart Rhino for Persian names. Persian names also work as input values, e.g. a bond name.']:
    story.append(Paragraph('•&nbsp;&nbsp;'+t,step))
story.append(PageBreak())

# ---------- showcase
story.append(Paragraph('Showcase: a curved parametric wall with arched openings',h1))
story.append(img(IMG+'showcase_hero.png',W,9.5*cm))
story.append(Paragraph(f'8 m S-shaped wall, 3.4 m high: three Persian pointed openings (two windows and a door), bricks turning up to 30° in diagonal waves that calm down around the openings and the ends, and three brick colours from light at the base to dark at the top. {DATA["showcase_count"]} bricks, <b>{DATA["showcase_problems"]} buildability problems</b>, {DATA["showcase_stencils"]} rotation stencils.',cap))
story.append(Paragraph('Build it step by step',h3))
show_steps=[
 '<b>The curve.</b> Draw an S-shaped curve about 8 m long in plan (in Grasshopper: a <i>Graph Mapper</i> or a sine through points, amplitude 0.7 m). Set it in a Curve parameter.',
 '<b>The wall.</b> Brick Wall: C = curve, H = 3.4, F = <i>210x100x50 j25/12</i>. The wider 25 mm head joint leaves room for turned bricks. No OU/OV here.',
 '<b>The openings.</b> Arched Openings: C = same curve, W = the wall. OU = three intervals <i>1.0 To 1.9</i>, <i>3.0 To 4.2</i>, <i>5.3 To 6.2</i>; OV = <i>0.8 To 1.9</i>, <i>0 To 2.0</i>, <i>0.8 To 1.9</i> (a Panel with one interval per line). K = 2 (pointed), S = 0.3.',
 '<b>The wave.</b> Field Wave: A = 35 (diagonal), L = 1.8.',
 '<b>Calm around the openings.</b> Field Attractor: W = curve, P = one point in the middle of each opening, R = 1.2. Field Combine with I = True (invert) gives 1 away from the openings and 0 at them.',
 '<b>Mix.</b> Field Combine: A = the wave, B = the inverted attractor, M = 1 (multiply).',
 '<b>Turn the bricks.</b> Brick Operation: W = the arched wall, Fd = the mix, M = 0 (rotate), A = 30, S = 5.',
 '<b>Colours.</b> Brick Tones: W = the turned wall, F = a Field Wave with A = 90 and a long wavelength (or Field Bitmap with a vertical gradient), N = 3.',
 '<b>Check and build.</b> Brick Output (C = 1 for the colours, 0 for the checks). Then Rotation Stencils, Raj Sheet and Elevation Drawing for the site.']
for i,t in enumerate(show_steps): story.append(Paragraph(f'{i+1}.&nbsp;&nbsp;{t}',step))
story.append(CondPageBreak(9*cm))
t=Table([[img(IMG+'showcase_status.png',W/2-0.3*cm,6*cm),img(IMG+'showcase_back.png',W/2-0.3*cm,6*cm)],
         [Paragraph('Check colours (Brick Output C = 0): no red. Arches deep brown, turned bricks dark.',cap),Paragraph('The back of the wall: the same bricks, seen from behind.',cap)]],colWidths=[W/2,W/2])
t.setStyle(TableStyle([('VALIGN',(0,0),(-1,-1),'TOP')])); story.append(t)
story.append(Paragraph('Why it works',h3))
for tt in ['The <b>wide head joint</b> (25 mm) lets bricks turn 30° without touching. Brick Output would show red if they collided.',
           'The <b>calm zone</b> keeps the bricks straight where the arches spring and at the wall ends, which is where masons need a clean, strong edge.',
           'Only <b>5 steps</b> of rotation, so the whole wall needs a handful of stencils.',
           'Arches keep their own colour; Brick Tones only mixes the wall bricks.']:
    story.append(Paragraph('•&nbsp;&nbsp;'+tt,step))
story.append(PageBreak())

# ---------- components
for part,cl in ORDER:
    story.append(Paragraph(part,S(fontName='DVB',fontSize=26,leading=32,textColor=CLAY,spaceBefore=6*cm)))
    story.append(Paragraph(f'{len(cl)} components: ' + ', '.join(NAMES[c][0] for c in cl),small))
    story.append(PageBreak())
    for cls in cl:
        c=C[cls]; meta=COMPS[cls]
        story.append(header(cls))
        story.append(Paragraph(c['tab'],tab)); story.append(Spacer(1,4))
        story.append(Paragraph(c['what'],body)); story.append(Spacer(1,6))
        diag=img(COMP+cls+'.png',4.6*cm,9*cm)
        t=Table([[diag,param_table(meta)]],colWidths=[5*cm,W-5*cm]); t.setStyle(TableStyle([('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),0)]))
        story.append(t)
        story.append(Paragraph('Try it',h3))
        for i,s_ in enumerate(c['steps']): story.append(Paragraph(f'{i+1}.&nbsp;&nbsp;{s_}',step))
        if c.get('img'):
            story.append(CondPageBreak(6*cm)); story.append(Spacer(1,4))
            story.append(img(IMG+c['img']+'.png',W,c.get('imgh',5.6)*cm)); story.append(Paragraph(c['caption'],cap))
        if c.get('text'):
            story.append(Spacer(1,4))
            story.append(Preformatted(DATA[c['text']][:1400],S(fontName='DVM',fontSize=7.2,leading=9.2,backColor=SAND,borderPadding=6)))
            story.append(Paragraph(c['caption'],cap))
        if c.get('extra'):
            cells=[img(IMG+k+'.png',W/2-0.3*cm,3.0*cm) for k,_ in c['extra']]
            caps=[Paragraph(v,cap) for _,v in c['extra']]
            t=Table([cells,caps],colWidths=[W/2,W/2]); story.append(t)
        if c.get('bonds'):
            ks=['stretcher','stack','english','flemish']
            t=Table([[img(IMG+'bond_'+k+'.png',W/4-0.2*cm,3*cm) for k in ks],[Paragraph(k.title(),cap) for k in ks]],colWidths=[W/4]*4); story.append(t)
        if c.get('tips'):
            story.append(Paragraph('Tips',h3))
            for tt in c['tips']: story.append(Paragraph('•&nbsp;&nbsp;'+tt,step))
        story.append(PageBreak())

# ---------- troubleshooting
story.append(Paragraph('Troubleshooting',h1))
rows=[('A component is red: "W needs a Hezarbaaf wall…, but a Curve is connected"','You wired a curve into a Wall input. Put Brick Wall (or Presets) in between: Curve → Brick Wall → W.'),
 ('The opening starts at the corner of the wall','A single slider in OU/OV becomes "0 To value". Use Construct Domain (start, end) or a Panel like <i>1.2 To 2.4</i>.'),
 ('Red bricks','Hover over Brick Output · Wn to read why. "collides": widen the head joint or turn less. "not enough support": the course below is missing or too far away. "overhang": a course sticks out more than 1/3 of the brick depth; use fewer steps or less amount.'),
 ('Arched Openings warns "the arch rises above the wall"','The crown is taller than the wall. Make the wall taller, or the opening narrower or lower.'),
 ('Pink or red Grasshopper preview over the bricks','Older components on the canvas still show Grasshopper\'s default preview. Right-click their mesh outputs › Preview off. New components start hidden.'),
 ('Bricks face the wrong way','The front of a wall is on the left of the curve direction. Flip the curve (Rhino: Dir / Flip), or use Flip in Quick Façade.'),
 ('Persian names do not show','Language component: fa, then restart Rhino.'),
 ('Everything is tiny or huge','Check Rhino\'s units (File › Properties › Units). Inputs use model units; brick sizes are always in mm.')]
t=Table([[Paragraph(f'<b>{a}</b>',cell),Paragraph(b,cell)] for a,b in rows],colWidths=[6*cm,W-6*cm])
t.setStyle(TableStyle([('GRID',(0,0),(-1,-1),0.4,LINE),('VALIGN',(0,0),(-1,-1),'TOP'),('TOPPADDING',(0,0),(-1,-1),4),('BOTTOMPADDING',(0,0),(-1,-1),4)]))
story.append(t)

def on_page(canv, doc):
    canv.saveState()
    if doc.page>1:
        canv.setFont('DV',8); canv.setFillColor(SOFT)
        canv.drawString(1.8*cm,1.1*cm,'Hezarbaaf · component guide')
        canv.drawRightString(A4[0]-1.8*cm,1.1*cm,str(doc.page))
        canv.setStrokeColor(LINE); canv.line(1.8*cm,1.45*cm,A4[0]-1.8*cm,1.45*cm)
    canv.restoreState()
doc=BaseDocTemplate('/tmp/tut/Hezarbaaf-Guide.pdf',pagesize=A4,leftMargin=1.8*cm,rightMargin=1.8*cm,topMargin=1.6*cm,bottomMargin=1.8*cm,
                    title='Hezarbaaf — component guide',author='Hezarbaaf')
doc.addPageTemplates([PageTemplate(id='p',frames=[Frame(doc.leftMargin,doc.bottomMargin,doc.width,doc.height,id='f')],onPage=on_page)])
doc.build(story)
print('ok')
