using Roguelike.Dungeon;
using Roguelike.Player;
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

        private static GUIStyle _nameStyle;
        private static GUIStyle _priceStyle;
        private static GUIStyle _dialogTitleStyle;
        private static GUIStyle _dialogBodyStyle;
        private static GUIStyle _dialogPriceStyle;
        private static GUIStyle _warningStyle;
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

            if (_nameStyle == null)
            {
                _nameStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                };
                _nameStyle.normal.textColor = Color.white;

                _priceStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                };
                _priceStyle.normal.textColor = new Color(0.98f, 0.84f, 0.32f);
            }

            float nameX = nameScreen.x;
            float nameY = Screen.height - nameScreen.y;
            GUI.Box(new Rect(nameX - 90f, nameY - 12f, 180f, 24f), item.itemName, _nameStyle);

            float priceX = priceScreen.x;
            float priceY = Screen.height - priceScreen.y;
            GUI.Box(new Rect(priceX - 46f, priceY - 11f, 92f, 22f), Price + " 金币", _priceStyle);
        }

        private void DrawDialog()
        {
            if (_dialogTitleStyle == null)
            {
                _dialogTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                };
                _dialogTitleStyle.normal.textColor = new Color(0.85f, 0.93f, 1f);

                _dialogBodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                };
                _dialogBodyStyle.normal.textColor = Color.white;

                _dialogPriceStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleLeft,
                };
                _dialogPriceStyle.normal.textColor = new Color(0.98f, 0.84f, 0.32f);

                _warningStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    alignment = TextAnchor.MiddleCenter,
                };
                _warningStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
            }

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            var panel = new Rect(cx - 280f, cy - 150f, 560f, 300f);

            GUI.Box(panel, GUIContent.none);

            if (item.icon != null && item.icon.texture != null)
            {
                GUI.DrawTexture(
                    new Rect(panel.x + 24f, panel.y + 26f, 64f, 64f),
                    item.icon.texture,
                    ScaleMode.ScaleToFit,
                    true);
            }

            GUI.Label(new Rect(panel.x + 100f, panel.y + 28f, panel.width - 124f, 44f), item.itemName, _dialogTitleStyle);

            string description = string.IsNullOrWhiteSpace(item.description)
                ? "没有详细说明。"
                : item.description;
            GUI.Label(new Rect(panel.x + 28f, panel.y + 102f, panel.width - 56f, 96f), description, _dialogBodyStyle);

            int coins = _playerWallet != null ? _playerWallet.Coins : 0;
            GUI.Label(
                new Rect(panel.x + 28f, panel.y + 202f, panel.width - 56f, 28f),
                $"价格 {Price} 金币    持有 {coins}",
                _dialogPriceStyle);

            bool canAfford = CanAfford;
            if (!canAfford)
            {
                GUI.Label(
                    new Rect(panel.x + 28f, panel.y + 228f, panel.width - 56f, 22f),
                    "金币不足，无法购买",
                    _warningStyle);
            }

            if (GUI.Button(new Rect(cx - 150f, panel.y + 248f, 130f, 44f), "返回"))
                Back();

            GUI.enabled = canAfford;
            if (GUI.Button(new Rect(cx + 20f, panel.y + 248f, 130f, 44f), "购买"))
                Buy();
            GUI.enabled = true;
        }
    }
}
