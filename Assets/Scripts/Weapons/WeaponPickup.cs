using Roguelike.Audio;
using Roguelike.Combat;
using Roguelike.Core;
using Roguelike.Dungeon;
using Roguelike.Items;
using Roguelike.Player;
using Roguelike.UI;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Roguelike.Weapons
{
    /// <summary>
    /// 武器架：可用于免费奖励（起始房/宝箱/Boss），也可用于商店购买。
    ///
    /// 免费奖励靠近后按 E 装备；商店会显示价格，按 E 扣金币并装备。
    /// 两个槽位都满时替换当前槽位。
    /// </summary>
    public sealed class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private float pickupRadius = 1.8f;
        [SerializeField] private float bobHeight = 0.10f;
        [SerializeField] private float bobSpeed = 2.6f;

        private WeaponDefinition _weapon;
        private WeaponInventory _inventory;
        private PlayerWallet _wallet;
        private PlayerInventory _priceInventory;
        private RunManager _runManager;
        private CombatDebugHud _hud;
        private RoomView _rewardRoom;
        private Camera _camera;

        private Vector3 _basePosition;
        private bool _near;
        private bool _taken;
        private bool _forSale;
        private int _price;

        public WeaponDefinition Weapon => _weapon;
        public bool IsPurchased => _taken;
        public bool ForSale => _forSale;
        public Vector3 BasePosition => _basePosition;

        public int Price
        {
            get
            {
                if (!_forSale || _price <= 0) return 0;

                if (_priceInventory == null)
                    _priceInventory = FindFirstObjectByType<PlayerInventory>();

                if (_priceInventory != null && _priceInventory.HasSynergy(ItemSynergy.ShopDiscount))
                    return Mathf.Max(1, Mathf.CeilToInt(_price * 0.5f));

                return _price;
            }
        }

        public void Setup(
            WeaponDefinition weapon,
            WeaponInventory inventory,
            RoomView rewardRoom = null)
        {
            _weapon = weapon;
            _inventory = inventory;
            _rewardRoom = rewardRoom;
            _basePosition = transform.position;
            _runManager = FindFirstObjectByType<RunManager>();
            _hud = FindFirstObjectByType<CombatDebugHud>();
            _wallet = FindFirstObjectByType<PlayerWallet>();
            _priceInventory = FindFirstObjectByType<PlayerInventory>();
            _forSale = false;
            _price = 0;

            ConfigureView();
        }

        public void SetupForSale(
            WeaponDefinition weapon,
            WeaponInventory inventory,
            int price,
            RoomView rewardRoom = null)
        {
            Setup(weapon, inventory, rewardRoom);
            _forSale = true;
            _price = Mathf.Max(1, price);
        }

        private void ConfigureView()
        {
            var renderer = GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = _weapon != null
                ? (_weapon.icon != null ? _weapon.icon : _weapon.FirstProjectileFrame)
                : null;
            renderer.color = Color.white;
            renderer.sortingOrder = 7;

            var collider = GetComponent<CircleCollider2D>();
            if (collider == null) collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.65f;
            collider.isTrigger = true;

            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            if (_taken || _weapon == null || _inventory == null) return;

            float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = _basePosition + new Vector3(0f, bob, 0f);

            float pulse = 1f + Mathf.Sin(Time.time * bobSpeed + 0.8f) * 0.04f;
            transform.localScale = Vector3.one * pulse;

            // 玩家已从其它奖励处拿到同一件武器时，商店货架自动消失，避免重复购买。
            if (_forSale && _inventory.Owns(_weapon))
            {
                MarkTaken(false);
                return;
            }

            Vector2 delta = _inventory.transform.position - transform.position;
            _near = delta.sqrMagnitude <= pickupRadius * pickupRadius;

            if (_near && WasPickupPressed() && CanUse())
            {
                if (_forSale) TryBuy();
                else TakeFree();
            }
        }

        private bool CanUse()
        {
            if (Time.timeScale <= 0.001f) return false;
            if (MapUI.IsOpen) return false;
            if (CombatDebugHud.IsInventoryOpen) return false;
            if (_runManager != null && _runManager.State != RunManager.RunState.Playing) return false;
            return true;
        }

        private void TakeFree()
        {
            if (_weapon == null || _inventory == null) return;

            bool isNew = _inventory.Equip(_weapon);
            AudioService.Play(isNew ? "bookOpen" : "drawKnife1", 0.7f);

            if (_hud != null)
            {
                string action = isNew ? "获得武器" : "切换武器";
                _hud.ShowToast($"{action}：{_weapon.weaponName}\n{_weapon.description}", 2.8f);
            }

            MarkTaken(true);
        }

        private void TryBuy()
        {
            if (_weapon == null || _inventory == null) return;

            if (_inventory.Owns(_weapon))
            {
                if (_hud != null) _hud.ShowToast("已拥有该武器", 1.6f);
                MarkTaken(false);
                return;
            }

            int cost = Price;
            if (_wallet == null) _wallet = FindFirstObjectByType<PlayerWallet>();
            if (_wallet == null || _wallet.Coins < cost)
            {
                if (_hud != null)
                    _hud.ShowToast($"金币不足：{_weapon.weaponName} 需要 {cost} 金币", 2f);
                return;
            }

            if (!_wallet.TrySpend(cost)) return;

            _inventory.Equip(_weapon);
            AudioService.Play("handleCoins", 0.55f);
            AudioService.Play("drawKnife1", 0.65f);

            if (_hud != null)
                _hud.ShowToast($"购买武器：{_weapon.weaponName}\n-{cost} 金币", 2.6f);

            MarkTaken(true);
        }

        private void MarkTaken(bool markReward)
        {
            _taken = true;
            if (markReward && _rewardRoom != null)
                _rewardRoom.RewardClaimed = true;

            Destroy(gameObject);
        }

        private static bool WasPickupPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null) return keyboard.eKey.wasPressedThisFrame;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(KeyCode.E);
