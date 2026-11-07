using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>ช่องสกิล 1 ช่อง (ไอคอน + ปุ่มลัด + ราคา Mana + คูลดาวน์) ชื่อสกิลดูได้จาก Tooltip</summary>
    public class SkillSlotUI : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private TMP_Text costText;
        [Tooltip("Image แบบ Filled (Radial) ทับด้านบนตอนติดคูลดาวน์")]
        [SerializeField] private Image cooldownOverlay;
        [SerializeField] private TMP_Text cooldownText;
        [SerializeField] private Image background;
        [SerializeField] private TooltipTrigger tooltip;
        [Tooltip("ภาพแม่กุญแจทับตอนสกิลยังล็อก")]
        [SerializeField] private GameObject lockedOverlay;
        [SerializeField] private Color readyColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
        [SerializeField] private Color unavailableColor = new Color(0.35f, 0.08f, 0.08f, 0.95f);

        public Image Icon => icon;

        public void Setup(string key, string cost, string title, string description)
        {
            if (keyText != null) keyText.text = key;
            if (costText != null)
            {
                costText.text = cost;
                if (costText.transform.parent != transform) costText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(cost));
            }
            _title = title;
            _desc = description;
            if (tooltip != null) tooltip.Set(title, description);
        }

        private string _title, _desc;
        private bool _locked;

        /// <summary>ล็อก = โชว์แม่กุญแจ + Tooltip บอกวิธีปลด</summary>
        public void SetLocked(bool locked, string unlockHint)
        {
            if (_locked == locked && lockedOverlay != null && lockedOverlay.activeSelf == locked) return;
            _locked = locked;
            if (lockedOverlay != null) lockedOverlay.SetActive(locked);
            if (icon != null) icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, locked ? 0.25f : 1f);
            if (tooltip != null) tooltip.Set(_title + (locked ? " (Locked)" : ""), locked ? unlockHint : _desc);
        }

        public void SetState(float remaining, float total, bool affordable)
        {
            float t = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;
            if (cooldownOverlay != null) cooldownOverlay.fillAmount = t;
            if (cooldownText != null) cooldownText.text = remaining > 0.05f ? Mathf.CeilToInt(remaining).ToString() : "";
            if (background != null) background.color = affordable ? readyColor : unavailableColor;
        }
    }
}
