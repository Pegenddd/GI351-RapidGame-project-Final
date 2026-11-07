using UnityEngine;

namespace NecroDefense
{
    /// <summary>แท่นวางป้อม 1 จุด (คลิกซ้ายเพื่อเปิดหน้าต่างสร้าง/อัปเกรด) - ย้าย/ก๊อป/ลบได้อิสระใน Hierarchy</summary>
    public class TowerSlot : MonoBehaviour
    {
        [SerializeField] private int slotNumber = 1;
        [SerializeField] private SpriteRenderer slotRenderer;
        [SerializeField] private Color normalColor = new Color(0.35f, 0.35f, 0.35f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.2f);
        [Tooltip("ป้อมที่สร้างแล้วจะไปอยู่เป็นลูกของ Transform นี้ (ว่าง = ตัวเอง)")]
        [SerializeField] private Transform towerAnchor;

        public int SlotNumber => slotNumber;
        public Tower CurrentTower { get; private set; }
        public bool IsEmpty => CurrentTower == null;
        public Transform TowerAnchor => towerAnchor != null ? towerAnchor : transform;

        private void OnEnable() => Registry.TowerSlots.Add(this);
        private void OnDisable() => Registry.TowerSlots.Remove(this);

        private void Start() => SetSelected(false);

        public void SetTower(Tower tower) => CurrentTower = tower;
        public void ClearTower() => CurrentTower = null;

        public void SetSelected(bool selected)
        {
            if (slotRenderer != null) slotRenderer.color = selected ? selectedColor : normalColor;
        }
    }
}
