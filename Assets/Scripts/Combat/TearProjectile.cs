using System.Collections.Generic;
using Roguelike.Audio;
using Roguelike.Dungeon;
using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Combat
{
    /// <summary>一颗眼泪的附加触发属性；由 PlayerCombat 根据背包里的自动道具填充。</summary>
    public struct TearModifiers
    {
        public RoomView ownerRoom;
        public float explosionRadius;
        public float explosionDamage;
        public float homingTurnSpeed;
        public float homingRange;
        public float poisonDamage;
        public int poisonTicks;
        public float poisonInterval;
        public float critChance;
        public int splitCount;
        public float splitDamageScale;
        public float auraDamage;
        public float auraRadius;
        public float auraInterval;
        public int bounceCount;
        public float bounceSpeedScale;
    }

    /// <summary>
    /// 玩家的眼泪弹。
    ///
    /// 使用 Dynamic Rigidbody2D + Trigger 碰撞体：
    ///   - 命中敌人 → 造成伤害；有穿透次数时继续飞行，否则销毁；
    ///   - 命中墙体（非 Trigger 碰撞体）→ 销毁；
    ///   - 忽略玩家与其它 Trigger。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class TearProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 14f;
        [SerializeField] private float lifetime = 1f;
        [SerializeField] private float damage = 1f;

        [Header("爆炸眼泪")]
        [SerializeField] private Sprite explosionSprite;

        private readonly HashSet<Enemy> _hitEnemies = new HashSet<Enemy>();

        private Rigidbody2D _rb;
        private int _remainingPierce;
        private float _dieAt;
        private float _explosionRadius;
        private float _explosionDamage;
        private bool _exploded;
        private RoomView _ownerRoom;
        private float _homingTurnSpeed;
        private float _homingRange = 6f;
        private Enemy _homingTarget;
        private float _nextHomingSearch;
        private float _poisonDamage;
        private int _poisonTicks;
        private float _poisonInterval = 0.6f;
        private TearModifiers _modifiers;
        private float _critChance;
        private int _splitCount;
        private float _splitDamageScale = 0.5f;
        private bool _canSplit = true;
        private float _auraDamage;
        private float _auraRadius;
        private float _auraInterval = 0.3f;
        private float _nextAuraTick;
        private int _remainingBounces;
        private float _bounceSpeedScale = 1f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.gravityScale = 0f;
            _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        public void Initialize(
            Vector2 direction,
            float shotSpeed,
            float amount,
            float life,
            int pierce,
            TearModifiers modifiers = default)
        {
            speed = shotSpeed;
            damage = amount;
            lifetime = life;
            _remainingPierce = Mathf.Max(0, pierce);

            _ownerRoom = modifiers.ownerRoom;
            _explosionRadius = Mathf.Max(0f, modifiers.explosionRadius);
            _explosionDamage = Mathf.Max(0f, modifiers.explosionDamage);
            _homingTurnSpeed = Mathf.Max(0f, modifiers.homingTurnSpeed);
            _homingRange = modifiers.homingRange > 0f ? modifiers.homingRange : 6f;
            _poisonDamage = Mathf.Max(0f, modifiers.poisonDamage);
            _poisonTicks = Mathf.Max(0, modifiers.poisonTicks);
            _poisonInterval = modifiers.poisonInterval > 0f ? modifiers.poisonInterval : 0.6f;
            _modifiers = modifiers;
            _critChance = Mathf.Clamp01(modifiers.critChance);
            _splitCount = Mathf.Max(0, modifiers.splitCount);
            _splitDamageScale = Mathf.Clamp(modifiers.splitDamageScale, 0.1f, 1f);
            _auraDamage = Mathf.Max(0f, modifiers.auraDamage);
            _auraRadius = Mathf.Max(0f, modifiers.auraRadius);
            _auraInterval = modifiers.auraInterval > 0f ? modifiers.auraInterval : 0.3f;
            _nextAuraTick = Time.time + _auraInterval;
            _remainingBounces = Mathf.Max(0, modifiers.bounceCount);
            _bounceSpeedScale = modifiers.bounceSpeedScale > 0f
                ? Mathf.Clamp(modifiers.bounceSpeedScale, 0.1f, 1f)
                : 1f;

            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            _rb.linearVelocity = direction.normalized * speed;

            _dieAt = Time.time + lifetime;
        }

        private void Update()
        {
            if (Time.time >= _dieAt)
            {
                Destroy(gameObject);
                return;
            }

            // 眼泪不跨越房间边界：站在门外朝未进入的房间射击时，弹体会在门口消失
            if (_ownerRoom != null && !_ownerRoom.WorldRect.Contains(transform.position))
            {
                Destroy(gameObject);
                return;
            }

            if (_homingTurnSpeed > 0f) UpdateHoming();
            if (_auraDamage > 0f) UpdateAura();
        }

        /// <summary>弯勺：朝当前房间内最近的敌人缓慢转向。</summary>
        private void UpdateHoming()
        {
            if (_rb == null) return;
            if (!IsValidHomingTarget(_homingTarget))
            {
                if (Time.time < _nextHomingSearch) return;

                _homingTarget = FindHomingTarget();
                _nextHomingSearch = Time.time + 0.2f;
                if (_homingTarget == null) return;
            }

            Vector2 current = _rb.linearVelocity;
            if (current.sqrMagnitude < 0.0001f) return;

            Vector2 toTarget = (Vector2)_homingTarget.transform.position - _rb.position;
            if (toTarget.sqrMagnitude < 0.0001f) return;

            float angle = Vector2.SignedAngle(current, toTarget.normalized);
            float maxTurn = _homingTurnSpeed * Time.deltaTime;
            if (Mathf.Abs(angle) > maxTurn) angle = Mathf.Sign(angle) * maxTurn;

            Vector2 direction = Rotate(current.normalized, angle);
            _rb.linearVelocity = direction * speed;
        }

        private bool IsValidHomingTarget(Enemy enemy)
        {
            return enemy != null
                && !enemy.IsDead
                && enemy.CombatActive
                && _ownerRoom != null
                && enemy.Room == _ownerRoom;
        }

        private Enemy FindHomingTarget()
        {
            if (_ownerRoom == null || _rb == null) return null;

            Enemy best = null;
            float bestSqr = _homingRange * _homingRange;
            IReadOnlyList<Enemy> all = Enemy.AllActiveEnemies;

            for (int i = 0; i < all.Count; i++)
            {
                Enemy enemy = all[i];
                if (!IsValidHomingTarget(enemy)) continue;

                float sqr = ((Vector2)enemy.transform.position - _rb.position).sqrMagnitude;
                if (sqr > bestSqr) continue;

                bestSqr = sqr;
                best = enemy;
            }

            return best;
        }

        /// <summary>神性微光：眼泪飞行时周期性灼烧周围同房间的敌人。</summary>
        private void UpdateAura()
        {
            if (_ownerRoom == null || _auraRadius <= 0f) return;
            if (Time.time < _nextAuraTick) return;

            _nextAuraTick = Time.time + _auraInterval;

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(transform.position, _auraRadius);
            var damaged = new HashSet<Enemy>();

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider2D collider = overlaps[i];
                if (collider == null) continue;

                Enemy enemy = collider.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDead || !enemy.CombatActive) continue;
                if (enemy.Room != _ownerRoom) continue;
                if (!damaged.Add(enemy)) continue;

                enemy.TakeDamageSilent(_auraDamage);
            }
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            // 忽略玩家自己
            if (other.GetComponentInParent<PlayerHealth>() != null) return;

            // 可击破的场景道具：每次命中计数一次，眼泪随即消失
            var breakable = other.GetComponentInParent<BreakableProp>();
            if (breakable != null)
            {
                breakable.TakeHit();
                Destroy(gameObject);
                return;
            }

            var enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                // 预生成但尚未激活战斗的敌人不参与受击结算
                if (!enemy.CombatActive) return;

                // 兜底：不伤害发射时所在房间之外的敌人
                if (_ownerRoom != null && enemy.Room != _ownerRoom) return;

                if (!_hitEnemies.Add(enemy)) return; // 同一敌人只结算一次

                Vector2 pushDirection = _rb != null ? _rb.linearVelocity : Vector2.zero;

                float dealt = damage;
                if (_critChance > 0f && Random.value < _critChance) dealt *= 2f;
                enemy.TakeDamage(dealt);
                enemy.ApplyKnockback(pushDirection, 5f);

                if (_poisonTicks > 0 && _poisonDamage > 0f)
                    enemy.ApplyPoison(_poisonDamage, _poisonTicks, _poisonInterval);

                TrySplit();

                // 每颗眼泪只爆炸一次；后续穿透命中不会再触发
                TryExplode(enemy);

                if (_remainingPierce > 0)
                {
                    _remainingPierce--;
                    return; // 继续飞行
                }

                Destroy(gameObject);
                return;
            }

            // 撞到实心墙体：弹跳泪滴会反弹，其它眼泪直接消失
            if (!other.isTrigger)
            {
                if (TryBounce(other)) return;
                Destroy(gameObject);
            }
        }

        /// <summary>弹跳泪滴：按墙体最近的表面反弹，并略微降低速度。</summary>
        private bool TryBounce(Collider2D wall)
        {
            if (_remainingBounces <= 0 || _rb == null) return false;

            Vector2 velocity = _rb.linearVelocity;
            if (velocity.sqrMagnitude < 0.0001f) return false;

            Vector2 position = _rb.position;
            Vector2 closest = wall.ClosestPoint(position);
            Vector2 delta = position - closest;

            Vector2 normal;
            if (delta.sqrMagnitude > 0.0001f)
            {
                normal = delta.normalized;
            }
            else
            {
                // 圆心已经进入墙内，用包围盒最近的一面作为反弹法线。
                Bounds bounds = wall.bounds;
                float left = Mathf.Abs(position.x - bounds.min.x);
                float right = Mathf.Abs(position.x - bounds.max.x);
                float bottom = Mathf.Abs(position.y - bounds.min.y);
                float top = Mathf.Abs(position.y - bounds.max.y);
                float nearest = Mathf.Min(Mathf.Min(left, right), Mathf.Min(bottom, top));

                if (nearest == left) normal = Vector2.left;
                else if (nearest == right) normal = Vector2.right;
                else if (nearest == bottom) normal = Vector2.down;
                else normal = Vector2.up;
            }

            _remainingBounces--;

            Vector2 reflected = Vector2.Reflect(velocity.normalized, normal)
                * velocity.magnitude
                * _bounceSpeedScale;

            _rb.position = position + normal * 0.12f;
            _rb.linearVelocity = reflected;
            transform.position = new Vector3(_rb.position.x, _rb.position.y, transform.position.z);

            AudioService.Play("metalClick", 0.2f);
            return true;
        }

        /// <summary>裂体寄生虫：首次命中后分裂成数颗较弱的小眼泪。</summary>
        private void TrySplit()
        {
            if (!_canSplit || _splitCount <= 0) return;

            _canSplit = false;

            TearProjectile prefab = Resources.Load<TearProjectile>("Combat/Tear");
            if (prefab == null || _rb == null) return;

            Vector2 baseDirection = _rb.linearVelocity.sqrMagnitude > 0.0001f
                ? _rb.linearVelocity.normalized
                : Vector2.right;

            int count = Mathf.Clamp(_splitCount, 1, 4);

            for (int i = 0; i < count; i++)
            {
                float angle = count == 1 ? 0f : (i - (count - 1) * 0.5f) * 30f;
                Vector2 direction = Rotate(baseDirection, angle);
                Vector2 position = (Vector2)transform.position + direction * 0.25f;

                TearProjectile child = Instantiate(prefab, position, Quaternion.identity);

                TearModifiers modifiers = _modifiers;
                modifiers.explosionRadius = 0f;
                modifiers.explosionDamage = 0f;
                modifiers.splitCount = 0;
                modifiers.auraDamage = 0f;
                modifiers.auraRadius = 0f;
                modifiers.bounceCount = 0;

                child.Initialize(
                    direction,
                    speed * 0.9f,
                    damage * _splitDamageScale,
                    Mathf.Max(0.2f, lifetime * 0.75f),
                    0,
                    modifiers);

                child._canSplit = false;
            }
        }

        /// <summary>爆炸眼泪：对命中点周围同房间的敌人造成一次小范围伤害，排除直接命中目标。</summary>
        private void TryExplode(Enemy directHit)
        {
            if (_exploded || _explosionRadius <= 0f || _explosionDamage <= 0f) return;

            _exploded = true;

            Vector2 center = transform.position;
            ExplosionFlash.Spawn(center, explosionSprite, _explosionRadius);
            AudioService.Play("explosion", 0.4f);

            RoomView room = directHit != null ? directHit.Room : null;
            if (room == null) return;

            Collider2D[] overlaps = Physics2D.OverlapCircleAll(center, _explosionRadius);
            var damaged = new HashSet<Enemy>();

            for (int i = 0; i < overlaps.Length; i++)
            {
                Collider2D collider = overlaps[i];
                if (collider == null) continue;

                Enemy enemy = collider.GetComponentInParent<Enemy>();
                if (enemy == null || enemy.IsDead) continue;
                if (enemy == directHit) continue;
                if (enemy.Room != room) continue;
                if (!damaged.Add(enemy)) continue;

                enemy.TakeDamage(_explosionDamage);
            }
        }
    }
}
