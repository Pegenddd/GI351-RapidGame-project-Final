using UnityEngine;

namespace NecroDefense.Towers
{
    [RequireComponent(typeof(Collider2D))]
    public class TowerBaseSlot : MonoBehaviour
    {
        [Header("Slot State")]
        [SerializeField] private bool isOccupied = false;

        [Header("Placement Offset")]
        [Tooltip("จุดสำหรับวางป้อม (ถ้าลาก Transform ลูกมาใส่ ป้อมจะไปเกิดตรงนั้น)")]
        [SerializeField] private Transform buildPoint;

        [Tooltip("ระยะเยื้องขึ้นด้านบนแนวตั้ง (ใช้กรณีไม่ได้ใส่ buildPoint)")]
        [SerializeField] private Vector2 buildOffset = new Vector2(0f, 0.5f);

        private GameObject _currentTower;

        public bool IsOccupied => isOccupied;

        /// <summary>
        /// คำนวณหาตำแหน่งวางป้อมที่แท้จริง (ขยับขึ้นข้างบนแท่น)
        /// </summary>
        public Vector3 GetBuildPosition()
        {
            if (buildPoint != null)
            {
                return buildPoint.position;
            }
            return transform.position + (Vector3)buildOffset;
        }

        public void AssignTower(GameObject tower)
        {
            _currentTower = tower;
            isOccupied = (_currentTower != null);
        }

        public void ClearSlot()
        {
            _currentTower = null;
            isOccupied = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(GetBuildPosition(), 0.2f);
        }
    }
}