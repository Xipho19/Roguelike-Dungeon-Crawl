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

        [Tooltip("战斗开始后的短暂接触伤害保护，给玩家反应时间")]
        [SerializeField] private float combatContactGrace = 0.5f;

        [Header("分离转向")]
        [Tooltip("与其它敌人的排斥半径")]
        [SerializeField] private float separationRadius = 0.95f;

        [Tooltip("排斥力强度")]
        [SerializeField] private float separationStrength = 2.2f;

        [Header("障碍规避")]
        [Tooltip("在身体半径之外额外探测的距离")]
        [SerializeField] private float avoidanceLookAhead = 0.35f;

        [Tooltip("绕障时每次尝试偏离目标方向的角度")]
        [SerializeField] private float avoidanceAngleStep = 15f;

        [Tooltip("卡住检测间隔（秒）")]
        [SerializeField] private float stuckCheckInterval = 0.6f;

        [Header("出生动画")]
        [SerializeField] private float spawnDuration = 0.22f;

        [Header("待机游荡")]
        [Tooltip("未进入战斗时在房间内游走的速度比例")]
        [SerializeField] private float wanderSpeedFactor = 0.35f;

        [Tooltip("更换游走目标的最短/最长间隔（秒）")]
        [SerializeField] private float wanderIntervalMin = 1.5f;
        [SerializeField] private float wanderIntervalMax = 3.5f;

        [Header("引用")]
        [SerializeField] private SpriteRenderer body;

        private static readonly List<Enemy> ActiveEnemies = new List<Enemy>();
        private static readonly Color PoisonTint = new Color(0.55f, 1f, 0.6f, 1f);

        /// <summary>当前场景中所有存活的敌人（供追踪眼泪查找目标）。</summary>
        public static IReadOnlyList<Enemy> AllActiveEnemies => ActiveEnemies;

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
        private float _detourUntil;
        private Vector2 _sidestepDirection;
        private float _sidestepUntil;
        private Vector2 _stuckLastPosition;
        private float _stuckNextCheck;
        private float _lastSteerTime;

        private RoomView _room;
        private Vector2 _wanderTarget;
        private float _wanderNextTime;

        private float _poisonDamage;
        private int _poisonTicks;
        private float _poisonInterval = 0.6f;
        private float _nextPoisonTick;

        public float Health => _health;
        public float MaxHealth => maxHealth;
        public bool IsDead => _health <= 0f;
        public bool IsBoss { get; private set; }
        public bool IsElite { get; private set; }
        public bool CombatActive { get; private set; }
        public RoomView Room => _room;

        /// <summary>精英怪的攻击倍率；普通敌人为 1。</summary>
        protected float DamageMultiplier { get; private set; } = 1f;

        private int _floorDamageBonus;

        /// <summary>由生成方写入当前楼层的固定攻击加成。</summary>
        public void SetFloorDamageBonus(int bonus)
        {
            _floorDamageBonus = Mathf.Max(0, bonus);
        }

        /// <summary>直接设置接触伤害的基础值（Boss 按楼层伤害使用）。</summary>
        public void SetContactDamage(int value)
        {
            contactDamage = Mathf.Max(0, value);
        }

        /// <summary>标记为精英：攻击力翻倍，并挂上描边/光效。</summary>
        public void MakeElite(float damageMultiplier = 1.2f)
        {
            if (IsElite || IsBoss) return;

            IsElite = true;
            DamageMultiplier = Mathf.Max(1f, damageMultiplier);
            EliteVisual.Attach(this);
        }

        /// <summary>基础伤害 + 楼层加成，再按精英倍率放大。</summary>
        protected int ScaleAttackDamage(int baseDamage)
        {
            int damage = Mathf.Max(1, baseDamage + _floorDamageBonus);
            return Mathf.Max(1, Mathf.CeilToInt(damage * DamageMultiplier));
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            _health = maxHealth;

            _room = GetComponentInParent<RoomView>();
            _wanderTarget = transform.position;

            if (body == null) body = GetComponentInChildren<SpriteRenderer>();
            if (body != null) _baseColor = body.color;
        }

        protected virtual void OnEnable()
        {
            if (!ActiveEnemies.Contains(this)) ActiveEnemies.Add(this);
        }

        protected virtual void OnDisable()
        {
            ActiveEnemies.Remove(this);
        }

        private void Update()
        {
            if (!CombatActive || IsDead || _poisonTicks <= 0) return;
            if (Time.time < _nextPoisonTick) return;

            TickPoison();
        }

        private void TickPoison()
        {
            if (IsDead || _poisonTicks <= 0) return;

            _poisonTicks--;
            _health -= _poisonDamage;
            _nextPoisonTick = Time.time + _poisonInterval;

            // 中毒期间保持绿色状态；最后一跳结束时恢复原色
            if (body != null) body.color = _poisonTicks > 0 ? PoisonTint : _baseColor;

            if (_health <= 0f) Die();
        }

        protected virtual void Start()
        {
            if (_room == null) _room = GetComponentInParent<RoomView>();

            var tracker = FindFirstObjectByType<PlayerRoomTracker>();
            if (tracker != null) _target = tracker.transform;
        }

        /// <summary>玩家进入房间时激活战斗；预生成的敌人在此之前只在房间里游荡。</summary>
        public void ActivateCombat()
        {
            if (CombatActive || IsDead) return;

            CombatActive = true;
            _nextContactTime = Mathf.Max(_nextContactTime, Time.time + combatContactGrace);
            OnCombatStarted();
        }

        protected virtual void OnCombatStarted()
        {
        }

        /// <summary>由生成方写入本次遭遇战的参数。</summary>
        public virtual void Initialize(
            float health,
            float speed,
            float scale,
            Color tint,
            bool isBoss,
            DungeonFlowController flow,
            bool playSpawnAnimation = true)
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

            if (playSpawnAnimation && isActiveAndEnabled)
            {
                transform.localScale = Vector3.one * (scale * 0.1f);
                StartCoroutine(SpawnRoutine());
            }
            else
            {
                transform.localScale = Vector3.one * scale;
            }
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
            if (IsDead || _rb == null) return;

            if (!CombatActive)
            {
                UpdatePassiveBehavior();
                return;
            }

            if (_target == null) return;
            if (Time.time < _knockbackUntil) return; // 击退期间让物理接管

            Vector2 toTarget = (Vector2)_target.position - _rb.position;
            Vector2 direction = toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector2.zero;

            Vector2 steered = GetSteeredDirection(direction);
            Vector2 velocity = steered * moveSpeed + ComputeSeparation() * separationStrength;
            _rb.linearVelocity = Vector2.ClampMagnitude(velocity, moveSpeed * 1.6f);
        }

        /// <summary>未进入战斗时的行为：默认在房间内自由走动。</summary>
        protected virtual void UpdatePassiveBehavior()
        {
            UpdateIdleWander();
        }

        /// <summary>在房间内部随机选点缓慢移动，让玩家在门外也能看到巡逻的怪物。</summary>
        private void UpdateIdleWander()
        {
            if (_rb == null) return;

            Vector2 position = _rb.position;
            Rect interior = _room != null
                ? _room.InteriorRect
                : new Rect(position.x - 1.5f, position.y - 1.5f, 3f, 3f);

            float now = Time.time;

            // 被其它敌人挤出房间时，先往房间中心走回去
            if (position.x < interior.xMin + 0.2f || position.x > interior.xMax - 0.2f ||
                position.y < interior.yMin + 0.2f || position.y > interior.yMax - 0.2f)
            {
                _wanderTarget = interior.center;
                _wanderNextTime = now + 1.2f;
            }
            else if (now >= _wanderNextTime ||
                     (_wanderTarget - position).sqrMagnitude < 0.25f)
            {
                _wanderTarget = PickWanderTarget(interior);
                _wanderNextTime = now + Random.Range(wanderIntervalMin, wanderIntervalMax);
            }

            Vector2 toTarget = _wanderTarget - position;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 steered = GetAvoidanceDirection(toTarget.normalized);
            Vector2 velocity = steered * (moveSpeed * wanderSpeedFactor) + ComputeSeparation() * 0.6f;
            _rb.linearVelocity = Vector2.ClampMagnitude(velocity, moveSpeed * Mathf.Max(0.2f, wanderSpeedFactor * 1.6f));
        }

        private Vector2 PickWanderTarget(Rect interior)
        {
            // 有一定概率走到门口附近，方便玩家在门外看到房间里的情况
            if (_room != null && Random.value < 0.45f)
            {
                var side = (WallSide)Random.Range(0, 4);
                if (_room.HasDoor(side))
                {
                    Transform entry = _room.GetEntryPoint(side);
                    if (entry != null)
                    {
                        Vector2 nearDoor = (Vector2)entry.position + Random.insideUnitCircle * 1.2f;
                        return ClampToInterior(nearDoor, interior);
                    }
                }
            }

            return RandomPointInInterior(interior);
        }

        private static Vector2 RandomPointInInterior(Rect interior)
        {
            const float margin = 0.75f;
            float minX = interior.xMin + margin;
            float maxX = interior.xMax - margin;
            float minY = interior.yMin + margin;
            float maxY = interior.yMax - margin;

            if (maxX <= minX) minX = maxX = interior.center.x;
            if (maxY <= minY) minY = maxY = interior.center.y;

            return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        }

        private static Vector2 ClampToInterior(Vector2 point, Rect interior)
        {
            const float margin = 0.75f;
            float minX = interior.xMin + margin;
            float maxX = interior.xMax - margin;
            float minY = interior.yMin + margin;
            float maxY = interior.yMax - margin;

            if (maxX <= minX) point.x = interior.center.x;
            else point.x = Mathf.Clamp(point.x, minX, maxX);

            if (maxY <= minY) point.y = interior.center.y;
            else point.y = Mathf.Clamp(point.y, minY, maxY);

            return point;
        }

        /// <summary>只做局部绕障，不把玩家当作寻路目标；用于待机游荡。</summary>
        protected Vector2 GetAvoidanceDirection(Vector2 desired)
        {
            if (_rb == null) return Vector2.zero;
            if (desired.sqrMagnitude < 0.0001f) return Vector2.zero;

            desired.Normalize();
            float radius = GetAvoidanceRadius();
            float look = radius + Mathf.Max(0.05f, avoidanceLookAhead);

            if (IsDirectionClear(desired, radius, look)) return desired;

            for (int i = 1; i <= 6; i++)
            {
                Vector2 candidate = Rotate(desired, avoidanceAngleStep * i);
                if (IsDirectionClear(candidate, radius, look)) return candidate;

                candidate = Rotate(desired, -avoidanceAngleStep * i);
                if (IsDirectionClear(candidate, radius, look)) return candidate;
            }

            return -desired;
        }

        /// <summary>与同房间的附近敌人保持距离，避免全部堆在玩家身上。</summary>
        protected Vector2 ComputeSeparation()
        {
            Vector2 push = Vector2.zero;
            Vector2 position = _rb.position;

            for (int i = 0; i < ActiveEnemies.Count; i++)
            {
                Enemy other = ActiveEnemies[i];
                if (other == null || other == this) continue;
                if (other.Room != _room) continue;

                Vector2 offset = position - (Vector2)other.transform.position;
                float distance = offset.magnitude;

                if (distance > 0.0001f && distance < separationRadius)
                    push += offset / distance * (1f - distance / separationRadius);
            }

            return push;
        }

        /// <summary>
        /// 朝目标方向移动时绕开静态障碍：
        /// 优先走直线；被挡住时在偏转一定角度的方向里找一个能走的，
        /// 斜着绕过障碍的边角，而不是贴着障碍面来回滑动。
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

            // 正在绕行时先走完这段承诺，避免刚让开一点就回头撞上同一个障碍，
            // 在障碍边缘反复上下/左右抖动。
            bool committed = now < _detourUntil;

            if (!committed && IsPathToTargetClear(radius))
            {
                _detourSign = 0;
                _detourUntil = 0f;
                _sidestepUntil = 0f;
                return desired;
            }

            if (_detourSign == 0)
                _detourSign = ChooseDetourSide(desired, radius, look);

            if (!committed)
                _detourUntil = now + 0.35f;

            // 从目标方向开始，向绕行侧逐步加大偏角，取第一个短距离畅通的方向。
            // 大多数情况下 15°~45° 就能斜着绕过去，路线更自然。
            for (int i = 1; i <= 6; i++)
            {
                Vector2 candidate = Rotate(desired, avoidanceAngleStep * i * _detourSign);
                if (!IsDirectionClear(candidate, radius, look)) continue;

                // 稍微朝目标回拉，避免绕行轨迹过于外扩
                Vector2 blended = (candidate + desired * 0.35f).normalized;
                return IsDirectionClear(blended, radius, look * 0.8f) ? blended : candidate;
            }

            // 当前一侧完全堵死，换另一侧
            _detourSign = -_detourSign;
            for (int i = 1; i <= 6; i++)
            {
                Vector2 candidate = Rotate(desired, avoidanceAngleStep * i * _detourSign);
                if (IsDirectionClear(candidate, radius, look)) return candidate;
            }

            // 四面都被挡住：如果刚判定卡住，先沿随机侧移方向脱困
            if (now < _sidestepUntil && _sidestepDirection.sqrMagnitude > 0.0001f)
            {
                Vector2 sidestep = _sidestepDirection.normalized;
                if (IsDirectionClear(sidestep, radius, look)) return sidestep;
            }

            // 否则沿绕行侧横移，至少不要顶死在障碍上
            return Rotate(desired, 90f * _detourSign);
        }

        /// <summary>两侧都可走时固定选一侧，避免每帧左右摇摆。</summary>
        private int ChooseDetourSide(Vector2 desired, float radius, float look)
        {
            Vector2 left = Rotate(desired, 90f);
            Vector2 right = Rotate(desired, -90f);

            bool leftClear = IsDirectionClear(left, radius, look);
            bool rightClear = IsDirectionClear(right, radius, look);

            if (leftClear && !rightClear) return 1;
            if (rightClear && !leftClear) return -1;
            return _detourSign != 0 ? _detourSign : 1;
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

            // 只检查到玩家身前为止：玩家贴着墙/门时，墙在身后不算挡路，
            // 否则敌人会误以为目标不可达，在墙边上下绕圈。
            float reach = Mathf.Max(0.05f, distance - 0.55f);
            return IsDirectionClear(toTarget / distance, radius, reach);
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
            // 未激活的预生成敌人不参与战斗，避免玩家从门外隔墙/隔门击杀
            if (!CombatActive || IsDead || amount <= 0f) return;

            _health -= amount;
            AudioService.Play("metalClick", 0.35f);

            if (_health > 0f) FlashHit();
            else Die();
        }

        /// <summary>无受击音效/闪白的伤害，用于光环等持续小额伤害。</summary>
        public void TakeDamageSilent(float amount)
        {
            if (!CombatActive || IsDead || amount <= 0f) return;

            _health -= amount;
            if (_health <= 0f) Die();
        }

        /// <summary>
        /// 附加中毒：命中时立即结算第一跳，之后每 interval 秒再结算一跳。
        /// 重复命中会补足跳数，但不会推迟已经排定的下一跳。
        /// </summary>
        public void ApplyPoison(float damagePerTick, int ticks, float interval)
        {
            if (!CombatActive || IsDead || damagePerTick <= 0f || ticks <= 0) return;

            bool wasPoisoned = _poisonTicks > 0;

            _poisonDamage = Mathf.Max(_poisonDamage, damagePerTick);
            _poisonTicks = Mathf.Max(_poisonTicks, ticks);
            _poisonInterval = Mathf.Max(0.15f, interval);

            if (!wasPoisoned)
            {
                // 第一跳立即生效，让中毒有明确的伤害反馈
                TickPoison();
            }
            else if (body != null)
            {
                body.color = PoisonTint;
            }
        }

        /// <summary>受击击退：短暂让出速度控制权，由物理把敌人推开。</summary>
        public virtual void ApplyKnockback(Vector2 direction, float force)
        {
            if (!CombatActive || IsDead || _rb == null || direction.sqrMagnitude < 0.0001f) return;

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
            if (body != null) body.color = _poisonTicks > 0 ? PoisonTint : _baseColor;
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
            // 未进入战斗时只是待机游荡，不会伤害站在门外的玩家
            if (!CombatActive) return;
            if (IsDead || Time.time < _nextContactTime) return;

            var health = collision.collider.GetComponentInParent<PlayerHealth>();
            if (health == null) return;

            if (health.TryDamage(ScaleAttackDamage(contactDamage)))
                _nextContactTime = Time.time + contactCooldown;
        }
    }
}
