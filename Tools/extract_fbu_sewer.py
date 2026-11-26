"""提取第 4 层「下水道」用的 FBU 32×32 地板、墙体与场景道具。

素材来源（全部来自 FBU 的免费图块包，授权见 Docs/FBU_Source/CREDITS.txt）：
- FBU's Tileset Package / Underwater / tilea5underwater.png → 苔绿石砖地板（32×32）
- FBU's Tileset Package / Underwater / tilea4mack.png       → 蓝色砖墙（32×32）
- FBU's Tileset Package / Underwater / tilebt.png           → 兜帽石像、白石柱、骸骨
- FBU's Tileset Package / Underwater / tilecb.png           → 石拱神龛、水草、蕨叶、水晶

与第 1–3 层一致的约定：
- 地板裁整块无缝石板（这里 64×64），PPU 32；矩形张量平铺
- 墙体取 32×32 单元，横竖各一张
- 道具只从"完整物件"裁切（坐标由连通域分析确认）
"""

from pathlib import Path

from PIL import Image

ROOT = Path(__file__).resolve().parent.parent
FBU = Path(r"C:/Users/houboxuan/Downloads/FBU 图块包 Tileset Package/FBU 图块包 Tileset Package")
UW = FBU / "FBU's Tileset Package/Underwater"

FLOOR_SHEET = UW / "tilea5underwater.png"
WALL_SHEET = UW / "tilea4mack.png"
BT_SHEET = UW / "tilebt.png"
CB_SHEET = UW / "tilecb.png"

OUT = ROOT / "Assets/Art/FBU/Sewer"
PROPS = OUT / "props"
SRC = ROOT / "Docs/FBU_Source"

# 地板：A5 图集第一行倒数第三个（苔绿石砖），32×32 一格 = 1 世界单位
FLOOR_CROP = (160, 0, 192, 32)

# 墙体：蓝色砖墙的 32×32 单元（横 / 竖各取一格）
WALL_H_CROP = (404, 264, 436, 296)
WALL_V_CROP = (412, 264, 444, 296)

# tilebt：连通域确认过的完整物件
BT_PROPS = {
    "prop_sewer_statue": (388, 449, 444, 512),   # 青蓝兜帽石像
    "prop_sewer_pillar": (483, 223, 512, 289),   # 白色大理石柱
    "prop_sewer_bones": (223, 0, 289, 33),       # 两具骸骨
}

# tilecb：完整物件
CB_PROPS = {
    "prop_sewer_shrine": (10, 160, 92, 224),     # 带红宝石的石拱神龛
    "prop_sewer_kelp_a": (352, 266, 384, 288),   # 绿色水草
    "prop_sewer_kelp_b": (352, 330, 384, 352),   # 绿色水草（变体）
    "prop_sewer_fern": (354, 227, 379, 255),     # 蕨叶
    "prop_sewer_crystal": (288, 0, 319, 32),     # 青蓝水晶
}


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PROPS.mkdir(parents=True, exist_ok=True)
    SRC.mkdir(parents=True, exist_ok=True)

    for src, name in [
        (FLOOR_SHEET, "Underwater_tilea5underwater.png"),
        (WALL_SHEET, "Underwater_tilea4mack.png"),
        (BT_SHEET, "Underwater_tilebt.png"),
        (CB_SHEET, "Underwater_tilecb.png"),
    ]:
        target = SRC / name
        if not target.exists():
            target.write_bytes(src.read_bytes())

    floor_sheet = Image.open(FLOOR_SHEET).convert("RGBA")
    wall_sheet = Image.open(WALL_SHEET).convert("RGBA")
    bt = Image.open(BT_SHEET).convert("RGBA")
    cb = Image.open(CB_SHEET).convert("RGBA")

    floor_sheet.crop(FLOOR_CROP).save(OUT / "floor_base.png", optimize=True)
    wall_sheet.crop(WALL_H_CROP).save(OUT / "wall_h.png", optimize=True)
    wall_sheet.crop(WALL_V_CROP).save(OUT / "wall_v.png", optimize=True)

    for name, box in BT_PROPS.items():
        bt.crop(box).save(PROPS / f"{name}.png", optimize=True)
    for name, box in CB_PROPS.items():
        cb.crop(box).save(PROPS / f"{name}.png", optimize=True)

    print(f"FBU sewer tiles -> {OUT}")
    print(f"  floor {FLOOR_CROP}")
    print(f"  walls H{WALL_H_CROP} V{WALL_V_CROP}")
    for name, box in list(BT_PROPS.items()) + list(CB_PROPS.items()):
        print(f"  {name} <- {box}")


if __name__ == "__main__":
    main()
