"""把用户提供的六张 3×4 方向行走图切成 Unity 可用的单帧精灵。

行顺序：A 下 / B 左 / C 右 / D 上。
游戏只使用“下 / 右 / 上”三组，左侧由 SpriteAnimator 水平翻转。
"""
from pathlib import Path
from PIL import Image
import math
import shutil

ROOT = Path(__file__).resolve().parent.parent
SOURCE = Path(r"C:/Users/HOUBOX~1/AppData/Local/Temp/claude/C--Users-houboxuan-AppData-Roaming-CherryStudio-Data-Agents-system-2026-10-22-99bbaea9-20e1-468c-ba42-1b1f4ee43ede/46578907-2f09-48ce-80e5-4da8f0fbf755/images")
RAW = ROOT / "Docs/BossArtSource"
ART = ROOT / "Assets/Art/Bosses"

# 用户给出的顺序：2 层、6 层、5 层、4 层、1 层、3 层
SHEETS = [
    (29, "BigZombie", "floor2"),
    (30, "ObsidianKing", "floor6"),
    (31, "RuinsGolem", "floor5"),
    (32, "SewerTroll", "floor4"),
    (33, "Ogre", "floor1"),
    (34, "BigDemon", "floor3"),
]

DIRECTIONS = [("down", 0), ("left", 1), ("right", 2), ("up", 3)]


def clear_chroma(sheet):
    """第 4 层素材带纯绿抠像背景；只清除完全匹配的背景色。"""
    chroma = (120, 195, 128)
    if sheet.getpixel((0, 0))[:3] != chroma:
        return sheet

    pixels = sheet.load()
    for y in range(sheet.height):
        for x in range(sheet.width):
            r, g, b, a = pixels[x, y]
            if (r, g, b) == chroma:
                pixels[x, y] = (0, 0, 0, 0)
    return sheet


def export_boss(source_id, folder, raw_name):
    sheet = clear_chroma(Image.open(SOURCE / f"{source_id}.png").convert("RGBA"))
    width, height = sheet.size
    cell_w = math.ceil(width / 3)
    cell_h = math.ceil(height / 4)

    target = ART / folder
    target.mkdir(parents=True, exist_ok=True)

    RAW.mkdir(parents=True, exist_ok=True)
    shutil.copy2(SOURCE / f"{source_id}.png", RAW / f"{raw_name}.png")

    for direction, row in DIRECTIONS:
        frames = []
        for column in range(3):
            cell = Image.new("RGBA", (cell_w, cell_h), (0, 0, 0, 0))
            left = column * cell_w
            top = row * cell_h
            right = min(left + cell_w, width)
            bottom = min(top + cell_h, height)
            crop = sheet.crop((left, top, right, bottom))
            cell.alpha_composite(crop, (0, 0))
            frames.append(cell)
            cell.save(target / f"run_{direction}_{column}.png", optimize=True)

        # 行走素材没有单独待机帧，用中间帧作为待机姿态。
        frames[1].save(target / f"idle_{direction}.png", optimize=True)

    print(f"{raw_name}: {width}x{height} -> cell {cell_w}x{cell_h}, 3 frames x 4 dirs")


def main():
    for source_id, folder, raw_name in SHEETS:
        export_boss(source_id, folder, raw_name)


if __name__ == "__main__":
    main()
