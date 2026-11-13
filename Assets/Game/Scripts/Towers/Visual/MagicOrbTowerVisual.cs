using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ภาพเคลื่อนไหวของป้อม Magic Orb
    /// - เปลี่ยนรูปตามเลเวล (Level_0..3 เปิดทีละอัน)
    /// - สร้าง: ผุดขึ้นจากพื้น / อัปเกรด: เรืองแสงแล้วเด้ง
    /// - โจมตี: กะโหลกอ้าปาก (TowerSkullPart) แล้วกระสุนออกจากปาก ยิง 2 ลูก = กะโหลกซ้าย/ขวาคนละลูก
    /// จุดหมุนของตัวนี้ = พื้นใต้ป้อม (ย้าย/ย่อขยายตัวนี้เพื่อจัดตำแหน่งรูปป้อมทั้งหมด)
    /// </summary>
    public class MagicOrbTowerVisual : TowerVisual
    {
        [Header("Levels (เปิดทีละอันตามเลเวล)")]
        [SerializeField] private GameObject[] levelRoots = new GameObject[0];
        [Tooltip("รูปไอคอนของแต่ละเลเวล (ใช้ในเมนูป้อม / แถบข้อมูล)")]
        [SerializeField] private Sprite[] levelIcons = new Sprite[0];

        [Header("Build (ตอนสร้าง)")]
        [Tooltip("ตอนเริ่มสร้าง ป้อมเตี้ยกี่เท่า (ค่อยๆ ผุดขึ้นจนเต็ม)")]
        [SerializeField] private float buildStartHeight = 0.25f;
        [Tooltip("ความใสตอนกำลังสร้าง")]
        [SerializeField] private float buildAlpha = 0.45f;

        [Header("Pop (ตอนสร้าง/อัปเกรดเสร็จ)")]
        [Tooltip("เด้งใหญ่ขึ้นกี่เท่าตอนเสร็จ")]
        [SerializeField] private float popScale = 0.14f;
        [Tooltip("เวลาเด้ง (วินาที)")]
        [SerializeField] private float popTime = 0.35f;
        [Tooltip("แสงวาบตอนเสร็จ (ว่างได้)")]
        [SerializeField] private SpriteRenderer flashGlow;
        [SerializeField] private float flashSize = 2.6f;

        [Header("Fire (ตอนยิง)")]
        [Tooltip("ฐานยุบลงนิดหน่อยตอนยิง")]
        [SerializeField] private float fireBaseSquash = 0.03f;

        private Vector3 _scale;
        private bool _isUpgradeBuild, _building;
        private float _popT = 99f, _fireKick;
        private readonly List<TowerSkullPart> _skulls = new List<TowerSkullPart>();
        private readonly List<TowerIdleMotion> _pulsers = new List<TowerIdleMotion>();
        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private readonly List<float> _baseAlpha = new List<float>();

        public override float FireDelay
        {
            get
            {
                float d = 0f;
                for (int i = 0; i < _skulls.Count; i++) d = Mathf.Max(d, _skulls[i].OpenTime);
                return d;
            }
        }

        private void Awake()
        {
            _scale = transform.localScale;
            foreach (GameObject root in levelRoots)
            {
                if (root == null) continue;
                foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.GetComponentInParent<TowerSkullPart>() != null && sr.name.Contains("Glow")) continue;
                    _renderers.Add(sr);
                    _baseAlpha.Add(sr.color.a);
                }
            }
            if (flashGlow != null) flashGlow.enabled = false;
            ShowLevel(0);
        }

        public override Sprite GetIcon(int level)
        {
            if (levelIcons == null || levelIcons.Length == 0) return null;
            return levelIcons[Mathf.Clamp(level, 0, levelIcons.Length - 1)];
        }

        public override void OnLevelChanged(int level, bool isUpgrade)
        {
            ShowLevel(level);
            _isUpgradeBuild = isUpgrade;
        }

        private void ShowLevel(int level)
        {
            int active = Mathf.Clamp(level, 0, Mathf.Max(0, levelRoots.Length - 1));
            for (int i = 0; i < levelRoots.Length; i++)
                if (levelRoots[i] != null) levelRoots[i].SetActive(i == active);

            _skulls.Clear();
            if (levelRoots.Length > 0 && levelRoots[active] != null)
                _skulls.AddRange(levelRoots[active].GetComponentsInChildren<TowerSkullPart>(true));
            _skulls.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x)); // ซ้าย -> ขวา

            // ชิ้นที่พองตอนโจมตี (เช่น ลูกไฟ Level 3)
            _pulsers.Clear();
            if (levelRoots.Length > 0 && levelRoots[active] != null)
                foreach (TowerIdleMotion m in levelRoots[active].GetComponentsInChildren<TowerIdleMotion>(true))
                    if (m.HasAttackPulse) _pulsers.Add(m);
        }

        public override void OnConstruction(float progress)
        {
            if (progress < 1f)
            {
                _building = true;
                SetAlpha(Mathf.Lerp(buildAlpha, 1f, progress * progress));
                if (!_isUpgradeBuild)
                {
                    // ผุดขึ้นจากพื้น
                    float e = 1f - (1f - progress) * (1f - progress);
                    float h = Mathf.Lerp(buildStartHeight, 1f, e);
                    transform.localScale = new Vector3(_scale.x * Mathf.Lerp(0.8f, 1f, e), _scale.y * h, _scale.z);
                }
                else
                {
                    // อัปเกรด: สั่นเต้นเบาๆ เหมือนกำลังรวมพลัง
                    float pulse = 1f + Mathf.Sin(Time.time * 30f) * 0.015f;
                    transform.localScale = new Vector3(_scale.x * pulse, _scale.y / pulse, _scale.z);
                }
            }
            else if (_building)
            {
                _building = false;
                SetAlpha(1f);
                _popT = 0f; // เด้ง + แสงวาบ
            }
        }

        public override void OnAttackStart(IReadOnlyList<Unit> targets)
        {
            for (int i = 0; i < _pulsers.Count; i++) _pulsers[i].PulseForAttack(FireDelay);
            if (_skulls.Count == 0) return;
            for (int i = 0; i < targets.Count; i++)
                if (targets[i] != null) _skulls[i % _skulls.Count].BeginAttack(targets[i].Position);
        }

        public override void OnShotFired(int shotIndex, Unit target)
        {
            if (_skulls.Count > 0) _skulls[shotIndex % _skulls.Count].Fire();
            _fireKick = 1f;
        }

        public override Vector2? GetFirePosition(int shotIndex)
        {
            if (_skulls.Count == 0) return null;
            return _skulls[shotIndex % _skulls.Count].MouthPosition;
        }

        private void LateUpdate()
        {
            if (_building) return;
            float dt = Time.deltaTime;
            _fireKick = Mathf.MoveTowards(_fireKick, 0f, dt * 6f);

            float s = 1f;
            if (_popT < popTime)
            {
                _popT += dt;
                float x = Mathf.Clamp01(_popT / popTime);
                s += popScale * Mathf.Sin(x * Mathf.PI) * (1f - x * 0.6f); // เด้งขึ้นแล้วกลับ
                UpdateFlash(x);
            }
            else if (flashGlow != null && flashGlow.enabled) flashGlow.enabled = false;

            float k = fireBaseSquash * _fireKick;
            transform.localScale = new Vector3(_scale.x * s * (1f + k), _scale.y * s * (1f - k), _scale.z);
        }

        private void UpdateFlash(float x)
        {
            if (flashGlow == null) return;
            flashGlow.enabled = true;
            Color c = flashGlow.color;
            c.a = (1f - x) * 0.85f;
            flashGlow.color = c;
            flashGlow.transform.localScale = Vector3.one * flashSize * (0.6f + 0.6f * x);
        }

        private void SetAlpha(float a)
        {
            for (int i = 0; i < _renderers.Count; i++)
            {
                if (_renderers[i] == null) continue;
                Color c = _renderers[i].color;
                c.a = _baseAlpha[i] * a;
                _renderers[i].color = c;
            }
        }
    }
}
