using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// เส้นทางเดินของศัตรู 1 ทาง (วางบนจุดเกิด) - ศัตรูเดินตาม Waypoints ทีละจุดแล้วค่อยเข้าตีฐาน
    /// ย้ายจุด WP_x ใน Scene ได้เลย (เส้นสีแดงใน Scene View)
    /// </summary>
    public class EnemyPath : MonoBehaviour
    {
        [SerializeField] private List<Transform> waypoints = new List<Transform>();

        [Header("Wave Warning (เตือนก่อนเวฟ)")]
        [Tooltip("ป้ายเตือนในฉาก (เปิดตอนนับถอยหลังถ้าเวฟหน้ามีศัตรูมาทางนี้)")]
        [SerializeField] private GameObject warningMarker;
        [Tooltip("จุดที่ลูกศรเตือนบนจอชี้ไป (ว่าง = จุดเกิด)")]
        [SerializeField] private Transform warningPoint;
        [SerializeField] private float pulseSpeed = 4f;
        [SerializeField] private float pulseAmount = 0.15f;

        private Vector3 _markerScale = Vector3.one;
        private bool _warning;

        public Vector2 WarningPosition => warningPoint != null ? (Vector2)warningPoint.position : SpawnPosition;

        private void Awake()
        {
            if (warningMarker != null)
            {
                _markerScale = warningMarker.transform.localScale;
                warningMarker.SetActive(false);
            }
        }

        public void SetWarning(bool on)
        {
            _warning = on;
            if (warningMarker != null && warningMarker.activeSelf != on) warningMarker.SetActive(on);
        }

        private void Update()
        {
            if (_warning && warningMarker != null)
                warningMarker.transform.localScale = _markerScale * (1f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount);
        }

        public int Count => waypoints.Count;
        public Vector2 SpawnPosition => transform.position;
        public Vector2 GetPoint(int index) => waypoints[index] != null ? (Vector2)waypoints[index].position : SpawnPosition;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f);
            Vector3 prev = transform.position;
            foreach (Transform t in waypoints)
            {
                if (t == null) continue;
                Gizmos.DrawLine(prev, t.position);
                Gizmos.DrawWireSphere(t.position, 0.25f);
                prev = t.position;
            }
            if (Registry.Crystal != null) Gizmos.DrawLine(prev, Registry.Crystal.transform.position);
        }
    }
}
