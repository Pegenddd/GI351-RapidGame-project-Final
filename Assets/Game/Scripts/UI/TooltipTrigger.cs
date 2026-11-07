using UnityEngine;
using UnityEngine.EventSystems;

namespace NecroDefense
{
    /// <summary>ใส่บนไอคอน UI ใดก็ได้ -> ชี้เมาส์แล้วโชว์ Tooltip (ข้อความแก้ใน Inspector หรือสคริปต์ตั้งให้)</summary>
    public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private string title;
        [TextArea(1, 4)]
        [SerializeField] private string body;

        private bool _hover;

        public void Set(string newTitle, string newBody)
        {
            title = newTitle;
            body = newBody;
            if (_hover && TooltipUI.Instance != null) TooltipUI.Instance.Show(title, body);
        }

        public void OnPointerEnter(PointerEventData e)
        {
            _hover = true;
            if (TooltipUI.Instance != null && !string.IsNullOrEmpty(title + body)) TooltipUI.Instance.Show(title, body);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hover = false;
            if (TooltipUI.Instance != null) TooltipUI.Instance.Hide();
        }

        private void OnDisable()
        {
            if (_hover && TooltipUI.Instance != null) TooltipUI.Instance.Hide();
            _hover = false;
        }
    }
}
