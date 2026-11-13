import json, sys, re
sys.path.insert(0, '/tmp/fa'); sys.path.insert(0, '/tmp/tut')
from content_fa import C, ORDER, PROJ
from content import NAMES
import arabic_reshaper
from bidi.algorithm import get_display
from reportlab.lib.pagesizes import A4
from reportlab.lib.units import cm
from reportlab.lib import colors
from reportlab.platypus import (BaseDocTemplate, PageTemplate, Frame, Spacer, Image, Table, TableStyle,
                                PageBreak, CondPageBreak, Flowable, KeepTogether)
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from PIL import Image as PI

FD = '/tmp/fonts/package/misc/Farsi-Digits/fonts/ttf/'
pdfmetrics.registerFont(TTFont('VZ', FD + 'Vazirmatn-FD-Regular.ttf'))
pdfmetrics.registerFont(TTFont('VZB', FD + 'Vazirmatn-FD-Bold.ttf'))
pdfmetrics.registerFont(TTFont('VZL', FD + 'Vazirmatn-FD-Light.ttf'))
pdfmetrics.registerFont(TTFont('MONO', '/usr/share/fonts/truetype/dejavu/DejaVuSansMono.ttf'))

CLAY = colors.HexColor('#B5562F'); INK = colors.HexColor('#2B1D16'); SOFT = colors.HexColor('#7A6558')
SAND = colors.HexColor('#F6EFE6'); LINE = colors.HexColor('#E3D6C8'); TEAL = colors.HexColor('#2E8C8C')
IMG = '/tmp/tut/img/'; FIMG = '/tmp/fa/img/'; COMP = '/tmp/tut/comp/'; ICON = '/home/claude/hezarbaaf/src/MasonryGH/Icons/'
COMPS = json.load(open('/tmp/tut/comps.json')); DATA = json.load(open('/tmp/tut/data.json')); P = json.load(open('/tmp/fa/proj2.json'))
W = A4[0] - 3.6 * cm
FA_DIG = str.maketrans('0123456789', '۰۱۲۳۴۵۶۷۸۹')

def isar(t): return any('؀' <= ch <= 'ۿ' or ch in '‌' for ch in t)

# ───────── right-to-left paragraph ─────────
class RTL(Flowable):
    """Right-to-left paragraph with **bold** spans, word wrap and justification."""
    def __init__(self, text, size=10, leading=None, font='VZ', bold='VZB', color=INK, align='justify',
                 space_before=0, space_after=0, indent=0, bullet=None, bg=None, pad=0):
        super().__init__()
        self.text, self.size, self.font, self.bold, self.color, self.align = text, size, font, bold, color, align
        self.leading = leading or size * 1.75
        self.sb, self.sa, self.indent, self.bullet, self.bg, self.pad = space_before, space_after, indent, bullet, bg, pad
        toks = []
        text = text.replace(' · ', ' | ').replace('·', '|')
        for i, part in enumerate(re.split(r'\*\*', text)):
            for w in part.split():
                # "4.2،" → "4.2" + "،" so Latin settings keep their own run
                if len(w) > 1 and w[-1] in '،؛' and not isar(w[:-1]):
                    toks.append((w[:-1], i % 2 == 1)); toks.append((w[-1], i % 2 == 1))
                elif len(w) > 1 and w[-1] == ':' and not isar(w[:-1]) and False:
                    pass
                else:
                    toks.append((w, i % 2 == 1))
        self.toks = toks

    def _vis(self, w):
        return get_display(arabic_reshaper.reshape(w)) if isar(w) else w

    def _width(self, w, b):
        return pdfmetrics.stringWidth(self._vis(w), self.bold if b else self.font, self.size)

    def wrap(self, aw, ah):
        self.aw = aw
        avail = aw - self.indent - 2 * self.pad
        sp = pdfmetrics.stringWidth(' ', self.font, self.size)
        lines, cur, cw = [], [], 0
        for w, b in self.toks:
            ww = self._width(w, b)
            if cur and cw + sp + ww > avail:
                lines.append(cur); cur, cw = [], 0
            cw += (sp if cur else 0) + ww; cur.append((w, b, ww))
        if cur: lines.append(cur)
        self.lines, self.sp, self.avail = lines, sp, avail
        self.h = len(lines) * self.leading + self.sb + self.sa + 2 * self.pad
        return aw, self.h

    def _order(self, line):
        # visual order for an RTL line: runs of LTR words keep their order, everything else reverses
        kinds = []
        for w, b, ww in line:
            if isar(w): kinds.append('R')
            elif any(ch.isalnum() and ord(ch) < 0x600 for ch in w): kinds.append('L')
            else: kinds.append('N')
        # a neutral word (=, →, ×, |) between two LTR words belongs to the LTR run
        for i, k in enumerate(kinds):
            if k == 'N':
                l = next((kinds[j] for j in range(i - 1, -1, -1) if kinds[j] != 'N'), None)
                r = next((kinds[j] for j in range(i + 1, len(kinds)) if kinds[j] != 'N'), None)
                kinds[i] = 'L' if (l == 'L' and r == 'L') else 'R'
        runs = []
        for (w, b, ww), k in zip(line, kinds):
            ltr = k == 'L'
            if runs and runs[-1][0] == ltr: runs[-1][1].append((w, b, ww))
            else: runs.append([ltr, [(w, b, ww)]])
        out = []
        for ltr, ws in reversed(runs):
            out.extend(ws if ltr else list(reversed(ws)))
        return out

    def draw(self):
        c = self.canv
        if self.bg:
            c.setFillColor(self.bg); c.roundRect(0, 0, self.aw, self.h - self.sb - self.sa + 0, 4, fill=1, stroke=0)
        y = self.h - self.sb - self.pad - self.size * 1.15
        right = self.aw - self.indent - self.pad
        if self.bullet:
            c.setFont(self.bold, self.size); c.setFillColor(CLAY)
            bv = self._vis(self.bullet)
            c.drawRightString(self.aw - self.pad, y, bv)
        for li, line in enumerate(self.lines):
            vis = self._order(line)
            total = sum(ww for _, _, ww in vis) + self.sp * (len(vis) - 1)
            gap = self.sp
            if self.align == 'justify' and li < len(self.lines) - 1 and len(vis) > 1:
                gap = self.sp + (self.avail - total) / (len(vis) - 1)
                x = right - self.avail
            elif self.align == 'center':
                x = self.pad + (self.aw - 2 * self.pad - total) / 2
            else:
                x = right - total
            for w, b, ww in vis:
                c.setFont(self.bold if b else self.font, self.size); c.setFillColor(self.color)
                c.drawString(x, y, self._vis(w)); x += ww + gap
            y -= self.leading

