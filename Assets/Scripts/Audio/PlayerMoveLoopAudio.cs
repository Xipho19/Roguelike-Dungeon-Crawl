using UnityEngine;

namespace Roguelike.Audio
{
    /// <summary>
    /// 玩家移动循环音：用独立 AudioSource 循环播放一段移动音，
    /// 按刚体速度淡入/淡出；停下、暂停、死亡时自动静音。
    ///
    /// 单独占用一个音源，避免和 AudioService 的一次性音效互相打断。
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerMoveLoopAudio : MonoBehaviour
    {
        [Tooltip("留空时从 Resources/Audio/Custom/Sticky_Run 加载")]
        [SerializeField] private AudioClip clip;

        [SerializeField, Range(0f, 1f)] private float moveVolume = 0.4f;

        [Tooltip("刚体速度超过该值视为正在移动")]
        [SerializeField] private float moveThreshold = 0.2f;

        [Tooltip("淡入淡出速度（音量/秒）")]
        [SerializeField] private float fadeSpeed = 6f;

        private Rigidbody2D _body;
        private AudioSource _source;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();

            if (clip == null)
                clip = Resources.Load<AudioClip>("Audio/Custom/Sticky_Run");

            if (clip == null)
            {
                Debug.LogWarning("[Audio] 找不到移动循环音 Audio/Custom/Sticky_Run，移动音效已禁用。", this);
                enabled = false;
                return;
            }

            var sourceObject = new GameObject("MoveLoopAudio");
            sourceObject.transform.SetParent(transform, false);

            _source = sourceObject.AddComponent<AudioSource>();
            _source.clip = clip;
            _source.loop = true;
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
            _source.volume = 0f;
            _source.Play(); // 一直在后台循环，靠音量控制有无
        }

        private void Update()
        {
            if (_source == null) return;

            // 暂停/死亡时 timeScale 为 0，此时强制静音
            bool paused = Time.timeScale <= 0.001f;
            bool moving = !paused
                && _body != null
                && _body.linearVelocity.sqrMagnitude > moveThreshold * moveThreshold;

            float target = moving ? moveVolume : 0f;
            _source.volume = Mathf.MoveTowards(
                _source.volume,
                target,
                fadeSpeed * Time.unscaledDeltaTime);
        }
    }
}
