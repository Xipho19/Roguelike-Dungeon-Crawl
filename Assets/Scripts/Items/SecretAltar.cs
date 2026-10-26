using Roguelike.Audio;
using Roguelike.Dungeon;
using Roguelike.Player;
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

        private static GUIStyle _nameStyle;
        private static GUIStyle _costStyle;
        private static GUIStyle _titleStyle;
        private static GUIStyle _itemNameStyle;
        private static GUIStyle _bodyStyle;
        private static GUIStyle _hintStyle;
        private static GUIStyle _warningStyle;
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

            if (_nameStyle == null)
            {
                _nameStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                };
                _nameStyle.normal.textColor = new Color(0.92f, 0.82f, 1f);

                _costStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                };
                _costStyle.normal.textColor = new Color(0.98f, 0.55f, 0.60f);
            }

            float nameX = nameScreen.x;
            float nameY = Screen.height - nameScreen.y;
            GUI.Box(new Rect(nameX - 90f, nameY - 12f, 180f, 24f), reward.itemName, _nameStyle);

            float costX = costScreen.x;
            float costY = Screen.height - costScreen.y;
            GUI.Box(new Rect(costX - 70f, costY - 11f, 140f, 22f), $"献祭 {healthCost} 点生命", _costStyle);
        }

        private void DrawDialog()
        {
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26,
                    alignment = TextAnchor.MiddleLeft,
                };
                _titleStyle.normal.textColor = new Color(0.80f, 0.66f, 1f);

                _itemNameStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleLeft,
                    wordWrap = true,
                };
                _itemNameStyle.normal.textColor = Color.white;

                _bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = true,
                };
                _bodyStyle.normal.textColor = new Color(0.86f, 0.88f, 0.95f);

                _hintStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleLeft,
                };
                _hintStyle.normal.textColor = new Color(0.98f, 0.72f, 0.78f);

                _warningStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    alignment = TextAnchor.MiddleLeft,
                };
                _warningStyle.normal.textColor = new Color(1f, 0.35f, 0.35f);
            }

            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.64f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;

            const float topBarHeight = 48f;
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            float panelWidth = Mathf.Min(580f, Screen.width - 70f);
            float panelHeight = Mathf.Min(340f, Screen.height - 100f);
            var panel = new Rect(
                cx - panelWidth * 0.5f,
                Mathf.Max(topBarHeight + 8f, cy - panelHeight * 0.5f),
                panelWidth,
                panelHeight);

            Color panelColor = GUI.color;
            GUI.color = new Color(0.09f, 0.06f, 0.12f, 0.98f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = panelColor;
            GUI.Box(panel, GUIContent.none);

            // 顶部紫色装饰线
            GUI.color = new Color(0.55f, 0.34f, 0.82f, 0.9f);
            GUI.DrawTexture(new Rect(panel.x + 20f, panel.y + 52f, panel.width - 40f, 1f), Texture2D.whiteTexture);
            GUI.color = previous;

            if (altarRenderer != null && altarRenderer.sprite != null)
            {
                GUI.DrawTexture(
                    new Rect(panel.x + 26f, panel.y + 12f, 38f, 38f),
                    altarRenderer.sprite.texture,
                    ScaleMode.ScaleToFit,
                    true);
            }

            GUI.Label(new Rect(panel.x + 66f, panel.y + 10f, panel.width - 90f, 36f), "秘密祭坛", _titleStyle);

            if (reward != null && reward.icon != null && reward.icon.texture != null)
            {
                GUI.DrawTexture(
                    new Rect(panel.x + 26f, panel.y + 64f, 64f, 64f),
                    reward.icon.texture,
                    ScaleMode.ScaleToFit,
                    true);
            }

            string itemName = reward != null ? reward.itemName : "未知祭品";
            GUI.Label(new Rect(panel.x + 104f, panel.y + 64f, panel.width - 128f, 30f), itemName, _itemNameStyle);

            string description = reward != null && !string.IsNullOrWhiteSpace(reward.description)
                ? reward.description
                : "没有详细说明。";
            GUI.Label(new Rect(panel.x + 104f, panel.y + 94f, panel.width - 128f, 50f), description, _bodyStyle);

            GUI.Label(
                new Rect(panel.x + 28f, panel.y + 152f, panel.width - 56f, 26f),
                $"献祭 {healthCost} 点生命，获得上面这件道具。",
                _hintStyle);

            int currentHealth = _health != null ? _health.Health : 0;
            int maxHealth = _health != null ? _health.MaxHealth : 0;
            GUI.Label(
                new Rect(panel.x + 28f, panel.y + 180f, panel.width - 56f, 24f),
                $"当前生命：{currentHealth}/{maxHealth}",
                _bodyStyle);

            bool canSacrifice = _health != null && _health.Health > healthCost;
            if (!canSacrifice)
            {
                GUI.Label(
                    new Rect(panel.x + 28f, panel.y + 206f, panel.width - 56f, 24f),
                    "生命不足，无法献祭。",
                    _warningStyle);
            }

            float buttonY = panel.yMax - 56f;

            if (GUI.Button(new Rect(cx - 160f, buttonY, 140f, 42f), "离开"))
                Leave();

            GUI.enabled = canSacrifice;
            if (GUI.Button(new Rect(cx + 20f, buttonY, 140f, 42f), $"献祭 {healthCost} 点生命"))
                Sacrifice();
            GUI.enabled = true;
        }
    }
}
