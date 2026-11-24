using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// ปุ่มหน้าเมนูหลักแบบ "จุ้ยซี่"
    /// - ชี้: ขยายแบบเด้ง (สปริง), แถบแดงกวาดเข้ามาจากซ้าย, ตัวหนังสือเลื่อนขวา + ห่างขึ้น, เสียง Hover
    /// - กด: ยุบ แล้วเด้งกลับ + แถบกระพริบขาว + เสียงคลิก
    /// - ตอนเปิดหน้า: ไถลเข้ามาจากซ้ายทีละปุ่ม (MainMenuUI สั่ง)
    /// ใช้เวลาจริง (unscaled) ทั้งหมด
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("ชิ้นส่วน")]
        public RectTransform visual;       // ชิ้นที่ขยาย/ยุบ (ลูกของปุ่ม)
        public RectTransform slab;         // แถบแดงด้านหลัง (pivot ซ้าย, ยืดแกน X)
        public Image slabImage;
        public RectTransform tick;         // ขีดเล็กด้านซ้าย
        public Image tickImage;
        public TMP_Text label;

        [Header("คำอธิบายตอนชี้ (แสดงที่ Hint ด้านล่าง)")]
        [TextArea] public string hint = "";

        [Header("สี")]
        public Color slabColor = new Color(0.66f, 0.13f, 0.14f, 1f);
        public Color labelIdle = new Color(0.86f, 0.86f, 0.85f, 1f);
        public Color labelHover = Color.white;
        public Color tickIdle = new Color(0.55f, 0.55f, 0.56f, 1f);

        [Header("การเคลื่อนไหว")]
        public float hoverScale = 1.12f;
        public float pressScale = 0.92f;
        public float stiffness = 520f;
        public float damping = 17f;
        public float labelShift = 30f;
        public float labelSpacingHover = 6f;

        [Header("เสียง")]
        public string hoverSound = "SFX_Menu_Hover";
        public string clickSound = "SFX_Menu_Click";

        public System.Action onClick;
        public bool Interactable { get; set; } = true;

        private bool _hover, _down;
        private float _scale = 1f, _vel;
        private float _slabK, _shake, _flash;
        private float _labelX, _labelBaseX, _spacingBase;
        private CanvasGroup _cg;
        private float _enterDelay = -1f, _enterT = 1f;
        private Vector2 _visualBase;

        private void Awake()
        {
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            if (label != null)
            {
                _labelBaseX = label.rectTransform.anchoredPosition.x;
                _spacingBase = label.characterSpacing;
            }
            if (visual != null) _visualBase = visual.anchoredPosition;
            if (slab != null) slab.localScale = new Vector3(0f, 1f, 1f);
        }

        private void OnDisable()
        {
            _hover = _down = false;
        }

        // ------------------------------------------------------------ entrance

        /// <summary>ไถลเข้ามาจากซ้ายหลังรอ delay วินาที</summary>
        public void PlayEntrance(float delay)
        {
            _enterDelay = delay;
            _enterT = 0f;
            if (_cg != null) _cg.alpha = 0f;
        }

        // ------------------------------------------------------------ pointer

        public void OnPointerEnter(PointerEventData e)
        {
            if (!Interactable) return;
            _hover = true;
            _vel += 6f;        // เตะสปริงให้เด้ง
            _shake = 1f;
            if (!string.IsNullOrEmpty(hoverSound)) Sfx.UI(hoverSound, 0.85f, Random.Range(0.94f, 1.08f));
            if (MainMenuUI.Instance != null) MainMenuUI.Instance.ShowHint(hint);
        }

        public void OnPointerExit(PointerEventData e)
        {
            _hover = false;
            _down = false;
            if (MainMenuUI.Instance != null) MainMenuUI.Instance.ShowHint("");
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!Interactable || e.button != PointerEventData.InputButton.Left) return;
            _down = true;
        }

        public void OnPointerUp(PointerEventData e) => _down = false;

        public void OnPointerClick(PointerEventData e)
        {
            if (!Interactable || e.button != PointerEventData.InputButton.Left) return;
            _vel += 9f;
            _flash = 1f;
            _shake = 1f;
            if (!string.IsNullOrEmpty(clickSound)) Sfx.UI(clickSound);
            onClick?.Invoke();
        }

        // ------------------------------------------------------------ update

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

            // entrance
            if (_enterT < 1f)
            {
                if (_enterDelay > 0f) _enterDelay -= dt;
                else _enterT = Mathf.Min(1f, _enterT + dt / 0.45f);
                float k = 1f - Mathf.Pow(1f - _enterT, 3f);
                if (_cg != null) _cg.alpha = k;
                if (visual != null) visual.anchoredPosition = _visualBase + new Vector2((1f - k) * -90f, 0f);
            }

            // spring scale
            float target = !Interactable ? 1f : _down ? pressScale : _hover ? hoverScale : 1f;
            float acc = (target - _scale) * stiffness - _vel * damping;
            _vel += acc * dt;
            _scale += _vel * dt;
            if (visual != null) visual.localScale = Vector3.one * _scale;

            // slab wipe
            float slabTarget = _hover && Interactable ? 1f : 0f;
            _slabK = Mathf.Lerp(_slabK, slabTarget, 1f - Mathf.Exp(-(slabTarget > _slabK ? 22f : 14f) * dt));
            _shake = Mathf.Max(0f, _shake - dt * 5f);
            _flash = Mathf.Max(0f, _flash - dt * 4f);
            if (slab != null)
            {
                float jitter = _shake > 0f ? Mathf.Sin(Time.unscaledTime * 90f) * _shake * 0.04f : 0f;
                slab.localScale = new Vector3(Mathf.Max(0f, _slabK + jitter), 1f, 1f);
            }
            if (slabImage != null) slabImage.color = Color.Lerp(slabColor, Color.white, _flash * 0.8f);

            // label
            _labelX = Mathf.Lerp(_labelX, _hover ? labelShift : 0f, 1f - Mathf.Exp(-16f * dt));
            if (label != null)
            {
                Vector2 p = label.rectTransform.anchoredPosition;
                p.x = _labelBaseX + _labelX;
                label.rectTransform.anchoredPosition = p;
                label.characterSpacing = _spacingBase + labelSpacingHover * (_labelX / Mathf.Max(1f, labelShift));
                label.color = Color.Lerp(labelIdle, labelHover, _slabK);
            }
            if (tickImage != null) tickImage.color = Color.Lerp(tickIdle, Color.white, _slabK);
            if (tick != null) tick.localScale = new Vector3(1f, 1f + 0.35f * _slabK, 1f);
        }
    }
}
