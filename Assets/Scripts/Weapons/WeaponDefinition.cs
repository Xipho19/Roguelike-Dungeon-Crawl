using UnityEngine;

namespace Roguelike.Weapons
{
    public enum WeaponKind
    {
        Tears = 0,
        Staff = 1,
        Rifle = 2,
        Katana = 3,
        Shotgun = 4,
        Crossbow = 5,
        EmberStaff = 6,
        Waraxe = 7,
    }

    public enum WeaponRarity
    {
        Common = 0,
        Rare = 1,
        Legendary = 2,
    }

    /// <summary>
    /// 一件武器的静态配置。
    ///
    /// 武器只描述自己的基础手感（伤害、间隔、弹速、弹匣、近战范围等），
    /// 道具的加伤/射速/多发/穿透仍然由 PlayerStats 统一结算，
    /// 因此新增武器不需要再复制一套道具逻辑。
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike/Weapon", fileName = "Weapon")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [Header("身份")]
        public string weaponName = "未命名武器";

        [TextArea(2, 4)]
        public string description;

        public WeaponKind kind = WeaponKind.Tears;
        public Sprite icon;

        [Header("获取与商店")]
        public WeaponRarity rarity = WeaponRarity.Common;

        [Tooltip("掉落权重；0 表示不会出现在商店/宝箱/Boss 奖励池")]
        [Min(0)] public int dropWeight = 10;

        [Tooltip("商店基础价格；0 时按稀有度自动计算")]
        [Min(0)] public int shopPrice;

        [Header("远程基础数值")]
        [Min(0f)] public float damage = 1f;
        [Min(0.02f)] public float fireInterval = 0.24f;
        [Min(0f)] public float projectileSpeed = 14f;
        [Min(0.05f)] public float projectileLifetime = 1f;
        [Min(0)] public int pierce;
        [Min(1)] public int projectiles = 1;
        [Min(0f)] public float spreadStep = 8f;

        [Header("弹丸表现")]
        public Sprite[] projectileFrames;
        [Min(0.05f)] public float projectileScale = 0.36f;
        public Color projectileColor = Color.white;

        [Tooltip("子弹是否随飞行方向旋转（步枪子弹）")]
        public bool rotateProjectile;

        [Header("弹丸附带效果")]
        [Tooltip("武器自带的命中爆炸半径；0 表示没有爆炸")]
        [Min(0f)] public float projectileExplosionRadius;

        [Tooltip("武器自带的爆炸伤害")]
        [Min(0f)] public float projectileExplosionDamage;

        [Header("弹匣")]
        [Tooltip("0 表示不消耗弹药，可以无限射击")]
        [Min(0)] public int magazineSize;

        [Min(0f)] public float reloadTime = 1.2f;

        [Header("近战")]
        [Min(0.2f)] public float meleeRadius = 2.15f;

        [Tooltip("挥砍扇形的总角度")]
        [Range(20f, 360f)] public float meleeArc = 110f;

        [Min(0f)] public float meleeKnockback = 7f;

        [Min(1)] public int comboHits = 3;

        [Min(0f)] public float comboWindow = 0.9f;

        [Min(1f)] public float comboFinisherScale = 1.55f;

        public Sprite[] slashFrames;
        [Min(0.1f)] public float slashScale = 1.3f;

        [Header("手持表现")]
        public Sprite heldSprite;
        [Min(0.05f)] public float heldScale = 0.65f;

        [Tooltip("手持 Sprite 的握把到玩家身体中心的距离")]
        [Min(0f)] public float heldDistance = 0.15f;

        [Tooltip("相对瞄准方向的侧向偏移，让武器握在身体侧面")]
        public float heldSideOffset = 0.12f;

        [Tooltip("玩家本地 Y 偏移；负值把武器从头顶下移到胸腹位置")]
        public float heldVerticalOffset = -0.18f;

        [Tooltip("手持 Sprite 的竖直方向与瞄准方向的夹角；竖握武器通常为 -90")]
        public float heldAngleOffset = -90f;

        [Tooltip("瞄准左侧时上下翻转手持 Sprite，避免枪械倒置")]
        public bool flipHeldWhenAimingLeft;

        [Header("音效")]
        [Tooltip("对应 Resources/Audio 下的音效名")]
        public string fireSound = "tear_shot";
        [Range(0f, 1f)] public float fireVolume = 0.55f;

        [Tooltip("近战连段的交替挥砍音效；留空时使用 fireSound")]
        public string altSound;
        [Range(0f, 1f)] public float altVolume = 0.55f;

        [Tooltip("近战终结技音效；留空时使用 fireSound")]
        public string finisherSound;
        [Range(0f, 1f)] public float finisherVolume = 0.7f;

        public bool IsMelee => kind == WeaponKind.Katana || kind == WeaponKind.Waraxe;
        public bool UsesMagazine => magazineSize > 0;

        public Sprite FirstProjectileFrame =>
            projectileFrames != null && projectileFrames.Length > 0
                ? projectileFrames[0]
                : icon;
    }
}
