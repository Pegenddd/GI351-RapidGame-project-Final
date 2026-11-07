using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ระบบป้อมปราการ (แบบ Kingdom Rush): คลิกแท่น -> เมนูวงกลมรอบแท่น -> กดไอคอน 1 ครั้งดูข้อมูล/ระยะ, กดซ้ำเพื่อยืนยัน
    /// สร้าง / อัปเกรด / ขายคืน (ได้ Souls คืน 70%)
    /// ลำดับใน Tower Prefabs = ลำดับไอคอนในเมนูวงกลม
    /// </summary>
    public class TowerManager : MonoBehaviour
    {
        [Header("Tower Prefabs (ลำดับ = ปุ่มใน UI)")]
        [SerializeField] private List<Tower> towerPrefabs = new List<Tower>();

        [Header("Economy")]
        [Tooltip("ขายคืนได้กี่ % ของที่จ่ายไปทั้งหมด (GDD = 70%)")]
        [Range(0f, 1f)]
        [SerializeField] private float refundPercent = 0.7f;
        [Tooltip("ตัวคูณราคา (การ์ด Soul Harvester ลดค่านี้)")]
        [SerializeField] private float costMultiplier = 1f;

        [Header("Scene")]
        [Tooltip("วงระยะยิงที่โชว์ตอนเลือก (Map/RangeIndicator)")]
        [SerializeField] private RangeIndicator rangeIndicator;

        public IReadOnlyList<Tower> TowerPrefabs => towerPrefabs;
        public TowerSlot SelectedSlot { get; private set; }
        public float RefundPercent => refundPercent;
        public RangeIndicator Range => rangeIndicator;

        public int GetBuildCost(Tower prefab) => prefab == null ? 0 : Mathf.CeilToInt(prefab.BaseCost * costMultiplier);
        public int GetUpgradeCost(Tower tower) => tower == null || tower.Next == null ? 0 : Mathf.CeilToInt(tower.Next.cost * costMultiplier);
        public int GetSellValue(Tower tower) => tower == null ? 0 : Mathf.FloorToInt(tower.TotalSpent * refundPercent);

        public void SelectSlot(TowerSlot slot)
        {
            if (SelectedSlot != null) SelectedSlot.SetSelected(false);
            SelectedSlot = slot;
            if (slot == null) { CloseSlotPanel(); return; }
            slot.SetSelected(true);
            GameManager.Instance.UI.TowerPanel.Show(slot);
        }

        public void CloseSlotPanel()
        {
            if (SelectedSlot != null) SelectedSlot.SetSelected(false);
            SelectedSlot = null;
            if (rangeIndicator != null) rangeIndicator.Hide();
            if (GameManager.Instance != null) GameManager.Instance.UI.TowerPanel.Hide();
        }

        public bool TryBuild(int towerIndex)
        {
            TowerSlot slot = SelectedSlot;
            if (slot == null || !slot.IsEmpty || towerIndex < 0 || towerIndex >= towerPrefabs.Count) return false;
            Tower prefab = towerPrefabs[towerIndex];
            int cost = GetBuildCost(prefab);
            if (!GameManager.Instance.Economy.TrySpendSouls(cost)) return false;

            Tower t = Instantiate(prefab, slot.TowerAnchor.position, Quaternion.identity, slot.TowerAnchor);
            t.name = prefab.name;
            t.Initialize(slot, cost);
            slot.SetTower(t);
            CloseSlotPanel();
            return true;
        }

        public bool TryUpgrade()
        {
            Tower t = SelectedSlot != null ? SelectedSlot.CurrentTower : null;
            if (t == null) return false;
            if (!t.CanUpgrade) { GameManager.Instance.UI.ShowMessage("Max level"); return false; }
            int cost = GetUpgradeCost(t);
            if (!GameManager.Instance.Economy.TrySpendSouls(cost)) return false;
            t.Upgrade(cost);
            CloseSlotPanel();
            return true;
        }

        public bool Sell()
        {
            TowerSlot slot = SelectedSlot;
            Tower t = slot != null ? slot.CurrentTower : null;
            if (t == null) return false;
            int refund = GetSellValue(t);
            GameManager.Instance.Economy.AddSouls(refund);
            slot.ClearTower();
            Destroy(t.gameObject);
            GameManager.Instance.UI.ShowMessage("+" + refund + " Souls");
            CloseSlotPanel();
            return true;
        }

        /// <summary>การ์ด Soul Harvester: ลดราคา percent (0.15 = ถูกลง 15%)</summary>
        public void ApplyDiscount(float percent) => costMultiplier = Mathf.Max(0.1f, costMultiplier * (1f - percent));
    }
}