def P_(t, **k): return RTL(t, **k)
def H1(t): return RTL(t, size=21, font='VZB', color=CLAY, align='right', space_after=8)
def H2(t): return RTL(t, size=15, font='VZB', color=INK, align='right', space_after=2)
def H3(t): return RTL(t, size=11.5, font='VZB', color=CLAY, align='right', space_before=8, space_after=2)
def CAP(t): return RTL(t, size=8.6, font='VZ', color=SOFT, align='center', leading=14, space_after=4)
def SMALL(t): return RTL(t, size=8.6, color=SOFT, align='right', leading=14)
def STEP(i, t): return RTL(t, bullet=f'{i}.'.translate(FA_DIG), indent=16, size=9.8, space_after=2)
def BUL(t): return RTL(t, bullet='•', indent=12, size=9.8, space_after=2)

import os, hashlib
def img(path, maxw=W, maxh=8 * cm):
    im = PI.open(path); w, h = im.size; s = min(maxw / w, maxh / h)
    if path.endswith('.png') and '/Icons/' not in path and '/comp/' not in path:
        os.makedirs('/tmp/fa/jpg', exist_ok=True)
        j = '/tmp/fa/jpg/' + hashlib.md5(path.encode()).hexdigest() + '.jpg'
        if not os.path.exists(j) or os.path.getmtime(j) < os.path.getmtime(path):
            k = min(1.0, 1800 / max(w, h)); im2 = im.convert('RGB')
            if k < 1: im2 = im2.resize((int(w * k), int(h * k)), PI.LANCZOS)
            im2.save(j, quality=86)
        path = j
    t = Image(path, w * s, h * s); t.hAlign = 'CENTER'; return t

# ───────── parameter table ─────────
def fa_desc(desc, nick, name):
    segs = desc.split(' · ')
    out = []
    for s_ in segs:
        m = re.match(r'^(\S+) [^(]*\(([^)]*[؀-ۿ][^)]*)\)', s_)
        if m and not isar(m.group(1)): out.append(f'{m.group(1)} {m.group(2)}'); continue
        if isar(s_): out.append(re.sub(r'\s*\([^)]*\)', lambda mm: mm.group(0) if isar(mm.group(0)) else '', s_).strip())
    if not out: return desc
    return ' · '.join(out)

def defv(d):
    d = d.replace('"', '').replace('Plane.WorldXY', 'XY').replace('Point3d.Origin', 'مبدأ')
    if 'new Plane' in d: d = 'XZ (نما)'
    if 'L10n' in d: d = 'زبان فعلی'
    if d.startswith('Waal (NL'): d = 'Waal (NL)'
    if d.startswith('Ali (square'): d = 'Ali'
    if d == 'true': d = 'روشن'
    if d == 'false': d = 'خاموش'
    return d or '—'

def cell(t, b=False, size=8.2):
    return RTL(t, size=size, font='VZB' if b else 'VZ', align='right', leading=size * 1.6)

def param_table(meta):
    rows = [[cell('پیش‌فرض', True), cell('توضیح', True), cell('ورودی', True)]]
    for p in meta['in']:
        rows.append([cell(defv(p['default'])), cell(fa_desc(p['desc'], p['nick'], p['name'])), cell(f"**{p['nick']}** {p['name']}")])
    outs = meta['out'] or [dict(nick='Fd', name='Field', desc='میدان (۰ تا ۱ روی نما)')]
    rows.append([cell(''), cell('توضیح', True), cell('خروجی', True)])
    for p in outs:
        rows.append([cell(''), cell(fa_desc(p['desc'], p['nick'], p['name'])), cell(f"**{p['nick']}** {p['name']}")])
    t = Table(rows, colWidths=[1.9 * cm, 6.6 * cm, 3.0 * cm])
    oi = len(meta['in']) + 1
    st = [('GRID', (0, 0), (-1, -1), 0.4, LINE), ('VALIGN', (0, 0), (-1, -1), 'TOP'), ('BACKGROUND', (0, 0), (-1, 0), SAND),
          ('BACKGROUND', (0, oi), (-1, oi), SAND), ('TOPPADDING', (0, 0), (-1, -1), 1), ('BOTTOMPADDING', (0, 0), (-1, -1), 1),
          ('LEFTPADDING', (0, 0), (-1, -1), 3), ('RIGHTPADDING', (0, 0), (-1, -1), 3)]
    t.setStyle(TableStyle(st)); return t

