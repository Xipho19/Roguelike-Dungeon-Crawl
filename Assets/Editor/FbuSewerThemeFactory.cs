using Roguelike.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 第 4 层下水道的 FBU 32px 地图：配置贴图并写入 FloorTheme_Sewer。
    /// 菜单：Tools / Roguelike / FBU / 应用第 4 层下水道试点
    ///
    /// 与第 1–3 层同一套几何与开关，只换素材与配色：
    ///   - 地板：Underwater A5 第一行倒数第三个苔绿石砖（32×32，平铺无缝）
    ///   - 墙体：Underwater 蓝色砖墙（32×32）
    ///   - 道具：石拱神龛、兜帽石像、白石柱、骸骨、水草、蕨叶、水晶
    /// </summary>
    public static class FbuSewerThemeFactory
    {
        private const string ArtRoot = "Assets/Art/FBU/Sewer";
        private const string SharedArtRoot = "Assets/Art/FBU/Dungeon";
        private const string ThemePath = "Assets/Resources/FloorThemes/FloorTheme_Sewer.asset";

        [MenuItem("Tools/Roguelike/FBU/应用第 4 层下水道试点")]
        public static void ApplySewerPilot()
        {
            AssetDatabase.Refresh();

            // 地板是 A5 图集的 32×32 石砖：PPU 32 → 每格 1×1 世界单位。
            ConfigureSprite($"{ArtRoot}/floor_base.png");
            ConfigureSprite($"{ArtRoot}/wall_h.png");
            ConfigureSprite($"{ArtRoot}/wall_v.png");

            string[] props =
            {
                "prop_sewer_statue", "prop_sewer_pillar", "prop_sewer_bones",
                "prop_sewer_shrine", "prop_sewer_kelp_a", "prop_sewer_kelp_b",
                "prop_sewer_fern", "prop_sewer_crystal",
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

            // 可破坏容器：沿用陶罐，下水道里当成污水罐
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
            // 墙边"骷髅位"改成石拱神龛：实心、不可破坏
            theme.skullSprite = LoadProp("prop_sewer_shrine");
            // 立柱用青蓝兜帽石像：完整碰撞（不再拆上下段）
            theme.columnSprite = LoadProp("prop_sewer_statue");
            theme.columnLowerSprite = null;
            theme.columnUpperSprite = null;
            // 火盆沿用第 1 层的铁盆火把，水下城邦也需要照明
            theme.brazierSprite = LoadSharedProp("prop_brazier");
            // 石瓮位改成白色大理石柱
            theme.urnSprite = LoadProp("prop_sewer_pillar");

            // 角落随机杂物：骸骨 / 两种水草 / 蕨叶 / 青蓝水晶
            theme.floorClutterSprites = new[]
            {
                LoadProp("prop_sewer_bones"),
                LoadProp("prop_sewer_kelp_a"),
                LoadProp("prop_sewer_kelp_b"),
                LoadProp("prop_sewer_fern"),
                LoadProp("prop_sewer_crystal"),
            };

            // 下水道配色：湿石板压绿 + 更暗的藻绿墙，背景近黑偏青
            theme.floorTint = new Color(0.78f, 0.95f, 0.76f, 1f);
            theme.wallTint = new Color(0.55f, 0.78f, 0.62f, 1f);
            theme.cameraBackground = new Color(0.008f, 0.026f, 0.022f, 1f);

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[FBU] 第 4 层下水道试点已应用到 FloorTheme_Sewer");
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
