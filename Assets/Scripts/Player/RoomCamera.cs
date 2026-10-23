using Roguelike.Dungeon;
using UnityEngine;

namespace Roguelike.Player
{
    /// <summary>
    /// 房间吸附相机。
    ///
    /// 以撒式的固定视角：相机永远对准当前房间中心，切换房间时做一次短促的平滑位移，
    /// 而不是跟随玩家。缩放按房间 footprint 自动计算，保证整间房完整可见。
    /// 玩家受伤时附带一次短促的屏幕震动。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class RoomCamera : MonoBehaviour
    {
        [SerializeField] private PlayerRoomTracker tracker;

        [Tooltip("房间切换时的镜头过渡时长（秒），0 表示瞬间切换")]
        [SerializeField] private float transitionDuration = 0.18f;

        [Tooltip("房间四周额外留出的视野（单位）")]
        [SerializeField] private float padding = 0.5f;

        [Header("受击震动")]
        [SerializeField] private float shakeDuration = 0.22f;
        [SerializeField] private float shakeMagnitude = 0.35f;

        private Camera _camera;
        private PlayerHealth _playerHealth;
        private Vector3 _from;
        private Vector3 _target;
        private Vector3 _basePosition;
        private float _progress = 1f;
        private bool _hasTarget;
        private float _shakeUntil;
        private float _shakeDuration = 0.22f;
        private float _shakeMagnitude;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            _playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (_playerHealth != null) _playerHealth.Damaged += OnPlayerDamaged;
        }

        private void OnDestroy()
        {
            if (_playerHealth != null) _playerHealth.Damaged -= OnPlayerDamaged;
        }

        private void OnEnable()
        {
            if (tracker != null) tracker.RoomChanged += HandleRoomChanged;
        }

        private void OnDisable()
        {
            if (tracker != null) tracker.RoomChanged -= HandleRoomChanged;
        }

        private void LateUpdate()
        {
            if (!_hasTarget) return;

            if (_progress < 1f)
            {
                _progress = Mathf.Min(1f, _progress + Time.deltaTime / Mathf.Max(0.01f, transitionDuration));
                _basePosition = Vector3.Lerp(_from, _target, Mathf.SmoothStep(0f, 1f, _progress));
            }
            else
            {
                _basePosition = _target;
            }

            Vector3 shake = Vector3.zero;
            if (Time.time < _shakeUntil)
            {
                float remaining = Mathf.Clamp01((_shakeUntil - Time.time) / Mathf.Max(0.01f, _shakeDuration));
                Vector2 offset = Random.insideUnitCircle * (_shakeMagnitude * remaining);
                shake = new Vector3(offset.x, offset.y, 0f);
            }

            transform.position = _basePosition + shake;
        }

        private void OnPlayerDamaged()
        {
            Shake(shakeDuration, shakeMagnitude);
        }

        public void Shake(float duration, float magnitude)
        {
            _shakeDuration = Mathf.Max(0.01f, duration);
            _shakeUntil = Time.time + _shakeDuration;
            _shakeMagnitude = magnitude;
        }

        private void HandleRoomChanged(RoomView room)
        {
            if (room == null) return;

            _target = new Vector3(room.WorldCenter.x, room.WorldCenter.y, transform.position.z);
            ApplyZoom();

            if (!_hasTarget)
            {
                SnapTo(room);
                return;
            }

            _from = _basePosition;
            _progress = 0f;
        }

        /// <summary>立即把相机对准指定房间（编辑器搭建、重开与开局使用）。</summary>
        public void SnapTo(RoomView room)
        {
            if (room == null) return;

            _hasTarget = true;
            _target = new Vector3(room.WorldCenter.x, room.WorldCenter.y, transform.position.z);
            _from = _target;
            _basePosition = _target;
            _progress = 1f;
            transform.position = _target;
            ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null) return;

            float halfHeight = RoomGeometry.FootprintHeight * 0.5f + padding;
            float halfWidth = RoomGeometry.FootprintWidth * 0.5f + padding;
            float aspect = _camera.aspect > 0.01f ? _camera.aspect : 16f / 9f;

            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Max(halfHeight, halfWidth / aspect);
        }
    }
}
