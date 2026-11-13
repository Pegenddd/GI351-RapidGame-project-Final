using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ภาพเคลื่อนไหวของป้อม Soul Beam (กะโหลกเขาวัวบนกระดูกสันหลังบิดเกลียว)
    /// - เปลี่ยนรูปตามเลเวล (Level_0..3 เปิดทีละอัน)
    /// - ว่าง: กะโหลกลอย, เศษหินลอยหมุน, กระดูกหายใจ
    /// - โจมตี: กะโหลกทำหน้าโกรธ + รวมพลังกลางระหว่างเขา -> ลำแสงออกจากจุดนั้น (กะโหลกเล็กข้างฐานโกรธตามด้วย)
    /// - สร้าง: ผุดขึ้นจากพื้น / อัปเกรด: สั่นแล้วเด้ง
    /// จุดหมุนของตัวนี้ = พื้นใต้ป้อม (ย้าย/ย่อขยายตัวนี้เพื่อจัดตำแหน่งรูปป้อมทั้งหมด)
    /// </summary>
    public class SoulBeamTowerVisual : TowerVisual
    {
        [Header("Levels (เปิดทีละอันตามเลเวล)")]
        [SerializeField] private GameObject[] levelRoots = new GameObject[0];
        [Tooltip("รูปไอคอนของแต่ละเลเวล (ใช้ในเมนูป้อม / แถบข้อมูล)")]
        [SerializeField] private Sprite[] levelIcons = new Sprite[0];

        [Header("Build (ตอนสร้าง)")]
        [SerializeField] private float buildStartHeight = 0.25f;
        [SerializeField] private float buildAlpha = 0.45f;

        [Header("Pop (ตอนสร้าง/อัปเกรดเสร็จ)")]
        [SerializeField] private float popScale = 0.12f;
        [SerializeField] private float popTime = 0.35f;

        [Header("Fire (ตอนยิง)")]
        [Tooltip("ฐานยุบลงนิดหน่อยตอนยิง")]
        [SerializeField] private float fireBaseSquash = 0.025f;

        private Vector3 _scale;
        private bool _isUpgradeBuild, _building;
        private float _popT = 99f, _kick;
        private TowerAngrySkullPart _main;
        private readonly List<TowerAngrySkullPart> _all = new List<TowerAngrySkullPart>();
        private readonly List<TowerIdleMotion> _pulsers = new List<TowerIdleMotion>();
        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private readonly List<float> _baseAlpha = new List<float>();

        public override float FireDelay => _main != null ? _main.ChargeTime : 0f;

        private void Awake()
        {
            _scale = transform.localScale;
            foreach (GameObject root in levelRoots)
            {
                if (root == null) continue;
                foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    // หน้าโกรธ / แสง คุมความใสเอง ไม่นับรวม
                    if (sr.name.Contains("Angry") || sr.name.Contains("Glow")) continue;
                    _renderers.Add(sr);
                    _baseAlpha.Add(sr.color.a);
                }
            }
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

            _main = null;
            _all.Clear();
            _pulsers.Clear();
            if (levelRoots.Length == 0 || levelRoots[active] == null) return;
            _all.AddRange(levelRoots[active].GetComponentsInChildren<TowerAngrySkullPart>(true));
            foreach (TowerAngrySkullPart s in _all) if (s.CanFire && _main == null) _main = s;
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
                    float e = 1f - (1f - progress) * (1f - progress);
                    float h = Mathf.Lerp(buildStartHeight, 1f, e);
                    transform.localScale = new Vector3(_scale.x * Mathf.Lerp(0.8f, 1f, e), _scale.y * h, _scale.z);
                }
                else
                {
                    float pulse = 1f + Mathf.Sin(Time.time * 30f) * 0.015f;
                    transform.localScale = new Vector3(_scale.x * pulse, _scale.y / pulse, _scale.z);
                }
            }
            else if (_building)
            {
                _building = false;
                SetAlpha(1f);
                _popT = 0f;
            }
        }

        public override void OnAttackStart(IReadOnlyList<Unit> targets)
        {
            Vector2 aim = targets.Count > 0 && targets[0] != null ? targets[0].Position : (Vector2)transform.position + Vector2.down;
            for (int i = 0; i < _all.Count; i++) _all[i].BeginAttack(aim);
            for (int i = 0; i < _pulsers.Count; i++) _pulsers[i].PulseForAttack(FireDelay);
        }

        public override Vector2? GetFirePosition(int shotIndex) => _main != null ? _main.BeamPosition : (Vector2?)null;

        public override void OnShotFired(int shotIndex, Unit target)
        {
            for (int i = 0; i < _all.Count; i++) _all[i].Fire();
            _kick = 1f;
        }

        private void LateUpdate()
        {
            if (_building) return;
            float dt = Time.deltaTime;
            _kick = Mathf.MoveTowards(_kick, 0f, dt * 6f);

            float s = 1f;
            if (_popT < popTime)
            {
                _popT += dt;
                float x = Mathf.Clamp01(_popT / popTime);
                s += popScale * Mathf.Sin(x * Mathf.PI) * (1f - x * 0.6f);
            }
            float k = fireBaseSquash * _kick;
            transform.localScale = new Vector3(_scale.x * s * (1f + k), _scale.y * s * (1f - k), _scale.z);
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
