using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// หลอด UI ทั่วไป (Fill Image แบบ Filled + ข้อความ)
    /// - หลอดวิ่งนุ่มๆ ไปหาค่าใหม่
    /// - ตอนลด: มีแถบจาง (Trail) ค้างไว้แล้วค่อยไหลตาม ให้เห็นว่าเสียไปเท่าไหร่
    /// - ค่าต่ำกว่าที่ตั้งไว้: หลอดกระพริบเตือน
    /// </summary>
    public class BarUI : MonoBehaviour
    {
        [Tooltip("Image ที่ตั้ง Image Type = Filled")]
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label;
        [Tooltip("{0} = ชื่อ, {1} = ค่าปัจจุบัน, {2} = ค่าสูงสุด")]
        [SerializeField] private string format = "{0} {1}/{2}";
        [SerializeField] private string title = "HP";

        [Header("Animation")]
        [SerializeField] private bool animate = true;
        [Tooltip("ความเร็วหลอดวิ่งไปหาค่าใหม่")]
        [SerializeField] private float fillSpeed = 10f;
        [Tooltip("แถบจางตอนค่าลด (ปิดได้ เช่น หลอด XP)")]
        [SerializeField] private bool useTrail = true;
        [Tooltip("แถบจาง (ว่าง = สร้างให้อัตโนมัติจาก Fill)")]
        [SerializeField] private Image trail;
        [SerializeField] private Color trailColor = new Color(1f, 1f, 1f, 0.4f);
        [SerializeField] private float trailDelay = 0.35f;
        [Tooltip("แถบจางไหลตามกี่ % ต่อวินาที (1 = เต็มหลอดใน 1 วิ)")]
        [SerializeField] private float trailSpeed = 0.9f;
        [Tooltip("กระพริบเมื่อค่าต่ำกว่า (0.3 = 30%, 0 = ปิด)")]
        [Range(0f, 1f)] [SerializeField] private float lowPulseBelow = 0f;
        [SerializeField] private Color lowPulseColor = new Color(1f, 0.55f, 0.55f, 1f);

        private float _target = -1f, _shown, _trailValue, _trailWait;
        private Color _fillBase;
        private bool _init, _hasBaseColor;

        /// <summary>เปลี่ยนชื่อบนหลอด (เช่น Mana -> Soul)</summary>
        public void SetTitle(string newTitle) => title = newTitle;

        /// <summary>เปลี่ยนสีหลอด (ใช้ก่อน SetValue ครั้งแรกได้)</summary>
        public void SetBaseColor(Color color)
        {
            _hasBaseColor = true;
            _fillBase = color;
            if (fill != null) fill.color = color;
        }

        public void SetValue(float current, float max)
        {
            float v = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (label != null) label.text = string.Format(format, title, Mathf.CeilToInt(current), Mathf.CeilToInt(max));
            if (fill == null) return;

            if (!animate || !Application.isPlaying)
            {
                fill.fillAmount = v;
                return;
            }
            Init();
            if (_target < 0f) { _shown = _trailValue = v; }
            else if (v < _target - 0.0001f)
            {
                if (_trailWait <= 0f) _trailValue = Mathf.Max(_trailValue, _shown);
                _trailWait = trailDelay;
            }
            _target = v;
        }

        private void Init()
        {
            if (_init) return;
            _init = true;
            if (!_hasBaseColor) _fillBase = fill.color;
            if (useTrail && trail == null)
            {
                GameObject go = Instantiate(fill.gameObject, fill.transform.parent);
                go.name = "Trail";
                for (int i = go.transform.childCount - 1; i >= 0; i--) Destroy(go.transform.GetChild(i).gameObject);
                go.transform.SetSiblingIndex(fill.transform.GetSiblingIndex());
                trail = go.GetComponent<Image>();
                trail.raycastTarget = false;
            }
            if (trail != null) trail.color = trailColor;
        }

        private void Update()
        {
            if (!_init || fill == null || _target < 0f) return;
            float dt = Time.unscaledDeltaTime;
            _shown = Mathf.Lerp(_shown, _target, 1f - Mathf.Exp(-fillSpeed * dt));
            if (Mathf.Abs(_shown - _target) < 0.0005f) _shown = _target;

            if (_trailWait > 0f) _trailWait -= dt;
            else _trailValue = Mathf.MoveTowards(_trailValue, _shown, trailSpeed * dt);
            if (_trailValue < _shown) _trailValue = _shown;

            fill.fillAmount = _shown;
            if (trail != null) trail.fillAmount = useTrail ? _trailValue : 0f;

            if (lowPulseBelow > 0f && _target > 0f && _target < lowPulseBelow)
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
                fill.color = Color.Lerp(_fillBase, lowPulseColor, k);
            }
            else fill.color = _fillBase;
        }
    }
}
