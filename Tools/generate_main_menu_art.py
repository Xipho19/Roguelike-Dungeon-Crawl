"""生成主菜单基础像素美术：背景、标题、边框、纹章、分隔线和按钮框。

生成后运行 enhance_main_menu_background.py 补充旗帜、锁链、台阶与雾气。
"""
from PIL import Image, ImageDraw, ImageFont, ImageFilter
import random, math, os

OUT = 'Assets/Resources/UI/MainMenu'
os.makedirs(OUT, exist_ok=True)
W, H = 640, 300
random.seed(20261123)


def lerp(a, b, t):
    return int(a + (b - a) * t)


def clamp(v, a=0, b=255):
    return max(a, min(b, int(v)))


# ---------- Background ----------
img = Image.new('RGBA', (W, H), (6, 5, 10, 255))
px = img.load()
cx, cy = W // 2, int(H * 0.45)
for y in range(H):
    t = y / (H - 1)
    r = lerp(7, 20, t)
    g = lerp(6, 14, t)
    b = lerp(13, 30, t)
    for x in range(W):
        dx = (x - cx) / W
        dy = (y - cy) / H
        d = math.sqrt(dx * dx * 2.2 + dy * dy * 3.2)
        amb = max(0.0, 1.0 - d * 1.55)
        px[x, y] = (
            clamp(r + amb * 20),
            clamp(g + amb * 10),
            clamp(b + amb * 28),
            255,
        )

