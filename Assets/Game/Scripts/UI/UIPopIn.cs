using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>แผงเด้งเข้า (ขยาย + จางเข้า) ทุกครั้งที่เปิด ใช้เวลาจริง</summary>
    public class UIPopIn : MonoBehaviour
    {
        public float delay = 0f;
        public float duration = 0.2f;
        [Range(0.3f, 1f)] public float startScale = 0.86f;
        public bool fade = true;
        [Tooltip("เสียงตอนเปิด (ว่าง = ไม่มี)")]
        public string openSound = "";

        private CanvasGroup _cg;
        private UIButtonFX _fx;
        private float _t;
        private Vector3 _base = Vector3.one;
        private bool _hasBase;

        private void Awake()
        {
            _fx = GetComponent<UIButtonFX>();
            if (fade)
            {
                _cg = GetComponent<CanvasGroup>();
                if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            }
            _base = transform.localScale;
            _hasBase = true;
        }

        private void OnEnable()
        {
            if (!_hasBase) { _base = transform.localScale; _hasBase = true; }
            _t = -delay;
            Apply(0f);
            if (!string.IsNullOrEmpty(openSound) && Time.timeSinceLevelLoad > 0.3f) Sfx.UI(openSound);
        }

        private void OnDisable()
        {
            transform.localScale = _base;
            if (_cg != null) _cg.alpha = 1f;
        }

        private void Update()
        {
            _t += Time.unscaledDeltaTime;
            Apply(Mathf.Clamp01(_t / Mathf.Max(0.01f, duration)));
        }

        private void Apply(float k)
        {
            // ease-out-back (เกินนิดแล้วกลับ)
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float e = k <= 0f ? 0f : 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
            float s = Mathf.LerpUnclamped(startScale, 1f, e);
            float fx = _fx != null && _fx.animateScale ? _fx.FxScale : 1f;
            transform.localScale = _base * s * fx;
            if (_cg != null) _cg.alpha = Mathf.Clamp01(k * 1.6f);
        }
    }
}
