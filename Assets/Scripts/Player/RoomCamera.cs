using Roguelike.Dungeon;
using UnityEngine;

namespace Roguelike.Player
{
    /// <summary>
    /// 跟随玩家的相机。
    ///
    /// 相机平滑跟随角色移动，不再吸附房间中心；
    /// 切换房间时由平滑跟随自然过渡，不再有整屏的定位动画。
    /// 玩家受伤时附带一次短促的屏幕震动。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class RoomCamera : MonoBehaviour
    {
        [SerializeField] private PlayerRoomTracker tracker;

        [Tooltip("跟随的平滑时间（秒），越小越紧跟，0 表示完全硬跟随")]
        [SerializeField] private float followSmoothTime = 0.1f;

        [Tooltip("正交视野半高（世界单位）")]
        [SerializeField] private float followOrthoSize = 4.25f;

        [Header("受击震动")]
        [SerializeField] private float shakeDuration = 0.22f;
        [SerializeField] private float shakeMagnitude = 0.35f;

        private Camera _camera;
        private PlayerHealth _playerHealth;
        private Transform _followTarget;
        private Vector3 _basePosition;
        private Vector3 _velocity;
        private bool _hasBase;
        private float _shakeUntil;
        private float _shakeDuration = 0.22f;
        private float _shakeMagnitude;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _basePosition = transform.position;

            _playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (_playerHealth != null) _playerHealth.Damaged += OnPlayerDamaged;
        }

        private void OnDestroy()
        {
            if (_playerHealth != null) _playerHealth.Damaged -= OnPlayerDamaged;
        }

        private void LateUpdate()
        {
            Transform target = ResolveTarget();

            if (target != null)
            {
                Vector3 desired = new Vector3(
                    target.position.x,
                    target.position.y,
                    _basePosition.z);

                if (!_hasBase || followSmoothTime <= 0.0001f)
                {
                    _basePosition = desired;
                    _velocity = Vector3.zero;
                    _hasBase = true;
                }
                else
                {
                    _basePosition = Vector3.SmoothDamp(
                        _basePosition,
                        desired,
                        ref _velocity,
                        followSmoothTime,
                        Mathf.Infinity,
                        Time.deltaTime);
                }
            }
            else if (!_hasBase)
            {
                _basePosition = transform.position;
                _hasBase = true;
            }

            if (_camera != null)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = followOrthoSize;
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

        /// <summary>切换楼层主题时设置相机纯色背景。</summary>
        public void SetBackgroundColor(Color color)
        {
            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera == null) return;

            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = color;
        }

        /// <summary>立即把相机对准玩家（编辑器搭建、换层、重开与开局使用）。</summary>
        public void SnapTo(RoomView room)
        {
            Transform target = ResolveTarget();
            Vector3 point = target != null
                ? target.position
                : room != null
                    ? (Vector3)room.WorldCenter
                    : _basePosition;

            _basePosition = new Vector3(point.x, point.y, transform.position.z);
            _velocity = Vector3.zero;
            _hasBase = true;

            if (_camera == null) _camera = GetComponent<Camera>();
            if (_camera != null)
            {
                _camera.orthographic = true;
                _camera.orthographicSize = followOrthoSize;
            }

            transform.position = _basePosition;
        }

        private Transform ResolveTarget()
        {
            if (_followTarget != null) return _followTarget;

            if (tracker == null) tracker = FindFirstObjectByType<PlayerRoomTracker>();
            _followTarget = tracker != null ? tracker.transform : null;

            return _followTarget;
        }
    }
}