def header(cls):
    e, f = NAMES[cls]
    ic = Image(ICON + cls + '.png', 0.95 * cm, 0.95 * cm)
    t = Table([[RTL(e, size=10, color=SOFT, align='left', font='VZ'), RTL(f, size=17, font='VZB', color=CLAY, align='right'), ic]],
              colWidths=[4.5 * cm, W - 5.8 * cm, 1.3 * cm])
    t.setStyle(TableStyle([('VALIGN', (0, 0), (-1, -1), 'MIDDLE'), ('LEFTPADDING', (0, 0), (-1, -1), 0), ('RIGHTPADDING', (0, 0), (-1, -1), 0),
                           ('LINEBELOW', (0, 0), (-1, 0), 1.2, CLAY), ('BOTTOMPADDING', (0, 0), (-1, -1), 5)]))
    return t

class Mono(Flowable):
    """Preformatted Persian text block (RTL lines, no wrap)."""
    def __init__(self, text, size=8.2):
        super().__init__(); self.lines = text.split('\n'); self.size = size; self.lead = size * 1.7
    def wrap(self, aw, ah): self.aw = aw; return aw, len(self.lines) * self.lead + 12
    def draw(self):
        c = self.canv; c.setFillColor(SAND); c.roundRect(0, 0, self.aw, len(self.lines) * self.lead + 12, 4, fill=1, stroke=0)
        y = len(self.lines) * self.lead + 12 - 6 - self.size * 1.2
        for l in self.lines:
            c.setFont('VZ', self.size); c.setFillColor(INK)
            s = get_display(arabic_reshaper.reshape(l))
            w = pdfmetrics.stringWidth(s, 'VZ', self.size)
            if w > self.aw - 12:   # shrink very long lines
                c.setFont('VZ', self.size * (self.aw - 12) / w)
            c.drawRightString(self.aw - 6, y, s); y -= self.lead

story = []
fa = lambda n: str(n).translate(FA_DIG)

# ───────── cover ─────────
story.append(Spacer(1, 0.6 * cm))
story.append(img(FIMG + 'p5_axon.png', W, 8.5 * cm))
story.append(Spacer(1, 0.5 * cm))
story.append(RTL('هزارباف', size=44, font='VZB', color=CLAY, align='right', leading=60))
story.append(RTL('Hezarbaaf', size=18, font='VZ', color=SOFT, align='right', leading=26))
story.append(Spacer(1, 0.2 * cm))
story.append(RTL('آجرکاری پارامتریک و اجرایی برای گرس‌هاپر', size=16, font='VZB', align='right'))
story.append(RTL('راهنمای کامل فارسی: همهٔ کامپوننت‌ها، قواعد اجرایی و نُه پروژهٔ واقعی، قدم به قدم', size=12, align='right'))
story.append(Spacer(1, 0.4 * cm))
story.append(SMALL('نسخهٔ ۰٫۹ · راینو ۸ · همهٔ تصویرهای این راهنما با موتور هندسی خود هزارباف ساخته شده‌اند؛ همان کدی که افزونه اجرا می‌کند. اعداد هر پروژه (تعداد آجر، مشکل‌ها، شابلون‌ها، میلگرد) خروجی واقعی همان پروژه‌اند.'))
story.append(PageBreak())

# ───────── contents ─────────
story.append(H1('فهرست'))
toc = [('شروع کار', 'نصب، اولین دیوار در یک دقیقه، مفاهیم اصلی'),
       ('هزارباف چه کارهایی می‌کند', 'یک نگاه به همهٔ امکانات در یک صفحه'),
       ('نُه پروژهٔ واقعی', 'نمای مسکونی، پوستهٔ نورگذر، دیوار مشبک، کتیبهٔ کوفی، رواق پنج‌وهفت، دیوار موج، برج، نمای کافه، دیوار منحنی با طاق')]
for part, cl in ORDER: toc.append((part, '، '.join(NAMES[c][1] for c in cl)))
toc += [('قواعد اجرایی و منبع‌ها', 'هزارباف چه چیزهایی را بررسی می‌کند و از کجا'), ('رفع اشکال', 'اشتباه‌های رایج و معنی پیام‌ها')]
for a, b in toc:
    story.append(RTL(a, size=11, font='VZB', align='right', space_before=4)); story.append(SMALL(b))
story.append(PageBreak())

