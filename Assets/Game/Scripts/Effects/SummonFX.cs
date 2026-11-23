using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// เอฟเฟกต์ตอนเรียกลูกน้อง (ไม่ต้องใช้รูป - วาดเองตอนเล่น)
    /// 1) ประกายดาวกระพริบด้านบน  2) ลำแสงพุ่งลงพื้น + แสงวาบที่พื้น
    /// 3) วงโค้ง (โดม) 2 ชั้นขยายออก + ละอองลอยขึ้น + รอยไหม้บนพื้น  4) ลูกน้องผุดขึ้นจากพื้น (เงาดำ -> ตัวจริง)
    /// ปรับค่าได้ที่ MinionManager > Summon FX
    /// </summary>
    public class SummonFX : MonoBehaviour
    {
        [System.Serializable]
        public class Settings
        {
            [Tooltip("สีหลักของแสง (โทนเรา = ขาว)")]
            public Color glowColor = new Color(1f, 1f, 1f, 1f);
            [Tooltip("สีรอง (ละออง / วงโค้งชั้นใน)")]
            public Color accentColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            [Tooltip("ความยาวเอฟเฟกต์ทั้งหมด (วินาที)")]
            public float duration = 1.3f;
            [Tooltip("ความสูงลำแสง (หน่วย)")]
            public float beamHeight = 5f;
            [Tooltip("จำนวนละอองที่ลอยขึ้น")]
            public int particles = 18;
            [Tooltip("ลูกน้องผุดขึ้นจากพื้นช่วงไหน (วินาที เริ่ม-จบ)")]
            public Vector2 emergeTime = new Vector2(0.25f, 0.8f);
            [Tooltip("ลูกน้องโผล่มาเป็นเงาดำก่อนแล้วค่อยเป็นตัวจริง")]
            public bool silhouetteFirst = true;
        }

        private class Part { public SpriteRenderer sr; public Vector2 vel; public float life, age, size; }

        private static Sprite _soft, _arc, _line;
        private const float LineThick = 16f / 128f; // ความหนาของรูปเส้น (หน่วย) ที่สเกล 1
        private Settings _s;
        private float _t, _size;
        private SpriteRenderer _star, _beam, _glow, _scorch, _arcA, _arcB;
        private readonly List<Part> _parts = new List<Part>();
        private bool _partsSpawned;

        // ลูกน้องที่กำลังผุด
        private MinionUnit _minion;
        private Transform _visual;
        private SpriteRenderer _body;
        private Color _bodyColor;
        private Vector3 _visualPos;
        private float _visualScaleY;

        /// <summary>เล่นเอฟเฟกต์ที่ตำแหน่งนี้ (minion = ตัวที่จะให้ผุดขึ้นจากพื้น, ไม่ใส่ก็ได้)</summary>
        public static SummonFX Play(Vector2 pos, MinionUnit minion, Settings settings)
        {
            GameObject go = new GameObject("SummonFX");
            if (GameManager.Instance != null) go.transform.SetParent(GameManager.GetFolder(SpawnFolder.Effects), false);
            go.transform.position = pos;
            SummonFX fx = go.AddComponent<SummonFX>();
            fx.Init(pos, minion, settings ?? new Settings());
            return fx;
        }

        private void Init(Vector2 pos, MinionUnit minion, Settings s)
        {
            _s = s;
            BuildSprites();

            // ขนาดเอฟเฟกต์ตามขนาดตัว
            _size = 1f;
            if (minion != null && minion.BodyRenderer != null)
                _size = Mathf.Clamp(minion.BodyRenderer.bounds.size.y / 1.1f, 0.6f, 2.2f);

            _scorch = Make("Scorch", _soft, new Color(0f, 0f, 0f, 0f), -5);
            _glow = Make("GroundGlow", _soft, Color.clear, -4);
            _arcA = Make("ArcOuter", _arc, Color.clear, 25);
            _arcB = Make("ArcInner", _arc, Color.clear, 25);
            _beam = Make("Beam", _line, Color.clear, 30);
            _star = Make("Star", _line, Color.clear, 31);
            SpriteRenderer star2 = Make("Star2", _line, Color.clear, 31);
            star2.transform.SetParent(_star.transform, false);
            star2.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            if (minion != null)
            {
                _minion = minion;
                _visual = minion.VisualRoot;
                _body = minion.BodyRenderer;
                if (_visual != null) { _visualPos = _visual.localPosition; _visualScaleY = _visual.localScale.y; }
                if (_body != null) _bodyColor = _body.color;
                ApplyEmerge(0f);
            }
        }

        private SpriteRenderer Make(string name, Sprite sp, Color c, int order)
        {
            GameObject g = new GameObject(name);
            g.transform.SetParent(transform, false);
            SpriteRenderer sr = g.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = c;
            sr.sortingOrder = order;
            return sr;
        }

        private void Update()
        {
            _t += Time.deltaTime;
            float t = _t, sz = _size;
            Color g = _s.glowColor, a = _s.accentColor;

            // 1) ประกายดาวด้านบน
            float st = Mathf.InverseLerp(0f, 0.32f, t);
            float starPop = Mathf.Sin(st * Mathf.PI);
            _star.transform.localPosition = new Vector3(0f, 2.4f * sz, 0f);
            _star.transform.localScale = new Vector3(0.9f * sz * starPop + 0.001f, 0.06f * sz / LineThick, 1f);
            _star.transform.localRotation = Quaternion.Euler(0f, 0f, t * 90f);
            SetA(_star, g, starPop);
            foreach (Transform c in _star.transform) { SpriteRenderer s2 = c.GetComponent<SpriteRenderer>(); if (s2 != null) SetA(s2, g, starPop); }

            // 2) ลำแสงพุ่งลง
            float bt = Mathf.InverseLerp(0.08f, 0.5f, t);
            float beamA = bt <= 0f || bt >= 1f ? 0f : (bt < 0.2f ? bt / 0.2f : 1f - (bt - 0.2f) / 0.8f);
            float bw = 0.22f * sz * (bt < 0.2f ? bt / 0.2f : 1f - (bt - 0.2f) / 0.8f * 0.85f);
            float bh = _s.beamHeight * sz * Mathf.Clamp01(bt * 4f);
            _beam.transform.localPosition = new Vector3(0f, _s.beamHeight * sz - bh * 0.5f, 0f);
            _beam.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            _beam.transform.localScale = new Vector3(bh, Mathf.Max(0.001f, bw) / LineThick, 1f);
            SetA(_beam, g, beamA);

            // แสงวาบที่พื้น
            float gt = Mathf.InverseLerp(0.12f, 0.95f, t);
            float gw = Mathf.Lerp(0.5f, 2.0f, 1f - (1f - gt) * (1f - gt)) * sz;
            _glow.transform.localScale = new Vector3(gw, gw * 0.36f, 1f);
            SetA(_glow, g, gt <= 0f ? 0f : 0.85f * (1f - gt));

            // รอยไหม้บนพื้น
            float sct = Mathf.InverseLerp(0.15f, _s.duration, t);
            _scorch.transform.localScale = new Vector3(1.5f * sz, 0.5f * sz, 1f);
            SetA(_scorch, Color.black, sct <= 0f ? 0f : 0.45f * (1f - sct * sct));

            // 3) วงโค้ง (โดม) 2 ชั้น
            Arc(_arcA, t - 0.22f, 0.8f, 0.35f, 1.25f, g, sz);
            Arc(_arcB, t - 0.36f, 0.75f, 0.25f, 0.95f, a, sz);

            // ละอองลอยขึ้น
            if (!_partsSpawned && t >= 0.18f) SpawnParticles(sz);
            for (int i = _parts.Count - 1; i >= 0; i--)
            {
                Part p = _parts[i];
                p.age += Time.deltaTime;
                float k = p.age / p.life;
                if (k >= 1f) { Destroy(p.sr.gameObject); _parts.RemoveAt(i); continue; }
                p.sr.transform.localPosition += (Vector3)(p.vel * Time.deltaTime);
                p.vel *= 1f - 1.2f * Time.deltaTime;
                float tw = 0.6f + 0.4f * Mathf.Sin((p.age + i) * 18f);
                p.sr.transform.localScale = Vector3.one * p.size;
                SetA(p.sr, a, (1f - k) * tw);
            }

            // 4) ลูกน้องผุดขึ้นจากพื้น
            if (_minion != null)
            {
                float e = Mathf.InverseLerp(_s.emergeTime.x, _s.emergeTime.y, t);
                ApplyEmerge(e);
                if (e >= 1f) _minion = null;
            }

            if (t >= _s.duration && _parts.Count == 0) Destroy(gameObject);
        }

        private void Arc(SpriteRenderer sr, float t, float life, float r0, float r1, Color c, float sz)
        {
            float k = Mathf.Clamp01(t / life);
            if (t <= 0f) { SetA(sr, c, 0f); return; }
            float r = Mathf.Lerp(r0, r1, 1f - (1f - k) * (1f - k)) * sz;
            sr.transform.localScale = new Vector3(r * 2f, r * 2f * 0.85f, 1f);
            sr.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            SetA(sr, c, k < 0.15f ? k / 0.15f : 1f - (k - 0.15f) / 0.85f);
        }

        private void SpawnParticles(float sz)
        {
            _partsSpawned = true;
            for (int i = 0; i < _s.particles; i++)
            {
                SpriteRenderer sr = Make("Spark", _soft, Color.clear, 26);
                float ang = Random.Range(0f, Mathf.PI);
                float rad = Random.Range(0.2f, 1.1f) * sz;
                sr.transform.localPosition = new Vector3(Mathf.Cos(ang) * rad, Mathf.Sin(ang) * rad * 0.6f, 0f);
                _parts.Add(new Part
                {
                    sr = sr,
                    vel = new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(0.5f, 1.4f)) * sz,
                    life = Random.Range(0.6f, 1.2f),
                    size = Random.Range(0.05f, 0.11f) * sz,
                });
            }
        }

        /// <summary>e = 0 (ยังอยู่ใต้ดิน) -> 1 (ขึ้นมาครบ)</summary>
        private void ApplyEmerge(float e)
        {
            if (_minion == null) return;
            float ease = 1f - (1f - e) * (1f - e);
            if (_visual != null)
            {
                float overshoot = e < 0.75f ? Mathf.Lerp(0.2f, 1.12f, e / 0.75f) : Mathf.Lerp(1.12f, 1f, (e - 0.75f) / 0.25f);
                Vector3 sc = _visual.localScale;
                sc.y = _visualScaleY * overshoot;
                _visual.localScale = sc;
                _visual.localPosition = _visualPos + new Vector3(0f, -0.3f * _size * (1f - ease), 0f);
            }
            if (_body != null)
            {
                Color c = _bodyColor;
                if (_s.silhouetteFirst)
                {
                    float fill = Mathf.InverseLerp(0.35f, 1f, e);
                    c = Color.Lerp(new Color(0.08f, 0.08f, 0.08f, _bodyColor.a), _bodyColor, fill);
                }
                c.a = _bodyColor.a * Mathf.Clamp01(e * 2.5f);
                _body.color = c;
            }
        }

        private void OnDestroy()
        {
            // จบก่อนเวลา (เช่น ลูกน้องตายระหว่างผุด) -> คืนค่าเดิม
            if (_minion != null) ApplyEmerge(1f);
        }

        private static void SetA(SpriteRenderer sr, Color c, float a)
        {
            c.a *= Mathf.Clamp01(a);
            sr.color = c;
        }

        // ------------------------------------------------------------------
        // รูปที่วาดเอง (สร้างครั้งเดียว ใช้ซ้ำ)
        // ------------------------------------------------------------------

        private static void BuildSprites()
        {
            if (_soft != null) return;
            _soft = MakeSprite(64, 64, (x, y) => { float d = Mathf.Sqrt(x * x + y * y); return Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) ; });
            _line = MakeSprite(128, 16, (x, y) => { float fy = 1f - Mathf.Abs(y); float fx = 1f - Mathf.Abs(x); return Mathf.Clamp01(fy * fy * 1.4f) * Mathf.Clamp01(fx * 2.5f); });
            // ครึ่งวงแหวนด้านบน (โดม)
            _arc = MakeSprite(128, 128, (x, y) =>
            {
                if (y < -0.05f) return 0f;
                float d = Mathf.Sqrt(x * x + y * y);
                float ring = 1f - Mathf.Abs(d - 0.9f) / 0.07f;
                float fadeEnds = Mathf.Clamp01((y + 0.05f) / 0.25f);
                return Mathf.Clamp01(ring) * fadeEnds;
            });
        }

        /// <summary>f(x, y) รับพิกัด -1..1 คืนค่าความทึบ 0..1</summary>
        private static Sprite MakeSprite(int w, int h, System.Func<float, float, float> f)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            Color32[] px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float a = f((x + 0.5f) / w * 2f - 1f, (y + 0.5f) / h * 2f - 1f);
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(a) * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), Mathf.Max(w, h), 0, SpriteMeshType.FullRect);
        }
    }
}
