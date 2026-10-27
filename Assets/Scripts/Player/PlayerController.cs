using Roguelike.Items;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Player
{
    /// <summary>
    /// 玩家移动控制。
    ///
    /// 使用 Dynamic Rigidbody2D + 手动速度控制：
    ///   - 重力为 0、锁定旋转，因此墙体碰撞由物理系统负责；
    ///   - 输入带加速度/减速度，避免"瞬间满速"的塑料感；
    ///   - 视角固定、无旋转，后续可直接接入击退、冲刺等状态。
    ///
    /// 输入优先使用新 Input System（WASD / 方向键）；若工程切回旧输入管理器则自动回退。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("移动")]
        [Tooltip("最大移动速度（单位/秒）")]
        [SerializeField] private float moveSpeed = 6f;

        [Tooltip("加速率（单位/秒²）")]
        [SerializeField] private float acceleration = 70f;

        [Tooltip("减速率（单位/秒²）")]
        [SerializeField] private float deceleration = 90f;

        private Rigidbody2D _rb;
        private PlayerStats _stats;
        private float _slowFactor = 1f;
        private float _slowUntil;
        private float _hasteFactor = 1f;
        private float _hasteUntil;

        /// <summary>当前物理速度，供门触发判断移动方向。</summary>
        public Vector2 Velocity => _rb != null ? _rb.linearVelocity : Vector2.zero;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _stats = GetComponent<PlayerStats>();
        }

        private void OnEnable()
        {
            ClearSlow();
            ClearSpeedBoost();
        }

        private void FixedUpdate()
        {
            Vector2 input = ReadMoveInput();
            if (input.sqrMagnitude > 1f) input = input.normalized;

            float speed = _stats != null ? _stats.MoveSpeed : moveSpeed;
            if (Time.time < _slowUntil) speed *= _slowFactor;
            if (Time.time < _hasteUntil) speed *= _hasteFactor;

            Vector2 target = input * speed;
            float rate = input.sqrMagnitude > 0.001f ? acceleration : deceleration;

            _rb.linearVelocity = Vector2.MoveTowards(_rb.linearVelocity, target, rate * Time.fixedDeltaTime);
        }

        /// <summary>地面冰面等效果：按比例降低移动速度一段时间。</summary>
        public void ApplySlow(float factor, float duration)
        {
            if (duration <= 0f) return;

            _slowFactor = Mathf.Clamp01(factor);
            _slowUntil = Mathf.Max(_slowUntil, Time.time + duration);
        }

        public void ClearSlow()
        {
            _slowFactor = 1f;
            _slowUntil = 0f;
        }

        /// <summary>击杀触发等临时加速：按比例提高移动速度一段时间。</summary>
        public void ApplySpeedBoost(float factor, float duration)
        {
            if (duration <= 0f || factor <= 1f) return;

            _hasteFactor = Mathf.Max(_hasteFactor, factor);
            _hasteUntil = Mathf.Max(_hasteUntil, Time.time + duration);
        }

        public void ClearSpeedBoost()
        {
            _hasteFactor = 1f;
            _hasteUntil = 0f;
        }

        private static Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float x = 0f;
                float y = 0f;

                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

                return new Vector2(x, y);
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#else
            return Vector2.zero;
#endif
        }
    }
}
