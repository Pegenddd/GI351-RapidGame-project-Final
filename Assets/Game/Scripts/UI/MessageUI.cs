using TMPro;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>ข้อความแจ้งเตือนกลางจอ (เช่น "Not enough Soul") แล้วค่อยๆ จางหาย</summary>
    public class MessageUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private float showTime = 1.6f;
        [SerializeField] private float fadeTime = 0.4f;

        private float _timer;
        private float _shake;
        private float _punch;
        [Tooltip("สีข้อความเตือน (เงินไม่พอ / คูลดาวน์ ฯลฯ)")]
        [SerializeField] private Color errorColor = new Color(1f, 0.2f, 0.2f, 1f);
        private Color _baseColor = Color.white;
        private bool _hasColor;
        private Vector2 _basePos;
        private bool _hasBase;

        private void Awake()
        {
            if (text != null) text.text = "";
        }

        /// <summary>ข้อความแบบเตือน (สั่นสั้นๆ)</summary>
        public void ShowError(string message)
        {
            Show(message);
            _shake = 0.3f;
            Color c = errorColor; c.a = text.color.a; text.color = c;
        }

        public void Show(string message)
        {
            if (text == null) return;
            _punch = 1f;
            if (!_hasColor) { _baseColor = text.color; _hasColor = true; }
            Color bc = _baseColor; bc.a = text.color.a; text.color = bc;
            text.text = message;
            _timer = showTime + fadeTime;
            SetAlpha(1f);
        }

        private void Update()
        {
            if (text == null) return;
            if (!_hasBase) { _basePos = text.rectTransform.anchoredPosition; _hasBase = true; }
            if (_shake > 0f)
            {
                _shake -= Time.unscaledDeltaTime;
                float a = Mathf.Max(0f, _shake) / 0.3f * 9f;
                text.rectTransform.anchoredPosition = _basePos + new Vector2(Mathf.Sin(Time.unscaledTime * 70f) * a, 0f);
            }
            else text.rectTransform.anchoredPosition = _basePos;
            _punch = Mathf.Max(0f, _punch - Time.unscaledDeltaTime * 6f);
            text.rectTransform.localScale = Vector3.one * (1f + 0.12f * _punch * _punch);
            if (_timer <= 0f) return;
            _timer -= Time.unscaledDeltaTime;
            SetAlpha(Mathf.Clamp01(_timer / fadeTime));
            if (_timer <= 0f) text.text = "";
        }

        private void SetAlpha(float a)
        {
            Color c = text.color;
            c.a = a;
            text.color = c;
        }
    }
}
