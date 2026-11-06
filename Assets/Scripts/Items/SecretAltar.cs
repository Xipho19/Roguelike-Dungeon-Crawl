using Roguelike.Audio;
using Roguelike.Dungeon;
using Roguelike.Player;
using Roguelike.UI;
using UnityEngine;

namespace Roguelike.Items
{
    /// <summary>
    /// 秘密房祭坛。
    ///
    /// 玩家靠近后暂停游戏并弹出交易窗口：
    /// 献祭 1 点生命，换取祭坛上方显示的、玩家尚未持有的道具。
    /// 每个祭坛只能使用一次；生命不足时献祭按钮不可点击。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class SecretAltar : MonoBehaviour
    {
        [SerializeField] private ItemDefinition reward;
        [SerializeField] private SpriteRenderer rewardIcon;
        [SerializeField] private SpriteRenderer altarRenderer;

        [Header("动画")]
        [SerializeField] private float iconHeight = 1.55f;
        [SerializeField] private float bobHeight = 0.1f;
        [SerializeField] private float bobSpeed = 2.2f;

        [Header("交易")]
        [SerializeField] private int healthCost = 1;

        private static SecretAltar _activeDialog;

        private RoomView _room;
        private PlayerInventory _inventory;
        private PlayerHealth _health;
        private PlayerWallet _wallet;
        private Camera _camera;
        private float _phase;

        private bool _dialogVisible;
        private bool _dismissed;
        private bool _pausedByDialog;
        private bool _used;

        public ItemDefinition Reward => reward;
        public bool IsDialogVisible => _dialogVisible;
        public bool IsUsed => _used;

        /// <summary>由秘密房生成逻辑调用：指定祭品与所属房间。</summary>
        public void Setup(ItemDefinition item, RoomView room)
        {
            reward = item;
            _room = room;

            if (rewardIcon == null)
            {
                Transform iconTransform = transform.Find("RewardIcon");
                if (iconTransform != null) rewardIcon = iconTransform.GetComponent<SpriteRenderer>();
            }

            if (rewardIcon != null && item != null && item.icon != null)
            {
                rewardIcon.sprite = item.icon;

                Vector2 size = item.icon.bounds.size;
                float maxSide = Mathf.Max(size.x, size.y);
                if (maxSide > 0.01f)
                    rewardIcon.transform.localScale = Vector3.one * (0.8f / maxSide);
            }
        }

        private void Start()
        {
            _camera = Camera.main;
            _phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (rewardIcon == null) return;

            float offset = Mathf.Sin(Time.unscaledTime * bobSpeed + _phase) * bobHeight;
            rewardIcon.transform.localPosition = new Vector3(0f, iconHeight + offset, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == null) return;
            if (_used || _dismissed || _dialogVisible || _activeDialog != null) return;
            if (Time.timeScale <= 0.001f) return;

            _inventory = other.GetComponentInParent<PlayerInventory>();
            _health = other.GetComponentInParent<PlayerHealth>();
            _wallet = other.GetComponentInParent<PlayerWallet>();

            OpenDialog();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerHealth>() == null) return;

            _dismissed = false;
            if (_dialogVisible) CloseDialog();
        }

        private void OpenDialog()
        {
            if (_activeDialog != null && _activeDialog != this) return;

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

        /// <summary>点击「离开」：关闭窗口，祭坛保持可用。</summary>
        public void Leave()
        {
            if (!_dialogVisible) return;

            _dismissed = true;
            CloseDialog();
        }

        /// <summary>点击「献祭」：扣除生命并发放道具。</summary>
        public void Sacrifice()
        {
            if (!_dialogVisible || _used || reward == null || _health == null) return;
            if (_health.Health <= healthCost) return;
            if (!_health.TrySacrifice(healthCost)) return;

            _used = true;
            ItemGrant.Apply(reward, _inventory, _health, _wallet);

            if (_room != null) _room.SecretEventUsed = true;

            CloseDialog();
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            if (_activeDialog == this) _activeDialog = null;
            _dialogVisible = false;

            if (_pausedByDialog) Time.timeScale = 1f;
            _pausedByDialog = false;
        }

        private void OnDestroy()
        {
            if (_activeDialog == this) _activeDialog = null;
            if (_pausedByDialog) Time.timeScale = 1f;
            _pausedByDialog = false;
        }

        private void OnGUI()
        {
            if (_used) return;

            if (_dialogVisible)
            {
                DrawDialog();
                return;
            }

            if (reward == null || _activeDialog != null) return;

            DrawFloatingInfo();
        }

        private void DrawFloatingInfo()
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector3 nameScreen = _camera.WorldToScreenPoint(transform.position + Vector3.up * 2.55f);
            Vector3 costScreen = _camera.WorldToScreenPoint(transform.position + Vector3.up * 2.18f);
            if (nameScreen.z < 0f) return;

            float nameX = nameScreen.x;
            float nameY = Screen.height - nameScreen.y;
            UiTheme.DrawPill(
                new Rect(nameX - 100f, nameY - 14f, 200f, 28f),
                reward.itemName,
                UiTheme.Mana,
                UiTheme.Center);

            float costX = costScreen.x;
            float costY = Screen.height - costScreen.y;
            UiTheme.DrawPill(
                new Rect(costX - 76f, costY - 12f, 152f, 24f),
                $"献祭 {healthCost} 点生命",
                UiTheme.Danger,
                UiTheme.Center);
        }

