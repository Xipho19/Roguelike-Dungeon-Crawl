using System.Collections.Generic;
using System.Text;
using Roguelike.Dungeon;
using UnityEditor;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// DungeonBuilder 的几何自检。
    /// 菜单：Tools / Roguelike / DungeonBuilder 测试
    ///
    /// 对每个种子执行一次完整的"数据 → 场景 → 校验"，检查：
    ///   1. 房间数量、类型、门位、世界坐标与数据一致
    ///   2. 每面墙的归属与门洞开关状态正确
    ///   3. 门标记只出现在该出现的一侧，入口点在房间内
    ///   4. 相邻房间中心之间不存在任何墙体碰撞体（不会断路/封堵）
    ///
    /// 测试对象创建在临时根节点下，结束后立即销毁，不会污染当前场景。
    /// </summary>
    public static class DungeonBuilderSelfTest
    {
        [MenuItem("Tools/Roguelike/DungeonBuilder 测试")]
        public static void Run()
        {
            RoomView prefab = AssetDatabase.LoadAssetAtPath<RoomView>(RoomPrefabFactory.PrefabPath);
            if (prefab == null) prefab = RoomPrefabFactory.Rebuild();
            if (prefab == null)
            {
                Debug.LogError("[Dungeon] 测试中止：房间 Prefab 创建失败。");
                return;
            }

            var sb = new StringBuilder();
            int failed = 0;
            int total = 0;

            RunConfig(prefab, "默认 9×9", new DungeonConfig(), 20, sb, ref failed, ref total);
            RunConfig(prefab, "宽扁 12×6", new DungeonConfig
            {
                width = 12,
                height = 6,
                mainPathLength = 10,
                branchCount = 8,
                branchLengthMax = 4,
            }, 10, sb, ref failed, ref total);

            if (failed == 0)
                Debug.Log($"[Dungeon] Builder 测试通过：{total} 次构建全部一致。\n{sb}");
            else
                Debug.LogError($"[Dungeon] Builder 测试失败：{failed}/{total}。\n{sb}");
        }

        private static void RunConfig(
            RoomView prefab,
            string label,
            DungeonConfig config,
            int seedCount,
            StringBuilder sb,
            ref int failed,
            ref int total)
        {
            var root = new GameObject("DungeonBuilderTest");
            try
            {
                var builder = root.AddComponent<DungeonBuilder>();

                var serialized = new SerializedObject(builder);
                SerializedProperty prefabProperty = serialized.FindProperty("roomPrefab");
                if (prefabProperty != null)
                {
                    prefabProperty.objectReferenceValue = prefab;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                sb.AppendLine($"[{label}]");

                for (int seed = 1; seed <= seedCount; seed++)
                {
                    DungeonData data = DungeonGenerator.Generate(config, seed);
                    builder.Build(data);
                    total++;

                    if (Validate(builder, data, out string error))
                    {
                        sb.AppendLine($"  seed {seed}: OK（{data.RoomCount} 房间）");
                    }
                    else
                    {
                        failed++;
                        sb.AppendLine($"  seed {seed}: FAIL - {error}");
                        Debug.LogError($"[Dungeon] {label} seed {seed}: {error}");
                    }

                    builder.Clear();
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static bool Validate(DungeonBuilder builder, DungeonData data, out string error)
        {
            error = null;
            Vector2Int spacing = builder.RoomSpacing;
            Vector3 origin = builder.transform.position;

            if (builder.Rooms.Count != data.RoomCount)
            {
                error = $"房间数量不符：{builder.Rooms.Count} != {data.RoomCount}";
                return false;
            }

            // 只统计真正会挡住"门到门"通路的墙体碰撞体：
            //   - 门洞触发盒是 isTrigger，不阻挡移动
            //   - 封门板默认关闭（enabled = false），也不应计入
            //   - 木箱、立柱等房间装饰是有意摆放的掩体，不算断路
            var solidColliders = new List<BoxCollider2D>();
            foreach (BoxCollider2D collider in builder.GetComponentsInChildren<BoxCollider2D>(false))
            {
                if (!collider.enabled || collider.isTrigger) continue;
                if (collider.GetComponentInParent<RoomWall>() == null) continue;
                solidColliders.Add(collider);
            }

            BoxCollider2D[] colliders = solidColliders.ToArray();

            foreach (RoomNode node in data.Rooms)
            {
                RoomView view = builder.GetRoom(node.Grid);
                if (view == null)
                {
                    error = $"缺少房间 {node.Grid}";
                    return false;
                }

                if (view.RoomType != node.Type)
                {
                    error = $"房间 {node.Grid} 类型不符";
                    return false;
                }

                if (view.Doors != node.Doors)
                {
                    error = $"房间 {node.Grid} 门位不符";
                    return false;
                }

                Vector3 expected = origin + new Vector3(node.Grid.x * spacing.x, node.Grid.y * spacing.y, 0f);
                if ((view.transform.position - expected).sqrMagnitude > 1e-4f)
                {
                    error = $"房间 {node.Grid} 世界坐标偏移";
                    return false;
                }

                for (int i = 0; i < 4; i++)
                {
                    bool check = ValidateSide(node, view, (WallSide)i, out error);
                    if (!check) return false;
                }
            }

            return ValidatePassages(builder, data, colliders, out error);
        }

        private static bool ValidateSide(RoomNode node, RoomView view, WallSide side, out string error)
        {
            error = null;

            bool hasDoor = (node.Doors & side.ToDoor()) != 0;
            bool owned = RoomView.IsSideOwned(node, side);

            RoomWall wall = view.GetWall(side);
            if (wall == null)
            {
                error = $"房间 {node.Grid} 缺少 {side} 墙";
                return false;
            }

            if (wall.gameObject.activeSelf != owned)
            {
                error = $"房间 {node.Grid} {side} 墙归属错误（应为 {(owned ? "保留" : "隐藏")}）";
                return false;
            }

            if (owned && wall.IsDoorSegmentActive == hasDoor)
            {
                error = $"房间 {node.Grid} {side} 门洞状态错误（{(hasDoor ? "有门却被封死" : "无门却开了洞")}）";
                return false;
            }

            Transform marker = view.GetDoorMarker(side);
            if (marker != null && marker.gameObject.activeSelf != (owned && hasDoor))
            {
                error = $"房间 {node.Grid} {side} 门标记状态错误";
                return false;
            }

            Transform trigger = view.GetDoorTrigger(side);
            if (trigger != null && trigger.gameObject.activeSelf != hasDoor)
            {
                error = $"房间 {node.Grid} {side} 门洞触发盒状态错误（应 {(hasDoor ? "启用" : "禁用")}）";
                return false;
            }

            RoomDoorBlocker blocker = view.GetDoorBlocker(side);
            if (blocker != null)
            {
                if (blocker.gameObject.activeSelf != hasDoor)
                {
                    error = $"房间 {node.Grid} {side} 封门板状态错误（应 {(hasDoor ? "存在" : "隐藏")}）";
                    return false;
                }

                if (blocker.IsLocked)
                {
                    error = $"房间 {node.Grid} {side} 新建后不应处于锁死状态";
                    return false;
                }
            }

            Transform entry = view.GetEntryPoint(side);
            if (entry != null)
            {
                if (entry.gameObject.activeSelf != hasDoor)
                {
                    error = $"房间 {node.Grid} {side} 入口点状态错误";
                    return false;
                }

                if (hasDoor && !view.WorldRect.Contains(entry.position))
                {
                    error = $"房间 {node.Grid} {side} 入口点位于房间外";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 从每个房间中心向每个邻居中心画直线并采样，确认沿途没有任何启用的墙体碰撞体。
        /// 这是"相邻房间不断路 / 不被墙体封堵"的硬性检查。
        ///
        /// 注意：编辑模式下 Collider2D.bounds 依赖物理系统同步，刚实例化并移动过的对象
        /// 可能仍是旧值；因此这里直接用 transform + BoxCollider2D.size 解析计算世界矩形。
        /// </summary>
        private static bool ValidatePassages(DungeonBuilder builder, DungeonData data, BoxCollider2D[] colliders, out string error)
        {
            error = null;
            const int samples = 24;

            var rects = new Rect[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
                rects[i] = GetWorldRect(colliders[i]);

            foreach (RoomNode node in data.Rooms)
            {
                RoomView a = builder.GetRoom(node.Grid);

                foreach (RoomNode neighbor in node.Neighbors)
                {
                    // 每条无向边只检查一次
                    if (neighbor.Grid.x < node.Grid.x) continue;
                    if (neighbor.Grid.x == node.Grid.x && neighbor.Grid.y < node.Grid.y) continue;

                    RoomView b = builder.GetRoom(neighbor.Grid);
                    if (b == null)
                    {
                        error = $"缺少邻居房间 {neighbor.Grid}";
                        return false;
                    }

                    Vector2 from = a.WorldCenter;
                    Vector2 to = b.WorldCenter;

                    for (int s = 0; s <= samples; s++)
                    {
                        Vector2 point = Vector2.Lerp(from, to, s / (float)samples);

                        for (int c = 0; c < colliders.Length; c++)
                        {
                            if (!rects[c].Contains(point)) continue;

                            error = $"房间 {node.Grid} → {neighbor.Grid} 的通路被 " +
                                    $"{colliders[c].transform.parent.parent.name}/{colliders[c].name} 阻挡";
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>不依赖物理同步的世界空间 AABB（房间与墙体均无旋转）。</summary>
        private static Rect GetWorldRect(BoxCollider2D collider)
        {
            Vector3 scale = collider.transform.lossyScale;
            var size = new Vector2(
                Mathf.Abs(collider.size.x * scale.x),
                Mathf.Abs(collider.size.y * scale.y));

            Vector3 center = collider.transform.TransformPoint(collider.offset);
            return new Rect(center.x - size.x * 0.5f, center.y - size.y * 0.5f, size.x, size.y);
        }
    }
}
