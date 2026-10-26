using Roguelike.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 生成三套楼层主题资产。
    /// 菜单：Tools / Roguelike / 生成楼层主题
    ///
    /// 资产放在 Resources/FloorThemes，RunManager 启动时自动加载。
    /// Rebuild 会覆盖颜色；EnsureThemes 只补缺失资产，保留手工调色。
    /// </summary>
    public static class FloorThemeAssetFactory
    {
        public const string ThemeFolder = "Assets/Resources/FloorThemes";

        public const string DungeonPath = ThemeFolder + "/FloorTheme_Dungeon.asset";
        public const string CryptPath = ThemeFolder + "/FloorTheme_Crypt.asset";
        public const string InfernoPath = ThemeFolder + "/FloorTheme_Inferno.asset";

        private static readonly Color DungeonBackground = new Color(0.055f, 0.05f, 0.075f, 1f);

        private static readonly Color CryptFloorTint = new Color(0.68f, 0.92f, 0.95f, 1f);
        private static readonly Color CryptWallTint = new Color(0.70f, 0.88f, 0.92f, 1f);
        private static readonly Color CryptBackground = new Color(0.015f, 0.04f, 0.055f, 1f);

        private static readonly Color InfernoFloorTint = new Color(1.00f, 0.72f, 0.58f, 1f);
        private static readonly Color InfernoWallTint = new Color(1.00f, 0.66f, 0.52f, 1f);
        private static readonly Color InfernoBackground = new Color(0.09f, 0.02f, 0.015f, 1f);

        [MenuItem("Tools/Roguelike/生成楼层主题")]
        public static void RebuildFromMenu()
        {
            Rebuild();
        }

        /// <summary>创建或覆盖三套主题，使用默认配色。</summary>
        public static void Rebuild()
        {
            EnsureFolder(ThemeFolder);

            CreateOrUpdate(
                DungeonPath,
                "地牢",
                0,
                Color.white,
                Color.white,
                DungeonBackground);

            CreateOrUpdate(
                CryptPath,
                "墓穴",
                1,
                CryptFloorTint,
                CryptWallTint,
                CryptBackground);

            CreateOrUpdate(
                InfernoPath,
                "熔岩",
                2,
                InfernoFloorTint,
                InfernoWallTint,
                InfernoBackground);

            AssetDatabase.SaveAssets();
            Debug.Log("[FloorTheme] 已生成 3 套楼层主题");
        }

        /// <summary>只补齐缺失的主题资产，不覆盖已有配色。</summary>
        public static void EnsureThemes()
        {
            EnsureFolder(ThemeFolder);

            Ensure(
                DungeonPath,
                "地牢",
                0,
                Color.white,
                Color.white,
                DungeonBackground);

            Ensure(
                CryptPath,
                "墓穴",
                1,
                CryptFloorTint,
                CryptWallTint,
                CryptBackground);

            Ensure(
                InfernoPath,
                "熔岩",
                2,
                InfernoFloorTint,
                InfernoWallTint,
                InfernoBackground);

            AssetDatabase.SaveAssets();
        }

        private static void CreateOrUpdate(
            string path,
            string displayName,
            int order,
            Color floorTint,
            Color wallTint,
            Color cameraBackground)
        {
            FloorTheme theme = AssetDatabase.LoadAssetAtPath<FloorTheme>(path);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<FloorTheme>();
                AssetDatabase.CreateAsset(theme, path);
            }

            theme.displayName = displayName;
            theme.order = order;
            theme.floorTint = floorTint;
            theme.wallTint = wallTint;
            theme.cameraBackground = cameraBackground;

            EditorUtility.SetDirty(theme);
        }

        private static void Ensure(
            string path,
            string displayName,
            int order,
            Color floorTint,
            Color wallTint,
            Color cameraBackground)
        {
            if (AssetDatabase.LoadAssetAtPath<FloorTheme>(path) != null) return;

            CreateOrUpdate(path, displayName, order, floorTint, wallTint, cameraBackground);
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
