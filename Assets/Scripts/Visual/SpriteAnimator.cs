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

        [Header("方向动画（Boss 用）")]
        [SerializeField] private Sprite[] idleDownFrames;
        [SerializeField] private Sprite[] runDownFrames;
        [SerializeField] private Sprite[] idleSideFrames;
        [SerializeField] private Sprite[] runSideFrames;
        [SerializeField] private Sprite[] idleUpFrames;
        [SerializeField] private Sprite[] runUpFrames;

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
        private bool _directional;
        private int _facingDirection; // 0 下 / 1 侧 / 2 上

        private Sprite[] CurrentFrames
        {
            get
            {
                if (!_directional) return _running ? runFrames : idleFrames;

                Sprite[] frames;
                if (_facingDirection == 0)
                    frames = _running ? runDownFrames : idleDownFrames;
                else if (_facingDirection == 2)
                    frames = _running ? runUpFrames : idleUpFrames;
                else
                    frames = _running ? runSideFrames : idleSideFrames;

                if (frames == null || frames.Length == 0)
                    frames = _running ? runDownFrames : idleDownFrames;

                return frames;
            }
        }

        /// <summary>由 Prefab 生成器写入帧序列。</summary>
        public void Configure(Sprite[] idle, Sprite[] run)
        {
            idleFrames = idle;
            runFrames = run;
            _directional = false;
            _facingDirection = 0;

            if (_renderer == null) _renderer = GetComponentInChildren<SpriteRenderer>();

            _running = false;
            _frame = 0;
            _timer = 0f;
            _initialized = false;
            ApplyFrame();
        }

        /// <summary>Boss 用：下 / 侧 / 上三组方向帧；侧向默认朝右，代码负责翻转。</summary>
        public void ConfigureDirectional(
            Sprite[] idleDown,
            Sprite[] runDown,
            Sprite[] idleSide,
            Sprite[] runSide,
            Sprite[] idleUp,
            Sprite[] runUp)
        {
            idleDownFrames = idleDown;
            runDownFrames = runDown;
            idleSideFrames = idleSide;
            runSideFrames = runSide;
            idleUpFrames = idleUp;
            runUpFrames = runUp;
            _directional = true;
            _facingDirection = 0;

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

            // 玩家、敌人、Boss 都挂了 SpriteAnimator，在这里统一补上 Y 排序，
            // 走动时才能正确地和石像、木箱等场景物件互相遮挡。
            // 显式把动画驱动的渲染器交给 YSort：Boss 的渲染器在 Visual 子物体上。
            YSort sort = GetComponent<YSort>();
            if (sort == null) sort = gameObject.AddComponent<YSort>();
            sort.SetTarget(_renderer);
        }

        private void OnEnable()
        {
            _running = false;
            _facingDirection = 0;
            _frame = 0;
            _timer = 0f;
            _initialized = false;
            ApplyFrame();
        }

        private void Update()
        {
            if (_renderer == null) return;
            if (idleFrames == null || idleFrames.Length == 0) return;

            bool running = (_directional
                ? (runDownFrames != null && runDownFrames.Length > 0)
                : (runFrames != null && runFrames.Length > 0)) && IsMoving();

            UpdateDirection();

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

        private void UpdateDirection()
        {
            if (!_directional || _body == null) return;

            Vector2 velocity = _body.linearVelocity;
            if (velocity.sqrMagnitude <= movingThreshold * movingThreshold) return;

            int direction;
            if (Mathf.Abs(velocity.x) >= Mathf.Abs(velocity.y) * 0.9f)
                direction = 1;
            else
                direction = velocity.y > 0f ? 2 : 0;

            if (direction == _facingDirection) return;

            _facingDirection = direction;
            _frame = 0;
            _timer = 0f;
        }

        private void UpdateFacing()
        {
            if (_renderer == null || _body == null) return;

            if (_directional)
            {
                if (_facingDirection == 1)
                {
                    float vx = _body.linearVelocity.x;
                    if (Mathf.Abs(vx) > movingThreshold)
                        _renderer.flipX = vx < 0f;
                }
                else
                {
                    _renderer.flipX = false;
                }

                return;
            }

            if (!flipToMovement) return;

            float horizontal = _body.linearVelocity.x;
            if (Mathf.Abs(horizontal) > movingThreshold)
                _renderer.flipX = horizontal < 0f;
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
