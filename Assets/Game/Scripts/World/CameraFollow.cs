using UnityEngine;
using UnityEngine.InputSystem;

namespace NecroDefense
{
    /// <summary>
    /// กล้องตามผู้เล่นแบบนุ่มๆ (ซูมด้วยลูกกลิ้งเมาส์ปิดไว้ - เปิดได้ที่ Enable Scroll Zoom)
    /// ขนาดซูมเริ่มต้น = Camera > Orthographic Size (ยิ่งน้อยยิ่งใกล้)
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [Tooltip("ยิ่งน้อยยิ่งตามไว (วินาที)")]
        [SerializeField] private float smoothTime = 0.15f;
        [SerializeField] private Vector2 offset = Vector2.zero;
        [Tooltip("ไม่ให้กล้องเลยขอบแผนที่ (ใส่ MapBounds)")]
        [SerializeField] private MapBounds clampToBounds;

        [Header("Zoom (ลูกกลิ้งเมาส์)")]
        [Tooltip("ปิดอยู่ = ซูมไม่ได้ กล้องใช้ขนาดคงที่ตาม Camera > Orthographic Size (ติ๊กถ้าอยากให้ซูมได้อีก)")]
        [SerializeField] private bool enableScrollZoom = false;
        [Tooltip("ซูมใกล้สุด (Orthographic Size)")]
        [SerializeField] private float minSize = 4.5f;
        [Tooltip("ซูมไกลสุด (Orthographic Size)")]
        [SerializeField] private float maxSize = 11f;
        [SerializeField] private float zoomStep = 0.8f;
        [SerializeField] private float zoomSmooth = 10f;

        private Vector3 _velocity;
        private Camera _cam;
        private float _targetSize;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            if (_cam != null) _targetSize = _cam.orthographicSize;
        }

        private void Start()
        {
            // เริ่มเกมให้กล้องอยู่ที่ผู้เล่นทันที (ไม่เลื่อนมาจากกลางแมพ)
            if (target != null) transform.position = Desired();
        }

        private void LateUpdate()
        {
            if (_cam != null && enableScrollZoom && _cam.orthographic)
            {
                Mouse m = Mouse.current;
                bool overUI = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
                if (m != null && !overUI)
                {
                    float scroll = m.scroll.ReadValue().y;
                    if (Mathf.Abs(scroll) > 0.01f) _targetSize = Mathf.Clamp(_targetSize - Mathf.Sign(scroll) * zoomStep, minSize, maxSize);
                }
                _cam.orthographicSize = Mathf.Lerp(_cam.orthographicSize, _targetSize, 1f - Mathf.Exp(-zoomSmooth * Time.unscaledDeltaTime));
            }

            if (target == null) return;
            transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref _velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
        }

        private Vector3 Desired()
        {
            Vector3 desired = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            if (clampToBounds != null && _cam != null && _cam.orthographic)
            {
                float halfH = _cam.orthographicSize;
                float halfW = halfH * _cam.aspect;
                Vector2 c = clampToBounds.Center;
                Vector2 half = clampToBounds.Size * 0.5f;
                desired.x = half.x > halfW ? Mathf.Clamp(desired.x, c.x - half.x + halfW, c.x + half.x - halfW) : c.x;
                desired.y = half.y > halfH ? Mathf.Clamp(desired.y, c.y - half.y + halfH, c.y + half.y - halfH) : c.y;
            }
            return desired;
        }
    }
}