draw = ImageDraw.Draw(img)
for row in range(0, 240, 22):
    off = 0 if (row // 22) % 2 == 0 else 26
    for bx in range(-30 + off, W + 30, 52):
        col = (
            18 + random.randint(-4, 5),
            15 + random.randint(-3, 5),
            26 + random.randint(-4, 7),
            255,
        )
        x0, y0 = bx, row
        x1, y1 = bx + 50, row + 20
        draw.rectangle([x0, y0, x1, y1], fill=col)
        draw.line([x0, y0, x1, y0], fill=(32, 27, 45, 255))
        draw.line([x0, y1, x1, y1], fill=(8, 7, 12, 255))
        draw.line([x1, y0, x1, y1], fill=(9, 8, 14, 255))
        if random.random() < 0.18:
            sx = x0 + random.randint(6, 38)
            sy = y0 + random.randint(4, 14)
            draw.line(
                [sx, sy, sx + random.randint(4, 12), sy + random.randint(2, 7)],
                fill=(8, 7, 12, 200),
            )

# floor
draw.rectangle([0, 232, W, H], fill=(10, 8, 14, 255))
for i in range(8):
    y = 236 + i * 8
    draw.line([0, y, W, y], fill=(6, 5, 9, 255))
for x in range(0, W, 42):
    draw.line([x, 232, x + 18, H], fill=(6, 5, 9, 180))
for _ in range(22):
    x = random.randint(0, W)
    y = random.randint(238, H - 4)
    w = random.randint(3, 12)
    h = random.randint(2, 5)
    draw.rectangle([x, y, x + w, y + h], fill=(24, 20, 30, 255))
    draw.rectangle([x, y, x + w, y + 1], fill=(36, 30, 42, 255))

# stone texture
stone = Image.new('RGBA', (W, H), (0, 0, 0, 0))
sd = ImageDraw.Draw(stone)
for row in range(20, 260, 16):
    off = 0 if (row // 16) % 2 == 0 else 18
    for bx in range(0, W + 30, 36):
        c = (
            44 + random.randint(-7, 8),
            39 + random.randint(-6, 7),
            52 + random.randint(-7, 9),
            255,
        )
        x0, y0 = bx + off, row
        sd.rectangle([x0, y0, x0 + 34, y0 + 14], fill=c)
        sd.line([x0, y0, x0 + 34, y0], fill=(78, 66, 84, 255))
        sd.line([x0, y0 + 14, x0 + 34, y0 + 14], fill=(20, 17, 26, 255))

# architecture mask
mask = Image.new('L', (W, H), 0)
md = ImageDraw.Draw(mask)
md.rectangle([34, 28, 92, 252], fill=255)
md.rectangle([548, 28, 606, 252], fill=255)
md.rectangle([26, 24, 100, 48], fill=255)
md.rectangle([540, 24, 614, 48], fill=255)
md.rectangle([30, 238, 96, 258], fill=255)
md.rectangle([544, 238, 610, 258], fill=255)
md.rectangle([196, 104, 444, 248], fill=255)
md.ellipse([196, 32, 444, 178], fill=255)
md.rectangle([236, 104, 404, 248], fill=0)
md.ellipse([236, 72, 404, 176], fill=0)
img.paste(stone, (0, 0), mask)
draw = ImageDraw.Draw(img)

# inner darkness and glow
inner = Image.new('RGBA', (W, H), (0, 0, 0, 0))
id = ImageDraw.Draw(inner)
id.rectangle([236, 104, 404, 248], fill=(4, 3, 7, 255))
id.ellipse([236, 72, 404, 176], fill=(4, 3, 7, 255))
for r in range(70, 0, -4):
    a = int(10 * (1 - r / 70))
    id.ellipse([320 - r, 214 - r * 0.55, 320 + r, 214 + r * 0.55], fill=(150, 100, 40, a))
img = Image.alpha_composite(img, inner)
draw = ImageDraw.Draw(img)

# arch outlines
draw.arc([196, 32, 444, 178], 180, 360, fill=(92, 76, 98, 255), width=3)
draw.line([196, 104, 196, 248], fill=(88, 72, 94, 255), width=3)
draw.line([444, 104, 444, 248], fill=(88, 72, 94, 255), width=3)
draw.arc([236, 72, 404, 176], 180, 360, fill=(12, 9, 16, 255), width=3)
draw.line([236, 104, 236, 248], fill=(12, 9, 16, 255), width=3)
draw.line([404, 104, 404, 248], fill=(12, 9, 16, 255), width=3)
draw.polygon([(316, 30), (324, 30), (330, 42), (324, 58), (316, 58), (310, 42)], fill=(96, 78, 48, 255))
draw.polygon([(318, 34), (322, 34), (326, 42), (322, 52), (318, 52), (314, 42)], fill=(20, 15, 12, 255))

# runes
for i in range(13):
    ang = math.pi + i * math.pi / 12
    rx = 320 + math.cos(ang) * 123
    ry = 104 + math.sin(ang) * 72
    if i % 2 == 0:
        draw.rectangle([rx - 1, ry - 1, rx + 2, ry + 2], fill=(190, 145, 60, 210))
    else:
        draw.line([rx - 2, ry, rx + 2, ry], fill=(130, 96, 45, 170))

for x0, x1 in [(34, 92), (548, 606)]:
    draw.line([x0 + 4, 48, x0 + 4, 250], fill=(75, 63, 80, 255))
    draw.line([x1 - 4, 48, x1 - 4, 250], fill=(16, 13, 20, 255))
    for _ in range(7):
        yy = random.randint(56, 238)
        xx = random.randint(x0 + 8, x1 - 10)
        draw.line([xx, yy, xx + random.randint(2, 7), yy + random.randint(2, 9)], fill=(15, 12, 20, 220))

for _ in range(12):
    x = random.randint(210, 430)
    y = random.randint(50, 240)
    draw.line([x, y, x + random.randint(3, 10), y + random.randint(2, 8)], fill=(12, 9, 16, 190))


def torch(x, y):
    draw.rectangle([x - 3, y, x + 3, y + 24], fill=(48, 32, 22, 255))
    draw.rectangle([x - 8, y + 20, x + 8, y + 24], fill=(28, 20, 16, 255))
    draw.polygon([(x, y - 18), (x - 5, y - 4), (x, y + 2), (x + 5, y - 4)], fill=(210, 88, 36, 255))
    draw.polygon([(x, y - 12), (x - 3, y - 3), (x, y + 1), (x + 3, y - 3)], fill=(255, 176, 70, 255))
    draw.rectangle([x - 1, y - 6, x + 1, y], fill=(255, 230, 150, 255))
    for _ in range(5):
        ex = x + random.randint(-8, 8)
        ey = y - 20 + random.randint(-14, 2)
        draw.rectangle([ex, ey, ex + 1, ey + 1], fill=(255, 180, 80, random.randint(120, 230)))


torch(160, 142)
torch(480, 142)

for _ in range(10):
    x = random.randint(250, 390)
    y = random.randint(242, 292)
    draw.rectangle([x, y, x + random.randint(2, 7), y + random.randint(1, 3)], fill=(55, 45, 38, 255))

# glow layer
glow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
gd = ImageDraw.Draw(glow)
for tx, ty, col, rad in [
    (160, 128, (255, 120, 40), 62),
    (480, 128, (255, 120, 40), 62),
    (320, 214, (190, 130, 50), 105),
]:
    for r in range(rad, 0, -3):
        a = int(28 * (1 - r / rad) ** 1.7)
        gd.ellipse([tx - r, ty - r * 0.8, tx + r, ty + r * 0.8], fill=(col[0], col[1], col[2], a))
glow = glow.filter(ImageFilter.GaussianBlur(5))
img = Image.alpha_composite(img, glow)

draw = ImageDraw.Draw(img)
for _ in range(55):
    x = random.randint(30, W - 30)
    y = random.randint(20, 250)
    a = random.randint(25, 110)
    draw.rectangle([x, y, x + 1, y + 1], fill=(255, 185, 90, a))
for _ in range(14):
    x = random.randint(80, W - 80)
    y = random.randint(120, 250)
    draw.rectangle([x, y, x + 2, y + 2], fill=(255, 205, 120, random.randint(90, 170)))

vig = Image.new('RGBA', (W, H), (0, 0, 0, 0))
vp = vig.load()
for y in range(H):
    for x in range(W):
        dx = (x - W / 2) / (W / 2)
        dy = (y - H / 2) / (H / 2)
        d = math.sqrt(dx * dx * 0.8 + dy * dy)
        a = int(max(0, min(1, (d - 0.55) / 0.55)) * 170)
        vp[x, y] = (0, 0, 0, a)
img = Image.alpha_composite(img, vig)
img.convert('RGB').save(os.path.join(OUT, 'main_menu_background.png'), optimize=True)

# ---------- Frame ----------
fr = Image.new('RGBA', (W, H), (0, 0, 0, 0))
fd = ImageDraw.Draw(fr)
gold = (214, 178, 96, 220)
dim = (140, 110, 58, 150)
bright = (255, 226, 150, 235)
fd.rectangle([14, 14, W - 15, H - 15], outline=dim, width=1)
fd.rectangle([20, 20, W - 21, H - 21], outline=(80, 62, 34, 120), width=1)


def bracket(x, y, sx, sy):
    fd.line([x, y, x + sx * 44, y], fill=gold, width=2)
    fd.line([x, y, x, y + sy * 44], fill=gold, width=2)
    fd.line([x + sx * 6, y + sy * 6, x + sx * 36, y + sy * 6], fill=dim, width=1)
    fd.line([x + sx * 6, y + sy * 6, x + sx * 6, y + sy * 36], fill=dim, width=1)
    fd.polygon(
        [(x + sx * 10, y + sy * 10), (x + sx * 16, y + sy * 10), (x + sx * 16, y + sy * 16), (x + sx * 10, y + sy * 16)],
        outline=bright,
    )


bracket(20, 20, 1, 1)
bracket(W - 21, 20, -1, 1)
bracket(20, H - 21, 1, -1)
bracket(W - 21, H - 21, -1, -1)
for yy in [H // 2 - 18, H // 2 + 18]:
    fd.line([20, yy, 34, yy], fill=dim, width=1)
    fd.line([W - 35, yy, W - 21, yy], fill=dim, width=1)
fd.polygon([(W // 2 - 8, H - 18), (W // 2, H - 26), (W // 2 + 8, H - 18), (W // 2, H - 10)], fill=(120, 92, 48, 180))
for x in range(80, W - 80, 32):
    fd.rectangle([x, 18, x + 1, 22], fill=(150, 120, 62, 110))
    fd.rectangle([x, H - 22, x + 1, H - 18], fill=(150, 120, 62, 110))
fr.save(os.path.join(OUT, 'main_menu_frame.png'), optimize=True)

# ---------- Title ----------
font_path = 'Assets/Resources/UI/ArkPixel12.ttf'
font = ImageFont.truetype(font_path, 60)
TW, TH = 640, 150
title = Image.new('RGBA', (TW, TH), (0, 0, 0, 0))
td = ImageDraw.Draw(title)
text = '地牢回响'
bbox = td.textbbox((0, 0), text, font=font, stroke_width=4)
tw = bbox[2] - bbox[0]
th = bbox[3] - bbox[1]
tx = (TW - tw) // 2 - bbox[0]
ty = (TH - th) // 2 - bbox[1] - 2
td.text((tx + 5, ty + 7), text, font=font, fill=(8, 5, 4, 220), stroke_width=5, stroke_fill=(8, 5, 4, 220))
td.text((tx, ty), text, font=font, fill=(90, 62, 26, 255), stroke_width=4, stroke_fill=(90, 62, 26, 255))
mask = Image.new('L', (TW, TH), 0)
md = ImageDraw.Draw(mask)
md.text((tx, ty), text, font=font, fill=255)
grad = Image.new('RGBA', (TW, TH), (0, 0, 0, 0))
gp = grad.load()
for y in range(TH):
    t = y / TH
    r = lerp(255, 196, t)
    g = lerp(240, 130, t)
    b = lerp(170, 42, t)
    for x in range(TW):
        gp[x, y] = (r, g, b, 255)
title = Image.composite(grad, title, mask)
td = ImageDraw.Draw(title)
for side in (-1, 1):
    if side < 0:
        x1, x2 = 18, tx - 38
    else:
        x1, x2 = tx + tw + 38, TW - 18
    yy = ty + th // 2 + 4
    td.line([x1, yy, x2, yy], fill=(190, 150, 72, 170), width=1)
    td.polygon([(x1, yy - 4), (x1 + 6, yy), (x1, yy + 4), (x1 - 6, yy)], outline=(230, 195, 110, 190))
    td.polygon([(x2, yy - 4), (x2 + 6, yy), (x2, yy + 4), (x2 - 6, yy)], outline=(230, 195, 110, 190))
title.save(os.path.join(OUT, 'main_menu_title.png'), optimize=True)

# ---------- Crest ----------
CW = 256
crest = Image.new('RGBA', (CW, CW), (0, 0, 0, 0))
cd = ImageDraw.Draw(crest)
c = CW // 2
cd.polygon([(c, 8), (CW - 8, c), (c, CW - 8), (8, c)], outline=(214, 178, 96, 170))
cd.polygon([(c, 22), (CW - 22, c), (c, CW - 22), (22, c)], outline=(120, 94, 48, 130))
cd.arc([c - 88, c - 88, c + 88, c + 88], 0, 360, fill=(180, 146, 72, 120), width=2)
for i in range(8):
    a = math.radians(i * 45)
    x = c + math.cos(a) * 88
    y = c + math.sin(a) * 88
    cd.polygon([(x, y - 5), (x + 5, y), (x, y + 5), (x - 5, y)], fill=(230, 195, 110, 160))
cd.rectangle([c - 32, c + 6, c + 32, c + 78], fill=(8, 6, 10, 255))
cd.ellipse([c - 32, c - 30, c + 32, c + 40], fill=(8, 6, 10, 255))
cd.arc([c - 36, c - 34, c + 36, c + 42], 180, 360, fill=(230, 195, 110, 210), width=2)
cd.line([c - 36, c + 6, c - 36, c + 80], fill=(230, 195, 110, 210), width=2)
cd.line([c + 36, c + 6, c + 36, c + 80], fill=(230, 195, 110, 210), width=2)
cd.line([c - 36, c + 80, c + 36, c + 80], fill=(230, 195, 110, 210), width=2)
for r in range(26, 0, -2):
    a = int(16 * (1 - r / 26))
    cd.ellipse([c - r, c + 14 - r * 0.6, c + r, c + 14 + r * 0.6], fill=(190, 130, 50, a))
for i in range(16):
    a = math.radians(i * 22.5)
    x = c + math.cos(a) * 104
    y = c + math.sin(a) * 104
    cd.rectangle([x - 1, y - 1, x + 2, y + 2], fill=(170, 136, 66, 150))
crest.save(os.path.join(OUT, 'main_menu_crest.png'), optimize=True)

# ---------- Divider ----------
DW, DH = 640, 36
div = Image.new('RGBA', (DW, DH), (0, 0, 0, 0))
dd = ImageDraw.Draw(div)
y = DH // 2
for x in range(28, 300, 2):
    a = int(40 + 100 * (x - 28) / 272)
    dd.line([x, y, x + 2, y], fill=(200, 164, 86, min(170, a)), width=1)
for x in range(340, 612, 2):
    a = int(140 - 100 * (x - 340) / 272)
    dd.line([x, y, x + 2, y], fill=(200, 164, 86, max(40, a)), width=1)
dd.polygon([(320, y - 13), (333, y), (320, y + 13), (307, y)], outline=(255, 226, 150, 220))
dd.polygon([(320, y - 7), (326, y), (320, y + 7), (314, y)], fill=(214, 178, 96, 200))
for x in (286, 354):
    dd.polygon([(x, y - 5), (x + 5, y), (x, y + 5), (x - 5, y)], fill=(190, 150, 72, 170))
div.save(os.path.join(OUT, 'main_menu_divider.png'), optimize=True)

# ---------- Button frame ----------
BW, BH = 96, 64
btn = Image.new('RGBA', (BW, BH), (0, 0, 0, 0))
bd = ImageDraw.Draw(btn)
bd.rectangle([2, 2, BW - 3, BH - 3], outline=(212, 174, 92, 235), width=2)
bd.rectangle([6, 6, BW - 7, BH - 7], outline=(80, 62, 36, 220), width=1)
bd.line([8, 8, BW - 9, 8], fill=(255, 226, 150, 150))
bd.line([8, 8, 8, BH - 9], fill=(255, 226, 150, 100))
bd.line([8, BH - 9, BW - 9, BH - 9], fill=(40, 30, 22, 220))
bd.line([BW - 9, 8, BW - 9, BH - 9], fill=(40, 30, 22, 220))
for x, y in [(9, 9), (BW - 15, 9), (9, BH - 15), (BW - 15, BH - 15)]:
    bd.polygon([(x + 3, y), (x + 6, y + 3), (x + 3, y + 6), (x, y + 3)], fill=(255, 220, 130, 220))
for sx, sy in [(1, 1), (-1, 1), (1, -1), (-1, -1)]:
    x = 6 if sx > 0 else BW - 7
    y = 6 if sy > 0 else BH - 7
    bd.line([x, y, x + sx * 10, y], fill=(150, 118, 60, 180), width=1)
    bd.line([x, y, x, y + sy * 10], fill=(150, 118, 60, 180), width=1)
btn.save(os.path.join(OUT, 'main_menu_button_frame.png'), optimize=True)

print('generated', sorted(os.listdir(OUT)))
