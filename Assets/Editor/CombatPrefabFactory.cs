using Roguelike.Combat;
using Roguelike.Core;
using Roguelike.Visual;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 生成战斗用的临时 Prefab（敌人 / 眼泪）。
    /// 菜单：Tools / Roguelike / 生成战斗 Prefab
    ///
    /// 与房间 Prefab 一样，目前用纯色 Sprite 表示，方便先跑通战斗流程。
    /// </summary>
    public static class CombatPrefabFactory
    {
        public const string Folder = "Assets/Resources/Combat";
        public const string EnemyPath = Folder + "/Enemy.prefab";
        public const string TearPath = Folder + "/Tear.prefab";
        public const string CoinPath = Folder + "/Coin.prefab";
        public const string HeartPath = Folder + "/Heart.prefab";
        public const string ShooterPath = Folder + "/Shooter.prefab";
        public const string ChargerPath = Folder + "/Charger.prefab";
        public const string BomberPath = Folder + "/Bomber.prefab";
        public const string NecromancerPath = Folder + "/Necromancer.prefab";
        public const string SplitterPath = Folder + "/Splitter.prefab";
        public const string SkeletonPath = Folder + "/Skeleton.prefab";
        public const string TinyZombiePath = Folder + "/TinyZombie.prefab";
        public const string BossPath = Folder + "/Boss.prefab";
        public const string EnemyProjectilePath = Folder + "/EnemyProjectile.prefab";
        public const string PortalPath = Folder + "/NextFloorPortal.prefab";

        // 三层专属小怪
        public const string GoblinPath = Folder + "/Goblin.prefab";
        public const string OrcWarriorPath = Folder + "/OrcWarrior.prefab";
        public const string MuddyPath = Folder + "/Muddy.prefab";
        public const string SkeletPath = Folder + "/Skelet.prefab";
        public const string ZombiePath = Folder + "/Zombie.prefab";
        public const string IceZombiePath = Folder + "/IceZombie.prefab";
        public const string WogolPath = Folder + "/Wogol.prefab";
        public const string ImpPath = Folder + "/Imp.prefab";
        public const string ChortPath = Folder + "/Chort.prefab";
        public const string MaskedOrcPath = Folder + "/MaskedOrc.prefab";
        public const string OrcShamanPath = Folder + "/OrcShaman.prefab";
        public const string SwampyPath = Folder + "/Swampy.prefab";
        public const string KnightPath = Folder + "/Knight.prefab";
        public const string WizardPath = Folder + "/Wizard.prefab";
        public const string KnightFemalePath = Folder + "/KnightFemale.prefab";
        public const string WizardFemalePath = Folder + "/WizardFemale.prefab";
        public const string ElfPath = Folder + "/Elf.prefab";
        public const string ElfFemalePath = Folder + "/ElfFemale.prefab";
        public const string SkeletonWarriorPath = Folder + "/SkeletonWarrior.prefab";
        public const string TinyZombieRaiderPath = Folder + "/TinyZombieRaider.prefab";
        public const string TinyZombieCloudPath = Folder + "/TinyZombieCloud.prefab";

        // Danaida 16×16 风格测试怪物
        public const string DanaidaRedImpPath = Folder + "/DanaidaRedImp.prefab";
        public const string DanaidaGreenSlimePath = Folder + "/DanaidaGreenSlime.prefab";
        public const string DanaidaPurpleGhostPath = Folder + "/DanaidaPurpleGhost.prefab";
        public const string DanaidaTealSpiderPath = Folder + "/DanaidaTealSpider.prefab";
        public const string DanaidaBrownCyclopsPath = Folder + "/DanaidaBrownCyclops.prefab";
        public const string DanaidaPurpleDemonPath = Folder + "/DanaidaPurpleDemon.prefab";

        // 16x16 Fantasy RPG Trash Mobs 测试怪物
        public const string TrashGreenSlimePath = Folder + "/TrashGreenSlime.prefab";
        public const string TrashSnakePath = Folder + "/TrashSnake.prefab";
        public const string TrashBlueBatPath = Folder + "/TrashBlueBat.prefab";
        public const string TrashMushroomPath = Folder + "/TrashMushroom.prefab";
        public const string TrashBeePath = Folder + "/TrashBee.prefab";
        public const string TrashFlowerPath = Folder + "/TrashFlower.prefab";
        public const string FireTotemPath = Folder + "/FireTotem.prefab";
        public const string EnemyHazardPath = Folder + "/EnemyHazard.prefab";

        private const string SpritePath = "Assets/Art/White.png";

        [MenuItem("Tools/Roguelike/生成战斗 Prefab")]
        public static void RebuildFromMenu() => Rebuild();

        public static void Rebuild()
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            if (sprite == null)
            {
                RoomPrefabFactory.Rebuild();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
            }

            if (sprite == null)
            {
                Debug.LogError("[Combat] 无法获得白色 Sprite，战斗 Prefab 未生成。");
                return;
            }

            EnsureFolder(Folder);

            EnemyHazard hazard = BuildEnemyHazard(sprite);
            Enemy fireTotem = BuildFireTotem(sprite);

            BuildEnemy(sprite);
            BuildShooter(sprite);
            BuildCharger(sprite);
            Enemy skeleton = BuildSkeleton(sprite);
            Enemy tinyZombie = BuildTinyZombie(sprite);
            BuildBomber(sprite);
            BuildNecromancer(sprite, skeleton);
            BuildSplitter(sprite, tinyZombie);
            BuildBoss(sprite, skeleton);

            BuildGoblin(sprite);
            BuildOrcWarrior(sprite);
            BuildMuddy(sprite, hazard);
            BuildSkelet(sprite);
            BuildZombie(sprite);
            BuildIceZombie(sprite, hazard);
            BuildWogol(sprite);
            BuildImp(sprite);
            BuildChort(sprite);
            BuildMaskedOrc(sprite);
            BuildOrcShaman(sprite, fireTotem);
            BuildSwampy(sprite, hazard);
            BuildKnight(sprite);
            BuildWizard(sprite);
            BuildKnightFemale(sprite);
            BuildWizardFemale(sprite);
            BuildElf(sprite);
            BuildElfFemale(sprite);
            BuildSkeletonWarrior(sprite);
            BuildTinyZombieRaider(sprite);
            BuildDanaidaMonsters(sprite);
            BuildTrashMobs(sprite, hazard);
            BuildTinyZombieCloud(sprite, hazard);

            BuildTear(sprite);
            BuildEnemyProjectile(sprite);
            BuildCoin(sprite);
            BuildHeart(sprite);
            BuildPortal(sprite);
            AssetDatabase.SaveAssets();

            Debug.Log("[Combat] 战斗 Prefab 已生成：基础六种 + 六层小怪 + Boss / Tear / EnemyProjectile / Coin / Heart / NextFloorPortal");
        }

        private static Sprite ArtOr(Sprite art, Sprite fallback) => art != null ? art : fallback;

        private static void BuildEnemy(Sprite sprite)
        {
            var root = new GameObject("Enemy");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.EnemyChaser, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.45f;

            root.AddComponent<LungeEnemy>();
            AttachAnimator(root, ThirdPartySprites.EnemyChaserIdle, ThirdPartySprites.EnemyChaserRun);

            PrefabUtility.SaveAsPrefabAsset(root, EnemyPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildShooter(Sprite sprite)
        {
            var root = new GameObject("Shooter");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.EnemyShooter, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;

            root.AddComponent<ShooterEnemy>();
            AttachAnimator(root, ThirdPartySprites.EnemyShooterIdle, ThirdPartySprites.EnemyShooterRun);

            PrefabUtility.SaveAsPrefabAsset(root, ShooterPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildCharger(Sprite sprite)
        {
            var root = new GameObject("Charger");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.EnemyCharger, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.46f;

            root.AddComponent<ChargerEnemy>();
            AttachAnimator(root, ThirdPartySprites.EnemyChargerIdle, ThirdPartySprites.EnemyChargerRun);

            PrefabUtility.SaveAsPrefabAsset(root, ChargerPath);
            Object.DestroyImmediate(root);
        }

        private static Enemy BuildSkeleton(Sprite sprite)
        {
            var root = new GameObject("Skeleton");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.Skeleton, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.32f;

            root.AddComponent<Enemy>();
            AttachAnimator(root, ThirdPartySprites.SkeletonIdle, ThirdPartySprites.SkeletonRun);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, SkeletonPath);
            Object.DestroyImmediate(root);
            return saved != null ? saved.GetComponent<Enemy>() : null;
        }

        private static Enemy BuildTinyZombie(Sprite sprite)
        {
            var root = new GameObject("TinyZombie");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.TinyZombie, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.28f;

            root.AddComponent<Enemy>();
            AttachAnimator(root, ThirdPartySprites.TinyZombieIdle, ThirdPartySprites.TinyZombieRun);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, TinyZombiePath);
            Object.DestroyImmediate(root);
            return saved != null ? saved.GetComponent<Enemy>() : null;
        }

        private static void BuildBomber(Sprite sprite)
        {
            var root = new GameObject("Bomber");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.Bomber, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;

            var bomber = root.AddComponent<BomberEnemy>();
            var serialized = new SerializedObject(bomber);
            serialized.FindProperty("explosionSprite").objectReferenceValue = ThirdPartySprites.Explosion;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AttachAnimator(root, ThirdPartySprites.BomberIdle, ThirdPartySprites.BomberRun);

            PrefabUtility.SaveAsPrefabAsset(root, BomberPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildNecromancer(Sprite sprite, Enemy minionPrefab)
        {
            var root = new GameObject("Necromancer");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.Necromancer, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;

            var necromancer = root.AddComponent<NecromancerEnemy>();
            if (minionPrefab != null)
            {
                var serialized = new SerializedObject(necromancer);
                serialized.FindProperty("minionPrefab").objectReferenceValue = minionPrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            AttachAnimator(root, ThirdPartySprites.NecromancerIdle, ThirdPartySprites.NecromancerRun);

            PrefabUtility.SaveAsPrefabAsset(root, NecromancerPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildSplitter(Sprite sprite, Enemy minionPrefab)
        {
            var root = new GameObject("Splitter");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.Splitter, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.42f;

            var splitter = root.AddComponent<SplitterEnemy>();
            if (minionPrefab != null)
            {
                var serialized = new SerializedObject(splitter);
                serialized.FindProperty("minionPrefab").objectReferenceValue = minionPrefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            AttachAnimator(root, ThirdPartySprites.SplitterIdle, ThirdPartySprites.SplitterRun);

            PrefabUtility.SaveAsPrefabAsset(root, SplitterPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildBoss(Sprite sprite, Enemy minionPrefab)
        {
            var root = new GameObject("Boss");

            // 16×16 的 4-6 层 Boss 需要放大到和 32×32 Boss 相近的体型；
            // 把渲染器放在子对象上，避免缩放根对象时连带放大碰撞体。
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.Boss, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.mass = 20f;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.9f;

            var boss = root.AddComponent<BossEnemy>();
            var bossSerialized = new SerializedObject(boss);
            if (minionPrefab != null)
                bossSerialized.FindProperty("minionPrefab").objectReferenceValue = minionPrefab;
            bossSerialized.FindProperty("slamSprite").objectReferenceValue = ThirdPartySprites.Explosion;
            SetSpriteArray(bossSerialized, "ogreIdleFrames", ThirdPartySprites.OgreIdle);
            SetSpriteArray(bossSerialized, "ogreRunFrames", ThirdPartySprites.OgreRun);
            SetSpriteArray(bossSerialized, "bigZombieIdleFrames", ThirdPartySprites.BigZombieIdle);
            SetSpriteArray(bossSerialized, "bigZombieRunFrames", ThirdPartySprites.BigZombieRun);
            SetSpriteArray(bossSerialized, "bigDemonIdleFrames", ThirdPartySprites.BossIdle);
            SetSpriteArray(bossSerialized, "bigDemonRunFrames", ThirdPartySprites.BossRun);
            SetSpriteArray(bossSerialized, "sewerTrollIdleFrames", ThirdPartySprites.ForestTrollIdle);
            SetSpriteArray(bossSerialized, "sewerTrollRunFrames", ThirdPartySprites.ForestTrollRun);
            SetSpriteArray(bossSerialized, "ruinsGolemIdleFrames", ThirdPartySprites.StoneGolemIdle);
            SetSpriteArray(bossSerialized, "ruinsGolemRunFrames", ThirdPartySprites.StoneGolemRun);
            SetSpriteArray(bossSerialized, "obsidianKingIdleFrames", ThirdPartySprites.UndeadKingIdle);
            SetSpriteArray(bossSerialized, "obsidianKingRunFrames", ThirdPartySprites.UndeadKingRun);
            bossSerialized.ApplyModifiedPropertiesWithoutUndo();

            AttachAnimator(root, ThirdPartySprites.BossIdle, ThirdPartySprites.BossRun);

            PrefabUtility.SaveAsPrefabAsset(root, BossPath);
            Object.DestroyImmediate(root);
        }

        public static bool RosterPrefabsExist()
        {
            string[] paths =
            {
                GoblinPath, OrcWarriorPath, MuddyPath, SkeletPath, ZombiePath,
                IceZombiePath, WogolPath, ImpPath, ChortPath, MaskedOrcPath,
                OrcShamanPath, FireTotemPath, SwampyPath, KnightPath, WizardPath,
                KnightFemalePath, WizardFemalePath, ElfPath, ElfFemalePath,
                SkeletonWarriorPath, TinyZombieRaiderPath, TinyZombieCloudPath,
                DanaidaRedImpPath, DanaidaGreenSlimePath, DanaidaPurpleGhostPath,
                DanaidaTealSpiderPath, DanaidaBrownCyclopsPath, DanaidaPurpleDemonPath,
                TrashGreenSlimePath, TrashSnakePath, TrashBlueBatPath,
                TrashMushroomPath, TrashBeePath, TrashFlowerPath,
            };

            for (int i = 0; i < paths.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<Enemy>(paths[i]) == null)
                    return false;
            }

            return AssetDatabase.LoadAssetAtPath<EnemyHazard>(EnemyHazardPath) != null;
        }

        // ---------- 三层专属小怪 ----------

        private static EnemyHazard BuildEnemyHazard(Sprite fallback)
        {
            var root = new GameObject("EnemyHazard");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.Hazard, fallback);
            renderer.color = Color.white;
            renderer.sortingOrder = 1;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            root.AddComponent<EnemyHazard>();
            return SaveMonster<EnemyHazard>(root, EnemyHazardPath);
        }

        private static Enemy BuildFireTotem(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "FireTotem",
                ThirdPartySprites.Altar,
                fallback,
                null,
                null,
                0.55f,
                0.28f);

            var totem = root.AddComponent<ShooterEnemy>();
            var serialized = ConfigureMonster(totem, 1f, 1f, 0);
            SetBool(serialized, "stationary", true);
            SetFloat(serialized, "preferredDistance", 0f);
            SetFloat(serialized, "shootInterval", 1.5f);
            SetFloat(serialized, "windupTime", 0.3f);
            SetFloat(serialized, "projectileSpeed", 4.5f);
            SetFloat(serialized, "projectileLifetime", 4f);
            SetInt(serialized, "projectileCount", 1);
            SetFloat(serialized, "spreadAngle", 0f);
            SetColor(serialized, "projectileTint", new Color(1f, 0.55f, 0.2f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, FireTotemPath);
        }

        private static Enemy BuildGoblin(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "Goblin",
                ThirdPartySprites.Goblin,
                fallback,
                ThirdPartySprites.GoblinIdle,
                ThirdPartySprites.GoblinRun,
                0.9f,
                0.34f);

            var enemy = root.AddComponent<LungeEnemy>();
            var serialized = ConfigureMonster(enemy, 0.9f, 1.25f, 1);
            SetFloat(serialized, "triggerRange", 2.8f);
            SetFloat(serialized, "windupTime", 0.3f);
            SetFloat(serialized, "lungeSpeed", 8f);
            SetFloat(serialized, "lungeDuration", 0.22f);
            SetFloat(serialized, "recoverTime", 0.55f);
            SetFloat(serialized, "lungeCooldown", 2.6f);
            SetColor(serialized, "windupTint", new Color(1f, 0.62f, 0.18f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, GoblinPath);
        }

        private static Enemy BuildOrcWarrior(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "OrcWarrior",
                ThirdPartySprites.OrcWarrior,
                fallback,
                ThirdPartySprites.OrcWarriorIdle,
                ThirdPartySprites.OrcWarriorRun,
                1.05f,
                0.42f);

            var enemy = root.AddComponent<CleaveEnemy>();
            var serialized = ConfigureMonster(enemy, 1.5f, 0.85f, 1);
            SetFloat(serialized, "triggerRange", 1.6f);
            SetFloat(serialized, "windupTime", 0.6f);
            SetInt(serialized, "damage", 2);
            SetFloat(serialized, "radius", 1.5f);
            SetFloat(serialized, "halfAngle", 65f);
            SetFloat(serialized, "recoverTime", 0.7f);
            SetFloat(serialized, "attackCooldown", 2.4f);
            SetBool(serialized, "secondStrike", false);
            SetColor(serialized, "windupTint", new Color(1f, 0.55f, 0.2f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, OrcWarriorPath);
        }

        private static Enemy BuildMuddy(Sprite fallback, EnemyHazard hazard)
        {
            GameObject root = CreateMonsterRoot(
                "Muddy",
                ThirdPartySprites.Muddy,
                fallback,
                ThirdPartySprites.MuddyIdle,
                ThirdPartySprites.MuddyRun,
                1.05f,
                0.42f);

            var enemy = root.AddComponent<TrapperEnemy>();
            var serialized = ConfigureMonster(enemy, 1.6f, 0.65f, 1);
            SetObject(serialized, "hazardPrefab", hazard);
            SetFloat(serialized, "triggerRange", 3f);
            SetFloat(serialized, "dropInterval", 3.2f);
            SetFloat(serialized, "dropWindup", 0.5f);
            SetInt(serialized, "hazardDamage", 1);
            SetFloat(serialized, "hazardTickInterval", 0.75f);
            SetFloat(serialized, "hazardLifetime", 2.5f);
            SetFloat(serialized, "hazardRadius", 1f);
            SetFloat(serialized, "slowFactor", 0.7f);
            SetFloat(serialized, "slowDuration", 0.5f);
            SetColor(serialized, "hazardTint", new Color(0.45f, 0.3f, 0.18f, 0.6f));
            SetColor(serialized, "windupTint", new Color(0.7f, 0.52f, 0.25f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, MuddyPath);
        }

        private static Enemy BuildSkelet(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "Skelet",
                ThirdPartySprites.Skelet,
                fallback,
                ThirdPartySprites.SkeletIdle,
                ThirdPartySprites.SkeletRun,
                0.95f,
                0.36f);

            var enemy = root.AddComponent<CleaveEnemy>();
            var serialized = ConfigureMonster(enemy, 1f, 1f, 1);
            SetFloat(serialized, "triggerRange", 1.6f);
            SetFloat(serialized, "windupTime", 0.4f);
            SetInt(serialized, "damage", 1);
            SetFloat(serialized, "radius", 1.25f);
            SetFloat(serialized, "halfAngle", 70f);
            SetFloat(serialized, "recoverTime", 0.45f);
            SetFloat(serialized, "attackCooldown", 1.8f);
            SetBool(serialized, "secondStrike", true);
            SetFloat(serialized, "secondDelay", 0.22f);
            SetColor(serialized, "windupTint", new Color(0.9f, 0.95f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, SkeletPath);
        }

        private static Enemy BuildZombie(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "Zombie",
                ThirdPartySprites.Zombie,
                fallback,
                ThirdPartySprites.ZombieIdle,
                ThirdPartySprites.ZombieRun,
                1.05f,
                0.42f);

            var enemy = root.AddComponent<CleaveEnemy>();
            var serialized = ConfigureMonster(enemy, 2f, 0.7f, 1);
            SetFloat(serialized, "triggerRange", 1.5f);
            SetFloat(serialized, "windupTime", 0.7f);
            SetInt(serialized, "damage", 2);
            SetFloat(serialized, "radius", 1.3f);
            SetFloat(serialized, "halfAngle", 55f);
            SetFloat(serialized, "recoverTime", 0.8f);
            SetFloat(serialized, "attackCooldown", 2.6f);
            SetBool(serialized, "secondStrike", false);
            SetColor(serialized, "windupTint", new Color(0.55f, 1f, 0.55f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, ZombiePath);
        }

        private static Enemy BuildIceZombie(Sprite fallback, EnemyHazard hazard)
        {
            GameObject root = CreateMonsterRoot(
                "IceZombie",
                ThirdPartySprites.IceZombie,
                fallback,
                ThirdPartySprites.IceZombieIdle,
                ThirdPartySprites.IceZombieRun,
                1f,
                0.4f);

            var enemy = root.AddComponent<TrapperEnemy>();
            var serialized = ConfigureMonster(enemy, 1.5f, 0.75f, 1);
            SetObject(serialized, "hazardPrefab", hazard);
            SetFloat(serialized, "triggerRange", 2.8f);
            SetFloat(serialized, "dropInterval", 3.5f);
            SetFloat(serialized, "dropWindup", 0.6f);
            SetInt(serialized, "hazardDamage", 1);
            SetFloat(serialized, "hazardTickInterval", 0.8f);
            SetFloat(serialized, "hazardLifetime", 2.2f);
            SetFloat(serialized, "hazardRadius", 1.2f);
            SetFloat(serialized, "slowFactor", 0.55f);
            SetFloat(serialized, "slowDuration", 0.7f);
            SetColor(serialized, "hazardTint", new Color(0.45f, 0.85f, 1f, 0.6f));
            SetColor(serialized, "windupTint", new Color(0.55f, 0.85f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, IceZombiePath);
        }

        private static Enemy BuildWogol(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "Wogol",
                ThirdPartySprites.Wogol,
                fallback,
                ThirdPartySprites.WogolIdle,
                ThirdPartySprites.WogolRun,
                1f,
                0.4f);

            var enemy = root.AddComponent<BlinkShooterEnemy>();
            var serialized = ConfigureMonster(enemy, 1.2f, 1f, 1);
            SetFloat(serialized, "preferredDistance", 4f);
            SetFloat(serialized, "blinkInterval", 4f);
            SetFloat(serialized, "blinkWindup", 0.35f);
            SetFloat(serialized, "blinkDistance", 3.2f);
            SetInt(serialized, "projectileCount", 3);
            SetFloat(serialized, "spreadAngle", 30f);
            SetFloat(serialized, "projectileSpeed", 5f);
            SetFloat(serialized, "projectileLifetime", 4f);
            SetFloat(serialized, "shootWindup", 0.35f);
            SetFloat(serialized, "recoverTime", 0.5f);
            SetColor(serialized, "projectileTint", new Color(0.85f, 0.6f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, WogolPath);
        }

        private static Enemy BuildImp(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "Imp",
                ThirdPartySprites.Imp,
                fallback,
                ThirdPartySprites.ImpIdle,
                ThirdPartySprites.ImpRun,
                0.9f,
                0.34f);

            var enemy = root.AddComponent<ChargerEnemy>();
            var serialized = ConfigureMonster(enemy, 0.9f, 1.2f, 1);
            SetFloat(serialized, "triggerRange", 6f);
            SetFloat(serialized, "windupTime", 0.4f);
            SetFloat(serialized, "chargeSpeed", 11f);
            SetFloat(serialized, "chargeDuration", 0.35f);
            SetFloat(serialized, "recoverTime", 0.5f);
            SetInt(serialized, "chargeCount", 1);
            SetFloat(serialized, "chargePause", 0.2f);
            SetColor(serialized, "windupTint", new Color(1f, 0.4f, 0.15f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, ImpPath);
        }

        private static Enemy BuildChort(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "Chort",
                ThirdPartySprites.Chort,
                fallback,
                ThirdPartySprites.ChortIdle,
                ThirdPartySprites.ChortRun,
                0.95f,
                0.38f);

            var enemy = root.AddComponent<ShooterEnemy>();
            var serialized = ConfigureMonster(enemy, 1f, 1f, 1);
            SetFloat(serialized, "preferredDistance", 4f);
            SetFloat(serialized, "shootInterval", 2.2f);
            SetFloat(serialized, "windupTime", 0.45f);
            SetFloat(serialized, "projectileSpeed", 5.5f);
            SetFloat(serialized, "projectileLifetime", 4f);
            SetInt(serialized, "projectileCount", 3);
            SetFloat(serialized, "spreadAngle", 30f);
            SetColor(serialized, "projectileTint", new Color(1f, 0.45f, 0.15f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, ChortPath);
        }

        private static Enemy BuildMaskedOrc(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "MaskedOrc",
                ThirdPartySprites.MaskedOrc,
                fallback,
                ThirdPartySprites.MaskedOrcIdle,
                ThirdPartySprites.MaskedOrcRun,
                1.05f,
                0.42f);

            var enemy = root.AddComponent<ChargerEnemy>();
            var serialized = ConfigureMonster(enemy, 1.8f, 0.95f, 2);
            SetFloat(serialized, "triggerRange", 6f);
            SetFloat(serialized, "windupTime", 0.5f);
            SetFloat(serialized, "chargeSpeed", 11f);
            SetFloat(serialized, "chargeDuration", 0.4f);
            SetFloat(serialized, "recoverTime", 0.8f);
            SetInt(serialized, "chargeCount", 2);
            SetFloat(serialized, "chargePause", 0.22f);
            SetColor(serialized, "windupTint", new Color(1f, 0.55f, 0.18f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, MaskedOrcPath);
        }

        private static Enemy BuildOrcShaman(Sprite fallback, Enemy totemPrefab)
        {
            GameObject root = CreateMonsterRoot(
                "OrcShaman",
                ThirdPartySprites.OrcShaman,
                fallback,
                ThirdPartySprites.OrcShamanIdle,
                ThirdPartySprites.OrcShamanRun,
                0.95f,
                0.38f);

            var enemy = root.AddComponent<NecromancerEnemy>();
            var serialized = ConfigureMonster(enemy, 1.2f, 0.9f, 1);
            SetObject(serialized, "minionPrefab", totemPrefab);
            SetFloat(serialized, "preferredDistance", 4.5f);
            SetFloat(serialized, "summonInterval", 6f);
            SetFloat(serialized, "summonWindup", 0.7f);
            SetInt(serialized, "maxMinions", 1);
            SetFloat(serialized, "minionHealth", 3f);
            SetFloat(serialized, "minionSpeed", 0f);
            SetFloat(serialized, "minionScale", 0.55f);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, OrcShamanPath);
        }

        private static Enemy BuildSwampy(Sprite fallback, EnemyHazard hazard)
        {
            GameObject root = CreateMonsterRoot(
                "Swampy",
                ThirdPartySprites.Swampy,
                fallback,
                ThirdPartySprites.SwampyIdle,
                ThirdPartySprites.SwampyRun,
                1f,
                0.4f);

            var enemy = root.AddComponent<TrapperEnemy>();
            var serialized = ConfigureMonster(enemy, 1.5f, 0.7f, 1);
            SetObject(serialized, "hazardPrefab", hazard);
            SetFloat(serialized, "triggerRange", 3f);
            SetFloat(serialized, "dropInterval", 3f);
            SetFloat(serialized, "dropWindup", 0.45f);
            SetInt(serialized, "hazardDamage", 1);
            SetFloat(serialized, "hazardTickInterval", 0.9f);
            SetFloat(serialized, "hazardLifetime", 2.8f);
            SetFloat(serialized, "hazardRadius", 1.1f);
            SetFloat(serialized, "slowFactor", 0.8f);
            SetFloat(serialized, "slowDuration", 0.4f);
            SetColor(serialized, "hazardTint", new Color(0.45f, 0.9f, 0.35f, 0.55f));
            SetColor(serialized, "windupTint", new Color(0.6f, 1f, 0.5f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, SwampyPath);
        }

        private static Enemy BuildKnight(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "Knight",
                ThirdPartySprites.Knight,
                fallback,
                ThirdPartySprites.KnightIdle,
                ThirdPartySprites.KnightRun,
                1f,
                0.4f);

            var enemy = root.AddComponent<CleaveEnemy>();
            var serialized = ConfigureMonster(enemy, 1.8f, 0.9f, 1);
            SetFloat(serialized, "triggerRange", 1.7f);
            SetFloat(serialized, "windupTime", 0.5f);
            SetInt(serialized, "damage", 2);
            SetFloat(serialized, "radius", 1.6f);
            SetFloat(serialized, "halfAngle", 75f);
            SetFloat(serialized, "recoverTime", 0.6f);
            SetFloat(serialized, "attackCooldown", 2.1f);
            SetBool(serialized, "secondStrike", true);
            SetFloat(serialized, "secondDelay", 0.25f);
            SetColor(serialized, "windupTint", new Color(1f, 0.8f, 0.35f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, KnightPath);
        }

        private static Enemy BuildWizard(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "Wizard",
                ThirdPartySprites.Wizard,
                fallback,
                ThirdPartySprites.WizardIdle,
                ThirdPartySprites.WizardRun,
                1f,
                0.4f);

            var enemy = root.AddComponent<BlinkShooterEnemy>();
            var serialized = ConfigureMonster(enemy, 1.15f, 1f, 1);
            SetFloat(serialized, "preferredDistance", 4.5f);
            SetFloat(serialized, "blinkInterval", 5f);
            SetFloat(serialized, "blinkWindup", 0.4f);
            SetFloat(serialized, "blinkDistance", 3.2f);
            SetInt(serialized, "projectileCount", 3);
            SetFloat(serialized, "spreadAngle", 36f);
            SetFloat(serialized, "projectileSpeed", 4.8f);
            SetFloat(serialized, "projectileLifetime", 4f);
            SetFloat(serialized, "shootWindup", 0.4f);
            SetFloat(serialized, "recoverTime", 0.5f);
            SetColor(serialized, "projectileTint", new Color(0.72f, 0.58f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, WizardPath);
        }

        private static Enemy BuildKnightFemale(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "KnightFemale",
                ThirdPartySprites.KnightFemale,
                fallback,
                ThirdPartySprites.KnightFemaleIdle,
                ThirdPartySprites.KnightFemaleRun,
                1f,
                0.4f);

            var enemy = root.AddComponent<ChargerEnemy>();
            var serialized = ConfigureMonster(enemy, 1.6f, 1f, 1);
            SetFloat(serialized, "triggerRange", 5.5f);
            SetFloat(serialized, "windupTime", 0.45f);
            SetFloat(serialized, "chargeSpeed", 11f);
            SetFloat(serialized, "chargeDuration", 0.4f);
            SetFloat(serialized, "recoverTime", 0.7f);
            SetInt(serialized, "chargeCount", 1);
            SetFloat(serialized, "chargePause", 0.25f);
            SetColor(serialized, "windupTint", new Color(0.55f, 0.85f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, KnightFemalePath);
        }

        private static Enemy BuildWizardFemale(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "WizardFemale",
                ThirdPartySprites.WizardFemale,
                fallback,
                ThirdPartySprites.WizardFemaleIdle,
                ThirdPartySprites.WizardFemaleRun,
                1f,
                0.4f);

            var enemy = root.AddComponent<ShooterEnemy>();
            var serialized = ConfigureMonster(enemy, 1.2f, 1f, 1);
            SetFloat(serialized, "preferredDistance", 5f);
            SetFloat(serialized, "shootInterval", 1.9f);
            SetFloat(serialized, "windupTime", 0.45f);
            SetFloat(serialized, "projectileSpeed", 5.5f);
            SetFloat(serialized, "projectileLifetime", 4f);
            SetInt(serialized, "projectileCount", 2);
            SetFloat(serialized, "spreadAngle", 18f);
            SetColor(serialized, "projectileTint", new Color(0.65f, 0.55f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, WizardFemalePath);
        }

        private static Enemy BuildElf(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "Elf",
                ThirdPartySprites.Elf,
                fallback,
                ThirdPartySprites.ElfIdle,
                ThirdPartySprites.ElfRun,
                1f,
                0.4f);

            var enemy = root.AddComponent<ShooterEnemy>();
            var serialized = ConfigureMonster(enemy, 1f, 1.15f, 1);
            SetFloat(serialized, "preferredDistance", 5.5f);
            SetFloat(serialized, "shootInterval", 1.4f);
            SetFloat(serialized, "windupTime", 0.3f);
            SetFloat(serialized, "projectileSpeed", 6.5f);
            SetFloat(serialized, "projectileLifetime", 4.5f);
            SetInt(serialized, "projectileCount", 1);
            SetFloat(serialized, "spreadAngle", 0f);
            SetColor(serialized, "projectileTint", new Color(0.55f, 1f, 0.65f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, ElfPath);
        }

        private static Enemy BuildElfFemale(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "ElfFemale",
                ThirdPartySprites.ElfFemale,
                fallback,
                ThirdPartySprites.ElfFemaleIdle,
                ThirdPartySprites.ElfFemaleRun,
                1f,
                0.4f);

            var enemy = root.AddComponent<BlinkShooterEnemy>();
            var serialized = ConfigureMonster(enemy, 1.1f, 1.1f, 1);
            SetFloat(serialized, "preferredDistance", 4.6f);
            SetFloat(serialized, "blinkInterval", 4.5f);
            SetFloat(serialized, "blinkWindup", 0.35f);
            SetFloat(serialized, "blinkDistance", 3.4f);
            SetInt(serialized, "projectileCount", 4);
            SetFloat(serialized, "spreadAngle", 48f);
            SetFloat(serialized, "projectileSpeed", 5f);
            SetFloat(serialized, "projectileLifetime", 4f);
            SetFloat(serialized, "shootWindup", 0.35f);
            SetFloat(serialized, "recoverTime", 0.45f);
            SetColor(serialized, "projectileTint", new Color(0.55f, 1f, 0.75f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, ElfFemalePath);
        }

        private static Enemy BuildSkeletonWarrior(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "SkeletonWarrior",
                ThirdPartySprites.Skeleton,
                fallback,
                ThirdPartySprites.SkeletonIdle,
                ThirdPartySprites.SkeletonRun,
                1f,
                0.32f);

            var enemy = root.AddComponent<CleaveEnemy>();
            var serialized = ConfigureMonster(enemy, 1.2f, 1.1f, 1);
            SetFloat(serialized, "triggerRange", 1.4f);
            SetFloat(serialized, "windupTime", 0.35f);
            SetInt(serialized, "damage", 1);
            SetFloat(serialized, "radius", 1.25f);
            SetFloat(serialized, "halfAngle", 60f);
            SetFloat(serialized, "recoverTime", 0.45f);
            SetFloat(serialized, "attackCooldown", 1.6f);
            SetBool(serialized, "secondStrike", false);
            SetColor(serialized, "windupTint", new Color(0.6f, 0.9f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, SkeletonWarriorPath);
        }

        private static Enemy BuildTinyZombieRaider(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "TinyZombieRaider",
                ThirdPartySprites.TinyZombie,
                fallback,
                ThirdPartySprites.TinyZombieIdle,
                ThirdPartySprites.TinyZombieRun,
                1f,
                0.28f);

            var enemy = root.AddComponent<LungeEnemy>();
            var serialized = ConfigureMonster(enemy, 0.8f, 1.25f, 1);
            SetFloat(serialized, "triggerRange", 2.4f);
            SetFloat(serialized, "windupTime", 0.25f);
            SetFloat(serialized, "lungeSpeed", 7.5f);
            SetFloat(serialized, "lungeDuration", 0.18f);
            SetFloat(serialized, "recoverTime", 0.4f);
            SetFloat(serialized, "lungeCooldown", 1.8f);
            SetColor(serialized, "windupTint", new Color(0.6f, 1f, 0.5f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, TinyZombieRaiderPath);
        }

        private static void BuildDanaidaMonsters(Sprite fallback)
        {
            BuildDanaidaRedImp(fallback);
            BuildDanaidaGreenSlime(fallback);
            BuildDanaidaPurpleGhost(fallback);
            BuildDanaidaTealSpider(fallback);
            BuildDanaidaBrownCyclops(fallback);
            BuildDanaidaPurpleDemon(fallback);
        }

        private static Enemy BuildDanaidaRedImp(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "DanaidaRedImp",
                DanaidaSprites.RedImp,
                fallback,
                DanaidaSprites.RedImpFrames,
                DanaidaSprites.RedImpFrames,
                1f,
                0.4f);

            var enemy = root.AddComponent<LungeEnemy>();
            var serialized = ConfigureMonster(enemy, 1f, 1.1f, 1);
            SetFloat(serialized, "triggerRange", 2.6f);
            SetFloat(serialized, "windupTime", 0.3f);
            SetFloat(serialized, "lungeSpeed", 8f);
            SetFloat(serialized, "lungeDuration", 0.2f);
            SetFloat(serialized, "recoverTime", 0.45f);
            SetFloat(serialized, "lungeCooldown", 2f);
            SetColor(serialized, "windupTint", new Color(1f, 0.7f, 0.4f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, DanaidaRedImpPath);
        }

        private static Enemy BuildDanaidaGreenSlime(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "DanaidaGreenSlime",
                DanaidaSprites.GreenSlime,
                fallback,
                DanaidaSprites.GreenSlimeFrames,
                DanaidaSprites.GreenSlimeFrames,
                1f,
                0.4f);

            var enemy = root.AddComponent<Enemy>();
            ConfigureMonster(enemy, 1f, 0.65f, 1).ApplyModifiedPropertiesWithoutUndo();
            return SaveMonster<Enemy>(root, DanaidaGreenSlimePath);
        }

        private static Enemy BuildDanaidaPurpleGhost(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "DanaidaPurpleGhost",
                DanaidaSprites.PurpleGhost,
                fallback,
                DanaidaSprites.PurpleGhostFrames,
                DanaidaSprites.PurpleGhostFrames,
                1f,
                0.4f);

            var enemy = root.AddComponent<BlinkShooterEnemy>();
            var serialized = ConfigureMonster(enemy, 0.9f, 0.9f, 1);
            SetFloat(serialized, "preferredDistance", 4.5f);
            SetFloat(serialized, "blinkInterval", 4.5f);
            SetFloat(serialized, "blinkWindup", 0.4f);
            SetFloat(serialized, "blinkDistance", 3f);
            SetInt(serialized, "projectileCount", 1);
            SetFloat(serialized, "spreadAngle", 0f);
            SetFloat(serialized, "projectileSpeed", 4.5f);
            SetFloat(serialized, "projectileLifetime", 4f);
            SetFloat(serialized, "shootWindup", 0.35f);
            SetFloat(serialized, "recoverTime", 0.5f);
            SetColor(serialized, "projectileTint", new Color(0.78f, 0.55f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, DanaidaPurpleGhostPath);
        }

        private static Enemy BuildDanaidaTealSpider(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "DanaidaTealSpider",
                DanaidaSprites.TealSpider,
                fallback,
                DanaidaSprites.TealSpiderFrames,
                DanaidaSprites.TealSpiderFrames,
                1f,
                0.4f);

            var enemy = root.AddComponent<LungeEnemy>();
            var serialized = ConfigureMonster(enemy, 0.8f, 1.3f, 1);
            SetFloat(serialized, "triggerRange", 2.2f);
            SetFloat(serialized, "windupTime", 0.2f);
            SetFloat(serialized, "lungeSpeed", 8.5f);
            SetFloat(serialized, "lungeDuration", 0.18f);
            SetFloat(serialized, "recoverTime", 0.35f);
            SetFloat(serialized, "lungeCooldown", 1.6f);
            SetColor(serialized, "windupTint", new Color(0.55f, 1f, 0.85f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, DanaidaTealSpiderPath);
        }

        private static Enemy BuildDanaidaBrownCyclops(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "DanaidaBrownCyclops",
                DanaidaSprites.BrownCyclops,
                fallback,
                DanaidaSprites.BrownCyclopsFrames,
                DanaidaSprites.BrownCyclopsFrames,
                1f,
                0.45f);

            var enemy = root.AddComponent<CleaveEnemy>();
            var serialized = ConfigureMonster(enemy, 2.2f, 0.7f, 1);
            SetFloat(serialized, "triggerRange", 1.8f);
            SetFloat(serialized, "windupTime", 0.65f);
            SetInt(serialized, "damage", 2);
            SetFloat(serialized, "radius", 1.7f);
            SetFloat(serialized, "halfAngle", 80f);
            SetFloat(serialized, "recoverTime", 0.7f);
            SetFloat(serialized, "attackCooldown", 2.6f);
            SetBool(serialized, "secondStrike", false);
            SetColor(serialized, "windupTint", new Color(1f, 0.55f, 0.35f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, DanaidaBrownCyclopsPath);
        }

        private static Enemy BuildDanaidaPurpleDemon(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "DanaidaPurpleDemon",
                DanaidaSprites.PurpleDemon,
                fallback,
                DanaidaSprites.PurpleDemonFrames,
                DanaidaSprites.PurpleDemonFrames,
                1f,
                0.4f);

            var enemy = root.AddComponent<ShooterEnemy>();
            var serialized = ConfigureMonster(enemy, 1.5f, 0.95f, 1);
            SetFloat(serialized, "preferredDistance", 5f);
            SetFloat(serialized, "shootInterval", 2.2f);
            SetFloat(serialized, "windupTime", 0.5f);
            SetFloat(serialized, "projectileSpeed", 5.2f);
            SetFloat(serialized, "projectileLifetime", 4f);
            SetInt(serialized, "projectileCount", 3);
            SetFloat(serialized, "spreadAngle", 24f);
            SetColor(serialized, "projectileTint", new Color(0.8f, 0.45f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, DanaidaPurpleDemonPath);
        }

        private static void BuildTrashMobs(Sprite fallback, EnemyHazard hazard)
        {
            BuildTrashGreenSlime(fallback);
            BuildTrashSnake(fallback);
            BuildTrashBlueBat(fallback);
            BuildTrashMushroom(fallback, hazard);
            BuildTrashBee(fallback);
            BuildTrashFlower(fallback);
        }

        private static Enemy BuildTrashGreenSlime(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "TrashGreenSlime",
                TrashMobSprites.GreenSlime,
                fallback,
                TrashMobSprites.GreenSlimeFrames,
                TrashMobSprites.GreenSlimeFrames,
                1f,
                0.4f);

            var enemy = root.AddComponent<Enemy>();
            ConfigureMonster(enemy, 1f, 0.65f, 1).ApplyModifiedPropertiesWithoutUndo();
            return SaveMonster<Enemy>(root, TrashGreenSlimePath);
        }

        private static Enemy BuildTrashSnake(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "TrashSnake",
                TrashMobSprites.Snake,
                fallback,
                TrashMobSprites.SnakeFrames,
                TrashMobSprites.SnakeFrames,
                1f,
                0.4f);

            var enemy = root.AddComponent<LungeEnemy>();
            var serialized = ConfigureMonster(enemy, 0.9f, 1.1f, 1);
            SetFloat(serialized, "triggerRange", 2.4f);
            SetFloat(serialized, "windupTime", 0.25f);
            SetFloat(serialized, "lungeSpeed", 7.5f);
            SetFloat(serialized, "lungeDuration", 0.2f);
            SetFloat(serialized, "recoverTime", 0.4f);
            SetFloat(serialized, "lungeCooldown", 1.8f);
            SetColor(serialized, "windupTint", new Color(0.75f, 1f, 0.5f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, TrashSnakePath);
        }

        private static Enemy BuildTrashBlueBat(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "TrashBlueBat",
                TrashMobSprites.BlueBat,
                fallback,
                TrashMobSprites.BlueBatFrames,
                TrashMobSprites.BlueBatFrames,
                1f,
                0.4f);

            var enemy = root.AddComponent<BlinkShooterEnemy>();
            var serialized = ConfigureMonster(enemy, 0.8f, 1.1f, 1);
            SetFloat(serialized, "preferredDistance", 4f);
            SetFloat(serialized, "blinkInterval", 4f);
            SetFloat(serialized, "blinkWindup", 0.35f);
            SetFloat(serialized, "blinkDistance", 3f);
            SetInt(serialized, "projectileCount", 1);
            SetFloat(serialized, "spreadAngle", 0f);
            SetFloat(serialized, "projectileSpeed", 5f);
            SetFloat(serialized, "projectileLifetime", 4f);
            SetFloat(serialized, "shootWindup", 0.35f);
            SetFloat(serialized, "recoverTime", 0.45f);
            SetColor(serialized, "projectileTint", new Color(0.55f, 0.75f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, TrashBlueBatPath);
        }

        private static Enemy BuildTrashMushroom(Sprite fallback, EnemyHazard hazard)
        {
            GameObject root = CreateMonsterRoot(
                "TrashMushroom",
                TrashMobSprites.Mushroom,
                fallback,
                TrashMobSprites.MushroomFrames,
                TrashMobSprites.MushroomFrames,
                1f,
                0.45f);

            var enemy = root.AddComponent<TrapperEnemy>();
            var serialized = ConfigureMonster(enemy, 1.1f, 0.6f, 1);
            SetObject(serialized, "hazardPrefab", hazard);
            SetFloat(serialized, "triggerRange", 2.8f);
            SetFloat(serialized, "dropInterval", 3.2f);
            SetFloat(serialized, "dropWindup", 0.5f);
            SetInt(serialized, "hazardDamage", 1);
            SetFloat(serialized, "hazardTickInterval", 0.9f);
            SetFloat(serialized, "hazardLifetime", 2.4f);
            SetFloat(serialized, "hazardRadius", 1.2f);
            SetFloat(serialized, "slowFactor", 0.7f);
            SetFloat(serialized, "slowDuration", 0.5f);
            SetColor(serialized, "hazardTint", new Color(0.9f, 0.4f, 0.55f, 0.55f));
            SetColor(serialized, "windupTint", new Color(1f, 0.55f, 0.65f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, TrashMushroomPath);
        }

        private static Enemy BuildTrashBee(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "TrashBee",
                TrashMobSprites.Bee,
                fallback,
                TrashMobSprites.BeeFrames,
                TrashMobSprites.BeeFrames,
                1f,
                0.38f);

            var enemy = root.AddComponent<LungeEnemy>();
            var serialized = ConfigureMonster(enemy, 0.7f, 1.3f, 1);
            SetFloat(serialized, "triggerRange", 2.8f);
            SetFloat(serialized, "windupTime", 0.2f);
            SetFloat(serialized, "lungeSpeed", 8.5f);
            SetFloat(serialized, "lungeDuration", 0.18f);
            SetFloat(serialized, "recoverTime", 0.35f);
            SetFloat(serialized, "lungeCooldown", 1.6f);
            SetColor(serialized, "windupTint", new Color(1f, 0.9f, 0.5f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, TrashBeePath);
        }

        private static Enemy BuildTrashFlower(Sprite fallback)
        {
            GameObject root = CreateMonsterRoot(
                "TrashFlower",
                TrashMobSprites.Flower,
                fallback,
                TrashMobSprites.FlowerFrames,
                TrashMobSprites.FlowerFrames,
                1f,
                0.4f);

            var enemy = root.AddComponent<ShooterEnemy>();
            var serialized = ConfigureMonster(enemy, 1.1f, 0.75f, 1);
            SetBool(serialized, "stationary", true);
            SetFloat(serialized, "preferredDistance", 5f);
            SetFloat(serialized, "shootInterval", 1.9f);
            SetFloat(serialized, "windupTime", 0.45f);
            SetFloat(serialized, "projectileSpeed", 5f);
            SetFloat(serialized, "projectileLifetime", 4f);
            SetInt(serialized, "projectileCount", 2);
            SetFloat(serialized, "spreadAngle", 20f);
            SetColor(serialized, "projectileTint", new Color(0.85f, 0.45f, 1f, 1f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, TrashFlowerPath);
        }

        private static Enemy BuildTinyZombieCloud(Sprite fallback, EnemyHazard hazard)
        {
            GameObject root = CreateMonsterRoot(
                "TinyZombieCloud",
                ThirdPartySprites.TinyZombie,
                fallback,
                ThirdPartySprites.TinyZombieIdle,
                ThirdPartySprites.TinyZombieRun,
                1f,
                0.28f);

            var enemy = root.AddComponent<DeathCloudEnemy>();
            var serialized = ConfigureMonster(enemy, 1f, 1.15f, 1);
            SetObject(serialized, "hazardPrefab", hazard);
            SetInt(serialized, "cloudDamage", 1);
            SetFloat(serialized, "cloudRadius", 1.2f);
            SetFloat(serialized, "cloudLifetime", 2f);
            SetFloat(serialized, "cloudTickInterval", 0.8f);
            SetColor(serialized, "cloudTint", new Color(0.45f, 1f, 0.35f, 0.55f));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return SaveMonster<Enemy>(root, TinyZombieCloudPath);
        }

        private static GameObject CreateMonsterRoot(
            string name,
            Sprite art,
            Sprite fallback,
            Sprite[] idle,
            Sprite[] run,
            float scale,
            float colliderRadius)
        {
            var root = new GameObject(name);

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(art, fallback);
            renderer.color = Color.white;
            renderer.sortingOrder = 8;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = colliderRadius;

            root.transform.localScale = Vector3.one * scale;
            AttachAnimator(root, idle, run);
            return root;
        }

        private static SerializedObject ConfigureMonster(
            Enemy enemy,
            float healthMultiplier,
            float speedMultiplier,
            int contactDamage)
        {
            var serialized = new SerializedObject(enemy);
            SetFloat(serialized, "healthMultiplier", healthMultiplier);
            SetFloat(serialized, "speedMultiplier", speedMultiplier);
            SetInt(serialized, "contactDamage", contactDamage);
            return serialized;
        }

        private static T SaveMonster<T>(GameObject root, string path) where T : Component
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved != null ? saved.GetComponent<T>() : null;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.intValue = value;
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.boolValue = value;
        }

        private static void SetColor(SerializedObject serialized, string propertyName, Color value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.colorValue = value;
        }

        private static void SetObject(SerializedObject serialized, string propertyName, Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void BuildEnemyProjectile(Sprite sprite)
        {
            var root = new GameObject("EnemyProjectile");
            root.transform.localScale = Vector3.one * 0.34f;

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.EnemyOrb, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 9;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            root.AddComponent<EnemyProjectile>();

            PrefabUtility.SaveAsPrefabAsset(root, EnemyProjectilePath);
            Object.DestroyImmediate(root);
        }

        private static void BuildTear(Sprite sprite)
        {
            var root = new GameObject("Tear");
            root.transform.localScale = Vector3.one * 0.36f;

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.Tear, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 9;

            var body = root.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            var tear = root.AddComponent<TearProjectile>();
            var serialized = new SerializedObject(tear);
            serialized.FindProperty("explosionSprite").objectReferenceValue = ThirdPartySprites.Explosion;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, TearPath);
            Object.DestroyImmediate(root);
        }

        /// <summary>给已有的 Tear Prefab 补上爆炸贴图引用，不重建整个 Prefab。</summary>
        public static void EnsureTearExplosionSprite()
        {
            var tear = AssetDatabase.LoadAssetAtPath<TearProjectile>(TearPath);
            if (tear == null)
            {
                Debug.LogWarning("[Combat] 找不到 Tear Prefab，无法补全爆炸贴图引用。");
                return;
            }

            var serialized = new SerializedObject(tear);
            SerializedProperty sprite = serialized.FindProperty("explosionSprite");
            if (sprite == null || sprite.objectReferenceValue != null) return;

            sprite.objectReferenceValue = ThirdPartySprites.Explosion;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(tear);
            AssetDatabase.SaveAssets();
            Debug.Log("[Combat] 已为 Tear Prefab 补上爆炸贴图引用。");
        }

        private static void BuildCoin(Sprite sprite)
        {
            var root = new GameObject("Coin");
            root.transform.localScale = Vector3.one * 0.7f;

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.Coin, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 7;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            root.AddComponent<Pickup>().Setup(PickupType.Coin, 1, 0);

            PrefabUtility.SaveAsPrefabAsset(root, CoinPath);
            Object.DestroyImmediate(root);
        }

        private static void BuildHeart(Sprite sprite)
        {
            var root = new GameObject("Heart");
            root.transform.localScale = Vector3.one * 0.45f;

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.Heart, sprite);
            renderer.color = Color.white;
            renderer.sortingOrder = 7;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.5f;
            collider.isTrigger = true;

            root.AddComponent<Pickup>().Setup(PickupType.Heart, 0, 1);

            PrefabUtility.SaveAsPrefabAsset(root, HeartPath);
            Object.DestroyImmediate(root);
        }

        private static void SetSpriteArray(SerializedObject serialized, string propertyName, Sprite[] sprites)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property == null) return;

            int count = sprites != null ? sprites.Length : 0;
            property.arraySize = count;

            for (int i = 0; i < count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        }

        /// <summary>挂上帧动画；没有美术帧时保持静态 Sprite。</summary>
        private static void AttachAnimator(GameObject root, Sprite[] idle, Sprite[] run)
        {
            bool hasIdle = idle != null && idle.Length > 0 && idle[0] != null;
            bool hasRun = run != null && run.Length > 0 && run[0] != null;
            if (!hasIdle && !hasRun) return;

            var animator = root.AddComponent<SpriteAnimator>();
            animator.Configure(idle, run);
        }

        private static void BuildPortal(Sprite fallback)
        {
            var root = new GameObject("NextFloorPortal");

            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = ArtOr(ThirdPartySprites.Portal, fallback);
            renderer.color = Color.white;
            renderer.sortingOrder = 3;

            var collider = root.AddComponent<CircleCollider2D>();
            collider.radius = 0.7f;
            collider.isTrigger = true;

            root.AddComponent<NextFloorPortal>();

            PrefabUtility.SaveAsPrefabAsset(root, PortalPath);
            Object.DestroyImmediate(root);
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
