"""提取第 1 层地牢试点用的 FBU 32×32 地板、墙体与场景道具。

素材来源：
- FBU's Tileset Package / Dungeon Bonus / 62328908.png（无缝花岗岩地面）
- FBU's Tileset Package / Dungeon Bonus / 45323392.png（墙体）
- Pack of Tilesets #2 / Tiles / Dungeons / TileB.png（骸骨、碎石、石瓮、雕像）
- FBU's Tileset Package / Winter Time / chest1.png（宝箱）
- FBU's Tileset Package / Golden Cave / other2.png（火盆）
- FBU's Tileset Package / Golden Cave / dungeonbn.png（陶罐）
仅用于本项目非商用版本；完整署名见 Docs/FBU_Source/CREDITS.txt。
"""
from pathlib import Path
from PIL import Image
import shutil

ROOT = Path(__file__).resolve().parent.parent
FBU = Path(r"C:/Users/houboxuan/Downloads/FBU 图块包 Tileset Package/FBU 图块包 Tileset Package")
FLOOR_SHEET = FBU / "FBU's Tileset Package/Dungeon Bonus/62328908.png"
WALL_SHEET = FBU / "FBU's Tileset Package/Dungeon Bonus/45323392.png"
TILEB_SHEET = FBU / "Pack of Tilesets #2/Tiles/Dungeons/TileB.png"
CHEST_SHEET = FBU / "FBU's Tileset Package/Winter Time/chest1.png"
BRAZIER_SHEET = FBU / "FBU's Tileset Package/Golden Cave/other2.png"
GOLD_SHEET = FBU / "FBU's Tileset Package/Golden Cave/dungeonbn.png"

OUT = ROOT / "Assets/Art/FBU/Dungeon"
PROPS = OUT / "props"
SRC = ROOT / "Docs/FBU_Source"
CELL = 32


def crop(sheet, x, y):
    return sheet.crop((x * CELL, y * CELL, (x + 1) * CELL, (y + 1) * CELL))


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    PROPS.mkdir(parents=True, exist_ok=True)
    SRC.mkdir(parents=True, exist_ok=True)

    shutil.copy2(FLOOR_SHEET, SRC / "DungeonBonus_62328908.png")
    shutil.copy2(WALL_SHEET, SRC / "DungeonBonus_45323392.png")
    shutil.copy2(TILEB_SHEET, SRC / "TileB.png")
    shutil.copy2(CHEST_SHEET, SRC / "Winter_chest1.png")
    shutil.copy2(BRAZIER_SHEET, SRC / "GoldenCave_other2.png")
    shutil.copy2(GOLD_SHEET, SRC / "GoldenCave_dungeonbn.png")

    floor_sheet = Image.open(FLOOR_SHEET).convert("RGBA")
    wall_sheet = Image.open(WALL_SHEET).convert("RGBA")
    tileb = Image.open(TILEB_SHEET).convert("RGBA")
    chest_sheet = Image.open(CHEST_SHEET).convert("RGBA")
    brazier_sheet = Image.open(BRAZIER_SHEET).convert("RGBA")
    gold = Image.open(GOLD_SHEET).convert("RGBA")

    # 取灰色花岗岩板内部的 48×48 区域：没有板边描边，平铺后完全无缝。
    floor_base = floor_sheet.crop((8, 360, 56, 408))
    floor_base.save(OUT / "floor_base.png", optimize=True)

    # 灰色砖墙；横/竖墙先共用同一张，后续可按主题替换。
    wall = crop(wall_sheet, 10, 8)
    wall.save(OUT / "wall_h.png", optimize=True)
    wall.save(OUT / "wall_v.png", optimize=True)

    statue = tileb.crop((160, 224, 192, 288))

    props = {
        # 宝箱：棕色木箱的关闭/打开状态
        "prop_chest_closed": chest_sheet.crop((0, 128, 32, 160)),
        "prop_chest_open": chest_sheet.crop((0, 192, 32, 224)),
        # 火盆：金色三脚架 + 橙色火焰
        "prop_brazier": brazier_sheet.crop((288, 400, 320, 448)),
        # 骸骨、碎石、石笋、石瓮、兜帽石像
        "prop_bones": tileb.crop((128, 192, 160, 224)),
        "prop_rubble": tileb.crop((64, 160, 96, 192)),
        "prop_rock_spire": tileb.crop((32, 160, 64, 224)),
        "prop_urn": tileb.crop((96, 288, 128, 352)),
        "prop_statue": statue,
        # 遮罩用上下半段：上半段排序在角色之上
        "prop_statue_base": statue.crop((0, 32, 32, 64)),
        "prop_statue_top": statue.crop((0, 0, 32, 32)),
        # 可破坏陶罐（裂纹罐 + 两种完好罐）
        "prop_pot_cracked": gold.crop((480, 352, 512, 384)),
        "prop_pot_gold": gold.crop((448, 384, 480, 416)),
        "prop_pot_stone": gold.crop((480, 384, 512, 416)),
        # 墙边结晶/苔藓
        "prop_crystal_green": tileb.crop((0, 160, 32, 192)),
        "prop_crystal_purple": tileb.crop((128, 160, 160, 192)),
    }

    for name, image in props.items():
        image.save(PROPS / f"{name}.png", optimize=True)

    print(f"FBU pilot tiles -> {OUT}, props -> {PROPS}")


if __name__ == "__main__":
    main()
