using UnityEngine;

namespace NecroDefense
{
    /// <summary>ขอบเขตแผนที่ (ผู้เล่นออกนอกกรอบนี้ไม่ได้) - ดูกรอบสีเหลืองใน Scene View</summary>
    public class MapBounds : MonoBehaviour
    {
        [Tooltip("ขนาดพื้นที่เล่นได้ (กว้าง x สูง)")]
        [SerializeField] private Vector2 size = new Vector2(40f, 24f);

        public Vector2 Center => transform.position;
        public Vector2 Size => size;

        public Vector2 Clamp(Vector2 p)
        {
            Vector2 half = size * 0.5f;
            Vector2 c = Center;
            return new Vector2(Mathf.Clamp(p.x, c.x - half.x, c.x + half.x), Mathf.Clamp(p.y, c.y - half.y, c.y + half.y));
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(size.x, size.y, 0f));
        }
    }
}
