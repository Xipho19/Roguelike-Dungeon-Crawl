using Roguelike.Dungeon;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Roguelike.EditorTools
{
    /// <summary>
    /// 一键把房间图构建到当前场景。
    /// 菜单：Tools / Roguelike / 构建地牢到当前场景
    ///
    /// 会创建（或复用）名为 "Dungeon" 的根对象，挂上 DungeonBuilder，
    /// 构建完成后把主相机对准整层地牢，方便直接观察。
    /// </summary>
    public static class DungeonSceneMenu
    {
        private const string RootName = "Dungeon";

        [MenuItem("Tools/Roguelike/构建地牢到当前场景")]
        public static void BuildIntoScene()
        {
            RoomView prefab = AssetDatabase.LoadAssetAtPath<RoomView>(RoomPrefabFactory.PrefabPath);
            if (prefab == null)
                prefab = RoomPrefabFactory.Rebuild();

            if (prefab == null)
            {
                Debug.LogError("[Dungeon] 房间 Prefab 不可用，构建中止。");
                return;
            }

            GameObject root = GameObject.Find(RootName);
            if (root == null) root = new GameObject(RootName);

            var builder = root.GetComponent<DungeonBuilder>();
            if (builder == null) builder = root.AddComponent<DungeonBuilder>();

            var serialized = new SerializedObject(builder);
            SerializedProperty prefabProperty = serialized.FindProperty("roomPrefab");
            if (prefabProperty != null)
            {
                prefabProperty.objectReferenceValue = prefab;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            DungeonData data = builder.BuildFromSeed();
            FrameMainCamera(data);

            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;

            Debug.Log($"[Dungeon] 已构建 {data.RoomCount} 个房间（seed={data.Seed}，最远 {data.MaxDistance} 步）。");
        }

        private static void FrameMainCamera(DungeonData data)
        {
            Camera camera = Camera.main;
            if (camera == null) return;

            RectInt bounds = data.GetBounds();
            Vector2Int spacing = RoomGeometry.Spacing;

            float minX = bounds.xMin * spacing.x;
            float minY = bounds.yMin * spacing.y;
            float maxX = (bounds.xMax - 1) * spacing.x + RoomGeometry.FootprintWidth;
            float maxY = (bounds.yMax - 1) * spacing.y + RoomGeometry.FootprintHeight;

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            float width = maxX - minX;
            float height = maxY - minY;

            const float padding = 2f;
            float aspect = camera.aspect > 0.01f ? camera.aspect : 16f / 9f;
            float size = Mathf.Max(height * 0.5f, width * 0.5f / aspect) + padding;

            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.transform.position = new Vector3(centerX, centerY, -10f);
        }
    }
}