# ───────── getting started ─────────
story.append(H1('شروع کار'))
story.append(H3('نصب'))
for i, t in enumerate(['دو فایل **Hezarbaaf.gha** و **Masonry.Core.dll** را در پوشهٔ Components گرس‌هاپر کپی کنید (در گرس‌هاپر: File / Special Folders / Components Folder).',
                       'در ویندوز روی هر فایل راست‌کلیک / Properties / تیک **Unblock** را بزنید.',
                       'راینو ۸ را دوباره باز کنید. دو زبانهٔ تازه می‌بینید: **Hezarbaaf** (کامپوننت‌های اصلی) و **Hezarbaaf Advanced** (بقیه).',
                       'برای نام‌های فارسی: کامپوننت «زبان» را بگذارید، fa بنویسید و راینو را دوباره باز کنید.']):
    story.append(STEP(i + 1, t))
story.append(H3('اولین دیوار در یک دقیقه'))
for i, t in enumerate(['در یک فایل راینو با واحد **متر** یک خط حدود ۴ متری بکشید.',
                       'در گرس‌هاپر یک پارامتر **Curve** بگذارید، راست‌کلیک / Set one Curve و خط را انتخاب کنید.',
                       'از زبانهٔ هزارباف «نمونه‌ها» و «خروجی آجر» را بگذارید. Curve را به C نمونه‌ها و W نمونه‌ها را به W خروجی آجر وصل کنید.',
                       'P را از ۰ تا ۱۳ عوض کنید و ۱۴ دیوار مختلف ببینید.']):
    story.append(STEP(i + 1, t))
story.append(Spacer(1, 4)); story.append(img(IMG + 'presets-gallery.png', W, 6.5 * cm)); story.append(CAP('۱۴ نمونهٔ آماده روی یک دیوار ۳ متری.'))
story.append(H3('مفاهیم اصلی'))
for t in ['**دادهٔ دیوار (W).** بیشتر کامپوننت‌ها یک «دیوار» را از ورودی و خروجی W به هم می‌دهند. با «دیوار آجری»، «نمونه‌ها» یا یک کامپوننت دیوار دیگر شروع کنید؛ عملیات‌ها دیوار را تغییر می‌دهند؛ خروجی آجر، پخت و کامپوننت‌های کارگاه آن را می‌خوانند.',
          '**میدان (Fd).** عددی بین ۰ تا ۱ روی سطح دیوار که به عملیات می‌گوید کجا و چقدر. با ترکیب میدان‌های ساده طرح‌های پیچیده بسازید.',
          '**گام‌ها.** عملیات‌ها به چند گام (مثلاً ۴ زاویه) گرد می‌شوند تا بنّا فقط چند شابلون لازم داشته باشد و دیوار اجرایی بماند.',
          '**رنگ‌ها.** قرمز = مشکل اجرایی · روشن = بُرش‌خورده · تیره = چرخیده · نارنجی = بیرون‌زده · بنفش = گوه‌ای · قهوه‌ای سیر = طاق · قهوه‌ای خاکستری = زیرپنجره و کلاهک · آبی روشن = شیشه.',
          '**واحدها.** ورودی‌ها به واحد مدل راینو هستند (در این راهنما متر). اندازهٔ آجر و بند همیشه میلی‌متر است.',
          '**جهت نما.** نمای دیوار سمت چپ جهت منحنی است. اگر آجرها پشت‌ورو بودند، جهت منحنی را برگردانید.']:
    story.append(BUL(t))
story.append(PageBreak())

# ───────── capabilities overview ─────────
story.append(H1('هزارباف چه کارهایی می‌کند'))
caps = [('دیوار', 'روی هر منحنی، پلی‌لاین با کنج درگیر، اتاق بسته، لافت ۲ یا ۳ منحنی، مقطع منحنی، روی هر سطح، برج و مناره و گنبد.'),
        ('آجر و چینش', '۸ اندازهٔ آماده (وال، دیک، وخت، هیلورسوم، انگلیس، آمریکا، ایرانی ۲۰×۱۰×۵، مربع سنتی ۲۰×۲۰×۴٫۵) یا هر اندازهٔ تایپ‌شده؛ راسته، روی‌هم، انگلیسی، فلمنکی یا چینش دلخواه.'),
        ('بازشو و طاق', 'پنجره و در؛ طاق نیم‌دایره، کم‌خیز، جناغی، پنج‌وهفت و نعل‌درگاه؛ ۱ تا ۳ حلقه؛ برش مایل آجرهای کنار طاق؛ زیرپنجره، کلاهک، لبه‌های مرتب.'),
        ('طراحی با میدان', 'جاذب نقطه و منحنی، موج، موج دایره‌ای، نویز، تصویر و ترکیب آن‌ها؛ برای چرخش، بیرون‌زدگی، تورفتگی، حذف و مشبک، همه با گام‌های اجرایی.'),
        ('رنگ و جنس', '۲ تا ۶ رنگ آجر با پخش نرم، آجر لعابی طلایی و فیروزه‌ای و لاجوردی، آجر شیشه‌ای، ملات جدا برای رندر.'),
        ('هزارباف', 'نقش‌های برجسته مثل پیکسل؛ کوفی بنایی «علی»؛ چرخش، قرینه، چهارگانه، قاب؛ نقش از تصویر.'),
        ('بررسی اجرایی', 'برخورد آجرها، تکیه‌گاه، کرنیش هر رج و کل (TMS 402)، بند و ضخامت طاق (BIA)، میلگرد بستر برای چینش روی‌هم و مشبک.'),
        ('کارگاه', 'متره و هزینه و نفر‌ساعت، رج‌شیت فارسی، شابلون چرخش ۱:۱، نقشهٔ نمای SVG، ترتیب اجرا برای بنّا و ربات، پخت به‌صورت بلاک.'),
        ('تحلیل', 'پل لیدی‌باگ: تابش روی خود آجرها و استفادهٔ نتیجه در طراحی.')]
