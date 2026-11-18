using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// ปุ่มตอบสนอง: ชี้ = ขยายเล็กน้อย + เสียง Hover / กด = ยุบ + เสียงคลิก (ใช้เวลาจริง เล่นได้ตอน Pause)
    /// ใส่ให้ทุกปุ่มอัตโนมัติด้วยเมนู Tools > NecroDefense > Update - UI Theme + Audio
    /// </summary>
    public class UIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Tooltip("เสียงตอนชี้ (ว่าง = ไม่มี)")]
        public string hoverSound = "SFX_UI_Hover";
        [Tooltip("เสียงตอนกด (ว่าง = ไม่มี เช่น ปุ่มที่โค้ดเล่นเสียงเองอยู่แล้ว)")]
        public string clickSound = "SFX_UI_Click";
        [Tooltip("ขยาย/ยุบตอนชี้-กด (ปิดไว้ถ้ามีสคริปต์อื่นคุม Scale อยู่)")]
        public bool animateScale = true;
        public float hoverScale = 1.06f;
        public float pressScale = 0.93f;
        public float speed = 18f;

        private Selectable _sel;
        private UIPopIn _pop;
        private bool _hover, _down;
        private float _s = 1f;
        private Vector3 _base = Vector3.one;

        public float FxScale => _s;

        private void Awake()
        {
            _sel = GetComponent<Selectable>();
            _pop = GetComponent<UIPopIn>();
            _base = transform.localScale;
        }

        private void OnDisable()
        {
            _hover = _down = false;
            _s = 1f;
            if (animateScale && _pop == null) transform.localScale = _base;
        }

        private bool Interactable => _sel == null || _sel.IsInteractable();

        public void OnPointerEnter(PointerEventData e)
        {
            _hover = true;
            if (Interactable && !string.IsNullOrEmpty(hoverSound)) Sfx.UI(hoverSound, 0.6f);
        }

        public void OnPointerExit(PointerEventData e) { _hover = false; _down = false; }
        public void OnPointerDown(PointerEventData e) { if (e.button == PointerEventData.InputButton.Left) _down = true; }
        public void OnPointerUp(PointerEventData e) { _down = false; }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || !Interactable) return;
            if (!string.IsNullOrEmpty(clickSound)) Sfx.UI(clickSound);
            _s = pressScale * 0.97f; // เด้งเล็กน้อย
        }

        private void Update()
        {
            if (!animateScale) return;
            float target = !Interactable ? 1f : _down ? pressScale : _hover ? hoverScale : 1f;
            _s = Mathf.Lerp(_s, target, 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime));
            if (_pop == null) transform.localScale = _base * _s;
        }
    }
}
