using Roguelike.Combat;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 把 Assets/Art/Bosses 下的六套方向行走帧配置成 Sprite，
    /// 并生成 Resources/BossVisuals 下的 BossVisualDefinition。
    /// </summary>
    public static class BossVisualFactory
    {
        private const string ArtRoot = "Assets/Art/Bosses";
        private const string DefinitionRoot = "Assets/Resources/BossVisuals";

        [MenuItem("Tools/Roguelike/生成新 Boss 视觉资产")]
        public static void BuildAll()
        {
            AssetDatabase.Refresh();
            EnsureFolder("Assets/Resources", "BossVisuals");

            Build("Ogre", "蓝晶雏龙", "冲撞与震地", 1.05f);
            Build("BigZombie", "死亡骑士", "亡者军团与腐蚀", 0.95f);
            Build("BigDemon", "赤金狮蝎", "螺旋弹幕与追踪火球", 1f);
            Build("SewerTroll", "瘟疫鼠王", "毒沼与连续冲锋", 0.68f);
            Build("RuinsGolem", "深蓝守望者", "震地与石弹", 0.92f);
            Build("ObsidianKing", "深渊魔君", "黑曜王座的主人 · 最终试炼", 0.9f);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Boss] 六套方向行走视觉资产已生成到 Resources/BossVisuals");
        }

        private static void Build(
            string folder,
            string displayName,
            string subtitle,
            float visualScale)
        {
            ConfigureFrames(folder);

            string assetPath = $"{DefinitionRoot}/{folder}.asset";
            BossVisualDefinition definition =
                AssetDatabase.LoadAssetAtPath<BossVisualDefinition>(assetPath);

            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BossVisualDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            definition.displayName = displayName;
            definition.subtitle = subtitle;
            definition.visualScale = visualScale;
            definition.idleDown = Idle(folder, "down");
            definition.runDown = Run(folder, "down");
            definition.idleSide = Idle(folder, "right");
            definition.runSide = Run(folder, "right");
            definition.idleUp = Idle(folder, "up");
            definition.runUp = Run(folder, "up");
            definition.portrait = Load($"{ArtRoot}/{folder}/idle_down.png");
            definition.hudIcon = definition.portrait;

            EditorUtility.SetDirty(definition);
        }

        private static void ConfigureFrames(string folder)
        {
            string[] directions = { "down", "left", "right", "up" };
            for (int d = 0; d < directions.Length; d++)
            {
                ConfigureSprite($"{ArtRoot}/{folder}/idle_{directions[d]}.png");
                for (int i = 0; i < 3; i++)
                    ConfigureSprite($"{ArtRoot}/{folder}/run_{directions[d]}_{i}.png");
            }
        }

        private static void ConfigureSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Boss] 找不到贴图：{path}");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static Sprite[] Idle(string folder, string direction)
        {
            return new[] { Load($"{ArtRoot}/{folder}/idle_{direction}.png") };
        }

        private static Sprite[] Run(string folder, string direction)
        {
            return new[]
            {
                Load($"{ArtRoot}/{folder}/run_{direction}_0.png"),
                Load($"{ArtRoot}/{folder}/run_{direction}_1.png"),
                Load($"{ArtRoot}/{folder}/run_{direction}_2.png"),
            };
        }

        private static Sprite Load(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
                AssetDatabase.CreateFolder(parent, child);
        }
    }
}
