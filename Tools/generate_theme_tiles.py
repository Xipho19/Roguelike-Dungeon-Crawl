from PIL import Image
import os
import random

ROOT = os.path.join("Assets", "Art", "ThirdParty", "0x72_DungeonTilesetII")
OUT_ROOT = os.path.join(ROOT, "theme-variants")
SRC_FLOOR = os.path.join(ROOT, "dungeon-floor")
SRC_WALL = os.path.join(ROOT, "dungeon-walls", "wall_mid.png")

THEMES = ["sewer", "overgrown", "obsidian"]


def to_gray(im):
    im = im.convert("RGBA")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            # 保留原图明暗层次，整体提亮，最终由 FloorTheme 的 Tint 上色
            l = int(0.299 * r + 0.587 * g + 0.114 * b)
            v = max(0, min(255, int(round(l * 1.35))))
            px[x, y] = (v, v, v, a)
    return im


def put(px, x, y, value):
    if 0 <= x < 16 and 0 <= y < 16:
        px[x, y] = (value, value, value, 255)


def puddle(px, rng):
    cx = rng.randint(3, 12)
    cy = rng.randint(4, 12)
    for dy in range(-1, 2):
        for dx in range(-2, 3):
            if abs(dx) / 2.0 + abs(dy) <= 1.0:
                put(px, cx + dx, cy + dy, 24)
    put(px, cx - 1, cy, 215)
    put(px, cx + 1, cy, 205)
    put(px, cx, cy - 1, 190)


def grate(px):
    for x in (4, 7, 10, 13):
        for y in range(3, 13):
            put(px, x, y, 20)
        put(px, x + 1, 3, 200)
        put(px, x + 1, 12, 185)


def moss(px, rng):
    cx = rng.randint(3, 12)
    cy = rng.randint(3, 12)
    for _ in range(rng.randint(4, 7)):
        x = cx + rng.randint(-2, 2)
        y = cy + rng.randint(-2, 2)
        put(px, x, y, rng.choice((185, 205, 225)))
    for _ in range(3):
        put(px, cx + rng.randint(-3, 3), cy + rng.randint(-3, 3), 35)


def obsidian_crack(px, rng):
    x = rng.randint(2, 13)
    y = rng.randint(2, 13)
    for _ in range(rng.randint(5, 9)):
        put(px, x, y, 235)
        if rng.random() < 0.55:
            put(px, x + 1, y, 175)
        x += rng.choice((-1, 0, 1))
        y += rng.choice((-1, 0, 1))
        x = max(0, min(15, x))
        y = max(0, min(15, y))


def ember(px, rng):
    for _ in range(3):
        put(px, rng.randint(2, 13), rng.randint(2, 13), 255)


def wall_motif(px, theme, rng):
    if theme == "sewer":
        # 水渍 + 少量亮面
        for x in range(16):
            put(px, x, 11 + (x % 2), 26)
        for x in (3, 5, 9, 12):
            put(px, x, 12, 200)
        for _ in range(5):
            put(px, rng.randint(1, 14), rng.randint(12, 15), 180)
    elif theme == "overgrown":
        # 斜向藤蔓 + 叶片
        for i in range(11):
            put(px, 2 + i, 14 - i, 190)
            if i % 3 == 0:
                put(px, 3 + i, 13 - i, 225)
        for _ in range(6):
            put(px, rng.randint(1, 14), rng.randint(8, 15), 35)
    else:
        # 玄武岩裂纹 + 余烬
        x, y = 3, 12
        for _ in range(9):
            put(px, x, y, 240)
            x += rng.choice((0, 1, 1))
            y -= rng.choice((0, 1, 1))
            x = max(0, min(15, x))
            y = max(0, min(15, y))
        for _ in range(3):
            put(px, rng.randint(2, 13), rng.randint(2, 13), 255)


def main():
    os.makedirs(OUT_ROOT, exist_ok=True)

    for theme_index, theme in enumerate(THEMES):
        out_dir = os.path.join(OUT_ROOT, theme)
        os.makedirs(out_dir, exist_ok=True)

        for i in range(1, 9):
            src = Image.open(os.path.join(SRC_FLOOR, f"floor_{i}.png")).convert("RGBA")
            im = to_gray(src)
            px = im.load()
            rng = random.Random(7000 + theme_index * 100 + i)

            if theme == "sewer":
                if i in (1, 4, 6):
                    puddle(px, rng)
                if i == 2:
                    grate(px)
                for _ in range(2):
                    put(px, rng.randint(1, 14), rng.randint(1, 14), rng.choice((30, 205)))
            elif theme == "overgrown":
                if i in (1, 3, 5, 7):
                    moss(px, rng)
                for _ in range(2):
                    put(px, rng.randint(1, 14), rng.randint(1, 14), 190)
            else:
                if i in (2, 4, 6, 7):
                    obsidian_crack(px, rng)
                if i == 5:
                    ember(px, rng)
                for _ in range(2):
                    put(px, rng.randint(1, 14), rng.randint(1, 14), 25)

            im.save(os.path.join(out_dir, f"floor_{i}.png"))

        wall = to_gray(Image.open(SRC_WALL))
        wpx = wall.load()
        wall_motif(wpx, theme, random.Random(9000 + theme_index))
        wall.save(os.path.join(out_dir, "wall.png"))

        print("generated", out_dir)


if __name__ == "__main__":
    main()
