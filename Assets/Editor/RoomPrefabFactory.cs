using System.IO;
using Roguelike.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 房间 Prefab 工厂。
    /// 菜单：Tools / Roguelike / 生成房间 Prefab
    ///
    /// 目前没有美术资源，因此用一张 4×4 纯白 Sprite（PPU=4，即 1 世界单位）
    /// 拼出地板与墙体，保证 DungeonBuilder 立刻可以跑通。
    /// 后续换正式美术时，只需要替换这个 Prefab 里的 SpriteRenderer 即可。
    ///
    /// 该操作是幂等的：重复执行会覆盖旧 Prefab。
    /// </summary>
    public static class RoomPrefabFactory
    {
        public const string PrefabFolder = "Assets/Resources/Dungeon";
        public const string PrefabPath = PrefabFolder + "/Room.prefab";

        /// <summary>未清空战斗房的门槛警示地砖（3×1 / 1×3 世界单位）。</summary>
        public const string DoorWarningHorizontalPath = "Assets/Art/DoorWarningH.png";
        public const string DoorWarningVerticalPath = "Assets/Art/DoorWarningV.png";

        /// <summary>所有房间外框尺寸组合（宽 × 高）。</summary>
        public static readonly Vector2Int[] SizeVariants =
        {
            new Vector2Int(15, 9),
            new Vector2Int(15, 11),
            new Vector2Int(17, 9),
            new Vector2Int(17, 11),
            new Vector2Int(19, 9),
            new Vector2Int(19, 11),
        };

        public static string VariantPath(int width, int height) =>
            $"{PrefabFolder}/{RoomGeometry.PrefabStem(width, height)}.prefab";

        /// <summary>六种尺寸的 Prefab 是否都已生成。</summary>
        public static bool AllVariantsExist()
        {
            for (int i = 0; i < SizeVariants.Length; i++)
            {
                Vector2Int size = SizeVariants[i];
                if (AssetDatabase.LoadAssetAtPath<RoomView>(VariantPath(size.x, size.y)) == null)
                    return false;
            }

            return true;
        }

        private const string ArtFolder = "Assets/Art";
        private const string SpritePath = "Assets/Art/White.png";

        [MenuItem("Tools/Roguelike/生成房间 Prefab")]
        public static void RebuildFromMenu()
        {
            Rebuild();
        }

        /// <summary>生成（或重建）房间 Prefab，返回其 RoomView 组件。</summary>
        public static RoomView Rebuild()
        {
            Sprite white = EnsureWhiteSprite();
            if (white == null)
            {
                Debug.LogError("[Dungeon] 无法创建白色 Sprite，房间 Prefab 未生成。");
                return null;
            }

            Sprite floorSprite = ThirdPartySprites.Floor != null ? ThirdPartySprites.Floor : white;
            Sprite wallSprite = ThirdPartySprites.Wall != null ? ThirdPartySprites.Wall : white;
            Sprite doorSprite = ThirdPartySprites.Door != null ? ThirdPartySprites.Door : white;
            Sprite doorHorizontal = ThirdPartySprites.DoorHorizontal != null ? ThirdPartySprites.DoorHorizontal : doorSprite;
            Sprite doorVertical = ThirdPartySprites.DoorVertical != null ? ThirdPartySprites.DoorVertical : doorSprite;

            Sprite doorWarningH = LoadDoorWarningSprite(DoorWarningHorizontalPath);
            Sprite doorWarningV = LoadDoorWarningSprite(DoorWarningVerticalPath);

            Sprite[] floorSprites = ThirdPartySprites.FloorVariants;
            bool hasVariants = floorSprites != null && floorSprites.Length > 0 && floorSprites[0] != null;
            if (!hasVariants)
            {
                floorSprites = new[] { floorSprite };
            }
            else
            {
                for (int i = 0; i < floorSprites.Length; i++)
                    if (floorSprites[i] == null) floorSprites[i] = floorSprite;
            }

            EnsureFolder(PrefabFolder);

            RoomView canonical = null;

            for (int i = 0; i < SizeVariants.Length; i++)
            {
                Vector2Int size = SizeVariants[i];
                GameObject root = BuildHierarchy(
                    white, floorSprites, floorSprite, wallSprite,
                    doorHorizontal, doorVertical, doorWarningH, doorWarningV,
                    size.x, size.y);

                string path = VariantPath(size.x, size.y);
                GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, path);

                if (size.x == RoomGeometry.FootprintWidth && size.y == RoomGeometry.FootprintHeight)
                {
                    canonical = prefabAsset != null ? prefabAsset.GetComponent<RoomView>() : null;
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }

                Object.DestroyImmediate(root);
                Debug.Log($"[Dungeon] 房间 Prefab 已生成：{path}（{size.x}×{size.y}）");
            }

            AssetDatabase.SaveAssets();

            Debug.Log($"[Dungeon] 房间 Prefab 完成：{SizeVariants.Length} 种尺寸，另保留 {PrefabPath}");
            return canonical;
        }

        // ------------------------------------------------------------------
        // 层级构建
        // ------------------------------------------------------------------

        private static GameObject BuildHierarchy(
            Sprite white,
            Sprite[] floorSprites,
            Sprite floorSprite,
            Sprite wallSprite,
            Sprite doorHorizontal,
            Sprite doorVertical,
            Sprite doorWarningH,
            Sprite doorWarningV,
            int width,
            int height)
        {
            var root = new GameObject("Room");
            var view = root.AddComponent<RoomView>();

            // 地板：width × height 个独立瓦片，由 RoomFloor 按房间种子挑选变体
            GameObject floor = BuildFloor(root.transform, floorSprites, floorSprite, width, height);

            // 墙面 / 地面装饰：空容器，由 RoomDecorations 在构建时按房间类型生成
            var decorationsRoot = new GameObject("Decorations");
            decorationsRoot.transform.SetParent(root.transform, false);
            var decorations = decorationsRoot.AddComponent<RoomDecorations>();
            decorations.EditorBind(
                decorationsRoot.transform,
                ThirdPartySprites.Banners,
                ThirdPartySprites.ColumnTop,
                ThirdPartySprites.ColumnMid,
                ThirdPartySprites.ColumnBase,
                ThirdPartySprites.Crate,
                ThirdPartySprites.Skull,
                ThirdPartySprites.WallHole,
                ThirdPartySprites.WallGoo,
                ThirdPartySprites.Chest,
                ThirdPartySprites.ChestOpen,
                width,
                height);

            // 房间类型调试标记：位于地板之上、墙体之下
            GameObject badge = CreateSprite("TypeBadge", root.transform, white, Color.white,
                RoomGeometry.GetCenter(width, height),
                new Vector2(2f, 2f),
                -9, false);

            // 四面墙，顺序必须与 WallSide 一致：上 / 右 / 下 / 左
            var wallsRoot = new GameObject("Walls").transform;
            wallsRoot.SetParent(root.transform, false);

            var walls = new RoomWall[4];
            for (int i = 0; i < 4; i++)
                walls[i] = BuildWall(wallsRoot, wallSprite, (WallSide)i, width, height);

            // 门标记（逻辑锚点，只在该侧墙体归本房间时启用）
            var doorsRoot = new GameObject("Doors").transform;
            doorsRoot.SetParent(root.transform, false);

            var doorMarkers = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                WallSide side = (WallSide)i;
                var go = new GameObject("Door_" + side);
                go.transform.SetParent(doorsRoot, false);
                go.transform.localPosition = RoomGeometry.DoorCenter(side, width, height);
                go.AddComponent<RoomDoor>().Setup(side);
                doorMarkers[i] = go.transform;
            }

            // 门槛警示地砖：未清空的战斗房 / Boss 房在门外就能看到
            var indicatorsRoot = new GameObject("DoorIndicators").transform;
            indicatorsRoot.SetParent(root.transform, false);

            var doorIndicators = new SpriteRenderer[4];
            for (int i = 0; i < 4; i++)
            {
                WallSide side = (WallSide)i;
                bool horizontal = side == WallSide.Up || side == WallSide.Down;

                Sprite warning = horizontal ? doorWarningH : doorWarningV;
                bool hasWarningSprite = warning != null;
                Sprite sprite = hasWarningSprite ? warning : white;
                Color color = hasWarningSprite ? Color.white : new Color(0.62f, 0.16f, 0.18f, 1f);
                Vector2 size = hasWarningSprite
                    ? Vector2.one
                    : (horizontal ? new Vector2(3f, 1f) : new Vector2(1f, 3f));

                GameObject go = CreateSprite(
                    "DoorIndicator_" + side,
                    indicatorsRoot,
                    sprite,
                    color,
                    RoomGeometry.DoorCenter(side, width, height),
                    size,
                    -8,
                    false);

                var renderer = go.GetComponent<SpriteRenderer>();
                renderer.enabled = false;
                doorIndicators[i] = renderer;
            }

            // 门洞触发盒：只要该方向有门就启用（左右两侧各一个，互不依赖）
            var triggersRoot = new GameObject("DoorTriggers").transform;
            triggersRoot.SetParent(root.transform, false);

            var doorTriggers = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                WallSide side = (WallSide)i;
                var go = new GameObject("DoorTrigger_" + side);
                go.transform.SetParent(triggersRoot, false);
                go.transform.localPosition = RoomGeometry.DoorCenter(side, width, height);

                var box = go.AddComponent<BoxCollider2D>();
                box.isTrigger = true;
                box.size = (side == WallSide.Up || side == WallSide.Down)
                    ? new Vector2(RoomGeometry.DoorWidth, 0.6f)
                    : new Vector2(0.6f, RoomGeometry.DoorWidth);

                go.AddComponent<RoomDoorTrigger>().Setup(side);
                doorTriggers[i] = go.transform;
            }

            // 封门板：锁门时出现，尺寸与门洞相同
            var blockersRoot = new GameObject("DoorBlockers").transform;
            blockersRoot.SetParent(root.transform, false);

            var doorBlockers = new RoomDoorBlocker[4];
            for (int i = 0; i < 4; i++)
            {
                WallSide side = (WallSide)i;
                bool horizontal = side == WallSide.Up || side == WallSide.Down;

                var size = horizontal
                    ? new Vector2(RoomGeometry.DoorWidth, RoomGeometry.WallThickness)
                    : new Vector2(RoomGeometry.WallThickness, RoomGeometry.DoorWidth);

                var go = new GameObject("DoorBlocker_" + side);
                go.transform.SetParent(blockersRoot, false);
                go.transform.localPosition = RoomGeometry.DoorCenter(side, width, height);

                var box = go.AddComponent<BoxCollider2D>();
                box.size = size;
                box.enabled = false;

                // 3×1 / 1×3 的完整门板，直接铺满门洞，不再平铺单张 2×2 门
                Sprite panelSprite = horizontal ? doorHorizontal : doorVertical;
                GameObject panel = CreateSprite("Panel", go.transform, panelSprite, Color.white,
                    Vector2.zero, Vector2.one, 5, false, SpriteDrawMode.Simple);

                var blocker = go.AddComponent<RoomDoorBlocker>();
                blocker.Setup(panel.GetComponent<SpriteRenderer>(), box);
                doorBlockers[i] = blocker;
            }

            // 入口点：从门洞向房内退一段距离
            var entriesRoot = new GameObject("EntryPoints").transform;
            entriesRoot.SetParent(root.transform, false);

            var entries = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                WallSide side = (WallSide)i;
                var go = new GameObject("Entry_" + side);
                go.transform.SetParent(entriesRoot, false);
                go.transform.localPosition = RoomGeometry.EntryPoint(side, width, height);
                entries[i] = go.transform;
            }

            // 房间中心
            var center = new GameObject("Spawn_Center");
            center.transform.SetParent(root.transform, false);
            center.transform.localPosition = RoomGeometry.GetCenter(width, height);

            // 敌人出生点
            var enemyRoot = new GameObject("EnemySpawns").transform;
            enemyRoot.SetParent(root.transform, false);

            Vector2[] spawnPoints = RoomGeometry.GetEnemySpawnPoints(width, height);
            var enemies = new Transform[spawnPoints.Length];
            for (int i = 0; i < spawnPoints.Length; i++)
            {
                var go = new GameObject("EnemySpawn_" + i);
                go.transform.SetParent(enemyRoot, false);
                go.transform.localPosition = spawnPoints[i];
                enemies[i] = go.transform;
            }

            view.EditorBind(
                center.transform,
                entries,
                doorMarkers,
                doorIndicators,
                doorTriggers,
                doorBlockers,
                enemies,
                walls,
                null,
                badge.GetComponent<SpriteRenderer>(),
                floor.GetComponent<RoomFloor>(),
                decorations,
                new Vector2Int(width, height));

            return root;
        }

        /// <summary>创建 width×height 的逐格地板，并把引用交给 RoomFloor。</summary>
        private static GameObject BuildFloor(Transform parent, Sprite[] sprites, Sprite fallback, int width, int height)
        {
            var floorRoot = new GameObject("Floor");
            floorRoot.transform.SetParent(parent, false);

            var roomFloor = floorRoot.AddComponent<RoomFloor>();
            var tiles = new SpriteRenderer[width * height];
            int index = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GameObject tile = CreateSprite(
                        $"Tile_{x}_{y}",
                        floorRoot.transform,
                        fallback,
                        Color.white,
                        new Vector2(x + 0.5f, y + 0.5f),
                        Vector2.one,
                        -10,
                        false);

                    tiles[index++] = tile.GetComponent<SpriteRenderer>();
                }
            }

            roomFloor.EditorBind(tiles, sprites);
            return floorRoot;
        }

        private static RoomWall BuildWall(Transform parent, Sprite sprite, WallSide side, int width, int height)
        {
            var go = new GameObject("Wall_" + side);
            go.transform.SetParent(parent, false);

            var wall = go.AddComponent<RoomWall>();
            WallSegment[] segments = RoomGeometry.GetWallSegments(side, width, height);

            CreateSprite("Seg_A", go.transform, sprite, Color.white, segments[0].Center, segments[0].Size, 0, true, SpriteDrawMode.Tiled);
            GameObject doorSegment = CreateSprite("Seg_Door", go.transform, sprite, Color.white, segments[1].Center, segments[1].Size, 0, true, SpriteDrawMode.Tiled);
            CreateSprite("Seg_B", go.transform, sprite, Color.white, segments[2].Center, segments[2].Size, 0, true, SpriteDrawMode.Tiled);

            wall.Setup(side, doorSegment);
            return wall;
        }

        private static GameObject CreateSprite(
            string name,
            Transform parent,
            Sprite sprite,
            Color color,
            Vector2 localPosition,
            Vector2 size,
            int sortingOrder,
            bool withCollider,
            SpriteDrawMode drawMode = SpriteDrawMode.Simple)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            if (drawMode == SpriteDrawMode.Tiled)
            {
                renderer.drawMode = SpriteDrawMode.Tiled;
                renderer.tileMode = SpriteTileMode.Continuous;
                renderer.size = size;
                go.transform.localScale = Vector3.one;
            }
            else
            {
                go.transform.localScale = new Vector3(size.x, size.y, 1f);
            }

            if (withCollider)
            {
                var collider = go.AddComponent<BoxCollider2D>();
                collider.size = drawMode == SpriteDrawMode.Tiled ? size : Vector2.one;
            }

            return go;
        }

        // ------------------------------------------------------------------
        // 资源
        // ------------------------------------------------------------------

        /// <summary>
        /// 加载门槛警示图：48×16 / 16×48，PPU = 16，
        /// 因此横向是 3×1 世界单位、纵向是 1×3。
        /// </summary>
        private static Sprite LoadDoorWarningSprite(string path)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            if (!File.Exists(Path.Combine(projectRoot, path))) return null;

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return AssetDatabase.LoadAssetAtPath<Sprite>(path);

            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, 16f))
            {
                importer.spritePixelsPerUnit = 16f;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.wrapMode = TextureWrapMode.Clamp;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (changed) importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>确保存在一张 4×4 白色 Sprite，PPU = 4，因此 1 个 Sprite = 1 世界单位。</summary>
        private static Sprite EnsureWhiteSprite()
        {
            EnsureFolder(ArtFolder);

            string absolutePath = Path.Combine(Application.dataPath, "Art/White.png");
            if (!File.Exists(absolutePath))
            {
                const int size = 4;
                var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int i = 0; i < pixels.Length; i++)
                    pixels[i] = new Color32(255, 255, 255, 255);

                texture.SetPixels32(pixels);
                texture.Apply();

                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SpritePath);
            }

            var importer = AssetImporter.GetAtPath(SpritePath) as TextureImporter;
            if (importer == null) return null;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 4f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
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
