using Roguelike.Dungeon;
using Roguelike.Player;
using Roguelike.UI;
using UnityEngine;

namespace Roguelike.Items
{
    /// <summary>
    /// 道具台。
    ///
    /// 宝箱房（price = 0）：碰到即免费拾取。
    /// 商店（price > 0）：道具上方显示名称和价格；
    /// 玩家靠近后暂停并弹出购买窗口，显示名称、描述、价格与「返回 / 购买」。
    /// 金币不足时购买按钮变灰且不可点击。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private SpriteRenderer icon;

        [Tooltip("领取后需要标记的房间（宝箱房防重复刷新）")]
        [SerializeField] private RoomView rewardRoom;

        [Tooltip("0 表示免费；大于 0 表示商店价格")]
        [SerializeField] private int price;

        [Header("动画")]
        [SerializeField] private float bobHeight = 0.15f;
        [SerializeField] private float bobSpeed = 2.5f;

        private static ItemPickup _activeDialog;
        private static float _dialogsSuppressedUntil;

        private Vector3 _basePosition;
        private bool _baseInitialized;
        private float _phase;
        private Camera _camera;

        private PlayerInventory _playerInventory;
        private PlayerHealth _playerHealth;
        private PlayerWallet _playerWallet;
        private PlayerInventory _priceInventory;

        private bool _dialogVisible;
        private bool _dismissed;
        private bool _pausedByDialog;

        public ItemDefinition Item => item;
        public bool IsDialogVisible => _dialogVisible;
        public bool IsPurchased { get; private set; }
        public bool CanAfford => _playerWallet != null && _playerWallet.Coins >= Price;
        public Vector3 BasePosition => _baseInitialized ? _basePosition : transform.position;
        public static bool HasActiveDialog => _activeDialog != null;

        /// <summary>商店刷新后短暂禁止自动弹窗，避免玩家站在货架位置时立刻重新暂停。</summary>
        public static void SuppressNewDialogs(float seconds)
        {
            _dialogsSuppressedUntil = Mathf.Max(
                _dialogsSuppressedUntil,
                Time.unscaledTime + Mathf.Max(0f, seconds));
        }

        /// <summary>
        /// 实时价格：基础价 + 楼层加价已经写入 price；
        /// 折扣券在显示/购买时再结算，保证在商店里买到折扣券后其它商品立刻半价。
        /// </summary>
        public int Price
        {
            get
            {
                if (price <= 0) return 0;

                PlayerInventory inventory = _playerInventory != null ? _playerInventory : _priceInventory;
                if (inventory == null)
                {
                    inventory = FindFirstObjectByType<PlayerInventory>();
                    _priceInventory = inventory;
                }

                if (inventory != null && inventory.HasSynergy(ItemSynergy.ShopDiscount))
                    return Mathf.Max(1, Mathf.CeilToInt(price * 0.5f));

                return price;
            }
        }

        public void Setup(ItemDefinition definition, RoomView room, int cost = 0)
        {
            item = definition;
            rewardRoom = room;
            price = Mathf.Max(0, cost);

            if (icon == null) icon = GetComponentInChildren<SpriteRenderer>();

            if (icon != null && definition != null)
            {
                if (definition.icon != null) icon.sprite = definition.icon;
                icon.color = Color.white;
                FitIcon(icon.sprite);
                gameObject.name = "Item_" + definition.itemName;
            }
        }

        /// <summary>
        /// 统一图标展示尺寸：细长的武器放大并略微倾斜，避免变成一根细线；
        /// 方形图标（药水、心、金币）保持较小的展示尺寸。
        /// </summary>
        private void FitIcon(Sprite sprite)
        {
            if (icon == null || sprite == null) return;
            if (icon.transform == transform) return; // 旧结构：图标即根对象，缩放会影响碰撞体

            Vector2 size = sprite.bounds.size;
            float maxSide = Mathf.Max(size.x, size.y);
            if (maxSide <= 0.01f) return;

            float aspect = size.y / Mathf.Max(0.01f, size.x);
            bool tall = aspect > 2f;
            float target = tall ? 1.15f : 0.85f;

            icon.transform.localScale = Vector3.one * (target / maxSide);
            icon.transform.localRotation = tall
                ? Quaternion.Euler(0f, 0f, -20f)
                : Quaternion.identity;
        }

        private void Start()
        {
            _basePosition = transform.position;
            _baseInitialized = true;
            _phase = Random.value * Mathf.PI * 2f;
            _camera = Camera.main;
        }

