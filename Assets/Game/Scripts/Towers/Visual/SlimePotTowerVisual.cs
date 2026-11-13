using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ภาพเคลื่อนไหวของป้อม Explosive (สไลม์แช่หม้อต้ม)
    /// - เปลี่ยนรูปตามเลเวล (Level_0..3 เปิดทีละอัน)
    /// - ว่าง: สไลม์ลอยในลาวา, ไฟใต้หม้อสั่น, ฟองลาวาผุด
    /// - โจมตี: สไลม์ย่อตัว + ไฟ/ลาวาพอง -> สไลม์กระโดดออก (กระสุนเป็นรูปสไลม์ ลอยโค้งไปตกใส่ศัตรู) -> ตัวใหม่ผุดจากลาวา
    /// - สร้าง: ผุดขึ้นจากพื้น / อัปเกรด: สั่นแล้วเด้ง
    /// จุดหมุนของตัวนี้ = พื้นใต้ป้อม (ย้าย/ย่อขยายตัวนี้เพื่อจัดตำแหน่งรูปป้อมทั้งหมด)
    /// </summary>
    public class SlimePotTowerVisual : TowerVisual
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
        [SerializeField] private float popScale = 0.14f;
        [SerializeField] private float popTime = 0.35f;

        [Header("Launch (ตอนสไลม์กระโดดออก)")]
        [Tooltip("หม้อยุบลงนิดหน่อยตอนสไลม์ดีดตัวออก")]
        [SerializeField] private float launchSquash = 0.05f;

        private Vector3 _scale;
        private bool _isUpgradeBuild, _building;
        private float _popT = 99f, _kick;
        private TowerSlimePart _slime;
        private readonly List<TowerIdleMotion> _pulsers = new List<TowerIdleMotion>();
        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private readonly List<float> _baseAlpha = new List<float>();

        public override float FireDelay => _slime != null ? _slime.CrouchTime : 0f;

        private void Awake()
        {
            _scale = transform.localScale;
            foreach (GameObject root in levelRoots)
            {
                if (root == null) continue;
                foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
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

            _slime = null;
            _pulsers.Clear();
            if (levelRoots.Length == 0 || levelRoots[active] == null) return;
            _slime = levelRoots[active].GetComponentInChildren<TowerSlimePart>(true);
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
            if (_slime != null) _slime.BeginCrouch();
            for (int i = 0; i < _pulsers.Count; i++) _pulsers[i].PulseForAttack(FireDelay);
        }

        public override Vector2? GetFirePosition(int shotIndex) => _slime != null ? _slime.LaunchPosition : (Vector2?)null;

        public override void OnProjectileSpawned(int shotIndex, Projectile projectile)
        {
            if (_slime == null || projectile == null || _slime.FullBodySprite == null) return;
            projectile.SetLook(_slime.FullBodySprite, _slime.WorldScale);
        }

        public override void OnShotFired(int shotIndex, Unit target)
        {
            if (_slime != null) _slime.Launch();
            _kick = 1f;
        }

        private void LateUpdate()
        {
            if (_building) return;
            float dt = Time.deltaTime;
            _kick = Mathf.MoveTowards(_kick, 0f, dt * 5f);

            float s = 1f;
            if (_popT < popTime)
            {
                _popT += dt;
                float x = Mathf.Clamp01(_popT / popTime);
                s += popScale * Mathf.Sin(x * Mathf.PI) * (1f - x * 0.6f);
            }
            float k = launchSquash * _kick;
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
