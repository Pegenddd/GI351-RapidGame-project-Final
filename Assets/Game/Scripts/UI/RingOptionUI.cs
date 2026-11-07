using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// ไอคอนกลม 1 ปุ่มในเมนูวงกลม (สร้างป้อม / อัปเกรด / ขาย / คำสั่งมินเนียน)
    /// มีป้ายราคา, เครื่องหมายถูก (ตอนรอยืนยัน), และสถานะไฮไลต์
    /// </summary>
    public class RingOptionUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [Tooltip("ป้ายราคาด้านล่าง (ซ่อนได้)")]
        [SerializeField] private GameObject costBadge;
        [SerializeField] private TMP_Text costText;
        [Tooltip("เครื่องหมายถูก โผล่ตอนกดครั้งแรก (กดอีกครั้ง = ยืนยัน)")]
        [SerializeField] private GameObject confirmMark;
        [SerializeField] private Color normalColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color highlightColor = new Color(0.55f, 0.35f, 0.85f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.3f, 0.1f, 0.1f, 1f);

        private Action _onClick;
        private bool _available = true;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void Bind(Action onClick) => _onClick = onClick;

        public void SetIcon(Sprite sprite, Color color)
        {
            if (icon == null) return;
            if (sprite != null) icon.sprite = sprite;
            icon.color = color;
            icon.preserveAspect = true;
        }

        /// <summary>null = ซ่อนป้ายราคา</summary>
        public void SetCost(string label)
        {
            if (costBadge != null) costBadge.SetActive(!string.IsNullOrEmpty(label));
            if (costText != null) costText.text = label ?? "";
        }

        /// <summary>false = เงินไม่พอ (ยังกดได้แต่ขึ้นสีแดง)</summary>
        public void SetAvailable(bool available)
        {
            _available = available;
            Paint(false);
        }

        public void SetConfirm(bool waitingConfirm)
        {
            if (confirmMark != null) confirmMark.SetActive(waitingConfirm);
            Paint(waitingConfirm);
        }

        public void SetHighlighted(bool on) => Paint(on);

        private void Paint(bool highlighted)
        {
            if (background == null) return;
            background.color = !_available ? disabledColor : highlighted ? highlightColor : normalColor;
        }
    }
}
