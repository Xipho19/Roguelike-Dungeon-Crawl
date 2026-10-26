using System.Collections;
using System.Collections.Generic;
using Roguelike.Audio;
using Roguelike.Dungeon;
using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>
    /// 基础追击型敌人。
    ///
    /// 行为：
    ///   - 朝玩家移动，并叠加"分离转向"避免同类挤成一团；
    ///   - 出生时有一个短促的放大动画；
    ///   - 接触玩家造成伤害，自身有接触冷却；
    ///   - 血量归零时通知 DungeonFlowController，由它处理掉落与房间清空。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour
    {
        [Header("属性")]
        [SerializeField] private float maxHealth = 2f;
        [SerializeField] protected float moveSpeed = 2.4f;
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private float contactCooldown = 0.8f;

        [Header("分离转向")]
        [Tooltip("与其它敌人的排斥半径")]
        [SerializeField] private float separationRadius = 0.95f;

        [Tooltip("排斥力强度")]
        [SerializeField] private float separationStrength = 2.2f;

        [Header("障碍规避")]
        [Tooltip("在身体半径之外额外探测的距离")]
        [SerializeField] private float avoidanceLookAhead = 0.35f;

        [Tooltip("绕障时每次尝试偏离目标方向的角度")]
        [SerializeField] private float avoidanceAngleStep = 25f;

        [Tooltip("卡住检测间隔（秒）")]
        [SerializeField] private float stuckCheckInterval = 0.6f;

        [Header("出生动画")]
        [SerializeField] private float spawnDuration = 0.22f;

        [Header("引用")]
        [SerializeField] private SpriteRenderer body;

        private static readonly List<Enemy> ActiveEnemies = new List<Enemy>();

        protected Rigidbody2D _rb;
        protected Transform _target;
        protected float _health;
        private float _nextContactTime;
        private float _targetScale = 1f;
        private float _knockbackUntil;
        protected Color _baseColor = Color.white;
        protected DungeonFlowController _flow;

        private float _baseAvoidRadius;
        private int _detourSign;
        private Vector2 _detourDirection;
        private float _detourUntil;
        private Vector2 _sidestepDirection;
        private float _sidestepUntil;
        private Vector2 _stuckLastPosition;
        private float _stuckNextCheck;
        private float _lastSteerTime;

        public float Health => _health;
        public float MaxHealth => maxHealth;
        public bool IsDead => _health <= 0f;
        public bool IsBoss { get; private set; }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _health = maxHealth;

            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (body != null) _baseColor = body.color;
        }

        private void OnEnable()
        {
            if (!ActiveEnemies.Contains(this)) ActiveEnemies.Add(this);
        }

        private void OnDisable()
        {
            ActiveEnemies.Remove(this);
        }

        protected virtual void Start()
        {
            var tracker = FindFirstObjectByType<PlayerRoomTracker>();
            if (tracker != null) _target = tracker.transform;
        }

        /// <summary>由生成方写入本次遭遇战的参数。</summary>
        public virtual void Initialize(float health, float speed, float scale, Color tint, bool isBoss, DungeonFlowController flow)
        {
            maxHealth = health;
            _health = health;
            moveSpeed = speed;
            _targetScale = scale;
            IsBoss = isBoss;
            _flow = flow;

            if (body != null)
            {
                body.color = tint;
                _baseColor = tint;
            }

            transform.localScale = Vector3.one * (scale * 0.1f);
            if (isActiveAndEnabled) StartCoroutine(SpawnRoutine());
            else transform.localScale = Vector3.one * scale;
        }

        private IEnumerator SpawnRoutine()
        {
            float elapsed = 0f;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime / Mathf.Max(0.01f, spawnDuration);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed));
                transform.localScale = Vector3.one * Mathf.Lerp(_targetScale * 0.1f, _targetScale, k);
                yield return null;
            }

            transform.localScale = Vector3.one * _targetScale;
        }

        protected virtual void FixedUpdate()
        {
            if (IsDead || _target == null) return;
            if (Time.time < _knockbackUntil) return; // 击退期间让物理接管

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            Vector2 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;

            Vector2 steered = GetSteeredDirection(direction);
            Vector2 velocity = steered * moveSpeed + ComputeSeparation() * separationStrength;
            _rb.linearVelocity = Vector2.ClampMagnitude(velocity, moveSpeed * 1.6f);
        }

        /// <summary>与附近敌人保持距离，避免全部堆在玩家身上。</summary>
        private Vector2 ComputeSeparation()
        {
            Vector2 push = Vector2.zero;
            Vector2 position = _rb.position;

            for (int i = 0; i < ActiveEnemies.Count; i++)
            {
                Enemy other = ActiveEnemies[i];
                if (other == null || other == this) continue;

                Vector2 offset = position - (Vector2)other.transform.position;
                float distance = offset.magnitude;

                if (distance > 0.0001f && distance < separationRadius)
                    push += offset / distance * (1f - distance / separationRadius);
            }

            return push;
        }

        /// <summary>
        /// 朝目标方向移动时绕开静态障碍：
        /// 优先走直线；被挡住时沿障碍边缘持续绕行，直到能重新直视目标。
        /// 只把静态碰撞体（墙、木箱、立柱）当作障碍，动态角色和子弹会被忽略。
        /// </summary>
        protected Vector2 GetSteeredDirection(Vector2 desired)
        {
            if (_rb == null) return Vector2.zero;
            if (desired.sqrMagnitude < 0.0001f) return Vector2.zero;

            desired.Normalize();
            float now = Time.time;

            UpdateStuckRecovery(desired);

            float radius = GetAvoidanceRadius();
            float look = radius + Mathf.Max(0.05f, avoidanceLookAhead);

            // 能直接看到目标：结束绕行
            if (IsPathToTargetClear(radius))
            {
                _detourSign = 0;
                _detourUntil = 0f;
                _sidestepUntil = 0f;
                return desired;
            }

            // 卡住时短暂侧移，帮助脱离死角
            if (now < _sidestepUntil)
            {
                Vector2 blended = desired + _sidestepDirection * 0.8f;
                if (blended.sqrMagnitude > 0.0001f) desired = blended.normalized;
            }

            // 正在绕行：沿障碍边缘走，直到目标重新可见
            if (now < _detourUntil && _detourDirection.sqrMagnitude > 0.0001f)
            {
                if (IsDirectionClear(_detourDirection, radius, look)) return _detourDirection;

                for (int i = 1; i <= 3; i++)
                {
                    Vector2 candidate = Rotate(_detourDirection, avoidanceAngleStep * _detourSign * i);
                    if (IsDirectionClear(candidate, radius, look))
                    {
                        _detourDirection = candidate.normalized;
                        return _detourDirection;
                    }
                }

                _detourSign = -_detourSign;
                _detourUntil = 0f;
            }

            // 选择新的绕行方向：沿阻挡物的法线取切线
            if (!TryGetBlockingNormal(desired, radius, look, out Vector2 normal))
                return desired;

            Vector2 tangent = new Vector2(-normal.y, normal.x);
            int sign = ChooseDetourSign(tangent, radius, look);
            _detourSign = sign;
            _detourDirection = (tangent * sign).normalized;
            _detourUntil = now + 0.5f;
            return _detourDirection;
        }

        private void UpdateStuckRecovery(Vector2 desired)
        {
            float now = Time.time;

            // 蓄力/冲锋等长时间没有调用转向时，重置检测基准，避免刚恢复追击就误判卡住
            if (now - _lastSteerTime > stuckCheckInterval)
            {
                _stuckLastPosition = _rb.position;
                _stuckNextCheck = now + stuckCheckInterval;
                _sidestepUntil = 0f;
            }
            _lastSteerTime = now;

            if (now < _stuckNextCheck) return;

            Vector2 position = _rb.position;
            bool barelyMoved = (position - _stuckLastPosition).sqrMagnitude < 0.12f * 0.12f;

            if (barelyMoved)
            {
                // 卡住时换一边绕，避免一直顶着同一个角
                _detourSign = _detourSign == 0 ? 1 : -_detourSign;
                _detourUntil = 0f;

                Vector2 perpendicular = new Vector2(-desired.y, desired.x);
                if (Random.value < 0.5f) perpendicular = -perpendicular;

                _sidestepDirection = perpendicular;
                _sidestepUntil = now + 0.35f;
            }

            _stuckLastPosition = position;
            _stuckNextCheck = now + stuckCheckInterval;
        }

        /// <summary>用自身碰撞体半径做障碍探测；Boss 的碰撞体更大，会自动用更大的半径。</summary>
        private float GetAvoidanceRadius()
        {
            if (_baseAvoidRadius <= 0f)
            {
                CircleCollider2D circle = GetComponent<CircleCollider2D>();
                _baseAvoidRadius = circle != null ? circle.radius : 0.4f;
            }

            Vector3 scale = transform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
            return Mathf.Max(0.12f, _baseAvoidRadius * maxScale * 0.95f);
        }

        /// <summary>身体沿该方向前进时是否会撞到静态障碍。</summary>
        private bool IsDirectionClear(Vector2 direction, float radius, float distance)
        {
            RaycastHit2D[] hits = Physics2D.CircleCastAll(_rb.position, radius, direction, distance);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D collider = hits[i].collider;
                if (collider == null || collider.isTrigger) continue;

                // 动态物体（玩家、其它敌人、子弹、掉落物）交给分离/碰撞处理
                if (collider.attachedRigidbody != null) continue;

                return false;
            }

            return true;
        }

        /// <summary>身体沿直线走向目标是否畅通（用于判断何时结束绕行）。</summary>
        private bool IsPathToTargetClear(float radius)
        {
            if (_target == null) return false;

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float distance = toTarget.magnitude;
            if (distance < 0.001f) return true;

            return IsDirectionClear(toTarget / distance, radius, distance);
        }

        /// <summary>找到前方第一个静态障碍的法线，用于沿边缘绕行。</summary>
        private bool TryGetBlockingNormal(Vector2 desired, float radius, float look, out Vector2 normal)
        {
            normal = Vector2.zero;

            RaycastHit2D[] hits = Physics2D.CircleCastAll(_rb.position, radius, desired, look);
            if (TryPickStaticNormal(hits, out normal)) return true;

            if (_target == null) return false;

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float distance = toTarget.magnitude;
            if (distance < 0.001f) return false;

            hits = Physics2D.CircleCastAll(_rb.position, radius, toTarget / distance, distance);
            return TryPickStaticNormal(hits, out normal);
        }

        private static bool TryPickStaticNormal(RaycastHit2D[] hits, out Vector2 normal)
        {
            normal = Vector2.zero;
            float nearest = float.MaxValue;
            bool found = false;

            for (int i = 0; i < hits.Length; i++)
            {
                Collider2D collider = hits[i].collider;
                if (collider == null || collider.isTrigger) continue;
                if (collider.attachedRigidbody != null) continue;
                if (hits[i].distance >= nearest) continue;

                nearest = hits[i].distance;
                normal = hits[i].normal;
                found = true;
            }

            return found;
        }

        /// <summary>在障碍切线两个方向中选一个能走、且更接近目标的一侧。</summary>
        private int ChooseDetourSign(Vector2 tangent, float radius, float look)
        {
            Vector2 forward = tangent.normalized;
            Vector2 backward = -forward;

            bool forwardClear = IsDirectionClear(forward, radius, look);
            bool backwardClear = IsDirectionClear(backward, radius, look);

            if (forwardClear && !backwardClear) return 1;
            if (backwardClear && !forwardClear) return -1;
            if (!forwardClear && !backwardClear) return _detourSign != 0 ? _detourSign : 1;

            // 两侧都能走时，保持当前绕行方向，避免在目标高度附近左右摇摆
            if (_detourSign != 0) return _detourSign;
            if (_target == null) return 1;

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            float dot = Vector2.Dot(forward, toTarget);
            if (Mathf.Abs(dot) > 0.05f) return dot > 0f ? 1 : -1;

            return _detourSign != 0 ? _detourSign : 1;
        }

        private static Vector2 Rotate(Vector2 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            _health -= amount;
            AudioService.Play("metalClick", 0.35f);

            if (_health > 0f) FlashHit();
            else Die();
        }

        /// <summary>受击击退：短暂让出速度控制权，由物理把敌人推开。</summary>
        public virtual void ApplyKnockback(Vector2 direction, float force)
        {
            if (IsDead || _rb == null || direction.sqrMagnitude < 0.0001f) return;

            _rb.linearVelocity = direction.normalized * force;
            _knockbackUntil = Time.time + 0.12f;
        }

        private void FlashHit()
        {
            if (body == null) return;

            StopCoroutine(nameof(HitFlashRoutine));
            StartCoroutine(nameof(HitFlashRoutine));
        }

        private System.Collections.IEnumerator HitFlashRoutine()
        {
            body.color = new Color(1f, 0.55f, 0.55f, 1f);
            yield return new WaitForSeconds(0.07f);
            if (body != null) body.color = _baseColor;
        }

        /// <summary>死亡结算；子类可以重写以加入分裂、爆炸等效果，再调用 base.Die()。</summary>
        protected virtual void Die()
        {
            _health = 0f;
            AudioService.Play("enemy_pop", 0.65f);

            if (_flow != null) _flow.NotifyEnemyDied(this);
            Destroy(gameObject);
        }

        protected virtual void OnCollisionStay2D(Collision2D collision)
        {
            if (IsDead || Time.time < _nextContactTime) return;

            var health = collision.collider.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            if (health.TryDamage(contactDamage))
                _nextContactTime = Time.time + contactCooldown;
        }
    }
}
