using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NecroDefense
{
    /// <summary>กล่องคำอธิบายเล็กๆ ที่ตามเมาส์ (โผล่ตอนชี้ที่ไอคอนที่มี TooltipTrigger)</summary>
    public class TooltipUI : MonoBehaviour
    {
        public static TooltipUI Instance { get; private set; }

        [SerializeField] private RectTransform box;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Vector2 offset = new Vector2(18f, 18f);

        private void Awake()
        {
            Instance = this;
            Hide();
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        public void Show(string title, string body)
        {
            if (box == null) return;
            if (titleText != null) titleText.text = title;
            if (bodyText != null) bodyText.text = body;
            box.gameObject.SetActive(true);
            Follow();
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
