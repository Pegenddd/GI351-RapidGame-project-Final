using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// "พื้นที่อิสระ" ลานกลางแมพ (กรอบฟ้าใน Scene View)
    /// ศัตรูที่ยังอยู่บนทางมอน (นอกกรอบนี้) จะสนแค่มินเนียน ไม่สนผู้เล่น/ป้อมหลัก
    /// พอเดินเข้ากรอบนี้แล้วถึงจะเริ่มเล็งเป้าตามที่ตั้งใน Prefab
    /// </summary>
    public class FreeZone : MonoBehaviour
    {
        [SerializeField] private Vector2 size = new Vector2(15f, 7f);

        public static FreeZone Instance { get; private set; }

        private void OnEnable() => Instance = this;
        private void OnDisable() { if (Instance == this) Instance = null; }

        public bool Contains(Vector2 p)
        {
            Vector2 c = transform.position;
            Vector2 h = size * 0.5f;
            return p.x >= c.x - h.x && p.x <= c.x + h.x && p.y >= c.y - h.y && p.y <= c.y + h.y;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f);
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
        }
    }
}
