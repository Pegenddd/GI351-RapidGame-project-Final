using UnityEngine;

namespace NecroDefense.Enemies
{
    public class EnemyPathController : MonoBehaviour
    {
        [Header("Path Settings (Tower Defense Corridor)")]
        [Tooltip("รายการจุด Waypoints ที่มอนสเตอร์ต้องเดินตามในช่วงแรก (รับค่าอัตโนมัติจาก WaveManager)")]
        [SerializeField] private Transform[] waypoints;

        [Header("Arena Settings (Free Roam Area)")]
        [Tooltip("จุดกึ่งกลางหรือขอบเขตของพื้นที่อิสระตรงกลางแผนที่")]
        [SerializeField] private Vector2 arenaCenter;
        [SerializeField] private float arenaRadius = 5f;

        private int _currentWaypointIndex = 0;
        private bool _isInArena = false;
        private Vector2 _currentArenaTarget;

        public bool IsInArena => _isInArena;

        /// <summary>
        /// ฟังก์ชันสำหรับให้ WaveManager ส่งชุดเส้นทาง (Waypoints) มาให้มอนสเตอร์ตัวนี้โดยอัตโนมัติ
        /// </summary>
        public void SetWaypoints(Transform[] newWaypoints)
        {
            waypoints = newWaypoints;
            _currentWaypointIndex = 0;
            _isInArena = false;
        }

        /// <summary>
        /// คำนวณทิศทางการเคลื่อนที่ของมอนสเตอร์ (สลับระหว่างเดินตามทาง กับเดินอิสระในลานกว้าง)
        /// </summary>
        public Vector2 GetNextMovementTarget(Vector2 currentPosition, float moveSpeed, out bool reachedEnd)
        {
            reachedEnd = false;

            // ระยะห่างที่ถือว่าถึงเป้าหมายจุดนั้นแล้ว
            float reachThreshold = 0.2f;

            if (!_isInArena)
            {
                // โหมดที่ 1: เดินตาม Waypoints ในช่องทางเดิน
                if (waypoints != null && _currentWaypointIndex < waypoints.Length)
                {
                    Vector2 targetPos = waypoints[_currentWaypointIndex].position;
                    if (Vector2.Distance(currentPosition, targetPos) <= reachThreshold)
                    {
                        _currentWaypointIndex++; // ไปจุดต่อไป
                    }

                    if (_currentWaypointIndex >= waypoints.Length)
                    {
                        // เดินพ้นทางเดินแล้ว เข้าสู่โซนพื้นที่อิสระ (Free Roam Arena)
                        _isInArena = true;
                        SetNewRandomArenaTarget();
                    }
                    else
                    {
                        return targetPos;
                    }
                }
                else
                {
                    _isInArena = true;
                    SetNewRandomArenaTarget();
                }
            }

            // โหมดที่ 2: พื้นที่อิสระ (เดินสุ่มหรือพุ่งเข้าหาเป้าหมายในลานกว้าง)
            if (_isInArena)
            {
                if (Vector2.Distance(currentPosition, _currentArenaTarget) <= reachThreshold)
                {
                    SetNewRandomArenaTarget(); // ถึงแล้วสุ่มจุดเดินใหม่ในพื้นที่อิสระ
                }
                return _currentArenaTarget;
            }

            return currentPosition;
        }

        private void SetNewRandomArenaTarget()
        {
            // สุ่มจุดเดินไปมาในพื้นที่อิสระรอบๆ Arena Center
            Vector2 randomCircle = Random.insideUnitCircle * arenaRadius;
            _currentArenaTarget = arenaCenter + randomCircle;
        }
    }
}