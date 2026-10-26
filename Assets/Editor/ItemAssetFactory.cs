using System.Collections.Generic;
using Roguelike.Items;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 生成初始道具资产与宝箱房道具台 Prefab。
    /// 菜单：Tools / Roguelike / 生成道具资产
    ///
    /// 幂等：重复执行会更新已有资产，不会产生重复文件。
    /// </summary>
    public static class ItemAssetFactory
    {
        public const string ItemFolder = "Assets/Resources/Items";
        public const string PickupPath = ItemFolder + "/ItemPickup.prefab";
        public const string AltarPath = ItemFolder + "/SecretAltar.prefab";

        private const string SpritePath = "Assets/Art/White.png";

        [MenuItem("Tools/Roguelike/生成道具资产")]
        public static void Rebuild()
        {
            EnsureFolder(ItemFolder);

            Create("Item_RapidTrigger", "快速触发器", "射速提升 25%。",
                new Color(0.40f, 0.85f, 1f), ThirdPartySprites.Weapon("weapon_regular_sword"), 0, 0,
                Mod(StatType.FireInterval, StatOp.Multiply, 0.75f));

            Create("Item_HeavyTear", "重型弹丸", "伤害 +1，射速降低 15%。",
                new Color(0.78f, 0.28f, 0.30f), ThirdPartySprites.Weapon("weapon_big_hammer"), 0, 0,
                Mod(StatType.Damage, StatOp.Add, 1f),
                Mod(StatType.FireInterval, StatOp.Multiply, 1.15f));

            Create("Item_SpeedBoots", "疾行之靴", "移动速度提升 20%。",
                new Color(0.40f, 0.90f, 0.50f), ThirdPartySprites.BootsIcon, 0, 0,
                Mod(StatType.MoveSpeed, StatOp.Multiply, 1.2f));

            Create("Item_HeartContainer", "生命容器", "生命上限 +2。",
                new Color(0.95f, 0.45f, 0.55f), ThirdPartySprites.Heart, 0, 0,
                Mod(StatType.MaxHealth, StatOp.Add, 2f));

            Create("Item_PiercingPrism", "穿透棱镜", "眼泪可以额外穿透 1 个敌人。",
                new Color(0.65f, 0.45f, 0.95f), ThirdPartySprites.Weapon("weapon_red_gem_sword"), 0, 0,
                Mod(StatType.Pierce, StatOp.Add, 1f));

            Create("Item_TripleShot", "三叉弹", "一次发射 3 发，射速降低 25%。",
                new Color(0.95f, 0.60f, 0.20f), ThirdPartySprites.TridentIcon, 0, 0,
                Mod(StatType.ShotCount, StatOp.Add, 2f),
                Mod(StatType.FireInterval, StatOp.Multiply, 1.25f));

            Create("Item_Slingshot", "强力弹弓", "弹丸速度提升 30%。",
                new Color(0.95f, 0.85f, 0.30f), ThirdPartySprites.SlingshotIcon, 0, 0,
                Mod(StatType.TearSpeed, StatOp.Multiply, 1.3f));

            Create("Item_GlassCannon", "玻璃大炮", "伤害翻倍，但生命上限 -2。",
                new Color(0.95f, 0.95f, 0.95f), ThirdPartySprites.GlassCannonIcon, 0, 0,
                Mod(StatType.Damage, StatOp.Multiply, 2f),
                Mod(StatType.MaxHealth, StatOp.Add, -2f));

            Create("Item_CoinBag", "幸运币袋", "每个敌人额外掉落 1 枚金币。",
                new Color(0.85f, 0.70f, 0.25f), ThirdPartySprites.CoinBagIcon, 0, 0,
                Mod(StatType.CoinBonus, StatOp.Add, 1f));

            Create("Item_LuckyCoin", "幸运币", "立即获得 5 枚金币。",
                new Color(0.95f, 0.80f, 0.20f), ThirdPartySprites.LuckyCoinIcon, 5, 0);

            Create("Item_HealingApple", "治疗苹果", "立即恢复 2 点生命。",
                new Color(0.90f, 0.30f, 0.30f), ThirdPartySprites.AppleIcon, 0, 2);

            EnsurePickupPrefab();
            EnsureAltarPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Items] 已生成道具资产、道具台与祭坛 Prefab：{ItemFolder}");
        }

        /// <summary>确保道具资产存在（创建玩家时调用）。</summary>
        public static bool EnsureItems()
        {
            const string samplePath = ItemFolder + "/Item_RapidTrigger.asset";
            bool hasItems = AssetDatabase.LoadAssetAtPath<ItemDefinition>(samplePath) != null;
            bool hasAltar = AssetDatabase.LoadAssetAtPath<SecretAltar>(AltarPath) != null;

            if (!hasItems || !hasAltar) Rebuild();

            return AssetDatabase.LoadAssetAtPath<ItemDefinition>(samplePath) != null;
        }

        private static void EnsurePickupPrefab()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                RoomPrefabFactory.Rebuild();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            }

            if (sprite == null) return;

            var root = new GameObject("ItemPickup");

            // 图标独立成子物体：不同武器尺寸差异很大，ItemPickup 会按 Sprite 尺寸统一缩放
            var iconObject = new GameObject("Icon");
            iconObject.transform.SetParent(root.transform, false);
            iconObject.transform.localScale = Vector3.one * 0.75f;

            var renderer = iconObject.AddComponent<SpriteRenderer>();
            Sprite potion = ThirdPartySprites.Potion;
            renderer.sprite = potion != null ? potion : sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 7;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            var pickup = root.AddComponent<ItemPickup>();
            var serialized = new SerializedObject(pickup);
            serialized.FindProperty("icon").objectReferenceValue = renderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PickupPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>秘密房祭坛：石台 + 悬浮的祭品图标。</summary>
        private static void EnsureAltarPrefab()
        {
            Sprite fallback = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (fallback == null)
            {
                RoomPrefabFactory.Rebuild();
                fallback = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            }

            if (fallback == null) return;

            var root = new GameObject("SecretAltar");

            var altarObject = new GameObject("Altar");
            altarObject.transform.SetParent(root.transform, false);
            var altarRenderer = altarObject.AddComponent<SpriteRenderer>();
            Sprite altarSprite = ThirdPartySprites.Altar;
            altarRenderer.sprite = altarSprite != null ? altarSprite : fallback;
            altarRenderer.sortingOrder = 3;

            var iconObject = new GameObject("RewardIcon");
            iconObject.transform.SetParent(root.transform, false);
            iconObject.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            iconObject.transform.localScale = Vector3.one * 0.8f;
            var iconRenderer = iconObject.AddComponent<SpriteRenderer>();
            Sprite potion = ThirdPartySprites.Potion;
            iconRenderer.sprite = potion != null ? potion : fallback;
            iconRenderer.sortingOrder = 7;

            // 石头底座挡住玩家，交互范围用较大的触发器
            var blockObject = new GameObject("Block");
            blockObject.transform.SetParent(root.transform, false);
            blockObject.transform.localPosition = new Vector3(0f, -0.25f, 0f);
            var block = blockObject.AddComponent<BoxCollider2D>();
            block.size = new Vector2(1.8f, 1.5f);

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 1.35f;
            collider.isTrigger = true;

            var altar = root.AddComponent<SecretAltar>();
            var serialized = new SerializedObject(altar);
            serialized.FindProperty("rewardIcon").objectReferenceValue = iconRenderer;
            serialized.FindProperty("altarRenderer").objectReferenceValue = altarRenderer;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, AltarPath);
            Object.DestroyImmediate(root);
        }

        private static StatModifier Mod(StatType stat, StatOp op, float value)
        {
            return new StatModifier(stat, op, value);
        }

        private static void Create(
            string fileName,
            string displayName,
            string description,
            Color color,
            Sprite icon,
            int coins,
            int heal,
            params StatModifier[] modifiers)
        {
            string path = $"{ItemFolder}/{fileName}.asset";

            var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            bool created = item == null;
            if (created) item = ScriptableObject.CreateInstance<ItemDefinition>();

            item.itemName = displayName;
            item.description = description;
            item.color = color;
            item.icon = icon;
            item.coinBonus = coins;
            item.healAmount = heal;
            item.modifiers = new List<StatModifier>(modifiers);

            if (created) AssetDatabase.CreateAsset(item, path);
            else EditorUtility.SetDirty(item);
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
