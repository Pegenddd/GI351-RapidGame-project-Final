using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// ทำให้ภาพปกหน้าเมนู "มีชีวิต"
    /// - ภาพขยาย/หดช้าๆ เหมือนหายใจ และขยับตามเมาส์เล็กน้อย (Parallax)
    /// - เถ้าถ่าน/ประกายแดงลอยขึ้นจากด้านล่าง
    /// - แสงแดงกระพริบเบาๆ เป็นช่วงๆ
    /// - Punch(): กระแทกภาพตอนกดเริ่มเกม
    /// </summary>
    public class MenuBackgroundFX : MonoBehaviour
    {
        [Header("ภาพพื้นหลัง")]
        public RectTransform background;
        [Tooltip("กรอบที่ขยายให้เต็มจอตามสัดส่วนภาพ (ว่าง = ใช้ตัวแม่ของ background)")]
        public RectTransform fitRoot;
        [Tooltip("ของที่ต้องวางตรงกับภาพ (ปุ่ม, คำโปรย) จะถูกจัดขนาด/ตำแหน่งเหมือน fitRoot")]
        public RectTransform[] imageSpace;
        public float imageAspect = 16f / 9f;
        [Tooltip("จอไม่ใช่ 16:9 แล้วภาพล้น: 0 = ชิดซ้าย (ชื่อเกมไม่ตก), 0.5 = กลาง, 1 = ชิดขวา")]
        [Range(0f, 1f)] public float alignX = 0f;
        public float baseScale = 1.04f;
        public float breatheAmount = 0.012f;
        public float breathePeriod = 9f;
        public float parallaxPixels = 18f;

        [Header("เถ้าถ่านลอย")]
        public RectTransform ashRoot;
        public Sprite ashSprite;
        public int maxAsh = 45;
        public float ashPerSecond = 9f;
        public Color ashGrey = new Color(0.8f, 0.8f, 0.8f, 0.5f);
        public Color ashRed = new Color(0.85f, 0.15f, 0.15f, 0.7f);
        [Range(0f, 1f)] public float redChance = 0.35f;

        [Header("แสงแดงกระพริบ")]
        public Image redPulse;
        public float redPulseMax = 0.12f;

        private Vector2 _parallax;
        private float _punch, _punchVel;
        private float _spawnAcc, _pulseTimer = 3f, _pulse;

        private class Ash
        {
            public RectTransform rt;
            public Image img;
            public Vector2 vel;
            public float life, age, sway, phase, size;
            public Color color;
        }
        private readonly List<Ash> _ash = new List<Ash>();

        public void Punch(float amount = 0.06f) => _punchVel += amount * 30f;

        private void Awake()
        {
            if (fitRoot == null && background != null) fitRoot = background.parent as RectTransform;
            if (fitRoot != null)
            {
                AspectRatioFitter f = fitRoot.GetComponent<AspectRatioFitter>();
                if (f != null) f.enabled = false; // จัดเองด้านล่าง (ฟิตเตอร์ของ Unity จัดกลางเสมอ)
            }
            if (background != null) background.pivot = new Vector2(alignX, 0.5f);
            Layout();
        }

        /// <summary>ขยายภาพให้เต็มจอ (ล้นได้) โดยยึดขอบตาม alignX</summary>
        private void Layout()
        {
            RectTransform parent = fitRoot != null ? fitRoot.parent as RectTransform : null;
            if (parent == null) return;
            Vector2 p = parent.rect.size;
            if (p.x <= 0f || p.y <= 0f) return;
            Vector2 size = p.x / p.y > imageAspect ? new Vector2(p.x, p.x / imageAspect) : new Vector2(p.y * imageAspect, p.y);
            Place(fitRoot, size);
            if (imageSpace != null) foreach (RectTransform r in imageSpace) if (r != null) Place(r, size);
        }

        private void Place(RectTransform r, Vector2 size)
        {
            Vector2 a = new Vector2(alignX, 0.5f);
            r.anchorMin = a; r.anchorMax = a; r.pivot = a;
            r.sizeDelta = size;
            r.anchoredPosition = Vector2.zero;
        }

        private void Update()
        {
            Layout();
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            float t = Time.unscaledTime;

            // ---- ภาพหายใจ + parallax + punch
            Vector2 target = Vector2.zero;
            Mouse m = Mouse.current;
            if (m != null && Screen.width > 0)
            {
                Vector2 p = m.position.ReadValue();
                target = new Vector2(p.x / Screen.width - 0.5f, p.y / Screen.height - 0.5f) * -2f * parallaxPixels;
            }
            _parallax = Vector2.Lerp(_parallax, target, 1f - Mathf.Exp(-3f * dt));
            _punchVel += (-_punch * 260f - _punchVel * 14f) * dt;
            _punch += _punchVel * dt;
            if (background != null)
            {
                float s = baseScale + Mathf.Sin(t * Mathf.PI * 2f / Mathf.Max(1f, breathePeriod)) * breatheAmount + _punch;
                background.localScale = new Vector3(s, s, 1f);
                // ขยับตามเมาส์ แต่ไม่ให้ขอบภาพด้านที่ยึดไว้หลุดเข้ามาในจอ
                Vector2 sz = background.rect.size;
                float extraX = sz.x * (s - 1f);
                float minX = -extraX * (1f - alignX);
                float maxX = extraX * alignX;
                float extraY = sz.y * (s - 1f) * 0.5f;
                background.anchoredPosition = new Vector2(Mathf.Clamp(_parallax.x, Mathf.Min(minX, 0f), Mathf.Max(maxX, 0f)), Mathf.Clamp(_parallax.y, -extraY, extraY));
            }

            // ---- แสงแดง
            if (redPulse != null)
            {
                _pulseTimer -= dt;
                if (_pulseTimer <= 0f) { _pulseTimer = Random.Range(2.5f, 6f); _pulse = 1f; }
                _pulse = Mathf.Max(0f, _pulse - dt * 1.4f);
                float flicker = _pulse * (0.7f + 0.3f * Mathf.Sin(t * 40f));
                Color c = redPulse.color;
                c.a = redPulseMax * flicker;
                redPulse.color = c;
            }

            // ---- เถ้าถ่าน
            if (ashRoot != null && ashSprite != null)
            {
                _spawnAcc += dt * ashPerSecond;
                while (_spawnAcc >= 1f) { _spawnAcc -= 1f; SpawnAsh(); }
                Rect r = ashRoot.rect;
                for (int i = 0; i < _ash.Count; i++)
                {
                    Ash a = _ash[i];
                    if (a.age >= a.life) continue;
                    a.age += dt;
                    float k = a.age / a.life;
                    Vector2 pos = a.rt.anchoredPosition + a.vel * dt;
                    pos.x += Mathf.Sin(t * a.sway + a.phase) * 18f * dt;
                    a.rt.anchoredPosition = pos;
                    a.rt.localRotation = Quaternion.Euler(0f, 0f, a.phase * 57f + t * 40f * a.sway);
                    Color c = a.color;
                    c.a *= Mathf.Clamp01(k * 6f) * (1f - k);
                    a.img.color = c;
                    if (a.age >= a.life || pos.y > r.yMax + 40f) { a.age = a.life; a.img.enabled = false; }
                }
            }
        }

        private void SpawnAsh()
        {
            Ash a = null;
            for (int i = 0; i < _ash.Count; i++) if (_ash[i].age >= _ash[i].life) { a = _ash[i]; break; }
            if (a == null)
            {
                if (_ash.Count >= maxAsh) return;
                GameObject go = new GameObject("Ash", typeof(RectTransform));
                go.layer = gameObject.layer;
                go.transform.SetParent(ashRoot, false);
                a = new Ash { rt = (RectTransform)go.transform, img = go.AddComponent<Image>() };
                a.img.raycastTarget = false;
                a.img.sprite = ashSprite;
                _ash.Add(a);
            }
            Rect r = ashRoot.rect;
            bool red = Random.value < redChance;
            a.size = red ? Random.Range(5f, 10f) : Random.Range(3f, 8f);
            a.rt.sizeDelta = new Vector2(a.size, a.size * Random.Range(0.6f, 1f));
            a.rt.anchoredPosition = new Vector2(Random.Range(r.xMin, r.xMax), r.yMin - 10f + Random.Range(0f, r.height * 0.35f));
            a.vel = new Vector2(Random.Range(-15f, 25f), Random.Range(40f, 110f));
            a.life = Random.Range(4f, 9f);
            a.age = 0f;
            a.sway = Random.Range(0.6f, 1.8f);
            a.phase = Random.Range(0f, 6.28f);
            a.color = red ? ashRed : ashGrey;
            a.img.enabled = true;
        }
    }
}
