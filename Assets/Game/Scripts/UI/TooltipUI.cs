using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// กล่องคำอธิบายเล็กๆ ที่ตามเมาส์ (โผล่ตอนชี้ที่ไอคอนที่มี TooltipTrigger)
    /// หน้าตา (ขนาดตัวอักษร / สี / พื้นหลัง) ปรับได้ที่หัวข้อ Look - ใช้ทับค่าในซีนตอนเริ่มเกม
    /// กล่องขยาย/หดตามความยาวข้อความอัตโนมัติ
    /// </summary>
    public class TooltipUI : MonoBehaviour
    {
        public static TooltipUI Instance { get; private set; }

        [SerializeField] private RectTransform box;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Vector2 offset = new Vector2(18f, 18f);

        [Header("Look (อ่านง่าย)")]
        [Tooltip("ใช้ค่าด้านล่างทับค่าในซีน (ปิด = ใช้ตามที่ตั้งในซีน)")]
        [SerializeField] private bool applyLook = true;
        [Tooltip("สีพื้นกล่อง (ทึบ ไม่ให้เห็นฉากข้างหลัง)")]
        [SerializeField] private Color boxColor = new Color(0.03f, 0.03f, 0.03f, 1f);
        [Tooltip("สีเส้นขอบกล่อง (Alpha 0 = ไม่มีขอบ)")]
        [SerializeField] private Color borderColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private float borderWidth = 2f;
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField] private Color bodyColor = new Color(0.9f, 0.9f, 0.9f, 1f);
        [Tooltip("ขนาดตัวอักษรหัวข้อ")]
        [SerializeField] private float titleSize = 28f;
        [Tooltip("ขนาดตัวอักษรเนื้อหา")]
        [SerializeField] private float bodySize = 21f;
        [Tooltip("ระยะห่างขอบในกล่อง (ซ้าย-ขวา, บน-ล่าง)")]
        [SerializeField] private Vector2 padding = new Vector2(16f, 12f);
        [Tooltip("ระยะระหว่างหัวข้อกับเนื้อหา")]
        [SerializeField] private float lineGap = 6f;
        [Tooltip("ความกว้างสูงสุดของกล่อง (ข้อความยาวกว่านี้จะขึ้นบรรทัดใหม่)")]
        [SerializeField] private float maxWidth = 460f;
        [Tooltip("ขยาย/หดกล่องตามความยาวข้อความ")]
        [SerializeField] private bool fitToText = true;

        private void Awake()
        {
            Instance = this;
            transform.SetAsLastSibling(); // อยู่บนสุดของ UI เสมอ
            ApplyLook();
            Hide();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void ApplyLook()
        {
            if (!applyLook || box == null) return;
            Image img = box.GetComponent<Image>();
            if (img != null) { img.color = boxColor; img.raycastTarget = false; }

            Outline border = box.GetComponent<Outline>();
            if (borderColor.a > 0f && borderWidth > 0f)
            {
                if (border == null) border = box.gameObject.AddComponent<Outline>();
                border.effectColor = borderColor;
                border.effectDistance = new Vector2(borderWidth, -borderWidth);
            }
            else if (border != null) border.enabled = false;

            // กล่องโปร่งเพราะ CanvasGroup -> ทำให้ทึบ
            CanvasGroup cg = box.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 1f;

            SetupText(titleText, titleColor, titleSize, FontStyles.Bold);
            SetupText(bodyText, bodyColor, bodySize, FontStyles.Normal);
        }

        private static void SetupText(TMP_Text t, Color c, float size, FontStyles style)
        {
            if (t == null) return;
            t.color = c;
            t.alpha = 1f;
            t.fontSize = size;
            t.enableAutoSizing = false;
            t.fontStyle = style;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.overflowMode = TextOverflowModes.Overflow;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.raycastTarget = false;
            RectTransform rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
        }

        public void Show(string title, string body)
        {
            if (box == null) return;
            if (titleText != null) { titleText.text = title; titleText.gameObject.SetActive(!string.IsNullOrEmpty(title)); }
            if (bodyText != null) { bodyText.text = body; bodyText.gameObject.SetActive(!string.IsNullOrEmpty(body)); }
            box.gameObject.SetActive(true);
            if (applyLook && fitToText) FitToText();
            Follow();
        }

        /// <summary>คำนวณขนาดกล่องจากความยาวข้อความ</summary>
        private void FitToText()
        {
            float inner = Mathf.Max(60f, maxWidth - padding.x * 2f);
            float w = 0f, y = padding.y;

            if (titleText != null && titleText.gameObject.activeSelf)
            {
                Vector2 s = titleText.GetPreferredValues(titleText.text, inner, 0f);
                s.x = Mathf.Min(s.x, inner);
                titleText.rectTransform.anchoredPosition = new Vector2(padding.x, -y);
                titleText.rectTransform.sizeDelta = new Vector2(inner, s.y);
                w = Mathf.Max(w, s.x);
                y += s.y + lineGap;
            }
            if (bodyText != null && bodyText.gameObject.activeSelf)
            {
                Vector2 s = bodyText.GetPreferredValues(bodyText.text, inner, 0f);
                s.x = Mathf.Min(s.x, inner);
                bodyText.rectTransform.anchoredPosition = new Vector2(padding.x, -y);
                bodyText.rectTransform.sizeDelta = new Vector2(inner, s.y);
                w = Mathf.Max(w, s.x);
                y += s.y;
            }
            else y -= lineGap;

            float boxW = Mathf.Ceil(w) + padding.x * 2f + 2f;
            if (titleText != null) titleText.rectTransform.sizeDelta = new Vector2(boxW - padding.x * 2f, titleText.rectTransform.sizeDelta.y);
            if (bodyText != null) bodyText.rectTransform.sizeDelta = new Vector2(boxW - padding.x * 2f, bodyText.rectTransform.sizeDelta.y);
            box.sizeDelta = new Vector2(boxW, Mathf.Ceil(y + padding.y));
        }

        public void Hide()
        {
            if (box != null) box.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (box != null && box.gameObject.activeSelf) Follow();
        }

        private void Follow()
        {
            Mouse m = Mouse.current;
            if (m == null) return;
            Vector2 p = m.position.ReadValue();
            // พลิกไปอีกฝั่งถ้าชิดขอบจอ
            float w = box.rect.width * box.lossyScale.x, h = box.rect.height * box.lossyScale.y;
            float x = p.x + offset.x + w > Screen.width ? p.x - offset.x - w : p.x + offset.x;
            float y = p.y + offset.y + h > Screen.height ? p.y - offset.y - h : p.y + offset.y;
            box.pivot = Vector2.zero;
            box.position = new Vector2(x, y);
        }
    }
}
