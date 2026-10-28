using System.Collections.Generic;
using Roguelike.Combat;
using Roguelike.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 生成六套楼层主题资产。
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
        public const string SewerPath = ThemeFolder + "/FloorTheme_Sewer.asset";
        public const string OvergrownPath = ThemeFolder + "/FloorTheme_Overgrown.asset";
        public const string ObsidianPath = ThemeFolder + "/FloorTheme_Obsidian.asset";

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

        // 第 4-6 层使用独立编成，避免完全复用前三层
        private static readonly string[] SewerRoster =
        {
            CombatPrefabFactory.TrashGreenSlimePath,
            CombatPrefabFactory.TrashSnakePath,
            CombatPrefabFactory.TrashBlueBatPath,
            CombatPrefabFactory.SwampyPath,
            CombatPrefabFactory.TinyZombieRaiderPath,
            CombatPrefabFactory.ChortPath,
        };

        // 骑士帧和玩家轮廓完全相同，暂时不放进敌人池；改用轮廓明显不同的法师/精灵/骷髅/Trash Mobs。
        private static readonly string[] RuinsRoster =
        {
            CombatPrefabFactory.TrashMushroomPath,
            CombatPrefabFactory.TrashBeePath,
            CombatPrefabFactory.TrashFlowerPath,
            CombatPrefabFactory.SkeletonWarriorPath,
            CombatPrefabFactory.WizardPath,
            CombatPrefabFactory.ElfPath,
        };

        private static readonly string[] ObsidianRoster =
        {
            CombatPrefabFactory.TrashMushroomPath,
            CombatPrefabFactory.TrashBeePath,
            CombatPrefabFactory.TrashFlowerPath,
            CombatPrefabFactory.SkeletonWarriorPath,
            CombatPrefabFactory.WizardPath,
            CombatPrefabFactory.ImpPath,
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

        private static readonly Color SewerFloorTint = new Color(0.50f, 0.82f, 0.78f, 1f);
        private static readonly Color SewerWallTint = new Color(0.38f, 0.64f, 0.60f, 1f);
        private static readonly Color SewerBackground = new Color(0.012f, 0.045f, 0.045f, 1f);

        private static readonly Color OvergrownFloorTint = new Color(0.66f, 0.84f, 0.52f, 1f);
        private static readonly Color OvergrownWallTint = new Color(0.48f, 0.64f, 0.38f, 1f);
        private static readonly Color OvergrownBackground = new Color(0.018f, 0.05f, 0.022f, 1f);

        // 第 6 层走暗紫黑曜，和第三层的橙红炼狱拉开色相
        private static readonly Color ObsidianFloorTint = new Color(0.56f, 0.30f, 0.82f, 1f);
        private static readonly Color ObsidianWallTint = new Color(0.30f, 0.16f, 0.44f, 1f);
        private static readonly Color ObsidianBackground = new Color(0.025f, 0.006f, 0.045f, 1f);

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
                "炼狱",
                2,
                InfernoFloorTint,
                InfernoWallTint,
                InfernoBackground,
                FloorBossVisual.BigDemon,
                infernoBgm);

            CreateOrUpdate(
                SewerPath,
                "下水道",
                3,
                SewerFloorTint,
                SewerWallTint,
                SewerBackground,
                FloorBossVisual.SewerTroll,
                dungeonBgm,
                ThirdPartySprites.SewerFloorVariants,
                ThirdPartySprites.SewerWall);

            CreateOrUpdate(
                OvergrownPath,
                "遗迹",
                4,
                OvergrownFloorTint,
                OvergrownWallTint,
                OvergrownBackground,
                FloorBossVisual.RuinsGolem,
                cryptBgm,
                ThirdPartySprites.OvergrownFloorVariants,
                ThirdPartySprites.OvergrownWall);

            CreateOrUpdate(
                ObsidianPath,
                "黑曜",
                5,
                ObsidianFloorTint,
                ObsidianWallTint,
                ObsidianBackground,
                FloorBossVisual.ObsidianKing,
                infernoBgm,
                ThirdPartySprites.ObsidianFloorVariants,
                ThirdPartySprites.ObsidianWall);

            AssetDatabase.SaveAssets();
            Debug.Log("[FloorTheme] 已生成 6 套楼层主题");
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
                "炼狱",
                2,
                InfernoFloorTint,
                InfernoWallTint,
                InfernoBackground,
                FloorBossVisual.BigDemon,
                infernoBgm);

            Ensure(
                SewerPath,
                "下水道",
                3,
                SewerFloorTint,
                SewerWallTint,
                SewerBackground,
                FloorBossVisual.SewerTroll,
                dungeonBgm,
                ThirdPartySprites.SewerFloorVariants,
                ThirdPartySprites.SewerWall);

            Ensure(
                OvergrownPath,
                "遗迹",
                4,
                OvergrownFloorTint,
                OvergrownWallTint,
                OvergrownBackground,
                FloorBossVisual.RuinsGolem,
                cryptBgm,
                ThirdPartySprites.OvergrownFloorVariants,
                ThirdPartySprites.OvergrownWall);

            Ensure(
                ObsidianPath,
                "黑曜",
                5,
                ObsidianFloorTint,
                ObsidianWallTint,
                ObsidianBackground,
                FloorBossVisual.ObsidianKing,
                infernoBgm,
                ThirdPartySprites.ObsidianFloorVariants,
                ThirdPartySprites.ObsidianWall);

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
            AudioClip bgm,
            Sprite[] floorVariants = null,
            Sprite wallSprite = null)
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
            theme.floorVariants = floorVariants ?? new Sprite[0];
            theme.wallSprite = wallSprite;
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
            AudioClip bgm,
            Sprite[] floorVariants = null,
            Sprite wallSprite = null)
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

                if (floorVariants != null && floorVariants.Length > 0 &&
                    (existing.floorVariants == null || existing.floorVariants.Length == 0))
                {
                    existing.floorVariants = floorVariants;
                    dirty = true;
                }

                if (wallSprite != null && existing.wallSprite == null)
                {
                    existing.wallSprite = wallSprite;
                    dirty = true;
                }

                if (dirty) EditorUtility.SetDirty(existing);
                return;
            }

            CreateOrUpdate(
                path,
                displayName,
                order,
                floorTint,
                wallTint,
                cameraBackground,
                bossVisual,
                bgm,
                floorVariants,
                wallSprite);
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
            if (path == SewerPath) return SewerRoster;
            if (path == OvergrownPath) return RuinsRoster;
            if (path == ObsidianPath) return ObsidianRoster;
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
