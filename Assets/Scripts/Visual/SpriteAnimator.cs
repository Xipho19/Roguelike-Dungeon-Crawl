using UnityEngine;

namespace Roguelike.Visual
{
    /// <summary>
    /// 轻量帧动画：根据刚体速度在 idle / run 两组帧之间自动切换，
    /// 并按水平速度方向翻转朝向。
    ///
    /// 不依赖 Animator 资产：帧数组由 Prefab 生成器直接写入，
    /// 既适用于玩家，也适用于所有继承 Enemy 的敌人与 Boss。
    /// </summary>
    public sealed class SpriteAnimator : MonoBehaviour
    {
        [Header("帧序列")]
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] runFrames;

        [Header("播放")]
        [SerializeField] private float idleFps = 5f;
        [SerializeField] private float runFps = 10f;

        [Tooltip("速度超过该值视为移动，切换行走动画")]
        [SerializeField] private float movingThreshold = 0.15f;

        [Tooltip("移动时按水平速度翻转 Sprite")]
        [SerializeField] private bool flipToMovement = true;

        private SpriteRenderer _renderer;
        private Rigidbody2D _body;
        private float _timer;
        private int _frame;
        private bool _running;
        private bool _initialized;

        private Sprite[] CurrentFrames => _running ? runFrames : idleFrames;

        /// <summary>由 Prefab 生成器写入帧序列。</summary>
        public void Configure(Sprite[] idle, Sprite[] run)
        {
            idleFrames = idle;
            runFrames = run;

            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();

            _running = false;
            _frame = 0;
            _timer = 0f;
            _initialized = false;
            ApplyFrame();
        }

        private void Awake()
        {
            _renderer = GetComponentInChildren<SpriteRenderer>();
            _body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _running = false;
            _frame = 0;
            _timer = 0f;
            _initialized = false;
            ApplyFrame();
        }

        private void Update()
        {
            if (_renderer == null) return;
            if (idleFrames == null || idleFrames.Length == 0) return;

            bool running = runFrames != null && runFrames.Length > 0 && IsMoving();

            if (!_initialized || running != _running)
            {
                _running = running;
                _frame = 0;
                _timer = 0f;
                _initialized = true;
                ApplyFrame();
            }

            Sprite[] frames = CurrentFrames;
            if (frames == null || frames.Length == 0) return;

            float fps = Mathf.Max(0.01f, _running ? runFps : idleFps);
            _timer += Time.deltaTime * fps;

            if (frames.Length > 1)
            {
                while (_timer >= 1f)
                {
                    _timer -= 1f;
                    _frame = (_frame + 1) % frames.Length;
                    ApplyFrame();
                }
            }
            else
            {
                _timer = 0f;
            }

            UpdateFacing();
        }

        private bool IsMoving()
        {
            if (_body == null) return false;
            return _body.linearVelocity.sqrMagnitude > movingThreshold * movingThreshold;
        }

        private void UpdateFacing()
        {
            if (!flipToMovement || _renderer == null || _body == null) return;

            float vx = _body.linearVelocity.x;
            if (Mathf.Abs(vx) > movingThreshold)
                _renderer.flipX = vx < 0f;
        }

        private void ApplyFrame()
        {
            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();

            Sprite[] frames = CurrentFrames;
            if (_renderer == null || frames == null || frames.Length == 0) return;

            _frame = Mathf.Clamp(_frame, 0, frames.Length - 1);
            _renderer.sprite = frames[_frame];
        }
    }
}
