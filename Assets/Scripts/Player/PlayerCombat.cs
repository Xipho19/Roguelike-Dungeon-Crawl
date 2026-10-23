using Roguelike.Audio;
using Roguelike.Combat;
using Roguelike.Items;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Player
{
    /// <summary>
    /// 玩家射击：鼠标瞄准，按住左键朝光标方向连发。
    ///
    /// 伤害、射速、弹速、射程、穿透、发数全部来自 PlayerStats，
    /// 因此道具只需要修改属性，不需要改这里。
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("射击")]
        [SerializeField] private TearProjectile tearPrefab;

        [Tooltip("没有 PlayerStats 时的回退值")]
        [SerializeField] private float fireInterval = 0.24f;

        [SerializeField] private float tearSpeed = 14f;
        [SerializeField] private float tearDamage = 1f;
        [SerializeField] private float tearLifetime = 1f;

        [Tooltip("多发时每发之间的夹角（度）")]
        [SerializeField] private float spreadStep = 8f;

        [Tooltip("眼泪生成点距离玩家中心的距离")]
        [SerializeField] private float spawnOffset = 0.8f;

        private Camera _camera;
        private PlayerStats _stats;
        private float _nextFireTime;

        private void Awake()
        {
            _camera = Camera.main;
            _stats = GetComponent<PlayerStats>();
        }

        private void Start()
        {
            if (tearPrefab == null)
                tearPrefab = Resources.Load<TearProjectile>("Combat/Tear");
        }

        private void Update()
        {
            if (!TryReadAim(out Vector2 direction)) return;
            Fire(direction);
        }

        /// <summary>朝给定方向发射，成功返回 true（受射速间隔限制）。</summary>
        public bool Fire(Vector2 direction)
        {
            if (Time.time < _nextFireTime) return false;
            if (tearPrefab == null || direction.sqrMagnitude < 0.0001f) return false;

            direction.Normalize();

            float interval = _stats != null ? _stats.FireInterval : fireInterval;
            float damage = _stats != null ? _stats.Damage : tearDamage;
            float speed = _stats != null ? _stats.TearSpeed : tearSpeed;
            float lifetime = _stats != null ? _stats.TearLifetime : tearLifetime;
            int pierce = _stats != null ? _stats.Pierce : 0;
            int shots = _stats != null ? _stats.ShotCount : 1;

            _nextFireTime = Time.time + interval;
            AudioService.Play("tear_shot", 0.55f);

            float startAngle = -(shots - 1) * spreadStep * 0.5f;

            for (int i = 0; i < shots; i++)
            {
                Vector2 shotDirection = Rotate(direction, startAngle + i * spreadStep);
                Vector2 spawnPosition = (Vector2)transform.position + shotDirection * spawnOffset;

                TearProjectile tear = Instantiate(tearPrefab, spawnPosition, Quaternion.identity);
                tear.Initialize(shotDirection, speed, damage, lifetime, pierce);
            }

            return true;
        }

        private bool TryReadAim(out Vector2 direction)
        {
            direction = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                return TryScreenToDirection(mouse.position.ReadValue(), out direction);
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButton(0))
            {
                return TryScreenToDirection(Input.mousePosition, out direction);
            }
#endif

            return false;
        }

        private bool TryScreenToDirection(Vector3 screenPosition, out Vector2 direction)
        {
            direction = Vector2.zero;

            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return false;

            Vector3 world = _camera.ScreenToWorldPoint(screenPosition);
            direction = (Vector2)world - (Vector2)transform.position;
            return direction.sqrMagnitude > 0.0001f;
        }

        private static Vector2 Rotate(Vector2 vector, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                vector.x * cos - vector.y * sin,
                vector.x * sin + vector.y * cos);
        }
    }
}
