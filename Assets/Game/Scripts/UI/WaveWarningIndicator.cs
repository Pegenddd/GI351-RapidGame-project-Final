using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>ไอคอนเตือนเวฟ 1 อัน (กะโหลก + ลูกศร + รูปศัตรู + จำนวน)</summary>
    public class WaveWarningIndicator : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private RectTransform arrow;
        [SerializeField] private TMP_Text countText;
        [Tooltip("ช่องรูปศัตรูที่จะมา (โชว์ได้สูงสุดเท่าจำนวนช่อง)")]
        [SerializeField] private List<Image> enemyIcons = new List<Image>();
        [SerializeField] private TooltipTrigger tooltip;

        private Action _onClick;
        private bool _wired;

        public void Bind(Action onClick)
        {
            _onClick = onClick;
            if (!_wired && button != null) { button.onClick.AddListener(() => _onClick?.Invoke()); _wired = true; }
        }

        public void Show(Vector2 screenPos, float arrowAngle, PathPreview pv, string tip)
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            ((RectTransform)transform).position = screenPos;
            if (arrow != null) arrow.localRotation = Quaternion.Euler(0f, 0f, arrowAngle);
            if (countText != null) countText.text = "x" + pv.Total;

            int i = 0;
            foreach (var kv in pv.counts)
            {
                if (i >= enemyIcons.Count) break;
                Image img = enemyIcons[i];
                if (img != null)
                {
                    img.gameObject.SetActive(true);
                    img.sprite = kv.Key.Icon;
                    img.preserveAspect = true;
                }
                i++;
            }
            for (; i < enemyIcons.Count; i++) if (enemyIcons[i] != null) enemyIcons[i].gameObject.SetActive(false);
            if (tooltip != null) tooltip.Set("Enemies incoming!", tip);
        }

        public void Hide()
        {
            if (gameObject.activeSelf) gameObject.SetActive(false);
        }
    }
}
