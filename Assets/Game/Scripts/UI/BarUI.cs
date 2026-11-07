using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>หลอด UI ทั่วไป (Fill Image แบบ Filled + ข้อความ)</summary>
    public class BarUI : MonoBehaviour
    {
        [Tooltip("Image ที่ตั้ง Image Type = Filled")]
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label;
        [Tooltip("{0} = ชื่อ, {1} = ค่าปัจจุบัน, {2} = ค่าสูงสุด")]
        [SerializeField] private string format = "{0} {1}/{2}";
        [SerializeField] private string title = "HP";

        public void SetValue(float current, float max)
        {
            if (fill != null) fill.fillAmount = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (label != null) label.text = string.Format(format, title, Mathf.CeilToInt(current), Mathf.CeilToInt(max));
        }
    }
}
