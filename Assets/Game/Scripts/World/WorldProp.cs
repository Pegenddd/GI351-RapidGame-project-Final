using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ของตกแต่งในแมพ (ต้นไม้ / พุ่มไม้ / หิน / ตอไม้)
    /// - ผู้เล่นเดินทะลุโคนไม่ได้ (วงรีสีเขียวใน Scene View = พื้นที่ที่ชน) แต่เดินอ้อมไปด้านหลังได้
    /// - เรียงหน้า-หลังกับผู้เล่น / ป้อม ตามแกน Y (อยู่ใน SortingGroup ของลูก "Sorted")
    /// </summary>
    public class WorldProp : MonoBehaviour
    {
        public static readonly List<WorldProp> All = new List<WorldProp>();

        [Tooltip("ผู้เล่นเดินทะลุโคนไม่ได้")]
        [SerializeField] private bool blockPlayer = true;
        [Tooltip("ขนาดวงรีที่ชน (กว้าง x สูง หน่วยในโลก)")]
        [SerializeField] private Vector2 footprintSize = new Vector2(0.5f, 0.25f);
        [Tooltip("เลื่อนวงรีจากจุดโคน")]
        [SerializeField] private Vector2 footprintOffset = Vector2.zero;

        public bool BlocksPlayer => blockPlayer && isActiveAndEnabled;
        public Vector2 FootprintCenter => (Vector2)transform.position + footprintOffset;
        public Vector2 FootprintSize => footprintSize;

        public void SetFootprint(Vector2 size, Vector2 offset, bool block)
        {
            footprintSize = size;
            footprintOffset = offset;
            blockPlayer = block;
        }

        private void OnEnable() => All.Add(this);
        private void OnDisable() => All.Remove(this);

        private void OnDrawGizmosSelected()
        {
            if (!blockPlayer) return;
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.9f);
            Vector3 c = FootprintCenter;
            const int n = 32;
            for (int i = 0; i < n; i++)
            {
                float a0 = i * Mathf.PI * 2f / n, a1 = (i + 1) * Mathf.PI * 2f / n;
                Gizmos.DrawLine(c + new Vector3(Mathf.Cos(a0) * footprintSize.x * 0.5f, Mathf.Sin(a0) * footprintSize.y * 0.5f),
                                c + new Vector3(Mathf.Cos(a1) * footprintSize.x * 0.5f, Mathf.Sin(a1) * footprintSize.y * 0.5f));
            }
        }
    }
}
