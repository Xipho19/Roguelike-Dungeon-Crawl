"""生成六层 Boss 图鉴卡片与总览图。

使用游戏内实际 Boss 精灵（0x72 + BossMonsters），不重绘。
输出到 Docs/BossImages/，不进入 Assets，避免 Unity 导入。
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parent.parent
FONT_PATH = ROOT / "Assets/Resources/UI/ArkPixel12.ttf"
LABEL_FONT_PATH = Path("C:/Windows/Fonts/simhei.ttf")
OUT = ROOT / "Docs/BossImages"
OUT.mkdir(parents=True, exist_ok=True)

BOSSES = [
    {
        "floor": 1, "theme": "地牢", "name": "蓝晶雏龙",
        "moves": "三连冲锋 · 震地重击 · 岩石冲击波",
        "sprite": "Assets/Art/Bosses/Ogre/idle_down.png",
        "bg": (18, 24, 36), "accent": (104, 178, 255), "final": False,
    },
    {
        "floor": 2, "theme": "墓穴", "name": "死亡骑士",
        "moves": "召唤骷髅 · 腐蚀弹幕 · 召唤毒环",
        "sprite": "Assets/Art/Bosses/BigZombie/idle_down.png",
        "bg": (27, 21, 38), "accent": (170, 120, 255), "final": False,
    },
    {
        "floor": 3, "theme": "黄金城", "name": "赤金狮蝎",
        "moves": "螺旋弹幕 · 追踪火球 · 闪现突袭",
        "sprite": "Assets/Art/Bosses/BigDemon/idle_down.png",
        "bg": (38, 20, 18), "accent": (255, 124, 66), "final": False,
    },
    {
        "floor": 4, "theme": "下水道", "name": "瘟疫鼠王",
        "moves": "额外冲锋 · 震地毒液环",
        "sprite": "Assets/Art/Bosses/SewerTroll/idle_down.png",
        "bg": (34, 20, 22), "accent": (235, 92, 92), "final": False,
    },
    {
        "floor": 5, "theme": "遗迹", "name": "深蓝守望者",
        "moves": "震地重击 · 石环冲击波 · 石弹弹幕",
        "sprite": "Assets/Art/Bosses/RuinsGolem/idle_down.png",
        "bg": (17, 26, 40), "accent": (120, 190, 255), "final": False,
    },
    {
        "floor": 6, "theme": "黑曜", "name": "深渊魔君",
        "moves": "三阶段 · 螺旋 / 追踪 / 闪现 / 召唤",
        "sprite": "Assets/Art/Bosses/ObsidianKing/idle_down.png",
        "bg": (24, 15, 38), "accent": (178, 100, 255), "final": True,
    },
]


def font(size):
    """标题使用项目像素字体。"""
    return ImageFont.truetype(str(FONT_PATH), size)


def label_font(size):
    """正文使用系统黑体，避免像素字体缺字（例如“旋”）。"""
    return ImageFont.truetype(str(LABEL_FONT_PATH), size)


def mix(a, b, t):
    return tuple(int(a[i] * (1 - t) + b[i] * t) for i in range(3))


def draw_panel(draw, box, bg, accent):
    x0, y0, x1, y1 = box
    draw.rounded_rectangle(box, radius=18, fill=bg, outline=(*accent, 220), width=3)
    # 顶部主题光晕
    draw.rounded_rectangle((x0 + 14, y0 + 14, x1 - 14, y0 + 54), radius=10,
                           fill=mix(bg, accent, 0.16))
    draw.line((x0 + 24, y0 + 54, x1 - 24, y0 + 54), fill=(*accent, 90), width=1)


def draw_boss_card(boss, size=(600, 600)):
    w, h = size
    img = Image.new("RGB", size, boss["bg"])
    draw = ImageDraw.Draw(img)
    draw_panel(draw, (8, 8, w - 8, h - 8), boss["bg"], boss["accent"])

    f_floor = label_font(22)
    f_name = label_font(30 if len(boss["name"]) <= 11 else 26)
    f_move = label_font(17)
    f_badge = label_font(15)

    draw.text((30, 24), f"第 {boss['floor']} 层 · {boss['theme']}", font=f_floor,
              fill=boss["accent"], anchor="la")
    if boss["final"]:
        draw.text((w - 30, 26), "FINAL BOSS", font=f_badge,
                  fill=mix(boss["accent"], (255, 255, 255), 0.35), anchor="ra")

    sprite = Image.open(ROOT / boss["sprite"]).convert("RGBA")
    target_h = 300
    target_w = 480
    scale = max(1, min(target_h // sprite.height, target_w // sprite.width))
    sprite = sprite.resize((sprite.width * scale, sprite.height * scale), Image.Resampling.NEAREST)

    # 背后光晕
    cx, cy = w // 2, 315
    glow = Image.new("RGBA", size, (0, 0, 0, 0))
    gd = ImageDraw.Draw(glow)
    for r in range(150, 0, -6):
        alpha = int(16 * (1 - r / 150))
        gd.ellipse((cx - r, cy - r, cx + r, cy + r), fill=(*boss["accent"], alpha))
    img = Image.alpha_composite(img.convert("RGBA"), glow).convert("RGB")
    draw = ImageDraw.Draw(img)
    img.paste(sprite, (cx - sprite.width // 2, cy - sprite.height // 2), sprite)

    draw.text((w // 2, 486), boss["name"], font=f_name, fill=(245, 232, 200), anchor="mm")
    draw.text((w // 2, 524), boss["moves"], font=f_move, fill=boss["accent"], anchor="mm")
    if boss["final"]:
        draw.text((w // 2, 554), "66% / 33% 阶段转换 · 最终阶段紫色新星", font=label_font(14),
                  fill=mix(boss["accent"], (255, 255, 255), 0.2), anchor="mm")
    else:
        draw.text((w // 2, 554), "Boss 血条显示名称与阶段", font=label_font(14),
                  fill=(150, 142, 160), anchor="mm")

    draw.rounded_rectangle((22, h - 30, w - 22, h - 22), radius=4,
                           fill=mix(boss["bg"], boss["accent"], 0.35))
    return img


def draw_roster(cards):
    cell_w, cell_h = 560, 560
    gap = 30
    margin = 30
    title_h = 90
    w = margin * 2 + cell_w * 3 + gap * 2
    h = title_h + margin + cell_h * 2 + gap + margin
    img = Image.new("RGB", (w, h), (13, 11, 18))
    draw = ImageDraw.Draw(img)

    for x in range(0, w, 4):
        draw.line((x, 0, x, h), fill=(18, 15, 24))
    draw.text((w // 2, 46), "地牢回响 · 六层 Boss 图鉴", font=font(42),
              fill=(232, 199, 120), anchor="mm")

    for i, card in enumerate(cards):
        col, row = i % 3, i // 3
        x = margin + col * (cell_w + gap)
        y = title_h + margin + row * (cell_h + gap)
        thumb = card.resize((cell_w, cell_h), Image.Resampling.LANCZOS)
        img.paste(thumb, (x, y))

    draw.text((w // 2, h - 16), "素材：0x72 DungeonTilesetII / BossMonsters · 项目内实际 Boss 精灵",
              font=label_font(13), fill=(118, 108, 126), anchor="ms")
    return img


def main():
    cards = []
    for boss in BOSSES:
        card = draw_boss_card(boss)
        path = OUT / f"boss_floor_{boss['floor']}_{boss['theme']}.png"
        card.save(path)
        cards.append(card)
        print(path)

    roster = draw_roster(cards)
    roster_path = OUT / "boss_roster.png"
    roster.save(roster_path)
    print(roster_path)


if __name__ == "__main__":
    main()