t = Table([[RTL(b, size=9.4, align='right'), RTL(a, size=10, font='VZB', color=CLAY, align='right')] for a, b in caps], colWidths=[W - 3.4 * cm, 3.4 * cm])
t.setStyle(TableStyle([('LINEBELOW', (0, 0), (-1, -1), 0.4, LINE), ('VALIGN', (0, 0), (-1, -1), 'TOP'), ('TOPPADDING', (0, 0), (-1, -1), 4), ('BOTTOMPADDING', (0, 0), (-1, -1), 4)]))
story.append(t)
story.append(Spacer(1, 8))
grid = [['p2_axon', 'p4_elev', 'p7_axon'], ['p3_elev', 'p8_axon', 'p6_good']]
cells = [[img(FIMG + k + '.png', W / 3 - 0.3 * cm, 4.2 * cm) for k in row] for row in grid]
t = Table(cells, colWidths=[W / 3] * 3); t.setStyle(TableStyle([('VALIGN', (0, 0), (-1, -1), 'MIDDLE'), ('ALIGN', (0, 0), (-1, -1), 'CENTER')]))
story.append(t)
story.append(PageBreak())

# ───────── projects ─────────
story.append(RTL('نُه پروژهٔ واقعی', size=30, font='VZB', color=CLAY, align='right', space_before=5 * cm, leading=44))
story.append(RTL('هر پروژه یک نوع کار واقعی آجری است. برای هر کدام: ایده، تصویر، زنجیرهٔ کامپوننت‌ها با مقدارها، اعداد واقعی (تعداد آجر، مشکل‌ها، شابلون‌ها، میلگرد) و نکته‌های اجرایی. همهٔ مقدارها به متر (واحد مدل) است مگر اندازهٔ آجر و بند.', size=11, align='right'))
story.append(PageBreak())

def stats_table(d):
    rows = [('تعداد آجر', fa(d['n'])), ('سفارش با پرت', fa(d['order'])), ('مشکل اجرایی (قرمز)', fa(d['problems'])),
            ('حالت‌های مختلف آجر', fa(d['variants'])), ('شابلون چرخش', fa(d['stencils'])),
            ('میلگرد بستر', ('—' if not d['wire'] else f"{fa(len(d['wire']))} بند، {fa(d['wirem'])} متر"))]
    t = Table([[RTL(v, size=9.2, font='VZB', align='left'), RTL(k, size=9.2, align='right')] for k, v in rows], colWidths=[3.2 * cm, 3.6 * cm])
    t.setStyle(TableStyle([('BACKGROUND', (0, 0), (-1, -1), SAND), ('LINEBELOW', (0, 0), (-1, -2), 0.4, LINE), ('TOPPADDING', (0, 0), (-1, -1), 1), ('BOTTOMPADDING', (0, 0), (-1, -1), 1)]))
    return t

for pr in PROJ:
    d = P[pr['key']]
    story.append(H1(pr['title']))
    story.append(P_(pr['idea'], size=10))
    story.append(Spacer(1, 4))
    for k, cap, h in pr['imgs']:
        story.append(img(FIMG + k + '.png', W, h * cm)); story.append(CAP(cap))
    if pr.get('side'):
        sd = pr['side']
        if len(sd) == 1:
            k, cap = sd[0]
            t = Table([[stats_table(d), img(FIMG + k + '.png', W - 7.4 * cm, 5.2 * cm)], ['', CAP(cap)]], colWidths=[7.2 * cm, W - 7.2 * cm])
        else:
            t = Table([[img(FIMG + sd[1][0] + '.png', W / 2 - 0.3 * cm, 4.8 * cm), img(FIMG + sd[0][0] + '.png', W / 2 - 0.3 * cm, 4.8 * cm)],
                       [CAP(sd[1][1]), CAP(sd[0][1])]], colWidths=[W / 2, W / 2])
            story.append(t); t = None
        if t is not None:
            t.setStyle(TableStyle([('VALIGN', (0, 0), (-1, -1), 'MIDDLE')])); story.append(t)
    story.append(PageBreak())
    story.append(H2(pr['title']))
    if not (pr.get('side') and len(pr['side']) == 1):
        story.append(H3('اعداد این پروژه')); story.append(stats_table(d))
    if d.get('notes') and pr['key'] in ('p5', 'p7'):
        story.append(Spacer(1, 3)); story.append(SMALL('گزارش: ' + d['notes'][0]))
    story.append(H3('زنجیرهٔ کامپوننت‌ها'))
    rows = [[RTL(s, size=9, align='right'), RTL(c, size=9, font='VZB', align='right', color=CLAY), RTL(fa(i + 1), size=9, font='VZB', align='center', color=SOFT)]
            for i, (c, s) in enumerate(pr['chain'])]
    t = Table(rows, colWidths=[W - 4.4 * cm, 3.6 * cm, 0.8 * cm])
    t.setStyle(TableStyle([('LINEBELOW', (0, 0), (-1, -1), 0.4, LINE), ('VALIGN', (0, 0), (-1, -1), 'TOP'), ('TOPPADDING', (0, 0), (-1, -1), 2), ('BOTTOMPADDING', (0, 0), (-1, -1), 2)]))
    story.append(t)
    story.append(H3('چرا اجرایی است'))
    for w in pr['why']: story.append(BUL(w))
    story.append(PageBreak())

