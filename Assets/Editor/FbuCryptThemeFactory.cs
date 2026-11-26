using Roguelike.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 第 2 层墓穴的 FBU 32px 地图：配置贴图并写入 FloorTheme_Crypt。
    /// 菜单：Tools / Roguelike / FBU / 应用第 2 层墓穴试点
    ///
    /// 与第 1 层同一套几何与开关，只换素材与配色：
    ///   - 地板：Underwater 冷灰绿石板（48×48 无描边内部）
    ///   - 墙体：Winter Time 冰蓝灰砖
    ///   - 道具：墓碑、十字碑、石棺、骨堆 + 复用的石像 / 火盆 / 石瓮 / 宝箱 / 陶罐
    /// </summary>
    public static class FbuCryptThemeFactory
    {
        private const string ArtRoot = "Assets/Art/FBU/Crypt";
        private const string ThemePath = "Assets/Resources/FloorThemes/FloorTheme_Crypt.asset";

        // 第 1 层已导入过的通用道具，墓穴直接复用
        private const string SharedArtRoot = "Assets/Art/FBU/Dungeon";

        [MenuItem("Tools/Roguelike/FBU/应用第 2 层墓穴试点")]
        public static void ApplyCryptPilot()
        {
            AssetDatabase.Refresh();

            ConfigureSprite($"{ArtRoot}/floor_base.png");
            Sprite floorBase = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/floor_base.png");

            ConfigureSprite($"{ArtRoot}/wall_h.png");
            ConfigureSprite($"{ArtRoot}/wall_v.png");
            Sprite wallH = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/wall_h.png");
            Sprite wallV = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/wall_v.png");

            string[] props =
            {
                "prop_grave_slab", "prop_grave_slab_b",
                "prop_skull_pile", "prop_skull_pile_b",
                "prop_angel_statue",
            };
            for (int i = 0; i < props.Length; i++)
                ConfigureSprite($"{ArtRoot}/props/{props[i]}.png");

            // 墙体阴影贴图（Resources，PPU 32）
            ConfigureSprite("Assets/Resources/FBU/wall_shadow.png");

            FloorTheme theme = AssetDatabase.LoadAssetAtPath<FloorTheme>(ThemePath);
            if (theme == null)
            {
                Debug.LogError($"[FBU] 找不到楼层主题：{ThemePath}");
                return;
            }

            theme.floorTexture = floorBase;
            theme.floorVariants = new Sprite[0];
            theme.allowFloorFlip = false;
            theme.useRoomTypeTint = false;
            theme.hideLegacyDecorations = true;
            // 门洞红色骷髅地毯是判断“门后还有战斗”的唯一提示，不能隐藏。
            theme.hideDoorIndicators = false;
            theme.useDoorThresholds = true;
            theme.wallSprite = wallH;
            theme.wallHorizontalSprite = wallH;
            theme.wallVerticalSprite = wallV;

            // 可破坏容器：沿用陶罐，墓穴里当成骨灰罐
            theme.crateSprite = LoadSharedProp("prop_pot_cracked");
            theme.crateAltSprite = LoadSharedProp("prop_pot_stone");
            theme.crateSprites = new[]
            {
                LoadSharedProp("prop_pot_cracked"),
                LoadSharedProp("prop_pot_stone"),
                LoadSharedProp("prop_pot_gold"),
            };

            theme.chestSprite = LoadSharedProp("prop_chest_closed");
            theme.chestOpenSprite = LoadSharedProp("prop_chest_open");
            // 起点/战斗中靠墙摆放的“骷髅”位改成骷髅墓碑：实心、不可破坏
            theme.skullSprite = LoadProp("prop_grave_slab");
            // 立柱用兜帽石像的上下分段，保持可绕行 + 遮挡
            theme.columnSprite = LoadSharedProp("prop_statue");
            theme.columnLowerSprite = LoadSharedProp("prop_statue_base");
            theme.columnUpperSprite = LoadSharedProp("prop_statue_top");
            theme.brazierSprite = LoadSharedProp("prop_brazier");
            theme.urnSprite = LoadSharedProp("prop_urn");

            // 角落随机杂物：两种骷髅墓碑 / 两种头骨堆 / 天使雕像 / 石瓮
            theme.floorClutterSprites = new[]
            {
                LoadProp("prop_grave_slab"),
                LoadProp("prop_grave_slab_b"),
                LoadProp("prop_skull_pile"),
                LoadProp("prop_skull_pile_b"),
                LoadProp("prop_angel_statue"),
                LoadSharedProp("prop_urn"),
            };

            // 墓穴配色：冷灰地板 + 更暗的青蓝墙，背景近黑偏蓝
            theme.floorTint = new Color(0.72f, 0.78f, 0.82f, 1f);
            theme.wallTint = new Color(0.50f, 0.60f, 0.68f, 1f);
            theme.cameraBackground = new Color(0.008f, 0.014f, 0.024f, 1f);

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[FBU] 第 2 层墓穴试点已应用到 FloorTheme_Crypt");
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
