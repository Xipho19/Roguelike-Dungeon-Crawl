using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 眼泪与敌方弹丸的轻量对象池。
    /// 复用同一个 Prefab 的实例，避免高频射击时反复 Instantiate/Destroy。
    /// </summary>
    public static class ProjectilePool
    {
        private static readonly Dictionary<GameObject, Stack<GameObject>> Pools =
            new Dictionary<GameObject, Stack<GameObject>>();

        private static readonly Dictionary<GameObject, GameObject> PrefabByInstance =
            new Dictionary<GameObject, GameObject>();

        private static Transform _root;

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            if (prefab == null) return null;

            EnsureRoot();
            GameObject prefabObject = prefab.gameObject;

            if (Pools.TryGetValue(prefabObject, out Stack<GameObject> stack))
            {
                while (stack.Count > 0)
                {
                    GameObject instance = stack.Pop();
                    if (instance == null) continue;

                    instance.SetActive(true);
                    instance.transform.SetPositionAndRotation(position, rotation);
                    T component = instance.GetComponent<T>();
                    if (component != null) return component;

                    Object.Destroy(instance);
                }
            }

            GameObject created = Object.Instantiate(prefabObject, position, rotation, _root);
            created.name = prefabObject.name + " (Pooled)";
            PrefabByInstance[created] = prefabObject;
            return created.GetComponent<T>();
        }

        public static void Release(Component instance)
        {
            if (instance == null) return;

            GameObject go = instance.gameObject;
            if (!PrefabByInstance.TryGetValue(go, out GameObject prefab) || prefab == null)
            {
                Object.Destroy(go);
                return;
            }

            go.SetActive(false);
            go.transform.SetParent(_root, false);

            if (!Pools.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                Pools[prefab] = stack;
            }

            stack.Push(go);
        }

        /// <summary>清理整池；换层、重开或场景重置时调用。</summary>
        public static void ResetAll()
        {
            Pools.Clear();
            PrefabByInstance.Clear();

            if (_root != null)
                Object.Destroy(_root.gameObject);

            _root = null;
        }

        private static void EnsureRoot()
        {
            if (_root != null) return;

            var root = new GameObject("[ProjectilePool]");
            Object.DontDestroyOnLoad(root);
            _root = root.transform;
        }
    }
}
