using Roguelike.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 第 1 层地牢的 FBU 32px 试点：配置贴图并写入 FloorTheme_Dungeon。
    /// 菜单：Tools / Roguelike / FBU / 应用第 1 层地牢试点
    /// </summary>
    public static class FbuPilotThemeFactory
    {
        private const string ArtRoot = "Assets/Art/FBU/Dungeon";
        private const string ThemePath = "Assets/Resources/FloorThemes/FloorTheme_Dungeon.asset";

        [MenuItem("Tools/Roguelike/FBU/应用第 1 层地牢试点")]
        public static void ApplyDungeonPilot()
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
                "prop_chest_closed", "prop_chest_open", "prop_brazier",
                "prop_bones", "prop_rubble", "prop_rock_spire", "prop_urn",
                "prop_statue", "prop_statue_base", "prop_statue_top",
                "prop_pot_cracked", "prop_pot_gold", "prop_pot_stone",
                "prop_crystal_green", "prop_crystal_purple",
            };
            for (int i = 0; i < props.Length; i++)
                ConfigureSprite($"{ArtRoot}/props/{props[i]}.png");

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
            // 保留红色骷髅门槛：这是玩家判断“门后还有战斗”的唯一提示，不能隐藏。
            theme.hideDoorIndicators = false;
            theme.useDoorThresholds = true;
            theme.wallSprite = wallH;
            theme.wallHorizontalSprite = wallH;
            theme.wallVerticalSprite = wallV;

            // 32px 场景道具：陶罐替代木箱、FBU 宝箱、骸骨、火盆、石瓮与兜帽石像
            theme.crateSprite = LoadProp("prop_pot_cracked");
            theme.crateAltSprite = LoadProp("prop_pot_stone");
            theme.crateSprites = new[]
            {
                LoadProp("prop_pot_cracked"),
                LoadProp("prop_pot_stone"),
                LoadProp("prop_pot_gold"),
            };
            theme.chestSprite = LoadProp("prop_chest_closed");
            theme.chestOpenSprite = LoadProp("prop_chest_open");
            theme.skullSprite = LoadProp("prop_bones");
            theme.columnSprite = LoadProp("prop_statue");
            theme.columnLowerSprite = LoadProp("prop_statue_base");
            theme.columnUpperSprite = LoadProp("prop_statue_top");
            theme.brazierSprite = LoadProp("prop_brazier");
            theme.urnSprite = LoadProp("prop_urn");
            theme.floorClutterSprites = new[]
            {
                LoadProp("prop_rubble"),
                LoadProp("prop_bones"),
                LoadProp("prop_pot_gold"),
                LoadProp("prop_pot_stone"),
                LoadProp("prop_crystal_purple"),
            };

            // 地板作为视觉主体，墙体压暗，形成“亮地暗墙”的俯视角层次。
            theme.floorTint = new Color(0.74f, 0.79f, 0.88f, 1f);
            theme.wallTint = new Color(0.55f, 0.62f, 0.78f, 1f);
            theme.cameraBackground = new Color(0.012f, 0.018f, 0.032f, 1f);

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[FBU] 第 1 层地牢试点已应用到 FloorTheme_Dungeon");
        }

        private static Sprite LoadProp(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/props/{name}.png");
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
