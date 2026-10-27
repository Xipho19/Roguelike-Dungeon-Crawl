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
            ItemIconFactory.EnsureIcons();

            Create("Item_RapidTrigger", "快速触发器", "射速提升 25%。",
                new Color(0.40f, 0.85f, 1f), ThirdPartySprites.RapidTriggerIcon, 0, 0,
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
                new Color(0.65f, 0.45f, 0.95f), ThirdPartySprites.PiercingPrismIcon, 0, 0,
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

            // 自动触发道具
            CreateTriggered("Item_ExplosiveTears", "爆炸眼泪",
                "被动：眼泪命中时在小范围内爆炸，造成 1 点伤害。",
                new Color(0.55f, 0.78f, 1f), ThirdPartySprites.Tear,
                ItemRarity.Rare, 1f, 1.8f, 1, ItemSynergy.ExplosiveTears, 12);

            CreateTriggered("Item_VampireFang", "吸血鬼獠牙",
                "被动：每击杀 5 个敌人回复 1 点生命。",
                new Color(0.78f, 0.22f, 0.28f), ThirdPartySprites.VampireFangIcon,
                ItemRarity.Rare, 1f, 0f, 5, ItemSynergy.KillHeal, 12);

            // 参考以撒的自动道具机制，按当前数值体系重新设计
            CreateTriggered("Item_BentSpoon", "弯勺魔咒",
                "被动：眼泪会缓慢追踪当前房间内的敌人。伤害 -10%。",
                new Color(0.72f, 0.62f, 0.96f), ThirdPartySprites.BentSpoonIcon,
                ItemRarity.Rare, 360f, 6f, 1, ItemSynergy.HomingTears, 12,
                Mod(StatType.Damage, StatOp.Multiply, 0.9f));

            CreateTriggered("Item_CorrosiveGland", "腐蚀腺体",
                "被动：眼泪命中时立即造成 0.5 点毒伤，随后 1.8 秒内每 0.6 秒再造成 0.5 点（共 2 点）。",
                new Color(0.38f, 0.82f, 0.42f), ThirdPartySprites.CorrosiveGlandIcon,
                ItemRarity.Rare, 0.5f, 0f, 1, ItemSynergy.PoisonTears, 11);

            CreateTriggered("Item_CorpseBomb", "尸爆菌",
                "被动：敌人死亡时爆炸，对周围敌人造成 2 点伤害。",
                new Color(0.86f, 0.72f, 0.50f), ThirdPartySprites.CorpseBombIcon,
                ItemRarity.Rare, 2f, 2.2f, 1, ItemSynergy.DeathExplosion, 13);

            CreateTriggered("Item_ThornHeart", "荆棘之心",
                "被动：受到伤害时，对周围敌人造成 2 点伤害。",
                new Color(0.86f, 0.24f, 0.34f), ThirdPartySprites.ThornHeartIcon,
                ItemRarity.Rare, 2f, 2.8f, 1, ItemSynergy.Thorns, 13);

            CreateTriggered("Item_Coupon", "折扣券",
                "被动：商店商品半价。",
                new Color(0.95f, 0.80f, 0.36f), ThirdPartySprites.CouponIcon,
                ItemRarity.Rare, 0f, 0f, 1, ItemSynergy.ShopDiscount, 10);

            CreateTriggered("Item_DoubleEye", "双生之眼",
                "被动：一次发射 2 发眼泪，单发伤害 -20%。",
                new Color(0.62f, 0.82f, 0.98f), ThirdPartySprites.DoubleEyeIcon,
                ItemRarity.Rare, 0f, 0f, 1, ItemSynergy.None, 10,
                Mod(StatType.ShotCount, StatOp.Add, 1f),
                Mod(StatType.Damage, StatOp.Multiply, 0.8f));

            CreateTriggered("Item_CyclopsEye", "独眼巨人",
                "被动：伤害翻倍，但射速降低 52%。",
                new Color(0.96f, 0.62f, 0.40f), ThirdPartySprites.CyclopsEyeIcon,
                ItemRarity.Rare, 0f, 0f, 1, ItemSynergy.None, 14,
                Mod(StatType.Damage, StatOp.Multiply, 2f),
                Mod(StatType.FireInterval, StatOp.Multiply, 2.1f));

            // 第二轮扩充：以撒式自动道具
            CreateTriggered("Item_GreedEye", "贪婪之眼",
                "被动：每持有 10 金币，伤害 +0.5（最多 +2）。",
                new Color(0.95f, 0.78f, 0.30f), ThirdPartySprites.GreedEyeIcon,
                ItemRarity.Rare, 2f, 10f, 1, ItemSynergy.CoinDamage, 13);

            CreateTriggered("Item_EmberChalice", "余烬圣餐",
                "被动：每清空一个房间，回复 1 点生命。",
                new Color(0.98f, 0.68f, 0.32f), ThirdPartySprites.EmberChaliceIcon,
                ItemRarity.Rare, 1f, 0f, 1, ItemSynergy.RoomClearHeal, 12);

            CreateTriggered("Item_HolyVeil", "圣光帷幕",
                "被动：进入新房间时获得 1.5 秒无敌。",
                new Color(0.68f, 0.86f, 1f), ThirdPartySprites.HolyVeilIcon,
                ItemRarity.Rare, 1.5f, 0f, 1, ItemSynergy.RoomEntryShield, 14);

            CreateTriggered("Item_LuckyFoot", "幸运兔脚",
                "被动：敌人掉落回血的概率 +15%，移动速度 +10%。",
                new Color(0.70f, 0.88f, 0.48f), ThirdPartySprites.LuckyFootIcon,
                ItemRarity.Rare, 0.15f, 0f, 1, ItemSynergy.LuckyDrops, 10,
                Mod(StatType.MoveSpeed, StatOp.Multiply, 1.1f));

            CreateTriggered("Item_CritFang", "尖牙暴击",
                "被动：眼泪有 20% 概率造成双倍伤害。",
                new Color(0.98f, 0.86f, 0.44f), ThirdPartySprites.CritFangIcon,
                ItemRarity.Rare, 0.2f, 0f, 1, ItemSynergy.CritTears, 11);

            CreateTriggered("Item_SplitParasite", "裂体寄生虫",
                "被动：眼泪首次命中后分裂成 2 颗小眼泪，每颗造成 50% 伤害。",
                new Color(0.52f, 0.82f, 0.42f), ThirdPartySprites.SplitParasiteIcon,
                ItemRarity.Rare, 0.5f, 0f, 2, ItemSynergy.SplitTears, 13);

            CreateTriggered("Item_DivineGlimmer", "神性微光",
                "被动：眼泪飞行时对周围 1.6 半径的敌人每 0.3 秒造成 0.25 点伤害。",
                new Color(1f, 0.92f, 0.62f), ThirdPartySprites.DivineGlimmerIcon,
                ItemRarity.Rare, 0.25f, 1.6f, 1, ItemSynergy.TearAura, 14);

            CreateTriggered("Item_GhostPrism", "幽灵棱镜",
                "被动：穿透 +2，眼泪速度 +10%。",
                new Color(0.72f, 0.62f, 0.98f), ThirdPartySprites.GhostPrismIcon,
                ItemRarity.Rare, 0f, 0f, 1, ItemSynergy.None, 10,
                Mod(StatType.Pierce, StatOp.Add, 2f),
                Mod(StatType.TearSpeed, StatOp.Multiply, 1.1f));

            CreateTriggered("Item_WindFeather", "风之羽",
                "被动：移动速度 +25%，眼泪速度 +15%。",
                new Color(0.52f, 0.90f, 0.92f), ThirdPartySprites.WindFeatherIcon,
                ItemRarity.Rare, 0f, 0f, 1, ItemSynergy.None, 10,
                Mod(StatType.MoveSpeed, StatOp.Multiply, 1.25f),
                Mod(StatType.TearSpeed, StatOp.Multiply, 1.15f));

            CreateTriggered("Item_BloodPact", "血契",
                "被动：伤害 ×1.5，生命上限 -1。",
                new Color(0.90f, 0.28f, 0.34f), ThirdPartySprites.BloodPactIcon,
                ItemRarity.Rare, 0f, 0f, 1, ItemSynergy.None, 12,
                Mod(StatType.Damage, StatOp.Multiply, 1.5f),
                Mod(StatType.MaxHealth, StatOp.Add, -1f));

            // 第三轮扩充：弹跳、条件增伤、射击节拍、击杀与清房联动
            CreateTriggered("Item_BounceTear", "弹跳泪滴",
                "被动：眼泪碰到墙壁后反弹 2 次，每次反弹后速度变为 90%。",
                new Color(0.42f, 0.72f, 1f), ThirdPartySprites.BounceTearIcon,
                ItemRarity.Rare, 0.9f, 0f, 2, ItemSynergy.BounceTears, 11);

            CreateTriggered("Item_BloodRage", "血怒",
                "被动：生命越低伤害越高，最多 +2。",
                new Color(0.90f, 0.22f, 0.28f), ThirdPartySprites.BloodRageIcon,
                ItemRarity.Rare, 2f, 0f, 1, ItemSynergy.BloodRage, 13);

            CreateTriggered("Item_CollectorHeart", "收藏家之心",
                "被动：每持有 5 件道具，伤害 +0.5（最多 +3）。",
                new Color(0.84f, 0.64f, 0.28f), ThirdPartySprites.CollectorHeartIcon,
                ItemRarity.Legendary, 3f, 0f, 5, ItemSynergy.ItemCountDamage, 15);

            CreateTriggered("Item_Metronome", "节拍器",
                "被动：每第 5 次射击额外发射 1 发眼泪。",
                new Color(0.98f, 0.58f, 0.24f), ThirdPartySprites.MetronomeIcon,
                ItemRarity.Rare, 1f, 0f, 5, ItemSynergy.EveryNthShot, 12);

            CreateTriggered("Item_HunterBoots", "猎手之靴",
                "被动：击杀敌人后 1.5 秒内移动速度 +30%。",
                new Color(0.34f, 0.78f, 0.46f), ThirdPartySprites.HunterBootsIcon,
                ItemRarity.Rare, 1.5f, 0f, 1, ItemSynergy.KillHaste, 12);

            CreateTriggered("Item_KillEmber", "杀戮余烬",
                "被动：每击杀 2 个敌人，获得 0.8 秒无敌。",
                new Color(1f, 0.52f, 0.22f), ThirdPartySprites.KillEmberIcon,
                ItemRarity.Rare, 0.8f, 0f, 2, ItemSynergy.KillShield, 13);

            CreateTriggered("Item_GuardShell", "守护之壳",
                "被动：只要还有护盾，受到的伤害只会扣除护盾，不会溢出扣血。",
                new Color(0.42f, 0.74f, 0.98f), ThirdPartySprites.ShieldIcon,
                ItemRarity.Rare, 0f, 0f, 1, ItemSynergy.ShieldGuard, 11);

            EnsurePickupPrefab();
            EnsureAltarPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[Items] 已生成道具资产、道具台与祭坛 Prefab：{ItemFolder}");
        }

        /// <summary>确保全部 37 件自动道具资产存在（创建玩家时调用）。</summary>
        public static bool EnsureItems()
        {
            ItemIconFactory.EnsureIcons();

            string[] required =
            {
                "Item_RapidTrigger", "Item_HeavyTear", "Item_SpeedBoots", "Item_HeartContainer",
                "Item_PiercingPrism", "Item_TripleShot", "Item_Slingshot", "Item_GlassCannon",
                "Item_CoinBag", "Item_LuckyCoin", "Item_HealingApple",
                "Item_ExplosiveTears", "Item_VampireFang",
                "Item_BentSpoon", "Item_CorrosiveGland", "Item_CorpseBomb", "Item_ThornHeart",
                "Item_Coupon", "Item_DoubleEye", "Item_CyclopsEye",
                "Item_GreedEye", "Item_EmberChalice", "Item_HolyVeil", "Item_LuckyFoot",
                "Item_CritFang", "Item_SplitParasite", "Item_DivineGlimmer", "Item_GhostPrism",
                "Item_WindFeather", "Item_BloodPact",
                "Item_BounceTear", "Item_BloodRage", "Item_CollectorHeart", "Item_Metronome",
                "Item_HunterBoots", "Item_KillEmber", "Item_GuardShell",
            };

            bool complete = true;
            for (int i = 0; i < required.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemFolder}/{required[i]}.asset") != null)
                    continue;

                complete = false;
                break;
            }

            bool hasAltar = AssetDatabase.LoadAssetAtPath<SecretAltar>(AltarPath) != null;
            if (!complete || !hasAltar) Rebuild();

            return AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemFolder}/{required[0]}.asset") != null;
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

            // 老资产可能残留触发字段，这里显式恢复为普通被动
            item.rarity = ItemRarity.Common;
            item.effectPower = 0f;
            item.effectRadius = 0f;
            item.triggerCount = 1;
            item.synergy = ItemSynergy.None;
            item.shopPrice = 0;

            if (created) AssetDatabase.CreateAsset(item, path);
            else EditorUtility.SetDirty(item);
        }

        /// <summary>自动触发道具：写入效果数值、触发条件与商店价格。</summary>
        private static void CreateTriggered(
            string fileName,
            string displayName,
            string description,
            Color color,
            Sprite icon,
            ItemRarity rarity,
            float effectPower,
            float effectRadius,
            int triggerCount,
            ItemSynergy synergy,
            int shopPrice,
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
            item.rarity = rarity;
            item.modifiers = new List<StatModifier>(modifiers);
            item.coinBonus = 0;
            item.healAmount = 0;

            item.effectPower = effectPower;
            item.effectRadius = effectRadius;
            item.triggerCount = Mathf.Max(1, triggerCount);
            item.synergy = synergy;
            item.shopPrice = Mathf.Max(0, shopPrice);

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