# showcase (existing English-guide project)
story.append(H1('۹. دیوار منحنی S شکل با سه طاق جناغی'))
story.append(P_(f'یک دیوار باغ ۸ متری به شکل S و ارتفاع ۳٫۴ متر: دو پنجره و یک در با طاق جناغی، آجرهایی که در موج‌های مورب تا ۳۰ درجه می‌چرخند و دور بازشوها و دو سر دیوار آرام می‌شوند، و سه رنگ آجر از پایینِ روشن به بالای تیره. {fa(DATA["showcase_count"])} آجر، **{fa(DATA["showcase_problems"])} مشکل اجرایی**، {fa(DATA["showcase_stencils"])} شابلون چرخش.', size=10))
story.append(img(IMG + 'showcase_hero.png', W, 7.2 * cm)); story.append(CAP('رنگ‌ها با C = 1 در خروجی آجر.'))
t = Table([[img(IMG + 'showcase_back.png', W / 2 - 0.3 * cm, 4.6 * cm), img(IMG + 'showcase_status.png', W / 2 - 0.3 * cm, 4.6 * cm)],
           [CAP('پشت دیوار: همان آجرها از پشت.'), CAP('رنگ بررسی (C = 0): بدون قرمز؛ طاق‌ها قهوه‌ای سیر.')]], colWidths=[W / 2, W / 2])
story.append(t)
story.append(H3('زنجیرهٔ کامپوننت‌ها'))
chain = [('منحنی', 'یک منحنی S حدود ۸ متر در پلان با دامنهٔ ۰٫۷ متر'),
         ('دیوار آجری', 'H = 3.4، F = 210x100x50 j25/12 (بند باز برای چرخش)، بدون OU/OV'),
         ('طاق و درگاه', 'OU = 1.0 To 1.9، 3.0 To 4.2، 5.3 To 6.2؛ OV = 0.8 To 1.9، 0 To 2.0، 0.8 To 1.9 (یک Panel با هر بازه در یک خط)؛ K = 2، S = 0.3'),
         ('موج', 'A = 35 (مورب)، L = 1.8'),
         ('جاذب + ترکیب میدان', 'یک نقطه وسط هر بازشو، R = 1.2؛ ترکیب با I = True تا دور بازشوها ۰ شود'),
         ('ترکیب میدان', 'A = موج، B = جاذب وارونه، M = 1 (ضرب)'),
         ('عملیات آجر', 'W = دیوار طاق‌دار، Fd = ترکیب، M = 0، A = 30، S = 5'),
         ('رنگ‌آمیزی آجر', 'میدانی که از پایین به بالا زیاد می‌شود، N = 3'),
         ('کارگاه', 'شابلون چرخش، رج‌شیت و نقشهٔ نما')]
rows = [[RTL(s, size=9, align='right'), RTL(c, size=9, font='VZB', align='right', color=CLAY), RTL(fa(i + 1), size=9, font='VZB', align='center', color=SOFT)] for i, (c, s) in enumerate(chain)]
t = Table(rows, colWidths=[W - 4.4 * cm, 3.6 * cm, 0.8 * cm])
t.setStyle(TableStyle([('LINEBELOW', (0, 0), (-1, -1), 0.4, LINE), ('VALIGN', (0, 0), (-1, -1), 'TOP'), ('TOPPADDING', (0, 0), (-1, -1), 2), ('BOTTOMPADDING', (0, 0), (-1, -1), 2)]))
story.append(t)
story.append(PageBreak())