        private void Update()
        {
            transform.position = _basePosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed + _phase) * bobHeight);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null || item == null) return;

            var wallet = other.GetComponentInParent<PlayerWallet>();
            var health = other.GetComponentInParent<PlayerHealth>();

            if (price > 0)
            {
                if (Time.unscaledTime < _dialogsSuppressedUntil) return;
                if (_dismissed || _dialogVisible || _activeDialog != null) return;
                OpenDialog(inventory, wallet, health);
                return;
            }

            GrantItem(inventory, wallet, health);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerInventory>() == null) return;

            _dismissed = false;
            if (_dialogVisible) CloseDialog();
        }

        private void OpenDialog(PlayerInventory inventory, PlayerWallet wallet, PlayerHealth health)
        {
            if (_activeDialog != null && _activeDialog != this) return;

            _playerInventory = inventory;
            _playerWallet = wallet;
            _playerHealth = health;

            _dialogVisible = true;
            _dismissed = false;
            _activeDialog = this;

            _pausedByDialog = Time.timeScale > 0f;
            if (_pausedByDialog) Time.timeScale = 0f;
        }

        private void CloseDialog()
        {
            if (_activeDialog == this) _activeDialog = null;

            _dialogVisible = false;

            if (_pausedByDialog) Time.timeScale = 1f;
            _pausedByDialog = false;
        }

        /// <summary>点击「返回」：关闭窗口，留在商店里。</summary>
        public void Back()
        {
            if (!_dialogVisible) return;

            _dismissed = true;
            CloseDialog();
        }

        /// <summary>点击「购买」：扣金币并拾取道具。</summary>
        public void Buy()
        {
            if (!_dialogVisible || item == null) return;

            int cost = Price;
            if (_playerWallet == null || _playerWallet.Coins < cost) return;
            if (!_playerWallet.TrySpend(cost)) return;

            var inventory = _playerInventory;
            var health = _playerHealth;
            var wallet = _playerWallet;

            CloseDialog();
            GrantItem(inventory, wallet, health);
        }

        private void GrantItem(PlayerInventory inventory, PlayerWallet wallet, PlayerHealth health)
        {
            if (item == null) return;

            IsPurchased = true;
            ItemGrant.Apply(item, inventory, health, wallet);

            if (rewardRoom != null) rewardRoom.RewardClaimed = true;

            Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (_activeDialog == this) _activeDialog = null;
            _dialogVisible = false;

            if (_pausedByDialog) Time.timeScale = 1f;
            _pausedByDialog = false;
        }

        private void OnGUI()
        {
            if (_dialogVisible)
            {
                DrawDialog();
                return;
            }

            if (price <= 0 || _activeDialog != null) return;

            DrawFloatingInfo();
        }

        private void DrawFloatingInfo()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector3 nameScreen = _camera.WorldToScreenPoint(transform.position + Vector3.up * 1.45f);
            Vector3 priceScreen = _camera.WorldToScreenPoint(transform.position + Vector3.up * 0.78f);
            if (nameScreen.z < 0f) return;

            float nameX = nameScreen.x;
            float nameY = Screen.height - nameScreen.y;
            UiTheme.DrawPill(
                new Rect(nameX - 100f, nameY - 14f, 200f, 28f),
                item.itemName,
                UiTheme.GoldDim,
                UiTheme.Center);

            float priceX = priceScreen.x;
            float priceY = Screen.height - priceScreen.y;
            UiTheme.DrawPill(
                new Rect(priceX - 56f, priceY - 12f, 112f, 24f),
                Price + " 金币",
                UiTheme.Coin,
                UiTheme.Center);
        }

        private void DrawDialog()
        {
            UiTheme.DrawDim();

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float width = Mathf.Min(580f, Screen.width - 52f);
            float height = Mathf.Min(318f, Screen.height - 72f);
            float panelY = Mathf.Max(66f, cy - height * 0.5f);
            var panel = new Rect(cx - width * 0.5f, panelY, width, height);

            UiTheme.DrawPanel(panel, UiTheme.Gold, false);

            UiTheme.DrawIconSlot(
                new Rect(panel.x + 20f, panel.y + 16f, 54f, 54f),
                item.icon,
                UiTheme.RarityColor(item.rarity));

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 86f, panel.y + 12f, panel.width - 108f, 32f),
                item.itemName,
                UiTheme.Title,
                UiTheme.Text);

            UiTheme.DrawRarityChip(
                new Rect(panel.x + 88f, panel.y + 43f, 64f, 18f),
                item.rarity);

            UiTheme.DrawDivider(new Rect(panel.x + 22f, panel.y + 80f, panel.width - 44f, 1f));

            string description = string.IsNullOrWhiteSpace(item.description)
                ? "没有详细说明。"
                : item.description;
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 24f, panel.y + 94f, panel.width - 48f, 86f),
                description,
                UiTheme.Body,
                UiTheme.Text);

            var priceRow = new Rect(panel.x + 24f, panel.y + 186f, panel.width - 48f, 38f);
            UiTheme.DrawInset(priceRow, UiTheme.Inset);
            UiTheme.DrawCoinIcon(new Rect(priceRow.x + 10f, priceRow.y + 10f, 18f, 18f));
            UiTheme.DrawLabelShadow(
                new Rect(priceRow.x + 36f, priceRow.y + 7f, 220f, 22f),
                $"价格 {Price} 金币",
                UiTheme.Body,
                UiTheme.Coin);

            int coins = _playerWallet != null ? _playerWallet.Coins : 0;
            UiTheme.DrawLabelShadow(
                new Rect(priceRow.xMax - 180f, priceRow.y + 7f, 166f, 22f),
                $"持有 {coins}",
                UiTheme.Small,
                UiTheme.Muted);

            bool canAfford = CanAfford;
            if (!canAfford)
            {
                UiTheme.DrawLabelShadow(
                    new Rect(panel.x + 24f, panel.y + 230f, panel.width - 48f, 20f),
                    "金币不足，无法购买",
                    UiTheme.Warning,
                    UiTheme.Danger);
            }

            float buttonY = panel.yMax - 56f;
            if (UiTheme.DrawButton(
                    new Rect(cx - 154f, buttonY, 140f, 40f),
                    "返回",
                    false,
                    false,
                    15))
            {
                Back();
            }

            if (UiTheme.DrawButton(
                    new Rect(cx + 14f, buttonY, 140f, 40f),
                    "购买",
                    true,
                    !canAfford,
                    15))
            {
                Buy();
            }
        }
    }
}
