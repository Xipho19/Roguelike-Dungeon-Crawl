"""在主菜单基础背景上补充旗帜、锁链、台阶、雾、光柱和符文。

先运行 generate_main_menu_art.py，再运行本脚本。
"""
from PIL import Image, ImageDraw, ImageFilter
import random
import math

PATH = 'Assets/Resources/UI/MainMenu/main_menu_background.png'
img = Image.open(PATH).convert('RGBA')
W, H = img.size
random.seed(777)
draw = ImageDraw.Draw(img)


def banner(x0, x1):
    top, bottom = 58, 172
    points = [
        (x0, top), (x1, top), (x1 - 2, bottom - 12), (x1 - 7, bottom),
        (x0 + 7, bottom), (x0 + 2, bottom - 12),
    ]
    draw.polygon(points, fill=(48, 16, 24, 255))
    draw.line(points + [points[0]], fill=(24, 8, 12, 255), width=2)
    draw.line([(x0 + 4, top + 4), (x1 - 4, top + 4)], fill=(150, 112, 52, 230))
    draw.line([(x0 + 5, bottom - 8), (x1 - 5, bottom - 8)], fill=(120, 86, 40, 200))
    for i in range(1, 4):
        x = x0 + (x1 - x0) * i / 4
        draw.line([(x, top + 6), (x, bottom - 10)], fill=(70, 22, 32, 180))
    cx, cy = (x0 + x1) // 2, 95
    draw.polygon([(cx, cy - 16), (cx + 12, cy), (cx, cy + 16), (cx - 12, cy)], outline=(180, 140, 68, 220))
    draw.polygon([(cx, cy - 8), (cx + 6, cy), (cx, cy + 8), (cx - 6, cy)], fill=(160, 120, 56, 200))
    for i in range(5):
        xx = x0 + 6 + i * 7
        draw.line([(xx, bottom - 2), (xx + random.randint(-3, 3), bottom + random.randint(2, 8))],
                  fill=(40, 14, 20, 220))


banner(106, 146)
banner(494, 534)

for cx in (121, 519):
    for y in range(22, 60, 7):
        draw.ellipse([cx - 2, y, cx + 2, y + 4], outline=(96, 90, 98, 220))
        draw.line([(cx, y + 1), (cx, y + 3)], fill=(150, 140, 130, 180))

steps = [(250, 390, 258, 270), (260, 380, 248, 258), (270, 370, 238, 248), (282, 358, 228, 238)]
for x0, x1, y0, y1 in steps:
    draw.rectangle([x0, y0, x1, y1], fill=(27, 22, 32, 255))
    draw.line([(x0, y0), (x1, y0)], fill=(66, 52, 64, 255))
    draw.line([(x0, y1), (x1, y1)], fill=(8, 6, 10, 255))

glow = Image.new('RGBA', (W, H), (0, 0, 0, 0))
gd = ImageDraw.Draw(glow)
for r in range(110, 0, -4):
    a = int(13 * (1 - r / 110))
    gd.ellipse([320 - r, 220 - r * 0.55, 320 + r, 220 + r * 0.55], fill=(190, 130, 50, a))
img = Image.alpha_composite(img, glow.filter(ImageFilter.GaussianBlur(9)))

shaft = Image.new('RGBA', (W, H), (0, 0, 0, 0))
sd = ImageDraw.Draw(shaft)
for tx in (160, 480):
    sd.polygon([(tx - 7, 125), (tx + 7, 125), (tx + 50, 275), (tx - 50, 275)], fill=(255, 120, 45, 10))
img = Image.alpha_composite(img, shaft.filter(ImageFilter.GaussianBlur(12)))

fog = Image.new('RGBA', (W, H), (0, 0, 0, 0))
fd = ImageDraw.Draw(fog)
for cx, cy, rx, ry in [(160, 260, 90, 22), (480, 260, 90, 22), (320, 245, 130, 28)]:
    fd.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], fill=(120, 100, 150, 18))
img = Image.alpha_composite(img, fog.filter(ImageFilter.GaussianBlur(10)))

draw = ImageDraw.Draw(img)
for _ in range(55):
    x = random.randint(40, W - 40)
    y = random.randint(40, 240)
    if 190 < x < 450 and 30 < y < 250:
        continue
    draw.rectangle([x, y, x + random.randint(1, 3), y + random.randint(1, 2)],
                   fill=(38, 56, 40, random.randint(35, 90)))

for x in (58, 574):
    for yy in (90, 130, 170, 210):
        draw.rectangle([x - 2, yy, x + 3, yy + 1], fill=(168, 132, 64, 130))
        draw.rectangle([x - 1, yy - 2, x + 2, yy + 3], fill=(168, 132, 64, 90))

for i in range(9):
    ang = math.pi + i * math.pi / 8
    x = 320 + math.cos(ang) * 112
    y = 104 + math.sin(ang) * 64
    draw.rectangle([x - 1, y - 1, x + 2, y + 2], fill=(220, 170, 80, 190))
    draw.line([(x - 3, y + 4), (x + 3, y + 4)], fill=(140, 104, 46, 150))

for _ in range(18):
    x = random.randint(230, 410)
    y = random.randint(252, 292)
    if random.random() < 0.3:
        draw.rectangle([x, y, x + 1, y + 1], fill=(220, 170, 70, random.randint(90, 170)))
    else:
        draw.rectangle([x, y, x + random.randint(2, 6), y + random.randint(1, 2)], fill=(50, 40, 36, 220))

haze = Image.new('RGBA', (W, H), (0, 0, 0, 0))
hd = ImageDraw.Draw(haze)
hd.ellipse([80, 10, 560, 140], fill=(80, 60, 110, 10))
img = Image.alpha_composite(img, haze.filter(ImageFilter.GaussianBlur(18)))
img.convert('RGB').save(PATH, optimize=True)
print('enhanced', PATH)
