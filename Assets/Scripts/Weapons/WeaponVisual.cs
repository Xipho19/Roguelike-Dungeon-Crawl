using Roguelike.Player;
using UnityEngine;

namespace Roguelike.Weapons
{
    /// <summary>
    /// 在玩家身边显示当前武器的手持 Sprite，并跟随鼠标瞄准方向旋转。
    /// 泪滴等没有手持模型的武器会自动隐藏。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WeaponVisual : MonoBehaviour
    {
        [SerializeField] private WeaponInventory inventory;
        [SerializeField] private PlayerCombat combat;

        private Transform _weaponTransform;
        private SpriteRenderer _renderer;
        private WeaponDefinition _shownWeapon;
        private Vector2 _lastAim = Vector2.right;
        private float _swingStartedAt = -10f;
        private int _swingStep;

        private void Awake()
        {
            if (inventory == null) inventory = GetComponent<WeaponInventory>();
            if (combat == null) combat = GetComponent<PlayerCombat>();

            EnsureRenderer();

            if (inventory != null) inventory.Changed += Refresh;
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.Changed -= Refresh;
        }

        private void LateUpdate()
        {
            if (_renderer == null) return;

            WeaponDefinition weapon = inventory != null ? inventory.Current : null;
            if (weapon == null || weapon.heldSprite == null)
            {
                _renderer.enabled = false;
                return;
            }

            if (_shownWeapon != weapon)
            {
                _shownWeapon = weapon;
                _renderer.sprite = weapon.heldSprite;
                _renderer.enabled = true;
            }

            Vector2 aim = combat != null ? combat.AimDirection : _lastAim;
            if (aim.sqrMagnitude > 0.0001f)
            {
                aim.Normalize();
                _lastAim = aim;
            }
            else
            {
                aim = _lastAim;
            }

            float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg + weapon.heldAngleOffset;
            float scaleY = weapon.heldScale;
            if (weapon.flipHeldWhenAimingLeft && aim.x < 0f) scaleY = -scaleY;

            Vector2 side = new Vector2(-aim.y, aim.x) * weapon.heldSideOffset;
            Vector2 position = aim * weapon.heldDistance + side;
            position.y += weapon.heldVerticalOffset;

            ApplySwing(aim, ref angle, ref position, ref scaleY);

            _weaponTransform.localPosition = position;
            _weaponTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            _weaponTransform.localScale = new Vector3(weapon.heldScale, scaleY, 1f);
            _renderer.enabled = true;
        }

        /// <summary>武士刀三段斩的手持摆动，让每一刀的动作方向不同。</summary>
        public void PlaySwing(int step)
        {
            _swingStep = Mathf.Clamp(step, 1, 3);
            _swingStartedAt = Time.time;
        }

        private void ApplySwing(Vector2 aim, ref float angle, ref Vector2 position, ref float scaleY)
        {
            if (_swingStep <= 0) return;

            const float duration = 0.17f;
            float t = (Time.time - _swingStartedAt) / duration;
            if (t < 0f || t >= 1f)
            {
                _swingStep = 0;
                return;
            }

            float curve = Mathf.Sin(t * Mathf.PI);

            WeaponDefinition current = inventory != null ? inventory.Current : null;
            if (current != null && current.kind == WeaponKind.Waraxe)
            {
                float direction = _swingStep == 1 ? 1f : -1f;
                angle += direction * 130f * curve;
                position += aim * (0.12f * curve);
                scaleY *= 1f + 0.10f * curve;
                return;
            }

            switch (_swingStep)
            {
                case 1:
                    angle -= 48f * curve;
                    position += aim * (0.10f * curve);
                    break;
                case 2:
                    angle += 52f * curve;
                    position += aim * (0.12f * curve);
                    break;
                default:
                    angle -= 88f * curve;
                    position += aim * (0.24f * curve);
                    scaleY *= 1f + 0.08f * curve;
                    break;
            }
        }

        private void Refresh()
        {
            _shownWeapon = null;

            if (inventory == null) inventory = GetComponent<WeaponInventory>();
            if (_renderer != null)
            {
                WeaponDefinition weapon = inventory != null ? inventory.Current : null;
                _renderer.sprite = weapon != null ? weapon.heldSprite : null;
                _renderer.enabled = weapon != null && weapon.heldSprite != null;
            }
        }

        private void EnsureRenderer()
        {
            Transform existing = transform.Find("HeldWeapon");
            if (existing == null)
            {
                var visual = new GameObject("HeldWeapon");
                existing = visual.transform;
                existing.SetParent(transform, false);
            }

            _weaponTransform = existing;

            _renderer = existing.GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = existing.gameObject.AddComponent<SpriteRenderer>();

            _renderer.sortingOrder = 11;
            _renderer.color = Color.white;
        }
    }
}
