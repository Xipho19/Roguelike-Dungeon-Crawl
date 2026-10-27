using System.IO;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 新增自动道具的 16×16 像素图标。
    /// 只在文件缺失时生成，避免覆盖手工修改过的图标。
    /// </summary>
    public static class ItemIconFactory
    {
        private const int Size = 16;
        private const string IconFolder = ThirdPartySprites.Root + "frames/icons";

        private static readonly Color32 Outline = new Color32(20, 22, 30, 255);
        private static readonly Color32 White = new Color32(255, 255, 255, 255);
        private static readonly Color32 Shine = new Color32(220, 245, 255, 255);

        private static readonly Color32 TearBlue = new Color32(74, 166, 245, 255);
        private static readonly Color32 TearDeep = new Color32(32, 92, 166, 255);
        private static readonly Color32 Gold = new Color32(244, 194, 62, 255);
        private static readonly Color32 GoldDeep = new Color32(178, 116, 24, 255);
        private static readonly Color32 Red = new Color32(226, 58, 66, 255);
        private static readonly Color32 RedDeep = new Color32(142, 28, 42, 255);
        private static readonly Color32 Green = new Color32(80, 190, 94, 255);
        private static readonly Color32 GreenDeep = new Color32(32, 106, 56, 255);
        private static readonly Color32 Orange = new Color32(248, 138, 48, 255);
        private static readonly Color32 OrangeDeep = new Color32(170, 62, 26, 255);
        private static readonly Color32 Purple = new Color32(160, 98, 230, 255);

        public static void EnsureIcons()
        {
            Ensure("icon_bounce_tear.png", DrawBounceTear());
            Ensure("icon_blood_rage.png", DrawBloodRage());
            Ensure("icon_collector_heart.png", DrawCollectorHeart());
            Ensure("icon_metronome.png", DrawMetronome());
            Ensure("icon_hunter_boots.png", DrawHunterBoots());
            Ensure("icon_kill_ember.png", DrawKillEmber());
        }

        private static void Ensure(string fileName, Color32[] pixels)
        {
            EnsureFolder(IconFolder);

            string path = IconFolder + "/" + fileName;
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string absolutePath = Path.Combine(projectRoot, path);

            if (!File.Exists(absolutePath))
            {
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                texture.SetPixels32(pixels);
                texture.Apply();
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        private static Color32[] DrawBounceTear()
        {
            Color32[] canvas = NewCanvas();

            FillCircle(canvas, 8, 6, 5, Outline);
            FillTriangle(canvas, 3, 6, 13, 6, 8, 15, Outline);
            FillCircle(canvas, 8, 6, 4, TearBlue);
            FillTriangle(canvas, 4, 7, 12, 7, 8, 14, TearBlue);

            FillCircle(canvas, 6, 7, 1, Shine);
            Set(canvas, 7, 8, White);
            Set(canvas, 5, 8, TearDeep);
            Set(canvas, 8, 4, TearDeep);

            // 两侧的弹跳弧线
            Line(canvas, 0, 6, 3, 4, Gold);
            Line(canvas, 0, 6, 3, 8, Gold);
            Line(canvas, 15, 6, 12, 4, Gold);
            Line(canvas, 15, 6, 12, 8, Gold);
            return canvas;
        }

        private static Color32[] DrawBloodRage()
        {
            Color32[] canvas = NewCanvas();

            FillCircle(canvas, 5, 9, 4, Outline);
            FillCircle(canvas, 10, 9, 4, Outline);
            FillTriangle(canvas, 1, 9, 14, 9, 8, 1, Outline);

            FillCircle(canvas, 5, 9, 3, Red);
            FillCircle(canvas, 10, 9, 3, Red);
            FillTriangle(canvas, 2, 9, 13, 9, 8, 2, Red);

            FillCircle(canvas, 4, 10, 1, White);
            Set(canvas, 5, 10, White);
            Set(canvas, 3, 8, RedDeep);
            Set(canvas, 11, 9, RedDeep);

            Line(canvas, 8, 13, 6, 9, Gold);
            Line(canvas, 6, 9, 9, 8, Gold);
            Line(canvas, 9, 8, 7, 5, Gold);
            Line(canvas, 7, 5, 10, 4, Gold);
            return canvas;
        }

        private static Color32[] DrawCollectorHeart()
        {
            Color32[] canvas = NewCanvas();

            // 金币底座
            FillCircle(canvas, 4, 3, 2, Outline);
            FillCircle(canvas, 12, 3, 2, Outline);
            FillCircle(canvas, 4, 3, 1, Gold);
            FillCircle(canvas, 12, 3, 1, Gold);

            // 大金币
            FillCircle(canvas, 8, 7, 6, Outline);
            FillCircle(canvas, 8, 7, 5, Gold);
            FillCircle(canvas, 8, 7, 3, GoldDeep);
            FillCircle(canvas, 8, 7, 2, Gold);

            // 金币上的红心
            FillCircle(canvas, 7, 8, 1, Red);
            FillCircle(canvas, 9, 8, 1, Red);
            FillTriangle(canvas, 6, 8, 10, 8, 8, 5, Red);
            Set(canvas, 7, 9, White);

            Set(canvas, 3, 5, GoldDeep);
            Set(canvas, 13, 5, GoldDeep);
            Set(canvas, 12, 11, Purple);
            return canvas;
        }

        private static Color32[] DrawMetronome()
        {
            Color32[] canvas = NewCanvas();

            FillTriangle(canvas, 8, 15, 2, 2, 14, 2, Outline);
            FillTriangle(canvas, 8, 13, 4, 3, 12, 3, Orange);
            FillTriangle(canvas, 8, 11, 6, 5, 10, 5, OrangeDeep);

            FillRect(canvas, 2, 0, 12, 3, Outline);
            FillRect(canvas, 3, 1, 10, 1, Gold);

            Line(canvas, 8, 4, 11, 10, White);
            FillCircle(canvas, 11, 10, 1, Red);
            Set(canvas, 8, 4, Gold);
            return canvas;
        }

        private static Color32[] DrawHunterBoots()
        {
            Color32[] canvas = NewCanvas();

            FillRect(canvas, 5, 5, 5, 10, Outline);
            FillRect(canvas, 5, 2, 10, 4, Outline);
            FillRect(canvas, 6, 6, 3, 8, Green);
            FillRect(canvas, 6, 3, 8, 2, Green);
            FillRect(canvas, 5, 1, 9, 2, OrangeDeep);

            Set(canvas, 7, 12, GreenDeep);
            Set(canvas, 9, 10, GreenDeep);
            Set(canvas, 8, 7, GreenDeep);
            Set(canvas, 7, 4, White);

            // 速度线
            Line(canvas, 0, 4, 4, 4, Gold);
            Line(canvas, 1, 7, 4, 7, Gold);
            Line(canvas, 0, 10, 3, 10, Gold);
            return canvas;
        }

        private static Color32[] DrawKillEmber()
        {
            Color32[] canvas = NewCanvas();

            FillCircle(canvas, 8, 6, 5, Outline);
            FillTriangle(canvas, 3, 7, 13, 7, 8, 15, Outline);
            FillCircle(canvas, 8, 6, 4, Orange);
            FillTriangle(canvas, 4, 7, 12, 7, 8, 14, Orange);

            FillCircle(canvas, 8, 6, 2, Red);
            Set(canvas, 7, 6, Gold);
            Set(canvas, 8, 7, Gold);
            Set(canvas, 9, 5, Gold);
            Set(canvas, 6, 4, OrangeDeep);

            // 火花
            Set(canvas, 2, 12, Gold);
            Set(canvas, 13, 12, Gold);
            Set(canvas, 11, 14, OrangeDeep);
            Set(canvas, 5, 14, OrangeDeep);
            return canvas;
        }

        private static Color32[] NewCanvas()
        {
            return new Color32[Size * Size];
        }

        private static void Set(Color32[] canvas, int x, int y, Color32 color)
        {
            if (x < 0 || x >= Size || y < 0 || y >= Size) return;
            canvas[y * Size + x] = color;
        }

        private static void FillRect(Color32[] canvas, int x, int y, int width, int height, Color32 color)
        {
            for (int yy = y; yy < y + height; yy++)
            {
                for (int xx = x; xx < x + width; xx++)
                    Set(canvas, xx, yy, color);
            }
        }

        private static void FillCircle(Color32[] canvas, int cx, int cy, int radius, Color32 color)
        {
            int radiusSq = radius * radius;
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy <= radiusSq)
                        Set(canvas, x, y, color);
                }
            }
        }

        private static void FillTriangle(
            Color32[] canvas,
            int ax, int ay,
            int bx, int by,
            int cx, int cy,
            Color32 color)
        {
            int minX = Mathf.Min(ax, Mathf.Min(bx, cx));
            int maxX = Mathf.Max(ax, Mathf.Max(bx, cx));
            int minY = Mathf.Min(ay, Mathf.Min(by, cy));
            int maxY = Mathf.Max(ay, Mathf.Max(by, cy));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (!IsInsideTriangle(x, y, ax, ay, bx, by, cx, cy)) continue;
                    Set(canvas, x, y, color);
                }
            }
        }

        private static bool IsInsideTriangle(
            int x, int y,
            int ax, int ay,
            int bx, int by,
            int cx, int cy)
        {
            int d1 = Sign(x, y, ax, ay, bx, by);
            int d2 = Sign(x, y, bx, by, cx, cy);
            int d3 = Sign(x, y, cx, cy, ax, ay);

            bool hasNeg = d1 < 0 || d2 < 0 || d3 < 0;
            bool hasPos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(hasNeg && hasPos);
        }

        private static int Sign(int x, int y, int ax, int ay, int bx, int by)
        {
            return (x - bx) * (ay - by) - (ax - bx) * (y - by);
        }

        private static void Line(Color32[] canvas, int x0, int y0, int x1, int y1, Color32 color)
        {
            int dx = Mathf.Abs(x1 - x0);
            int dy = -Mathf.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;

            while (true)
            {
                Set(canvas, x0, y0, color);
                if (x0 == x1 && y0 == y1) break;

                int doubled = error * 2;
                if (doubled >= dy)
                {
                    error += dy;
                    x0 += sx;
                }

                if (doubled <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
