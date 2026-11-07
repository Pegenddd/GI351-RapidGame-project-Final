using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// เมนูวงกลมรอบแท่นป้อม (แบบ Kingdom Rush)
    /// - แท่นว่าง : ไอคอนป้อมแต่ละแบบ + ราคา  -> กด 1 ครั้ง = ดูข้อมูล/ระยะ, กดซ้ำ = สร้าง
    /// - มีป้อม   : ไอคอนอัปเกรด (บน) + ขาย (ล่าง) -> กด 1 ครั้ง = ดูข้อมูล, กดซ้ำ = ยืนยัน
    /// ตัวเมนูจะลอยตามตำแหน่งแท่นบนจอเอง
    /// </summary>
    public class TowerPanelUI : MonoBehaviour
    {
        [Tooltip("เลื่อนตำแหน่งเมนูจากจุดกลางแท่น (พิกเซล)")]
        [SerializeField] private Vector2 screenOffset = Vector2.zero;

        [Header("Build (แท่นว่าง) - ไอคอนที่ i = TowerManager > Tower Prefabs[i]")]
        [SerializeField] private GameObject buildGroup;
        [SerializeField] private List<RingOptionUI> buildOptions = new List<RingOptionUI>();

        [Header("Manage (มีป้อมแล้ว)")]
        [SerializeField] private GameObject manageGroup;
        [SerializeField] private RingOptionUI upgradeOption;
        [SerializeField] private RingOptionUI sellOption;
        [SerializeField] private Sprite maxLevelIcon;

        [Header("Info Box (โผล่ข้างเมนูตอนกดครั้งแรก)")]
        [SerializeField] private GameObject infoBox;
        [SerializeField] private TMP_Text infoTitle;
        [SerializeField] private TMP_Text infoBody;

        [Header("Bottom Bar")]
        [SerializeField] private TowerInfoBarUI infoBar;

        private const int UpgradeId = 100, SellId = 101;
        private TowerSlot _slot;
        private int _pending = -1;
        private bool _wired;
        private RectTransform _rt;

        public bool IsOpen => _slot != null && gameObject.activeSelf;

        private void Wire()
        {
            if (_wired) return;
            _wired = true;
            _rt = (RectTransform)transform;
            for (int i = 0; i < buildOptions.Count; i++)
            {
                int index = i;
                if (buildOptions[i] != null) buildOptions[i].Bind(() => Click(index));
            }
            if (upgradeOption != null) upgradeOption.Bind(() => Click(UpgradeId));
            if (sellOption != null) sellOption.Bind(() => Click(SellId));
        }

        public void Show(TowerSlot slot)
        {
            Wire();
            _slot = slot;
            _pending = -1;
            gameObject.SetActive(slot != null);
            Refresh();
            Follow();
        }

        public void Hide()
        {
            _slot = null;
            _pending = -1;
            gameObject.SetActive(false);
            if (infoBar != null) infoBar.Hide();
        }

        private void Click(int id)
        {
            if (_slot == null || !GameManager.IsGameplayRunning) return;
            TowerManager tm = GameManager.Instance.Towers;

            if (_pending != id)   // กดครั้งแรก = ดูข้อมูล
            {
                _pending = id;
                Refresh();
                return;
            }

            // กดซ้ำ = ยืนยัน
            if (id == UpgradeId) tm.TryUpgrade();
            else if (id == SellId) tm.Sell();
            else tm.TryBuild(id);
            _pending = -1;
            if (IsOpen) Refresh();
        }

        private void LateUpdate()
        {
            if (_slot == null) return;
            Follow();
            Refresh();
        }

        private void Follow()
        {
            if (_slot == null || _rt == null) return;
            Camera cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(_slot.transform.position);
            _rt.position = new Vector3(sp.x, sp.y, 0f) + (Vector3)(screenOffset * _rt.lossyScale.x);
        }

        private void Refresh()
        {
            if (_slot == null || GameManager.Instance == null) return;
            TowerManager tm = GameManager.Instance.Towers;
            int souls = GameManager.Instance.Economy.Souls;
            Tower t = _slot.CurrentTower;
            Vector2 slotPos = _slot.transform.position;

            if (buildGroup != null) buildGroup.SetActive(t == null);
            if (manageGroup != null) manageGroup.SetActive(t != null);

            string title = null, body = null;
            float rangePreview = -1f;

            if (t == null)
            {
                for (int i = 0; i < buildOptions.Count; i++)
                {
                    RingOptionUI o = buildOptions[i];
                    if (o == null) continue;
                    bool has = i < tm.TowerPrefabs.Count && tm.TowerPrefabs[i] != null;
                    o.gameObject.SetActive(has);
                    if (!has) continue;
                    Tower p = tm.TowerPrefabs[i];
                    int cost = tm.GetBuildCost(p);
                    o.SetIcon(p.Icon, p.IconColor);
                    o.SetCost(cost.ToString());
                    o.SetAvailable(souls >= cost);
                    o.SetConfirm(_pending == i);
                    if (_pending == i)
                    {
                        title = p.TowerName;
                        body = p.Description + "\n" + Stats(p.Current);
                        rangePreview = p.Current.range;
                    }
                }
                if (infoBar != null) infoBar.Hide();
            }
            else
            {
                if (upgradeOption != null)
                {
                    bool can = t.CanUpgrade;
                    int cost = tm.GetUpgradeCost(t);
                    if (!can && maxLevelIcon != null) upgradeOption.SetIcon(maxLevelIcon, Color.white);
                    upgradeOption.SetCost(can ? cost.ToString() : null);
                    upgradeOption.SetAvailable(!can || souls >= cost);
                    upgradeOption.SetConfirm(_pending == UpgradeId);
                    if (_pending == UpgradeId)
                    {
                        if (can)
                        {
                            title = t.TowerName + " - " + t.Next.label;
                            body = t.Next.description + "\n" + Stats(t.Next);
                            rangePreview = t.Next.range;
                        }
                        else { title = t.TowerName; body = "Max level"; }
                    }
                }
                if (sellOption != null)
                {
                    sellOption.SetCost("+" + tm.GetSellValue(t));
                    sellOption.SetAvailable(true);
                    sellOption.SetConfirm(_pending == SellId);
                    if (_pending == SellId)
                    {
                        title = "Sell";
                        body = "Get back " + tm.GetSellValue(t) + " Souls (" + Mathf.RoundToInt(tm.RefundPercent * 100f) + "%)";
                    }
                }
                if (rangePreview < 0f) rangePreview = t.Current.range;
                if (infoBar != null) infoBar.Show(t.TowerName, t.Icon, t.IconColor, t.Current);
            }

            if (infoBox != null) infoBox.SetActive(title != null);
            if (infoTitle != null) infoTitle.text = title ?? "";
            if (infoBody != null) infoBody.text = body ?? "";

            if (tm.Range != null)
            {
                if (rangePreview > 0f) tm.Range.Show(slotPos, rangePreview);
                else tm.Range.Hide();
            }
        }

        private static string Stats(TowerLevel lv)
        {
            return "DMG " + lv.damage.ToString("0.#") + (lv.targetsPerShot > 1 ? " x" + lv.targetsPerShot : "") +
                   "   SPD " + lv.attackInterval.ToString("0.##") + "s   RNG " + lv.range.ToString("0.#");
        }
    }
}
