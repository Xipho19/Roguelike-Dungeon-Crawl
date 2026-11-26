using Roguelike.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 第 3 层黄金城的 FBU 地图：配置贴图并写入 FloorTheme_Inferno。
    /// 菜单：Tools / Roguelike / FBU / 应用第 3 层黄金城试点
    ///
    /// 与第 1、2 层同一套几何与开关，只换素材与配色：
    ///   - 地板：Golden Cave 金色砖地（48×48 无缝）
    ///   - 墙体：Golden Cave 金色砖墙
    ///   - 道具：金色神龛、金矿骷髅堆、金像、金柱、魔法火盆
    /// </summary>
    public static class FbuInfernoThemeFactory
    {
        private const string ArtRoot = "Assets/Art/FBU/Inferno";
        private const string SharedArtRoot = "Assets/Art/FBU/Dungeon";
        private const string ThemePath = "Assets/Resources/FloorThemes/FloorTheme_Inferno.asset";

        [MenuItem("Tools/Roguelike/FBU/应用第 3 层黄金城试点")]
        public static void ApplyInfernoPilot()
        {
            AssetDatabase.Refresh();

            ConfigureSprite($"{ArtRoot}/floor_base.png");
            ConfigureSprite($"{ArtRoot}/wall_h.png");
            ConfigureSprite($"{ArtRoot}/wall_v.png");

            string[] props =
            {
                "prop_gold_shrine", "prop_gold_ore", "prop_gold_pillar", "prop_gold_idol",
                "prop_magic_brazier_gold", "prop_magic_brazier_purple",
            };
            for (int i = 0; i < props.Length; i++)
                ConfigureSprite($"{ArtRoot}/props/{props[i]}.png");

            // 墙体阴影共用第 1 层导入的贴图
            ConfigureSprite("Assets/Resources/FBU/wall_shadow.png");

            FloorTheme theme = AssetDatabase.LoadAssetAtPath<FloorTheme>(ThemePath);
            if (theme == null)
            {
                Debug.LogError($"[FBU] 找不到楼层主题：{ThemePath}");
                return;
            }

            theme.floorTexture = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/floor_base.png");
            theme.floorVariants = new Sprite[0];
            theme.allowFloorFlip = false;
            theme.useRoomTypeTint = false;
            theme.hideLegacyDecorations = true;
            // 门洞红色骷髅地毯是判断"门后还有战斗"的唯一提示，不能隐藏
            theme.hideDoorIndicators = false;
            theme.useDoorThresholds = true;
            theme.wallSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/wall_h.png");
            theme.wallHorizontalSprite = theme.wallSprite;
            theme.wallVerticalSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/wall_v.png");

            // 可破坏容器：金/石陶罐，黄金城层当宝瓮
            theme.crateSprite = LoadSharedProp("prop_pot_gold");
            theme.crateAltSprite = LoadSharedProp("prop_pot_stone");
            theme.crateSprites = new[]
            {
                LoadSharedProp("prop_pot_gold"),
                LoadSharedProp("prop_pot_stone"),
                LoadSharedProp("prop_pot_cracked"),
            };

            theme.chestSprite = LoadSharedProp("prop_chest_closed");
            theme.chestOpenSprite = LoadSharedProp("prop_chest_open");
            // 骷髅位改成金矿骷髅堆：实心、不可破坏
            theme.skullSprite = LoadProp("prop_gold_ore");
            // 立柱沿用兜帽石像的上下分段，保持可绕行 + 遮挡
            theme.columnSprite = LoadSharedProp("prop_statue");
            theme.columnLowerSprite = LoadSharedProp("prop_statue_base");
            theme.columnUpperSprite = LoadSharedProp("prop_statue_top");
            theme.brazierSprite = LoadProp("prop_magic_brazier_gold");
            theme.urnSprite = LoadSharedProp("prop_urn");

            // 角落随机杂物：金矿 / 金像 / 金神龛 / 金柱 / 紫火盆
            theme.floorClutterSprites = new[]
            {
                LoadProp("prop_gold_ore"),
                LoadProp("prop_gold_idol"),
                LoadProp("prop_gold_shrine"),
                LoadProp("prop_gold_pillar"),
                LoadProp("prop_magic_brazier_purple"),
            };

            // 黄金城配色：暖金地板 + 暗金墙，背景近黑偏红
            theme.floorTint = new Color(1.0f, 0.86f, 0.70f, 1f);
            theme.wallTint = new Color(0.92f, 0.70f, 0.52f, 1f);
            theme.cameraBackground = new Color(0.085f, 0.020f, 0.012f, 1f);

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[FBU] 第 3 层黄金城试点已应用到 FloorTheme_Inferno");
        }

        private static Sprite LoadProp(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/props/{name}.png");
        }

        private static Sprite LoadSharedProp(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{SharedArtRoot}/props/{name}.png");
        }

        private static void ConfigureSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[FBU] 找不到贴图：{path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
}
