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
        // 第 4-6 层 Boss：CC0 外部 32x32/32x64 怪物帧
        public const string BossMonsterRoot = "Assets/Art/ThirdParty/BossMonsters/";

        public static Sprite LoadBossMonster(string name) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(BossMonsterRoot + name + ".png");

        public static Sprite[] ForestTrollIdle =>
            new[] { LoadBossMonster("forest_troll_0") };
        public static Sprite[] ForestTrollRun =>
            new[]
            {
                LoadBossMonster("forest_troll_0"),
                LoadBossMonster("forest_troll_1"),
                LoadBossMonster("forest_troll_2"),
                LoadBossMonster("forest_troll_3"),
            };

        public static Sprite[] StoneGolemIdle =>
            new[] { LoadBossMonster("stone_golem_0") };
        public static Sprite[] StoneGolemRun =>
            new[] { LoadBossMonster("stone_golem_0") };

        public static Sprite[] UndeadKingIdle =>
            new[] { LoadBossMonster("undead_king_0") };
        public static Sprite[] UndeadKingRun =>
            new[]
            {
                LoadBossMonster("undead_king_0"),
                LoadBossMonster("undead_king_1"),
                LoadBossMonster("undead_king_2"),
            };

        public static Sprite[] BossIdle => LoadFrames("frames/anim/boss_idle", 4);
        public static Sprite[] BossRun => LoadFrames("frames/anim/boss_run", 4);
        public static Sprite[] OgreIdle => LoadFrames("frames/anim/ogre_idle", 4);
        public static Sprite[] OgreRun => LoadFrames("frames/anim/ogre_run", 4);
        public static Sprite[] BigZombieIdle => LoadFrames("frames/anim/big_zombie_idle", 4);
        public static Sprite[] BigZombieRun => LoadFrames("frames/anim/big_zombie_run", 4);

        // 0x72 角色素材改造的人形怪：遗迹守卫 / 遗迹法师 / 遗迹游侠
        public static Sprite[] KnightIdle => LoadFrames("frames/anim/knight_idle", 4);
        public static Sprite[] KnightRun => LoadFrames("frames/anim/knight_run", 4);
        public static Sprite[] WizardIdle => LoadFrames("frames/anim/wizard_idle", 4);
        public static Sprite[] WizardRun => LoadFrames("frames/anim/wizard_run", 4);
        public static Sprite Knight => Load("frames/anim/knight_idle_0.png");
        public static Sprite Wizard => Load("frames/anim/wizard_idle_0.png");

        public static Sprite[] KnightFemaleIdle => LoadFrames("frames/anim/knight_f_idle", 4);
        public static Sprite[] KnightFemaleRun => LoadFrames("frames/anim/knight_f_run", 4);
        public static Sprite[] WizardFemaleIdle => LoadFrames("frames/anim/wizard_f_idle", 4);
        public static Sprite[] WizardFemaleRun => LoadFrames("frames/anim/wizard_f_run", 4);
        public static Sprite[] ElfIdle => LoadFrames("frames/anim/elf_idle", 4);
        public static Sprite[] ElfRun => LoadFrames("frames/anim/elf_run", 4);
        public static Sprite[] ElfFemaleIdle => LoadFrames("frames/anim/elf_f_idle", 4);
        public static Sprite[] ElfFemaleRun => LoadFrames("frames/anim/elf_f_run", 4);
        public static Sprite KnightFemale => Load("frames/anim/knight_f_idle_0.png");
        public static Sprite WizardFemale => Load("frames/anim/wizard_f_idle_0.png");
        public static Sprite Elf => Load("frames/anim/elf_idle_0.png");
        public static Sprite ElfFemale => Load("frames/anim/elf_f_idle_0.png");

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

        // 0x72 完整包新增怪物：三层各自的小怪池
        public static Sprite[] GoblinIdle => LoadFrames("frames/anim/goblin_idle", 4);
        public static Sprite[] GoblinRun => LoadFrames("frames/anim/goblin_run", 4);
        public static Sprite[] OrcWarriorIdle => LoadFrames("frames/anim/orc_warrior_idle", 4);
        public static Sprite[] OrcWarriorRun => LoadFrames("frames/anim/orc_warrior_run", 4);
        public static Sprite[] OrcShamanIdle => LoadFrames("frames/anim/orc_shaman_idle", 4);
        public static Sprite[] OrcShamanRun => LoadFrames("frames/anim/orc_shaman_run", 4);
        public static Sprite[] ImpIdle => LoadFrames("frames/anim/imp_idle", 4);
        public static Sprite[] ImpRun => LoadFrames("frames/anim/imp_run", 4);
        public static Sprite[] ChortIdle => LoadFrames("frames/anim/chort_idle", 4);
        public static Sprite[] ChortRun => LoadFrames("frames/anim/chort_run", 4);
        public static Sprite[] MaskedOrcIdle => LoadFrames("frames/anim/masked_orc_idle", 4);
        public static Sprite[] MaskedOrcRun => LoadFrames("frames/anim/masked_orc_run", 4);
        public static Sprite[] MuddyIdle => LoadFrames("frames/anim/muddy_idle", 4);
        public static Sprite[] MuddyRun => LoadFrames("frames/anim/muddy_run", 4);
        public static Sprite[] SwampyIdle => LoadFrames("frames/anim/swampy_idle", 4);
        public static Sprite[] SwampyRun => LoadFrames("frames/anim/swampy_run", 4);
        public static Sprite[] WogolIdle => LoadFrames("frames/anim/wogol_idle", 4);
        public static Sprite[] WogolRun => LoadFrames("frames/anim/wogol_run", 4);
        public static Sprite[] ZombieIdle => LoadFrames("frames/anim/zombie_idle", 4);
        public static Sprite[] ZombieRun => LoadFrames("frames/anim/zombie_run", 4);
        public static Sprite[] IceZombieIdle => LoadFrames("frames/anim/ice_zombie_idle", 4);
        public static Sprite[] IceZombieRun => LoadFrames("frames/anim/ice_zombie_run", 4);
        public static Sprite[] SkeletIdle => LoadFrames("frames/anim/skelet_idle", 4);
        public static Sprite[] SkeletRun => LoadFrames("frames/anim/skelet_run", 4);

        public static Sprite Goblin => Load("frames/anim/goblin_idle_0.png");
        public static Sprite OrcWarrior => Load("frames/anim/orc_warrior_idle_0.png");
        public static Sprite OrcShaman => Load("frames/anim/orc_shaman_idle_0.png");
        public static Sprite Imp => Load("frames/anim/imp_idle_0.png");
        public static Sprite Chort => Load("frames/anim/chort_idle_0.png");
        public static Sprite MaskedOrc => Load("frames/anim/masked_orc_idle_0.png");
        public static Sprite Muddy => Load("frames/anim/muddy_idle_0.png");
        public static Sprite Swampy => Load("frames/anim/swampy_idle_0.png");
        public static Sprite Wogol => Load("frames/anim/wogol_idle_0.png");
        public static Sprite Zombie => Load("frames/anim/zombie_idle_0.png");
        public static Sprite IceZombie => Load("frames/anim/ice_zombie_idle_0.png");
        public static Sprite Skelet => Load("frames/anim/skelet_idle_0.png");
        public static Sprite Hazard => Load("frames/hazard.png");

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
        public static Sprite RapidTriggerIcon => Load("frames/icons/icon_rapid_trigger.png");
        public static Sprite PiercingPrismIcon => Load("frames/icons/icon_piercing_prism.png");
        public static Sprite ShieldIcon => Load("frames/icons/icon_shield.png");
        public static Sprite MapShopIcon => Load("frames/icons/icon_map_shop.png");
        public static Sprite MapBossIcon => Load("frames/icons/icon_map_boss.png");
        public static Sprite MapExploredIcon => Load("frames/icons/icon_map_explored.png");
        public static Sprite MapUnexploredIcon => Load("frames/icons/icon_map_unexplored.png");
        public static Sprite VampireFangIcon => Load("frames/icons/icon_vampire_fang.png");
        public static Sprite BentSpoonIcon => Load("frames/icons/icon_bent_spoon.png");
        public static Sprite CorrosiveGlandIcon => Load("frames/icons/icon_corrosive_gland.png");
        public static Sprite CorpseBombIcon => Load("frames/icons/icon_corpse_bomb.png");
        public static Sprite ThornHeartIcon => Load("frames/icons/icon_thorn_heart.png");
        public static Sprite CouponIcon => Load("frames/icons/icon_coupon.png");
        public static Sprite DoubleEyeIcon => Load("frames/icons/icon_double_eye.png");
        public static Sprite CyclopsEyeIcon => Load("frames/icons/icon_cyclops_eye.png");
        public static Sprite GreedEyeIcon => Load("frames/icons/icon_greed_eye.png");
        public static Sprite EmberChaliceIcon => Load("frames/icons/icon_ember_chalice.png");
        public static Sprite HolyVeilIcon => Load("frames/icons/icon_holy_veil.png");
        public static Sprite LuckyFootIcon => Load("frames/icons/icon_lucky_foot.png");
        public static Sprite CritFangIcon => Load("frames/icons/icon_crit_fang.png");
        public static Sprite SplitParasiteIcon => Load("frames/icons/icon_split_parasite.png");
        public static Sprite DivineGlimmerIcon => Load("frames/icons/icon_divine_glimmer.png");
        public static Sprite GhostPrismIcon => Load("frames/icons/icon_ghost_prism.png");
        public static Sprite WindFeatherIcon => Load("frames/icons/icon_wind_feather.png");
        public static Sprite BloodPactIcon => Load("frames/icons/icon_blood_pact.png");
        public static Sprite BounceTearIcon => Load("frames/icons/icon_bounce_tear.png");
        public static Sprite BloodRageIcon => Load("frames/icons/icon_blood_rage.png");
        public static Sprite CollectorHeartIcon => Load("frames/icons/icon_collector_heart.png");
        public static Sprite MetronomeIcon => Load("frames/icons/icon_metronome.png");
        public static Sprite HunterBootsIcon => Load("frames/icons/icon_hunter_boots.png");
        public static Sprite KillEmberIcon => Load("frames/icons/icon_kill_ember.png");
        public static Sprite Floor => Load("dungeon-floor/floor_1.png");
        public static Sprite Wall => Load("dungeon-walls/wall_mid.png");
        public static Sprite Door => Load("dungeon-doors/doors_leaf_closed.png");

        // 完整门板：3×1 横向、1×3 纵向（由 0x72 门风格重绘，铺满整个门洞）
        public static Sprite DoorHorizontal => Load("frames/doors/door_horizontal.png");
        public static Sprite DoorVertical => Load("frames/doors/door_vertical.png");

        // 地板 8 种变体（裂纹 / 碎石 / 苔藓），用于逐格随机平铺
        public static Sprite[] FloorVariants => LoadFloorVariants("dungeon-floor");

        public static Sprite[] SewerFloorVariants => LoadFloorVariants("theme-variants/sewer");
        public static Sprite[] OvergrownFloorVariants => LoadFloorVariants("theme-variants/overgrown");
        public static Sprite[] ObsidianFloorVariants => LoadFloorVariants("theme-variants/obsidian");

        public static Sprite SewerWall => Load("theme-variants/sewer/wall.png");
        public static Sprite OvergrownWall => Load("theme-variants/overgrown/wall.png");
        public static Sprite ObsidianWall => Load("theme-variants/obsidian/wall.png");

        private static Sprite[] LoadFloorVariants(string folder)
        {
            var variants = new Sprite[8];
            for (int i = 0; i < variants.Length; i++)
                variants[i] = Load($"{folder}/floor_{i + 1}.png");
            return variants;
        }

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