        private void DrawDialog()
        {
            UiTheme.DrawDim();

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float width = Mathf.Min(590f, Screen.width - 54f);
            float height = Mathf.Min(340f, Screen.height - 72f);
            float panelY = Mathf.Max(66f, cy - height * 0.5f);
            var panel = new Rect(cx - width * 0.5f, panelY, width, height);

            UiTheme.DrawPanel(panel, UiTheme.Mana, false);

            UiTheme.DrawIconSlot(
                new Rect(panel.x + 20f, panel.y + 14f, 42f, 42f),
                altarRenderer != null ? altarRenderer.sprite : null,
                UiTheme.Mana);

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 72f, panel.y + 10f, panel.width - 92f, 30f),
                "秘密祭坛",
                UiTheme.Title,
                new Color(0.84f, 0.72f, 1f));

            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 74f, panel.y + 36f, panel.width - 94f, 18f),
                "以生命换取未知的力量",
                UiTheme.Small,
                UiTheme.Muted);

            UiTheme.DrawDivider(new Rect(panel.x + 22f, panel.y + 64f, panel.width - 44f, 1f));

            if (reward != null && reward.icon != null)
            {
                UiTheme.DrawIconSlot(
                    new Rect(panel.x + 24f, panel.y + 80f, 58f, 58f),
                    reward.icon,
                    UiTheme.RarityColor(reward.rarity));
            }

            string itemName = reward != null ? reward.itemName : "未知祭品";
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 96f, panel.y + 78f, panel.width - 120f, 30f),
                itemName,
                UiTheme.Title,
                UiTheme.Text);

            if (reward != null)
            {
                UiTheme.DrawRarityChip(
                    new Rect(panel.x + 98f, panel.y + 109f, 64f, 18f),
                    reward.rarity);
            }

            string description = reward != null && !string.IsNullOrWhiteSpace(reward.description)
                ? reward.description
                : "没有详细说明。";
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 96f, panel.y + 132f, panel.width - 120f, 44f),
                description,
                UiTheme.Body,
                UiTheme.Text);

            var costRow = new Rect(panel.x + 24f, panel.y + 190f, panel.width - 48f, 38f);
            UiTheme.DrawInset(costRow, UiTheme.Inset);
            UiTheme.Fill(new Rect(costRow.x + 2f, costRow.y + 2f, 3f, costRow.height - 4f), UiTheme.Danger);
            UiTheme.DrawLabelShadow(
                new Rect(costRow.x + 14f, costRow.y + 7f, costRow.width - 28f, 22f),
                $"献祭 {healthCost} 点生命，获得上面的道具",
                UiTheme.Body,
                UiTheme.Text);

            int currentHealth = _health != null ? _health.Health : 0;
            int maxHealth = _health != null ? _health.MaxHealth : 0;
            UiTheme.DrawLabelShadow(
                new Rect(panel.x + 28f, panel.y + 238f, panel.width - 56f, 20f),
                $"当前生命：{currentHealth}/{maxHealth}",
                UiTheme.Small,
                UiTheme.Muted);

            bool canSacrifice = _health != null && _health.Health > healthCost;
            if (!canSacrifice)
            {
                UiTheme.DrawLabelShadow(
                    new Rect(panel.x + 28f, panel.y + 262f, panel.width - 56f, 20f),
                    "生命不足，无法献祭。",
                    UiTheme.Warning,
                    UiTheme.Danger);
            }

            float buttonY = panel.yMax - 56f;
            if (UiTheme.DrawButton(
                    new Rect(cx - 164f, buttonY, 140f, 40f),
                    "离开",
                    false,
                    false,
                    15))
            {
                Leave();
            }

            if (UiTheme.DrawButton(
                    new Rect(cx + 24f, buttonY, 140f, 40f),
                    $"献祭 {healthCost} 点生命",
                    true,
                    !canSacrifice,
                    15))
            {
                Sacrifice();
            }
        }
    }
}
