using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>แถบข้อมูลป้อมด้านล่างจอ (ชื่อ / ดาเมจ / ความเร็ว / ระยะ) โชว์ตอนเลือกป้อม</summary>
    public class TowerInfoBarUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text rangeText;

        public void Show(string towerName, Sprite sprite, Color color, TowerLevel lv)
        {
            gameObject.SetActive(true);
            if (icon != null) { icon.sprite = sprite; icon.color = color; icon.preserveAspect = true; }
            if (nameText != null) nameText.text = towerName + "  <size=70%>" + lv.label + "</size>";
            if (damageText != null) damageText.text = lv.damage.ToString("0.#") + (lv.targetsPerShot > 1 ? " x" + lv.targetsPerShot : "");
            if (speedText != null) speedText.text = lv.attackInterval.ToString("0.##") + "s";
            if (rangeText != null) rangeText.text = lv.range.ToString("0.#");
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
