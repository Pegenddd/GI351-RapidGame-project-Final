using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// ขอบจอมืดลงเมื่อ HP ผู้เล่นต่ำ (เต้นตามจังหวะหัวใจ) และกระพริบตอนโดนตี
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LowHealthVignette : MonoBehaviour
    {
        private static LowHealthVignette _instance;

        [Range(0f, 1f)] public float lowHealthThreshold = 0.3f;
        [Range(0f, 1f)] public float maxAlpha = 0.85f;
        [Range(0f, 1f)] public float hitFlashAlpha = 0.45f;
        public Color tint = new Color(0.4f, 0f, 0.02f, 1f);   // ขอบจอแดงเลือด

        private Image _img;
        private float _flash;

        public static void FlashHit()
        {
            if (_instance != null) _instance._flash = 1f;
        }

        private void Awake()
        {
            _instance = this;
            _img = GetComponent<Image>();
            _img.raycastTarget = false;
        }

        private void OnDestroy() { if (_instance == this) _instance = null; }

        private void Update()
        {
            _flash = Mathf.Max(0f, _flash - Time.deltaTime * 3f);
            float low = 0f;
            PlayerNecromancer p = Registry.Player;
            if (p != null && p.MaxHealth > 0f)
            {
                float hp = p.IsAlive ? p.CurrentHealth / p.MaxHealth : 0f;
                if (hp < lowHealthThreshold)
                {
                    float k = 1f - hp / lowHealthThreshold;
                    float beat = 0.75f + 0.25f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.PI * 1.1f));
                    low = Mathf.Lerp(0.25f, maxAlpha, k) * beat;
                }
            }
            Color c = tint;
            c.a = Mathf.Max(low, _flash * hitFlashAlpha);
            _img.color = c;
            _img.enabled = c.a > 0.005f;
        }
    }
}