# ───────── components ─────────
for part, cl in ORDER:
    story.append(RTL(part, size=26, font='VZB', color=CLAY, align='right', space_before=6 * cm, leading=40))
    story.append(SMALL(f'{fa(len(cl))} کامپوننت: ' + '، '.join(NAMES[c][1] for c in cl)))
    story.append(PageBreak())
    for cls in cl:
        c = C[cls]; meta = COMPS[cls]
        story.append(header(cls))
        story.append(RTL(c['tab'], size=8.6, color=SOFT, align='right', leading=15)); story.append(Spacer(1, 2))
        story.append(P_(c['what'], size=10)); story.append(Spacer(1, 5))
        diag = img(COMP + cls + '.png', 4.6 * cm, 9 * cm)
        t = Table([[diag, param_table(meta)]], colWidths=[5 * cm, W - 5 * cm])
        t.setStyle(TableStyle([('VALIGN', (0, 0), (-1, -1), 'TOP'), ('LEFTPADDING', (0, 0), (-1, -1), 0), ('RIGHTPADDING', (0, 0), (-1, -1), 0)]))
        story.append(t)
        story.append(H3('امتحان کنید'))
        for i, s_ in enumerate(c['steps']): story.append(STEP(i + 1, s_))
        if c.get('img'):
            story.append(CondPageBreak(6 * cm)); story.append(Spacer(1, 4))
            story.append(img(IMG + c['img'] + '.png', W, c.get('imgh', 5.4) * cm)); story.append(CAP(c['caption']))
        if c.get('text'):
            story.append(Spacer(1, 4))
            txt = DATA[c['text']]
            if c['text'] == 'raj_fa': txt = '\n'.join(txt.split('\n')[:8])
            story.append(Mono(txt)); story.append(CAP(c['caption']))
        if c.get('extra'):
            cells = [img(IMG + k + '.png', W / 2 - 0.3 * cm, 3.0 * cm) for k, _ in c['extra']]
            caps = [CAP(v) for _, v in c['extra']]
            t = Table([cells, caps], colWidths=[W / 2, W / 2]); story.append(t)
        if c.get('extra_full'):
            k, v = c['extra_full']
            story.append(CondPageBreak(6 * cm)); story.append(img(FIMG + k + '.png', W, 5.2 * cm)); story.append(CAP(v))
        if c.get('bonds'):
            ks = [('stretcher', 'راسته'), ('stack', 'روی‌هم'), ('english', 'انگلیسی'), ('flemish', 'فلمنکی')]
            t = Table([[img(IMG + 'bond_' + k + '.png', W / 4 - 0.2 * cm, 3 * cm) for k, _ in reversed(ks)], [CAP(n) for _, n in reversed(ks)]], colWidths=[W / 4] * 4)
            story.append(t)
        if c.get('tips'):
            story.append(H3('نکته‌ها'))
            for tt in c['tips']: story.append(BUL(tt))
        story.append(PageBreak())

# ───────── rules ─────────
story.append(H1('قواعد اجرایی و منبع‌ها'))
story.append(P_('هزارباف هر دیوار را با این قواعد بررسی می‌کند. هر قاعده در آبان ۱۴۰۵ با یک منبع منتشرشده تطبیق داده شد. این بررسی‌ها کمک طراحی‌اند، نه طراحی سازه: دیوار بلند، باربر یا در معرض باد و زلزله را حتماً مهندس سازه بررسی کند (در ایران: مبحث هشتم مقررات ملی ساختمان و استاندارد ۲۸۰۰).', size=10))
rules = [('بیرون‌زدگی هر رج (کرنیش)', 'کمتر از نصف ارتفاع اسمی یا یک‌سوم عمق اسمی آجر (وال: ۳۱، وخت: ۲۶ میلی‌متر)', 'TMS 402؛ BIA Brick Brief: Corbels'),
         ('بیرون‌زدگی کل', 'حداکثر نصف عمق اسمی آجر نسبت به نمای زیر (وال: ۵۵ میلی‌متر)', 'همان'),
         ('تکیه‌گاه', 'دست‌کم ۴۰ درصد طول آجر روی آجر زیر، نعل‌درگاه یا طاق', 'پیش‌فرض هزارباف'),
         ('بند طاق', '۵ میلی‌متر زیر طاق تا ۱۹ میلی‌متر پشت طاق؛ بیرون از آن آجر تراشیده', 'BIA Technical Note 31 (۳ تا ۱۹ میلی‌متر)'),
         ('ضخامت طاق', 'دست‌کم یک‌دوازدهم دهانه و ۱۰ سانت؛ نعل‌درگاه ۱۰ سانت + یک‌دوازدهم دهانه و دست‌کم ۲۰ سانت', 'BIA Technical Note 31'),
         ('شعاع آجر معمولی', '(L + j)·D / (2(j − jmin)) — آجر ایرانی حدود ۲٫۱ متر', 'BIA Curved Brick Walls'),
         ('میلگرد بستر', 'وقتی بیشتر بندها روی هم‌اند (چینش روی‌هم) یا دیوار مشبک است: هر ۴۵ سانت در آن ناحیه', 'TMS 402؛ مجلهٔ STRUCTURE'),
         ('آجر چرخیده', 'بند عمودی باز می‌شود؛ تکه‌آجرها کمتر می‌چرخند', 'هندسه'),
         ('بندکشی توکار', 'یادداشت: برای نمای بارانی یا یخبندان توصیه نمی‌شود؛ توکار عمیق ۱۰ میلی‌متر', 'BDA؛ راهنمای سازندگان')]
