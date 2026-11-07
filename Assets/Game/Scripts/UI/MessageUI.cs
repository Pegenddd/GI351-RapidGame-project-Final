using TMPro;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>ข้อความแจ้งเตือนกลางจอ (เช่น "Not enough Mana") แล้วค่อยๆ จางหาย</summary>
    public class MessageUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private float showTime = 1.6f;
        [SerializeField] private float fadeTime = 0.4f;

        private float _timer;

        private void Awake()
        {
            if (text != null) text.text = "";
        }

        public void Show(string message)
        {
            if (text == null) return;
            text.text = message;
            _timer = showTime + fadeTime;
            SetAlpha(1f);
        }

        private void Update()
        {
            if (text == null || _timer <= 0f) return;
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