#else
            return false;
#endif
        }

        private void OnGUI()
        {
            if (_taken || _weapon == null) return;
            if (Time.timeScale <= 0.001f) return;
            if (MapUI.IsOpen) return;
            if (CombatDebugHud.IsInventoryOpen) return;

            if (_forSale || _rewardRoom != null)
                DrawFloatingInfo();

            if (!_near || !CanUse()) return;

            float width = Mathf.Clamp(Screen.width - 470f, 300f, 410f);
            const float height = 58f;
            var panel = new Rect(
                (Screen.width - width) * 0.5f,
                Screen.height - height - 36f,
                width,
                height);

            UiTheme.DrawPanel(panel);

            UiTheme.DrawKeyBadge(
                new Rect(panel.x + 10f, panel.y + 19f, 20f, 20f),
                "E");

            Sprite icon = _weapon.icon != null ? _weapon.icon : _weapon.FirstProjectileFrame;
            UiTheme.DrawIconSlot(
                new Rect(panel.x + 38f, panel.y + 8f, 42f, 42f),
                icon,
                _forSale ? UiTheme.Coin : UiTheme.GoldBright);

            string title = _forSale
                ? $"购买 {_weapon.weaponName}"
                : $"装备 {_weapon.weaponName}";
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 90f, panel.y + 6f, width - 100f, 22f),
                title,
                UiTheme.Body,
                _forSale ? UiTheme.GoldBright : UiTheme.GoldBright);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 90f, panel.y + 30f, width - 100f, 20f),
                GetDetailText(),
                UiTheme.Small,
                UiTheme.Muted);
        }

        private string GetDetailText()
        {
            if (_forSale)
            {
                int cost = Price;
                if (_wallet == null) _wallet = FindFirstObjectByType<PlayerWallet>();
                bool canAfford = _wallet != null && _wallet.Coins >= cost;
                return canAfford
                    ? $"按 E 购买 · {cost} 金币"
                    : $"金币不足 · 需要 {cost} 金币";
            }

            bool replacing = _inventory.SlotAt(0) != null && _inventory.SlotAt(1) != null;
            return replacing ? "两个槽位已满：替换当前武器" : "放入空武器槽";
        }

        private void DrawFloatingInfo()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            // 商店货架把名称与价格合并到更高处，避免和两侧道具的浮动标签重叠。
            float height = _forSale ? 2.15f : 1.45f;
            Vector3 screen = _camera.WorldToScreenPoint(transform.position + Vector3.up * height);
            if (screen.z < 0f) return;

            float x = screen.x;
            float y = Screen.height - screen.y;

            if (_forSale)
            {
                UiTheme.DrawPill(
                    new Rect(x - 116f, y - 14f, 232f, 28f),
                    $"{_weapon.weaponName} · {Price} 金币",
                    UiTheme.Coin,
                    UiTheme.Center);
                return;
            }

            UiTheme.DrawPill(
                new Rect(x - 100f, y - 14f, 200f, 28f),
                _weapon.weaponName,
                UiTheme.GoldDim,
                UiTheme.Center);
        }
    }
}
