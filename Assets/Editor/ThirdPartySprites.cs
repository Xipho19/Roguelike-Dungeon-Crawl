using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>0x72 地牢包的精灵入口，统一在这里改路径。</summary>
    public static class ThirdPartySprites
    {
        public const string Root = "Assets/Art/ThirdParty/0x72_DungeonTilesetII/";

        public static Sprite Load(string relativePath) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(Root + relativePath);

        /// <summary>加载一段按 _0.._n 命名的帧序列（由 Python 从横条动画裁出）。</summary>
        public static Sprite[] LoadFrames(string prefix, int count)
        {
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
                frames[i] = Load($"{prefix}_{i}.png");
            return frames;
        }

        public static Sprite Player => Load("frames/player.png");

        // 行走 / 待机帧（各 4 帧）
        public static Sprite[] PlayerIdle => LoadFrames("frames/anim/player_idle", 4);
        public static Sprite[] PlayerRun => LoadFrames("frames/anim/player_run", 4);
        public static Sprite[] EnemyChaserIdle => LoadFrames("frames/anim/enemy_chaser_idle", 4);
        public static Sprite[] EnemyChaserRun => LoadFrames("frames/anim/enemy_chaser_run", 4);
        public static Sprite[] EnemyShooterIdle => LoadFrames("frames/anim/enemy_shooter_idle", 4);
        public static Sprite[] EnemyShooterRun => LoadFrames("frames/anim/enemy_shooter_run", 4);
        public static Sprite[] EnemyChargerIdle => LoadFrames("frames/anim/enemy_charger_idle", 4);
        public static Sprite[] EnemyChargerRun => LoadFrames("frames/anim/enemy_charger_run", 4);
        public static Sprite[] BossIdle => LoadFrames("frames/anim/boss_idle", 4);
        public static Sprite[] BossRun => LoadFrames("frames/anim/boss_run", 4);

        // 新敌人：自爆史莱姆 / 亡灵法师 / 分裂僵尸及其召唤物
        public static Sprite[] BomberIdle => LoadFrames("frames/anim/bomber_idle", 4);
        public static Sprite[] BomberRun => LoadFrames("frames/anim/bomber_run", 4);
        public static Sprite[] NecromancerIdle => LoadFrames("frames/anim/necromancer_idle", 4);
        public static Sprite[] NecromancerRun => LoadFrames("frames/anim/necromancer_run", 4);
        public static Sprite[] SkeletonIdle => LoadFrames("frames/anim/skeleton_idle", 4);
        public static Sprite[] SkeletonRun => LoadFrames("frames/anim/skeleton_run", 4);
        public static Sprite[] SplitterIdle => LoadFrames("frames/anim/splitter_idle", 4);
        public static Sprite[] SplitterRun => LoadFrames("frames/anim/splitter_run", 4);
        public static Sprite[] TinyZombieIdle => LoadFrames("frames/anim/tiny_zombie_idle", 4);
        public static Sprite[] TinyZombieRun => LoadFrames("frames/anim/tiny_zombie_run", 4);
        public static Sprite Bomber => Load("frames/anim/bomber_idle_0.png");
        public static Sprite Necromancer => Load("frames/anim/necromancer_idle_0.png");
        public static Sprite Skeleton => Load("frames/anim/skeleton_idle_0.png");
        public static Sprite Splitter => Load("frames/anim/splitter_idle_0.png");
        public static Sprite TinyZombie => Load("frames/anim/tiny_zombie_idle_0.png");
        public static Sprite Explosion => Load("frames/explosion.png");

        public static Sprite EnemyChaser => Load("frames/enemy_chaser.png");
        public static Sprite EnemyShooter => Load("frames/enemy_shooter.png");
        public static Sprite EnemyCharger => Load("frames/enemy_charger.png");
        public static Sprite Boss => Load("frames/boss.png");
        public static Sprite Coin => Load("frames/coin.png");
        public static Sprite Heart => Load("ui/ui_heart_full.png");
        public static Sprite Potion => Load("potions/flask_big_blue.png");
        public static Sprite PotionIcon(string name) => Load($"potions/{name}.png");
        public static Sprite Weapon(string name) => Load($"weapons/{name}.png");
        public static Sprite LuckyCoinIcon => Load("frames/coin_lucky.png");
        public static Sprite Tear => Load("frames/tear_blue.png");
        public static Sprite EnemyOrb => Load("frames/orb_magenta.png");
        public static Sprite Portal => Load("frames/portal.png");
        public static Sprite FloorLadder => Load("dungeon-floor/floor_ladder.png");
        public static Sprite HudCoin => Load("frames/hud_coin.png");
        public static Sprite BackpackIcon => Load("frames/backpack.png");
        public static Sprite Altar => Load("frames/altar.png");

        // 自制 16×16 道具图标（原创，保持 0x72 风格）
        public static Sprite BootsIcon => Load("frames/icons/icon_boots.png");
        public static Sprite CoinBagIcon => Load("frames/icons/icon_coinbag.png");
        public static Sprite AppleIcon => Load("frames/icons/icon_apple.png");
        public static Sprite SlingshotIcon => Load("frames/icons/icon_slingshot.png");
        public static Sprite GlassCannonIcon => Load("frames/icons/icon_glasscannon.png");
        public static Sprite TridentIcon => Load("frames/icons/icon_trident.png");
        public static Sprite Floor => Load("dungeon-floor/floor_1.png");
        public static Sprite Wall => Load("dungeon-walls/wall_mid.png");
        public static Sprite Door => Load("dungeon-doors/doors_leaf_closed.png");

        // 完整门板：3×1 横向、1×3 纵向（由 0x72 门风格重绘，铺满整个门洞）
        public static Sprite DoorHorizontal => Load("frames/doors/door_horizontal.png");
        public static Sprite DoorVertical => Load("frames/doors/door_vertical.png");

        // 地板 8 种变体（裂纹 / 碎石 / 苔藓），用于逐格随机平铺
        public static Sprite[] FloorVariants => new[]
        {
            Load("dungeon-floor/floor_1.png"),
            Load("dungeon-floor/floor_2.png"),
            Load("dungeon-floor/floor_3.png"),
            Load("dungeon-floor/floor_4.png"),
            Load("dungeon-floor/floor_5.png"),
            Load("dungeon-floor/floor_6.png"),
            Load("dungeon-floor/floor_7.png"),
            Load("dungeon-floor/floor_8.png"),
        };

        // 房间装饰
        public static Sprite Crate => Load("dungeon-decor/crate.png");
        public static Sprite Skull => Load("dungeon-decor/skull.png");
        public static Sprite ColumnTop => Load("dungeon-walls/column_top.png");
        public static Sprite ColumnMid => Load("dungeon-walls/column_mid.png");
        public static Sprite ColumnBase => Load("dungeon-walls/coulmn_base.png");
        public static Sprite WallHole => Load("dungeon-walls/wall_hole_1.png");
        public static Sprite WallGoo => Load("dungeon-walls/wall_goo.png");
        public static Sprite Chest => Load("frames/chest.png");
        public static Sprite ChestOpen => Load("frames/chest_open.png");

        // 蓝 / 绿 / 红 / 黄
        public static Sprite[] Banners => new[]
        {
            Load("dungeon-walls/wall_banner_blue.png"),
            Load("dungeon-walls/wall_banner_green.png"),
            Load("dungeon-walls/wall_banner_red.png"),
            Load("dungeon-walls/wall_banner_yellow.png"),
        };
    }
}
