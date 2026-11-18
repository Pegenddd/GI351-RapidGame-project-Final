using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// วงระยะยิงที่โชว์ตอนเลือกป้อม/แท่น
    /// วาดวงเองตอนเล่น (ความละเอียดสูง ขอบเนียน ไม่แตก): พื้นจางตรงกลาง เข้มขึ้นที่ขอบ + เส้นขอบคม
    /// ปรับสี / ความเข้ม / ความหนาขอบ ได้ที่หัวข้อ Look
    /// </summary>
    public class RangeIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer fill;
        [Tooltip("ขอบแบบเก่า (รูปเล็ก แตก) - ปิดไว้เมื่อใช้วงที่วาดเอง")]
        [SerializeField] private SpriteRenderer outline;

        [Header("Look (วงที่วาดเอง)")]
        [Tooltip("ใช้วงที่วาดเองแบบเนียน (ปิด = ใช้รูปเดิมใน Fill / Outline)")]
        [SerializeField] private bool smoothCircle = true;
        [Tooltip("สีพื้นในวง")]
        [SerializeField] private Color fillColor = new Color(1f, 1f, 1f, 1f);
        [Tooltip("ความเข้มพื้นตรงกลางวง")]
        [Range(0f, 1f)] [SerializeField] private float centerAlpha = 0.05f;
        [Tooltip("ความเข้มพื้นช่วงใกล้ขอบ (ไล่จากกลางออกมา)")]
        [Range(0f, 1f)] [SerializeField] private float edgeAlpha = 0.28f;
        [Tooltip("สีเส้นขอบ")]
        [SerializeField] private Color rimColor = new Color(1f, 1f, 1f, 0.9f);
        [Tooltip("ความหนาเส้นขอบ (หน่วยในโลก)")]
        [SerializeField] private float rimWidth = 0.07f;
        [Tooltip("เส้นขอบนอกสีเข้ม (ช่วยให้เห็นวงบนพื้นสีขาว) - Alpha 0 = ไม่มี")]
        [SerializeField] private Color outerStrokeColor = new Color(0.08f, 0.08f, 0.08f, 0.55f);
        [SerializeField] private float outerStrokeWidth = 0.04f;
        [Tooltip("ความหนาแสงฟุ้งด้านในของขอบ (หน่วยในโลก)")]
        [SerializeField] private float rimGlow = 0.35f;
        [Range(0f, 1f)] [SerializeField] private float rimGlowAlpha = 0.25f;
        [Tooltip("จำนวนพิกเซลต่อ 1 หน่วย (ยิ่งมากยิ่งคม)")]
        [SerializeField] private int pixelsPerUnit = 96;

        [Header("Animation")]
        [Tooltip("วงขยายออกตอนโผล่ (วินาที)")]
        [SerializeField] private float popTime = 0.15f;
        [Tooltip("ขนาดเริ่มต้นตอนโผล่ (เท่าของขนาดจริง)")]
        [Range(0.5f, 1f)] [SerializeField] private float popFrom = 0.85f;

        private static readonly Dictionary<int, Sprite> _cache = new Dictionary<int, Sprite>();
        private float _radius, _popTimer;
        private bool _visible;

        private void Awake() => Hide();

        public void Show(Vector2 center, float radius)
        {
            transform.position = center;
            radius = Mathf.Max(0.05f, radius);
            bool fresh = !_visible || !Mathf.Approximately(radius, _radius);
            _radius = radius;
            _visible = true;

            if (smoothCircle && fill != null)
            {
                fill.sprite = GetSprite(radius);
                fill.color = Color.white;
                fill.transform.localScale = Vector3.one;
                fill.enabled = true;
                if (outline != null) outline.enabled = false;
            }
            else
            {
                if (fill != null) fill.enabled = true;
                if (outline != null) outline.enabled = true;
            }

            if (fresh) _popTimer = 0f;
            ApplyScale();
        }

        public void Hide()
        {
            _visible = false;
            if (fill != null) fill.enabled = false;
            if (outline != null) outline.enabled = false;
        }

        private void Update()
        {
            if (!_visible || _popTimer >= popTime) return;
            _popTimer += Time.unscaledDeltaTime;
            ApplyScale();
        }

        private void ApplyScale()
        {
            float t = popTime > 0f ? Mathf.Clamp01(_popTimer / popTime) : 1f;
            float pop = Mathf.Lerp(popFrom, 1f, 1f - (1f - t) * (1f - t));
            // วงที่วาดเอง = 1 หน่วยต่อเส้นผ่านศูนย์กลาง 1 หน่วย / รูปเดิม = สเกลตามเส้นผ่านศูนย์กลาง
            float diameter = _radius * 2f;
            transform.localScale = Vector3.one * diameter * pop;
        }

        /// <summary>สร้างรูปวง (เก็บไว้ใช้ซ้ำตามขนาดรัศมี)</summary>
        private Sprite GetSprite(float radius)
        {
            int key = Mathf.RoundToInt(radius * 100f) * 31 + GetInstanceSettingsHash();
            if (_cache.TryGetValue(key, out Sprite s) && s != null) return s;

            int size = Mathf.Clamp(Mathf.RoundToInt(radius * 2f * pixelsPerUnit), 128, 1024);
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = "RangeCircle_" + radius.ToString("0.00");

            float half = size * 0.5f;
            float pxPerUnit = size / (radius * 2f);           // พิกเซลต่อหน่วยในโลก
            float aa = 1.2f / half;                           // ขอบนุ่ม ~1 พิกเซล (สัดส่วนรัศมี)
            float rimW = Mathf.Max(rimWidth * pxPerUnit / half, aa * 1.5f);
            float glowW = rimGlow * pxPerUnit / half;
            float darkW = outerStrokeColor.a > 0f ? Mathf.Max(outerStrokeWidth * pxPerUnit / half, aa) : 0f;
            float rimEnd = 1f - darkW;                         // ขอบขาวอยู่ด้านในเส้นเข้ม

            Color32[] px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float inside = Mathf.Clamp01((1f - r) / aa);        // 1 ในวง -> 0 นอกวง (นุ่ม)
                    if (inside <= 0f) { px[y * size + x] = new Color32(0, 0, 0, 0); continue; }

                    // พื้น: จางตรงกลาง เข้มขึ้นใกล้ขอบ
                    float g = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 1f, r));
                    float fa = Mathf.Lerp(centerAlpha, edgeAlpha, g);
                    // แสงฟุ้งด้านในขอบ
                    float glow = glowW > 0f ? Mathf.Clamp01(1f - (1f - r) / glowW) : 0f;
                    fa = Mathf.Max(fa, glow * glow * rimGlowAlpha);
                    // ซ้อนชั้น: พื้น -> ขอบขาว -> เส้นเข้มนอกสุด
                    Color c = fillColor; float a = fa * fillColor.a;
                    float rim = Mathf.Clamp01((r - (rimEnd - rimW)) / aa) * rimColor.a;
                    Over(ref c, ref a, rimColor, rim);
                    if (darkW > 0f)
                    {
                        float dark = Mathf.Clamp01((r - rimEnd) / aa) * outerStrokeColor.a;
                        Over(ref c, ref a, outerStrokeColor, dark);
                    }
                    c.a = a * inside;
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);

            s = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
            s.name = tex.name;
            _cache[key] = s;
            return s;
        }

        /// <summary>วางสี top (ความทึบ ta) ทับบน (c, a)</summary>
        private static void Over(ref Color c, ref float a, Color top, float ta)
        {
            if (ta <= 0f) return;
            float na = ta + a * (1f - ta);
            if (na <= 0.0001f) return;
            Color nc = (top * ta + c * (a * (1f - ta))) / na;
            c = nc; a = na;
        }

        private int GetInstanceSettingsHash()
        {
            unchecked
            {
                int h = 17;
                h = h * 23 + fillColor.GetHashCode();
                h = h * 23 + rimColor.GetHashCode();
                h = h * 23 + centerAlpha.GetHashCode();
                h = h * 23 + edgeAlpha.GetHashCode();
                h = h * 23 + rimWidth.GetHashCode();
                h = h * 23 + rimGlow.GetHashCode();
                h = h * 23 + rimGlowAlpha.GetHashCode();
                h = h * 23 + pixelsPerUnit;
                h = h * 23 + outerStrokeColor.GetHashCode();
                h = h * 23 + outerStrokeWidth.GetHashCode();
                return h;
            }
        }
    }
}
