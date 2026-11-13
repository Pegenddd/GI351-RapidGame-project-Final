using System.Collections.Generic;
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
        [Tooltip("สร้างป้อมแล้วซ่อนรูปจุดสร้างป้อม (วงรี) / ขายป้อมแล้วโชว์กลับมา")]
        [SerializeField] private bool hideWhenBuilt = true;

        // รูปของจุดสร้างป้อมเอง (เก็บตอนเริ่ม ก่อนมีป้อมมาเป็นลูก)
        private readonly List<SpriteRenderer> _slotVisuals = new List<SpriteRenderer>();

        public int SlotNumber => slotNumber;
        public Tower CurrentTower { get; private set; }
        public bool IsEmpty => CurrentTower == null;
        public Transform TowerAnchor => towerAnchor != null ? towerAnchor : transform;

        private void OnEnable() => Registry.TowerSlots.Add(this);
        private void OnDisable() => Registry.TowerSlots.Remove(this);

        private void Awake()
        {
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
                if (sr.GetComponentInParent<Tower>() == null) _slotVisuals.Add(sr);
        }

        private void Start() => SetSelected(false);

        public void SetTower(Tower tower)
        {
            CurrentTower = tower;
            ShowSlot(tower == null || !hideWhenBuilt);
        }

        public void ClearTower()
        {
            CurrentTower = null;
            ShowSlot(true);
        }

        private void ShowSlot(bool show)
        {
            for (int i = 0; i < _slotVisuals.Count; i++)
                if (_slotVisuals[i] != null) _slotVisuals[i].enabled = show;
        }

        public void SetSelected(bool selected)
        {
            if (slotRenderer != null) slotRenderer.color = selected ? selectedColor : normalColor;
        }
    }
}
