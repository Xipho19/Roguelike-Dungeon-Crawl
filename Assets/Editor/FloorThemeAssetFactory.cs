using System.Collections.Generic;
using Roguelike.Combat;
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

        private static readonly string[] DungeonRoster =
        {
            CombatPrefabFactory.EnemyPath,
            CombatPrefabFactory.ShooterPath,
            CombatPrefabFactory.ChargerPath,
            CombatPrefabFactory.GoblinPath,
            CombatPrefabFactory.OrcWarriorPath,
            CombatPrefabFactory.MuddyPath,
        };

        private static readonly string[] CryptRoster =
        {
            CombatPrefabFactory.SkeletPath,
            CombatPrefabFactory.ZombiePath,
            CombatPrefabFactory.IceZombiePath,
            CombatPrefabFactory.NecromancerPath,
            CombatPrefabFactory.WogolPath,
            CombatPrefabFactory.TinyZombieCloudPath,
        };

        private static readonly string[] InfernoRoster =
        {
            CombatPrefabFactory.ImpPath,
            CombatPrefabFactory.ChortPath,
            CombatPrefabFactory.MaskedOrcPath,
            CombatPrefabFactory.OrcShamanPath,
            CombatPrefabFactory.BomberPath,
            CombatPrefabFactory.SplitterPath,
        };

        public const string BgmFolder = "Assets/Resources/Audio/BGM";
        public const string DungeonBgmPath = BgmFolder + "/BGM_Dungeon.ogg";
        public const string CryptBgmPath = BgmFolder + "/BGM_Crypt.ogg";
        public const string InfernoBgmPath = BgmFolder + "/BGM_Inferno.ogg";

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

            AudioClip dungeonBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(DungeonBgmPath);
            AudioClip cryptBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(CryptBgmPath);
            AudioClip infernoBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(InfernoBgmPath);

            CreateOrUpdate(
                DungeonPath,
                "地牢",
                0,
                Color.white,
                Color.white,
                DungeonBackground,
                FloorBossVisual.Ogre,
                dungeonBgm);

            CreateOrUpdate(
                CryptPath,
                "墓穴",
                1,
                CryptFloorTint,
                CryptWallTint,
                CryptBackground,
                FloorBossVisual.BigZombie,
                cryptBgm);

            CreateOrUpdate(
                InfernoPath,
                "熔岩",
                2,
                InfernoFloorTint,
                InfernoWallTint,
                InfernoBackground,
                FloorBossVisual.BigDemon,
                infernoBgm);

            AssetDatabase.SaveAssets();
            Debug.Log("[FloorTheme] 已生成 3 套楼层主题");
        }

        /// <summary>只补齐缺失的主题资产，不覆盖已有配色。</summary>
        public static void EnsureThemes()
        {
            EnsureFolder(ThemeFolder);

            AudioClip dungeonBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(DungeonBgmPath);
            AudioClip cryptBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(CryptBgmPath);
            AudioClip infernoBgm = AssetDatabase.LoadAssetAtPath<AudioClip>(InfernoBgmPath);

            Ensure(
                DungeonPath,
                "地牢",
                0,
                Color.white,
                Color.white,
                DungeonBackground,
                FloorBossVisual.Ogre,
                dungeonBgm);

            Ensure(
                CryptPath,
                "墓穴",
                1,
                CryptFloorTint,
                CryptWallTint,
                CryptBackground,
                FloorBossVisual.BigZombie,
                cryptBgm);

            Ensure(
                InfernoPath,
                "熔岩",
                2,
                InfernoFloorTint,
                InfernoWallTint,
                InfernoBackground,
                FloorBossVisual.BigDemon,
                infernoBgm);

            AssetDatabase.SaveAssets();
        }

        private static void CreateOrUpdate(
            string path,
            string displayName,
            int order,
            Color floorTint,
            Color wallTint,
            Color cameraBackground,
            FloorBossVisual bossVisual,
            AudioClip bgm)
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
            theme.bossVisual = bossVisual;
            if (bgm != null) theme.bgm = bgm;
            ApplyRoster(theme, path, true);

            EditorUtility.SetDirty(theme);
        }

        private static void Ensure(
            string path,
            string displayName,
            int order,
            Color floorTint,
            Color wallTint,
            Color cameraBackground,
            FloorBossVisual bossVisual,
            AudioClip bgm)
        {
            FloorTheme existing = AssetDatabase.LoadAssetAtPath<FloorTheme>(path);
            if (existing != null)
            {
                // 旧资产可能没有 bossVisual / bgm 序列化字段，补齐但不改配色
                bool dirty = false;

                if (existing.bossVisual != bossVisual)
                {
                    existing.bossVisual = bossVisual;
                    dirty = true;
                }

                if (bgm != null && existing.bgm != bgm)
                {
                    existing.bgm = bgm;
                    dirty = true;
                }

                if (existing.commonEnemies == null || existing.commonEnemies.Length == 0)
                {
                    ApplyRoster(existing, path, true);
                    dirty = true;
                }

                if (dirty) EditorUtility.SetDirty(existing);
                return;
            }

            CreateOrUpdate(path, displayName, order, floorTint, wallTint, cameraBackground, bossVisual, bgm);
        }

        private static void ApplyRoster(FloorTheme theme, string path, bool force)
        {
            if (theme == null) return;
            if (!force && theme.commonEnemies != null && theme.commonEnemies.Length > 0) return;

            string[] paths = RosterPathsFor(path);
            if (paths == null || paths.Length == 0) return;

            var roster = new List<Enemy>(paths.Length);
            for (int i = 0; i < paths.Length; i++)
            {
                Enemy enemy = AssetDatabase.LoadAssetAtPath<Enemy>(paths[i]);
                if (enemy != null) roster.Add(enemy);
            }

            if (roster.Count > 0)
                theme.commonEnemies = roster.ToArray();
        }

        private static string[] RosterPathsFor(string path)
        {
            if (path == DungeonPath) return DungeonRoster;
            if (path == CryptPath) return CryptRoster;
            if (path == InfernoPath) return InfernoRoster;
            return null;
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
