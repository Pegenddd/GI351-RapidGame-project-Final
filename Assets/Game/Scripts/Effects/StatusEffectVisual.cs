using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// เอฟเฟกต์สถานะบนตัวยูนิต (ใส่ให้อัตโนมัติกับศัตรู/มินเนียนทุกตัว ไม่ต้องแก้ Prefab)
    /// - Slow : เส้นขอบตัวเป็นฟ้าน้ำแข็ง + ไอคอนเกล็ดหิมะบนหัว + เกล็ดน้ำแข็งร่วง
    /// - พิษ  : เส้นขอบตัวเป็นแดงชมพู (สีเดียวกับบ่อพิษ) เต้นตุบๆ + ไอคอนหยดพิษ + ฟองพิษลอยขึ้น
    /// - โดนทั้งคู่: สีเส้นขอบสลับกัน ไอคอนขึ้น 2 อันเรียงกัน
    /// ปรับสี/ขนาดได้ใน Inspector ตอนกด Play หรือเพิ่ม component นี้ใส่ Prefab เองเพื่อตั้งค่าถาวร
    /// </summary>
    [DisallowMultipleComponent]
    public class StatusEffectVisual : MonoBehaviour
    {
        [Header("สี (ขาว-ดำ-เทา ให้เข้ากับธีม)")]
        [Tooltip("Slow = ขาวนิ่ง (เกล็ดน้ำแข็งร่วงลง)")]
        public Color slowColor = new Color(0.96f, 0.96f, 0.95f, 1f);
        [Tooltip("พิษ = เทาเข้ม กระพริบมืด-สว่าง (ฟองลอยขึ้น)")]
        public Color poisonColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [Tooltip("พิษกระพริบแรงแค่ไหน (0 = ไม่กระพริบ)")]
        [Range(0f, 1f)] public float poisonPulse = 0.5f;

        [Header("ไอคอนบนหัว")]
        public bool showIcons = true;
        public float iconSize = 0.42f;
        [Tooltip("ความสูงไอคอนเหนือหลอดเลือด")]
        public float iconLift = 0.32f;

        [Header("อนุภาค")]
        public bool showParticles = true;
        public float poisonBubbleInterval = 0.16f;
        public float frostInterval = 0.22f;

        [Header("เสียงตอนเริ่มติดสถานะ")]
        public bool playSounds = true;

        // ---------------------------------------------------------------- state
        private Unit _unit;
        private SpriteRenderer _body;
        private readonly List<SpriteRenderer> _outlines = new List<SpriteRenderer>();
        private readonly List<Color> _outlineBase = new List<Color>();
        private SpriteRenderer _aura;              // ใช้แทนเส้นขอบ ถ้ายูนิตไม่มีเส้นขอบ และไม่มี shader เงา
        private SpriteRenderer _glow;              // เงาเรืองรอบตัว (ตามรูปตัวที่กำลังเล่นท่า)
        private static Material _silhouetteMat;
        private static bool _silhouetteTried;

        [Header("เงาเรืองรอบตัว (ใช้เมื่อยูนิตไม่มีเส้นขอบ)")]
        [Tooltip("ขนาดเงาเทียบกับตัว (1.1 = ใหญ่กว่าตัว 10%)")]
        public float glowScale = 1.12f;
        [Range(0f, 1f)] public float glowAlpha = 0.85f;
        private Transform _iconRoot;
        private SpriteRenderer _slowIcon, _poisonIcon;
        private float _blend;                      // 0 = ไม่มีสถานะ, 1 = สีเต็ม
        private bool _wasSlow, _wasPoison;
        private float _bubbleTimer, _frostTimer;
        private int _order;
        private string _layer;

        private class Particle
        {
            public SpriteRenderer sr;
            public Vector2 vel;
            public float life, age, spin, size;
            public Color color;
        }
        private readonly List<Particle> _particles = new List<Particle>();

        private static Sprite _sCircle, _sFlake, _sDrop, _sSnowIcon, _sDropIcon;

        // ---------------------------------------------------------------- setup

        private void Awake()
        {
            _unit = GetComponent<Unit>();
            _body = _unit != null ? _unit.BodyRenderer : null;
            foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (!sr.name.StartsWith("Outline") || !sr.gameObject.activeInHierarchy) continue; // เส้นขอบที่ถูกปิดไว้ (ภาพใหม่) ไม่นับ
                _outlines.Add(sr);
                _outlineBase.Add(sr.color);
            }
            _layer = _body != null ? _body.sortingLayerName : "Default";
            _order = _body != null ? _body.sortingOrder : 5;
            BuildSprites();
        }

        private void BuildIcons()
        {
            if (_iconRoot != null) return;
            _iconRoot = new GameObject("StatusIcons").transform;
            _iconRoot.SetParent(transform, false);
            _slowIcon = MakeRenderer("Slow", _iconRoot, _sSnowIcon, _order + 40);
            _poisonIcon = MakeRenderer("Poison", _iconRoot, _sDropIcon, _order + 40);
            _slowIcon.enabled = _poisonIcon.enabled = false;
        }

        private SpriteRenderer MakeRenderer(string n, Transform parent, Sprite s, int order)
        {
            GameObject go = new GameObject(n);
            go.transform.SetParent(parent, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingLayerName = _layer;
            sr.sortingOrder = order;
            return sr;
        }

        // ---------------------------------------------------------------- update

        private void LateUpdate()
        {
            if (_unit == null) return;
            bool alive = _unit.IsAlive;
            bool slow = alive && _unit.IsSlowed;
            bool poison = alive && _unit.IsPoisoned;
            float dt = Time.deltaTime;

            if (playSounds)
            {
                if (slow && !_wasSlow) Sfx.At("SFX_Status_Slow", _unit.Position, 0.55f);
                if (poison && !_wasPoison) Sfx.At("SFX_Status_Poison", _unit.Position, 0.55f);
            }
            _wasSlow = slow;
            _wasPoison = poison;

            // ----- สีเส้นขอบ
            bool any = slow || poison;
            _blend = Mathf.MoveTowards(_blend, any ? 1f : 0f, dt * 6f);
            Color status = slow && poison
                ? (Mathf.Repeat(Time.time, 0.8f) < 0.4f ? slowColor : poisonColor)
                : poison ? poisonColor : slowColor;
            float pulse = poison ? 1f + poisonPulse * Mathf.Sin(Time.time * 7f) : 1f;
            Color glow = new Color(Mathf.Clamp01(status.r * pulse), Mathf.Clamp01(status.g * pulse), Mathf.Clamp01(status.b * pulse), 1f);

            if (_outlines.Count > 0)
            {
                for (int i = 0; i < _outlines.Count; i++)
                {
                    if (_outlines[i] == null) continue;
                    Color b = _outlineBase[i];
                    Color c = Color.Lerp(b, glow, _blend);
                    c.a = b.a;
                    _outlines[i].color = c;
                }
            }
            else UpdateAura(glow, dt);

            // ----- ไอคอน
            if (showIcons && (any || _iconRoot != null))
            {
                BuildIcons();
                UpdateIcons(slow, poison);
            }

            // ----- อนุภาค
            if (showParticles && alive)
            {
                if (poison && (_bubbleTimer -= dt) <= 0f) { _bubbleTimer = poisonBubbleInterval; SpawnBubble(); }
                if (slow && (_frostTimer -= dt) <= 0f) { _frostTimer = frostInterval; SpawnFrost(); }
            }
            UpdateParticles(dt);
        }

        private void UpdateAura(Color glow, float dt)
        {
            if (UpdateGlow(glow)) return;
            if (_blend <= 0f && _aura == null) return;
            if (_aura == null)
            {
                _aura = MakeRenderer("StatusAura", transform, _sCircle, _order - 1);
                _aura.transform.localPosition = new Vector3(0f, -0.35f, 0f);
            }
            float r = (_unit.BodyRadius + 0.35f) * 2.2f;
            _aura.transform.localScale = new Vector3(r, r * 0.45f, 1f);
            Color c = glow;
            c.a = 0.55f * _blend;
            _aura.color = c;
            _aura.enabled = _blend > 0.01f;
        }

        private void UpdateIcons(bool slow, bool poison)
        {
            float y = IconBaseY();
            _iconRoot.localPosition = new Vector3(0f, y, 0f);
            int n = (slow ? 1 : 0) + (poison ? 1 : 0);
            float gap = iconSize * 1.05f;
            float x = -(n - 1) * gap * 0.5f;
            float bob = Mathf.Sin(Time.time * 4f) * 0.04f;

            _slowIcon.enabled = slow;
            if (slow)
            {
                _slowIcon.transform.localPosition = new Vector3(x, bob, 0f);
                _slowIcon.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 40f);
                _slowIcon.transform.localScale = Vector3.one * iconSize;
                _slowIcon.color = slowColor;
                x += gap;
            }
            _poisonIcon.enabled = poison;
            if (poison)
            {
                float p = 1f + 0.12f * Mathf.Sin(Time.time * 9f);
                _poisonIcon.transform.localPosition = new Vector3(x, -bob, 0f);
                _poisonIcon.transform.localScale = Vector3.one * iconSize * p;
                _poisonIcon.color = poisonColor;
            }
        }

        private float IconBaseY()
        {
            if (_unit.HealthBarTransform != null) return _unit.HealthBarTransform.localPosition.y + iconLift;
            if (_body != null) return _body.bounds.max.y - transform.position.y + 0.25f;
            return 1f;
        }

        // ---------------------------------------------------------------- particles

        private Particle GetParticle()
        {
            for (int i = 0; i < _particles.Count; i++)
                if (_particles[i].age >= _particles[i].life) return _particles[i];
            if (_particles.Count >= 12) return null;
            Particle p = new Particle { sr = MakeRenderer("StatusFx", transform, _sCircle, _order + 2) };
            _particles.Add(p);
            return p;
        }

        private Vector2 BodyPoint()
        {
            float r = _unit.BodyRadius + 0.1f;
            return new Vector2(Random.Range(-r, r), Random.Range(-0.3f, 0.5f));
        }

        private void SpawnBubble()
        {
            Particle p = GetParticle();
            if (p == null) return;
            p.sr.sprite = _sCircle;
            p.sr.transform.localPosition = BodyPoint();
            p.vel = new Vector2(Random.Range(-0.15f, 0.15f), Random.Range(0.7f, 1.1f));
            p.life = Random.Range(0.45f, 0.7f);
            p.age = 0f;
            p.spin = 0f;
            p.size = Random.Range(0.1f, 0.18f);
            p.color = poisonColor;
            p.sr.enabled = true;
        }

        private void SpawnFrost()
        {
            Particle p = GetParticle();
            if (p == null) return;
            p.sr.sprite = _sFlake;
            Vector2 pt = BodyPoint();
            p.sr.transform.localPosition = new Vector3(pt.x, pt.y + 0.5f, 0f);
            p.vel = new Vector2(Random.Range(-0.2f, 0.2f), Random.Range(-0.55f, -0.35f));
            p.life = Random.Range(0.6f, 0.9f);
            p.age = 0f;
            p.spin = Random.Range(-180f, 180f);
            p.size = Random.Range(0.14f, 0.22f);
            p.color = Color.Lerp(slowColor, Color.white, 0.4f);
            p.sr.enabled = true;
        }

        private void UpdateParticles(float dt)
        {
            for (int i = 0; i < _particles.Count; i++)
            {
                Particle p = _particles[i];
                if (p.age >= p.life) { if (p.sr.enabled) p.sr.enabled = false; continue; }
                p.age += dt;
                float k = Mathf.Clamp01(p.age / p.life);
                Transform t = p.sr.transform;
                t.localPosition += (Vector3)(p.vel * dt);
                if (p.spin != 0f) t.localRotation = Quaternion.Euler(0f, 0f, t.localEulerAngles.z + p.spin * dt);
                float s = p.size * (p.sr.sprite == _sCircle ? Mathf.Lerp(0.6f, 1.2f, k) : 1f);
                t.localScale = Vector3.one * s;
                Color c = p.color;
                c.a = (1f - k) * (k < 0.15f ? k / 0.15f : 1f) * 0.9f;
                p.sr.color = c;
            }
        }

        /// <summary>เงารูปเดียวกับตัว (สีเดียวทึบ) อยู่ด้านหลัง ขยายนิดหน่อย = เส้นขอบเรือง ใช้ได้กับภาพที่เล่นท่า</summary>
        private bool UpdateGlow(Color glow)
        {
            if (_body == null) return false;
            if (_glow == null)
            {
                if (_blend <= 0f) return true;
                Material m = SilhouetteMaterial();
                if (m == null) return false;
                _glow = MakeRenderer("StatusGlow", _body.transform, _body.sprite, _body.sortingOrder - 1);
                _glow.sharedMaterial = m;
                _glow.sortingLayerID = _body.sortingLayerID;
            }
            bool on = _blend > 0.01f && _body.enabled && _body.gameObject.activeInHierarchy;
            _glow.enabled = on;
            if (!on) return true;
            _glow.sprite = _body.sprite;
            _glow.sortingOrder = _body.sortingOrder - 1;
            float pulse = 1f + 0.03f * Mathf.Sin(Time.time * 6f);
            float sx = glowScale * pulse * (_body.flipX ? -1f : 1f);
            _glow.transform.localScale = new Vector3(sx, glowScale * pulse, 1f);
            _glow.transform.localPosition = Vector3.zero;
            Color c = glow;
            c.a = glowAlpha * _blend;
            _glow.color = c;
            return true;
        }

        private static Material SilhouetteMaterial()
        {
            if (_silhouetteTried) return _silhouetteMat;
            _silhouetteTried = true;
            Shader sh = Resources.Load<Shader>("Shaders/NecroSpriteSilhouette");
            if (sh == null) sh = Shader.Find("NecroDefense/SpriteSilhouette");
            if (sh != null) _silhouetteMat = new Material(sh) { name = "StatusSilhouette" };
            return _silhouetteMat;
        }

        private void OnDisable()
        {
            if (_glow != null) _glow.enabled = false;
            for (int i = 0; i < _outlines.Count; i++)
                if (_outlines[i] != null) _outlines[i].color = _outlineBase[i];
        }

        // ---------------------------------------------------------------- procedural sprites

        private static void BuildSprites()
        {
            if (_sCircle != null) return;
            _sCircle = Make(32, (x, y) => { float d = Len(x, y); return Fill(Mathf.Clamp01((0.48f - d) * 12f), 1f, 0f); });
            _sFlake = Make(32, (x, y) => Fill(Flake(x, y, 0.45f, 0.045f), 1f, 0f));
            _sDrop = Make(32, (x, y) => Fill(Drop(x, y, 0.42f), 1f, 0f));
            // ไอคอนบนหัว: รูปขาว + ขอบดำ อ่านออกทั้งบนพื้นขาวและดำ (สีถูกย้อมตอนใช้)
            _sSnowIcon = Make(64, (x, y) =>
            {
                float inner = Flake(x, y, 0.4f, 0.05f);
                float outer = Flake(x, y, 0.44f, 0.095f);
                return Fill(inner, outer, 1f);
            });
            _sDropIcon = Make(64, (x, y) =>
            {
                float inner = Drop(x, y, 0.36f);
                float outer = Drop(x, y, 0.44f);
                float hl = Mathf.Clamp01((0.07f - Len(x + 0.08f, y + 0.02f)) * 30f);
                Vector2 c = Fill(inner, outer, 1f);
                return new Vector2(Mathf.Max(c.x, hl * 0.4f + c.x), c.y);
            });
        }

        private delegate Vector2 PixelFn(float x, float y);

        /// <summary>x = ความสว่าง (1 = ขาว, 0 = ดำ), y = alpha</summary>
        private static Sprite Make(int size, PixelFn fn)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            Color[] px = new Color[size * size];
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    float x = (i + 0.5f) / size - 0.5f, y = (j + 0.5f) / size - 0.5f;
                    Vector2 v = fn(x, y);
                    px[j * size + i] = new Color(v.x, v.x, v.x, v.y);
                }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>รวมรูปข้างใน (ขาว) กับขอบนอก (สีเข้ม)</summary>
        private static Vector2 Fill(float inner, float outer, float outlineDark)
        {
            float a = Mathf.Max(inner, outer);
            if (outlineDark <= 0f) return new Vector2(1f, inner);
            float lum = a > 0f ? inner / a : 0f;
            return new Vector2(Mathf.Lerp(0.05f, 1f, lum), a);
        }

        private static float Len(float x, float y) => Mathf.Sqrt(x * x + y * y);

        private static float Seg(float px, float py, float ax, float ay, float bx, float by)
        {
            float abx = bx - ax, aby = by - ay;
            float t = Mathf.Clamp01(((px - ax) * abx + (py - ay) * aby) / (abx * abx + aby * aby));
            float dx = px - (ax + abx * t), dy = py - (ay + aby * t);
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>เกล็ดหิมะ 6 แฉก (coverage 0-1)</summary>
        private static float Flake(float x, float y, float r, float w)
        {
            float d = 9f;
            for (int k = 0; k < 6; k++)
            {
                float a = k * Mathf.PI / 3f + Mathf.PI / 2f;
                float cx = Mathf.Cos(a), cy = Mathf.Sin(a);
                d = Mathf.Min(d, Seg(x, y, 0f, 0f, cx * r, cy * r));
                // กิ่งเล็ก
                float bx = cx * r * 0.55f, by = cy * r * 0.55f;
                for (int s = -1; s <= 1; s += 2)
                {
                    float a2 = a + s * 0.75f;
                    d = Mathf.Min(d, Seg(x, y, bx, by, bx + Mathf.Cos(a2) * r * 0.32f, by + Mathf.Sin(a2) * r * 0.32f));
                }
            }
            return Mathf.Clamp01((w - d) * 40f + 0.5f);
        }

        /// <summary>หยดน้ำ (พิษ) หัวแหลมขึ้นบน</summary>
        private static float Drop(float x, float y, float r)
        {
            float cy = -0.08f;
            float circle = Len(x, y - cy) - r * 0.62f;
            // สามเหลี่ยมด้านบน
            float t = Mathf.Clamp01((y - cy) / (r * 1.25f));
            float half = r * 0.62f * (1f - t);
            float tri = (y > cy && y < cy + r * 1.25f) ? Mathf.Abs(x) - half : 9f;
            float d = Mathf.Min(circle, tri);
            return Mathf.Clamp01(-d * 64f + 0.5f);
        }
    }
}
