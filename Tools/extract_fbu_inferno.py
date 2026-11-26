"""提取第 3 层「黄金城」用的 FBU 地板、墙体与场景道具。

素材来源（全部来自 FBU 的免费图块包，授权见 Docs/FBU_Source/CREDITS.txt）：
- FBU's Tileset Package / Golden Cave / tilea42.png    → 金色砖地（48×48 无缝）
- FBU's Tileset Package / Golden Cave / dungeonbn.png  → 金色砖墙、金像、金矿骷髅堆、金柱
- FBU's Tileset Package / Golden Cave / other2.png     → 魔法火盆（金/紫）

与第 1、2 层一致的约定：
- 地板裁 48×48（无描边内部），PPU 32
- 墙体取 32×32 单元，横竖各一张
- 道具只从"完整物件"裁切（连通域确认过包围盒）
"""

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
FBU = Path(r"C:/Users/houboxuan/Downloads/FBU 图块包 Tileset Package/FBU 图块包 Tileset Package")
GC = FBU / "FBU's Tileset Package/Golden Cave"

FLOOR_SHEET = GC / "tilea42.png"
WALL_SHEET = GC / "dungeonbn.png"
PROP_SHEET = GC / "dungeonbn.png"
BRAZIER_SHEET = GC / "other2.png"

OUT = ROOT / "Assets/Art/FBU/Inferno"
PROPS = OUT / "props"
SRC = ROOT / "Docs/FBU_Source"
CELL = 32

# 地板：金色砖地，3×3 拼接无缝
FLOOR_CROP = (200, 104, 200 + 48, 104 + 48)

# 墙体：金色砖墙（横 / 竖各取一格）
WALL_H_CELL = (4, 4)
WALL_V_CELL = (4, 4)

# 道具：连通域确认过的完整物件
PROP_CROPS = {
    "prop_gold_shrine": (432, 144, 505, 224),   # 带绿宝石的金色神龛
    "prop_gold_ore": (336, 160, 407, 224),      # 金矿堆 + 骷髅
    "prop_gold_pillar": (33, 320, 64, 383),     # 金柱
    "prop_gold_idol": (353, 225, 384, 288),     # 金像
}
BRAZIER_CROPS = {
    "prop_magic_brazier_gold": (293, 401, 316, 448),
    "prop_magic_brazier_purple": (293, 337, 316, 384),
}


def crop_cell(sheet, gx, gy):
    return sheet.crop((gx * CELL, gy * CELL, (gx + 1) * CELL, (gy + 1) * CELL))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PROPS.mkdir(parents=True, exist_ok=True)
    SRC.mkdir(parents=True, exist_ok=True)

    for src, name in [
        (FLOOR_SHEET, "GoldenCave_tilea42.png"),
        (WALL_SHEET, "GoldenCave_dungeonbn.png"),
        (BRAZIER_SHEET, "GoldenCave_other2.png"),
    ]:
        target = SRC / name
        if not target.exists():
            target.write_bytes(src.read_bytes())

    floor_sheet = Image.open(FLOOR_SHEET).convert("RGBA")
    wall_sheet = Image.open(WALL_SHEET).convert("RGBA")
    brazier_sheet = Image.open(BRAZIER_SHEET).convert("RGBA")

    floor_sheet.crop(FLOOR_CROP).save(OUT / "floor_base.png", optimize=True)
    crop_cell(wall_sheet, *WALL_H_CELL).save(OUT / "wall_h.png", optimize=True)
    crop_cell(wall_sheet, *WALL_V_CELL).save(OUT / "wall_v.png", optimize=True)

    for name, box in PROP_CROPS.items():
        wall_sheet.crop(box).save(PROPS / f"{name}.png", optimize=True)
    for name, box in BRAZIER_CROPS.items():
        brazier_sheet.crop(box).save(PROPS / f"{name}.png", optimize=True)

    print(f"FBU inferno tiles -> {OUT}")
    print(f"  floor {FLOOR_CROP}  walls {WALL_H_CELL}/{WALL_V_CELL}")
    for name, box in list(PROP_CROPS.items()) + list(BRAZIER_CROPS.items()):
        print(f"  {name} <- {box}")


if __name__ == "__main__":
    main()
