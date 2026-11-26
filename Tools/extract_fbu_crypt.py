"""提取第 2 层「墓穴」用的 FBU 32×32 地板、墙体与场景道具。

素材来源（全部来自 FBU 的免费图块包，授权见 Docs/FBU_Source/CREDITS.txt）：
- FBU's Tileset Package / Underwater / tilea4mack.png   → 冷灰绿石板地板
- FBU's Tileset Package / Winter Time / icedungeona4.png → 冰蓝灰砖墙
- Pack of Tilesets #2 / Tiles / Dungeons / TileB.png     → 骷髅墓碑、头骨堆
- Pack of Tilesets #2 / Tiles / Dungeons / Statue with Wings.png → 天使雕像

与第 1 层一致的约定：
- 地板裁 48×48（无描边内部），PPU 32
- 墙体取 32×32 单元，横竖各一张
- 道具只从"完整物件"裁切，坐标均由连通域分析确认过，不是按栅格硬切
"""

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
FBU = Path(r"C:/Users/houboxuan/Downloads/FBU 图块包 Tileset Package/FBU 图块包 Tileset Package")
FLOOR_SHEET = FBU / "FBU's Tileset Package/Underwater/tilea4mack.png"
WALL_SHEET = FBU / "FBU's Tileset Package/Winter Time/icedungeona4.png"
TILEB_SHEET = FBU / "Pack of Tilesets #2/Tiles/Dungeons/TileB.png"
WINGS_SHEET = FBU / "Pack of Tilesets #2/Tiles/Dungeons/Statue with Wings.png"

OUT = ROOT / "Assets/Art/FBU/Crypt"
PROPS = OUT / "props"
SRC = ROOT / "Docs/FBU_Source"
CELL = 32

# 地板：冷灰绿石板的无描边内部区域
FLOOR_CROP = (324, 36, 324 + 48, 36 + 48)

# 墙体：冰蓝灰砖的 32×32 单元（横 / 竖各取一格，略有差异）
WALL_H_CELL = (9, 8)
WALL_V_CELL = (8, 8)

# TileB 道具：连通域确认过的完整物件包围盒
#   - 两块 32×64 的骷髅墓碑（左右并列，各自独立）
#   - 两块 30×30 的头骨堆
TILEB_PROPS = {
    "prop_grave_slab": (96, 224, 128, 288),
    "prop_grave_slab_b": (128, 224, 160, 288),
    "prop_skull_pile": (98, 289, 128, 319),
    "prop_skull_pile_b": (98, 321, 128, 351),
}

# 天使雕像：整张图就是一个物件
WINGS_PROP = "prop_angel_statue"


def crop_cell(sheet, gx, gy):
    return sheet.crop((gx * CELL, gy * CELL, (gx + 1) * CELL, (gy + 1) * CELL))


def trim_white_border(im, threshold=246):
    """裁掉纯白背景的空白边（TileB / Wings 都是白底图集）。"""
    px = im.load()
    w, h = im.size

    def solid(x, y):
        r, g, b, a = px[x, y]
        return a > 30 and not (r > threshold and g > threshold and b > threshold)

    minx, miny, maxx, maxy = w, h, -1, -1
    for y in range(h):
        for x in range(w):
            if solid(x, y):
                minx = min(minx, x); maxx = max(maxx, x)
                miny = min(miny, y); maxy = max(maxy, y)

    if maxx < 0:
        return im
    return im.crop((minx, miny, maxx + 1, maxy + 1))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PROPS.mkdir(parents=True, exist_ok=True)
    SRC.mkdir(parents=True, exist_ok=True)

    for src, name in [
        (FLOOR_SHEET, "Underwater_tilea4mack.png"),
        (WALL_SHEET, "Winter_icedungeona4.png"),
        (TILEB_SHEET, "TileB.png"),
        (WINGS_SHEET, "Statue_with_Wings.png"),
    ]:
        target = SRC / name
        if not target.exists():
            target.write_bytes(src.read_bytes())

    floor_sheet = Image.open(FLOOR_SHEET).convert("RGBA")
    wall_sheet = Image.open(WALL_SHEET).convert("RGBA")
    tileb = Image.open(TILEB_SHEET).convert("RGBA")
    wings = Image.open(WINGS_SHEET).convert("RGBA")

    floor_sheet.crop(FLOOR_CROP).save(OUT / "floor_base.png", optimize=True)

    crop_cell(wall_sheet, *WALL_H_CELL).save(OUT / "wall_h.png", optimize=True)
    crop_cell(wall_sheet, *WALL_V_CELL).save(OUT / "wall_v.png", optimize=True)

    for name, box in TILEB_PROPS.items():
        tileb.crop(box).save(PROPS / f"{name}.png", optimize=True)

    trim_white_border(wings).save(PROPS / f"{WINGS_PROP}.png", optimize=True)

    print(f"FBU crypt tiles -> {OUT}")
    print(f"  floor {FLOOR_CROP}  walls {WALL_H_CELL}/{WALL_V_CELL}")
    for name, box in TILEB_PROPS.items():
        print(f"  {name} <- {box}")
    print(f"  {WINGS_PROP} <- Statue with Wings.png")


if __name__ == "__main__":
    main()