rows = [[cell('منبع', True, 8.6), cell('مقدار در هزارباف', True, 8.6), cell('قاعده', True, 8.6)]] + [[cell(c_, False, 8.4), cell(b_, False, 8.4), cell(a_, True, 8.4)] for a_, b_, c_ in rules]
t = Table(rows, colWidths=[4.2 * cm, W - 8 * cm, 3.8 * cm])
t.setStyle(TableStyle([('GRID', (0, 0), (-1, -1), 0.4, LINE), ('BACKGROUND', (0, 0), (-1, 0), SAND), ('VALIGN', (0, 0), (-1, -1), 'TOP')]))
story.append(t)
story.append(H3('فن‌های ایرانی'))
for t_ in ['**هزارباف**: آجرکاری برجسته با ظاهر بافته (سلجوقی، قرن ۵ و ۶ هجری) — دیوار هزارباف و نقش‌ها.',
           '**بنّایی**: آجر لعابی در کنار آجر ساده، اغلب برای نوشتن نام‌های مقدس — حرف‌های G، T و N.',
           '**کوفی بنایی**: خط کوفی با آجر مربع — نقش «علی» و آجر مربع سنتی ۲۰×۲۰×۴٫۵.',
           '**طاق پنج‌وهفت**: طاق تیزهٔ چهارمرکزی شاخص معماری ایرانی؛ زاویهٔ کمان پایهٔ حدود ۴۵ تا ۵۵ درجه کمترین رانش را دارد (حیاتی و همکاران، ۱۴۰۴).',
           'واژه‌ها هنوز باید با بنّایان و معماران آجرکار بررسی شوند؛ فهرست کامل در docs/GLOSSARY.fa.md.']:
    story.append(BUL(t_))
story.append(PageBreak())

# ───────── troubleshooting ─────────
story.append(H1('رفع اشکال'))
rows = [('کامپوننت قرمز است: «W به دیوار هزارباف نیاز دارد… ولی Curve وصل شده»', 'منحنی را به ورودی دیوار داده‌اید. «دیوار آجری» (یا نمونه‌ها) را وسط بگذارید: Curve ← دیوار آجری ← W.'),
        ('بازشو از گوشهٔ دیوار شروع می‌شود', 'یک اسلایدر تنها در OU/OV به «0 To عدد» تبدیل می‌شود. از Construct Domain یا Panel با متنی مثل 1.2 To 2.4 استفاده کنید.'),
        ('آجرهای قرمز', 'روی Wn در خروجی آجر بروید تا دلیل را بخوانید. «برخورد»: بند را باز کنید یا کمتر بچرخانید. «تکیه‌گاه ناکافی»: رج زیر نیست یا دور است. «بیرون‌زدگی زیاد»: گام هر رج بیش از حد است؛ گام بیشتر یا مقدار کمتر. «بیش از نصف عمق»: کل برجستگی را کم کنید.'),
        ('«طاق از دیوار بلندتر است»', 'تیزهٔ طاق بالاتر از سر دیوار است. دیوار را بلندتر یا دهانه را باریک‌تر یا پایین‌تر کنید.'),
        ('«طاق برای این دهانه نازک است»', 'N (تعداد حلقه) را بیشتر کنید یا Rg را ۰ بگذارید (آجر از طول).'),
        ('میلگرد بستر پیشنهاد می‌شود', 'چینش روی‌هم یا مشبک است. خطوط Wr را در نقشه بیاورید یا چینش راسته انتخاب کنید.'),
        ('پیش‌نمایش صورتی یا قرمز گرس‌هاپر روی آجرها', 'کامپوننت‌های قدیمی هنوز پیش‌نمایش پیش‌فرض دارند. روی خروجی مش راست‌کلیک / Preview را خاموش کنید.'),
        ('آجرها رو به داخل‌اند', 'نمای دیوار سمت چپ جهت منحنی است. جهت منحنی را برگردانید (Dir / Flip) یا در نمای سریع Fl را روشن کنید.'),
        ('نام‌ها فارسی نمی‌شوند', 'کامپوننت زبان: fa، بعد راینو را دوباره باز کنید.'),
        ('همه‌چیز خیلی کوچک یا بزرگ است', 'واحد راینو را ببینید (File / Properties / Units). ورودی‌ها به واحد مدل‌اند و اندازهٔ آجر همیشه میلی‌متر.')]
t = Table([[RTL(b, size=9, align='right'), RTL(a, size=9, font='VZB', align='right')] for a, b in rows], colWidths=[W - 6 * cm, 6 * cm])
t.setStyle(TableStyle([('GRID', (0, 0), (-1, -1), 0.4, LINE), ('VALIGN', (0, 0), (-1, -1), 'TOP'), ('TOPPADDING', (0, 0), (-1, -1), 3), ('BOTTOMPADDING', (0, 0), (-1, -1), 3)]))
story.append(t)

def on_page(canv, doc):
    canv.saveState()
    if doc.page > 1:
        canv.setFont('VZ', 8); canv.setFillColor(SOFT)
        canv.drawRightString(A4[0] - 1.8 * cm, 1.1 * cm, get_display(arabic_reshaper.reshape('هزارباف | راهنمای فارسی')))
        canv.drawString(1.8 * cm, 1.1 * cm, fa(doc.page))
        canv.setStrokeColor(LINE); canv.line(1.8 * cm, 1.45 * cm, A4[0] - 1.8 * cm, 1.45 * cm)
    canv.restoreState()

doc = BaseDocTemplate('/tmp/fa/Hezarbaaf-Guide.fa.pdf', pagesize=A4, leftMargin=1.8 * cm, rightMargin=1.8 * cm, topMargin=1.6 * cm, bottomMargin=1.8 * cm,
                      title='هزارباف — راهنمای کامل فارسی', author='Hezarbaaf')
doc.addPageTemplates([PageTemplate(id='p', frames=[Frame(doc.leftMargin, doc.bottomMargin, doc.width, doc.height, id='f')], onPage=on_page)])
doc.build(story)
print('ok')
