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
        public const string BossPath = Folder + "/Boss.prefab";
        public const string EnemyProjectilePath = Folder + "/EnemyProjectile.prefab";
        public const string PortalPath = Folder + "/NextFloorPortal.prefab";

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
            BuildEnemy(sprite);
            BuildShooter(sprite);
            BuildCharger(sprite);
            BuildBoss(sprite);
            BuildTear(sprite);
            BuildEnemyProjectile(sprite);
            BuildCoin(sprite);
            BuildHeart(sprite);
            BuildPortal(sprite);
            AssetDatabase.SaveAssets();

            Debug.Log("[Combat] 战斗 Prefab 已生成：Enemy / Shooter / Charger / Boss / Tear / EnemyProjectile / Coin / Heart / NextFloorPortal");
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

            root.AddComponent<Enemy>();
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

        private static void BuildBoss(Sprite sprite)
        {
            var root = new GameObject("Boss");

            var renderer = root.AddComponent<SpriteRenderer>();
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

            root.AddComponent<BossEnemy>();
            AttachAnimator(root, ThirdPartySprites.BossIdle, ThirdPartySprites.BossRun);

            PrefabUtility.SaveAsPrefabAsset(root, BossPath);
            Object.DestroyImmediate(root);
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

            root.AddComponent<TearProjectile>();

            PrefabUtility.SaveAsPrefabAsset(root, TearPath);
            Object.DestroyImmediate(root);
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
