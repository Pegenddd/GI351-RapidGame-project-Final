using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NecroDefense
{
    public enum TowerAttackType { Projectile, Beam }

    /// <summary>ค่าของป้อมแต่ละเลเวล (Level 0 = ตอนสร้าง, 1-3 = อัปเกรด Tier 1-3)</summary>
    [Serializable]
    public class TowerLevel
    {
        [Tooltip("ชื่อที่โชว์ใน UI เช่น Base / Tier 1")]
        public string label = "Base";
        [Tooltip("ราคา (Souls) ของเลเวลนี้ (ก่อนส่วนลด)")]
        public int cost = 30;
        [TextArea(1, 3)] public string description;
        public float damage = 10f;
        [Tooltip("วินาทีต่อการยิง 1 ครั้ง")]
        public float attackInterval = 1f;
        public float range = 5f;
        [Tooltip("ยิงกี่เป้าพร้อมกันต่อครั้ง (Magic Orb Tier 2 = 2 ลูก)")]
        public int targetsPerShot = 1;
        [Tooltip("รัศมีระเบิด (0 = ตัวเดียว)")]
        public float areaRadius = 0f;
        [Range(0f, 1f)] public float slowPercent = 0f;
        public float slowDuration = 0f;
        [Tooltip("ลำแสงกระโดดต่อได้อีกกี่เป้า (Soul Beam)")]
        public int chainTargets = 0;
        [Tooltip("โอกาสจับวิญญาณเข้า Minion Bag ทันทีเมื่อฆ่า (Magic Orb Tier 3)")]
        [Range(0f, 1f)] public float soulCaptureChance = 0f;
        [Tooltip("โอกาสทิ้งบ่อพิษเมื่อระเบิด (Explosive Tier 3)")]
        [Range(0f, 1f)] public float poisonPoolChance = 0f;
    }

    /// <summary>
    /// ป้อมปราการ 1 ป้อม (Magic Orb / Explosive / Soul Beam ใช้สคริปต์เดียวกัน ต่างกันที่ค่าใน Prefab)
    /// </summary>
    public class Tower : MonoBehaviour
    {
        [Header("Info")]
        [SerializeField] private string towerName = "Tower";
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("Attack")]
        [SerializeField] private TowerAttackType attackType = TowerAttackType.Projectile;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private BeamEffect beamPrefab;
        [Tooltip("ยิงตัวบินได้ไหม (Explosive = ไม่ได้)")]
        [SerializeField] private bool canHitFlying = true;
        [Tooltip("จุดปล่อยกระสุน (ว่าง = กลางป้อม)")]
        [SerializeField] private Transform firePoint;
        [Tooltip("ศัตรูน้อยกว่าจำนวนลูกที่ยิงต่อครั้ง (เช่น Tier 2 ยิง 2 ลูกแต่มีศัตรูตัวเดียว) -> ลูกที่เหลือยิงใส่ตัวเดิม (ปิด = ยิงแค่เท่าจำนวนศัตรู)")]
        [SerializeField] private bool extraShotsHitSameTarget = true;
        [Tooltip("ระยะกระโดดของลำแสง (Chain)")]
        [SerializeField] private float chainRange = 3f;
        [SerializeField] private DamageZone poisonPoolPrefab;

        [Header("Levels (0 = สร้าง, 1-3 = อัปเกรด)")]
        [SerializeField] private List<TowerLevel> levels = new List<TowerLevel>();

        [Header("Depth & Footprint (ผู้เล่นเดินหลังป้อม / ชนฐาน)")]
        [Tooltip("วาดป้อมทั้งต้นเป็นก้อนเดียว เรียงหน้า-หลังกับตัวผู้เล่นตามแกน Y (ผู้เล่นอยู่สูงกว่า = ถูกป้อมบัง)")]
        [SerializeField] private bool sortAsOneObject = true;
        [Tooltip("ลำดับการวาดของป้อมทั้งก้อน (ตัวผู้เล่น = 5 ต้องเท่ากันถึงจะเรียงตามแกน Y)")]
        [SerializeField] private int sortingOrder = 5;
        [Tooltip("ผู้เล่นเดินทะลุฐานป้อมไม่ได้")]
        [SerializeField] private bool blockPlayer = true;
        [Tooltip("ขนาดฐานที่ชน (กว้าง x สูง) เป็นวงรี - ดูเส้นเขียวใน Scene View")]
        [SerializeField] private Vector2 footprintSize = new Vector2(1.25f, 0.5f);
        [Tooltip("ตำแหน่งกลางฐานเทียบกับจุดสร้างป้อม")]
        [SerializeField] private Vector2 footprintOffset = new Vector2(0f, -0.4f);

        [Header("Construction")]
        [Tooltip("เวลาก่อสร้าง/อัปเกรด (วินาที) ระหว่างนี้ป้อมยังไม่ยิง")]
        [SerializeField] private float constructionTime = 1f;
        [SerializeField] private WorldHealthBar constructionBar;

        [Header("Visual")]
        [Tooltip("ภาพเคลื่อนไหวของป้อม (เช่น MagicOrbTowerVisual) - ว่าง = ใช้ Body รูปเดียวแบบเดิม")]
        [SerializeField] private TowerVisual visual;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("รูปที่ใช้ในเมนูสร้างป้อม (ว่าง = ใช้รูปของ Body)")]
        [SerializeField] private Sprite icon;
        [Tooltip("ป้อมใหญ่ขึ้นเท่านี้ต่อเลเวล")]
        [SerializeField] private float scalePerLevel = 0.08f;
        [Tooltip("ดาวบอกเลเวล (เปิดทีละอันตาม Tier)")]
        [SerializeField] private GameObject[] levelPips = new GameObject[0];

        [Header("Sound")]
        [Tooltip("ชนิดเสียง: Orb / Bomb / Beam (ว่าง = เดาจากชื่อป้อม)")]
        [SerializeField] private string soundKey = "";

        private int _levelIndex;
        private float _timer;
        private int _beamLoop;
        private float _beamIdle;
        private string _sk;

        private string SoundKey
        {
            get
            {
                if (_sk != null) return _sk;
                string n = !string.IsNullOrEmpty(soundKey) ? soundKey : towerName + gameObject.name;
                _sk = n.Contains("Beam") ? "Beam" : (n.Contains("Explosive") || n.Contains("Bomb")) ? "Bomb" : "Orb";
                return _sk;
            }
        }
        private Vector3 _baseScale;
        private float _buildTimer;
        private static readonly List<Unit> _targets = new List<Unit>();

        public string TowerName => towerName;
        public string Description => description;
        public int LevelIndex => _levelIndex;
        public int MaxLevelIndex => levels.Count - 1;
        public TowerLevel Current => levels.Count > 0 ? levels[Mathf.Clamp(_levelIndex, 0, levels.Count - 1)] : new TowerLevel();
        public TowerLevel Next => _levelIndex + 1 < levels.Count ? levels[_levelIndex + 1] : null;
        public bool CanUpgrade => Next != null;
        public int BaseCost => levels.Count > 0 ? levels[0].cost : 0;
        public float SoulCaptureChance => Current.soulCaptureChance;
        /// <summary>Souls ที่จ่ายไปทั้งหมด (สร้าง + อัปเกรด) ใช้คิดเงินคืน</summary>
        public int TotalSpent { get; private set; }
        public TowerSlot Slot { get; private set; }
        public bool BlocksPlayer => blockPlayer && isActiveAndEnabled;
        public Vector2 FootprintCenter => (Vector2)transform.position + footprintOffset;
        public Vector2 FootprintSize => footprintSize;
        public bool IsConstructing => _buildTimer > 0f;
        public Sprite Icon
        {
            get
            {
                Sprite v = visual != null ? visual.GetIcon(_levelIndex) : null;
                if (v != null) return v;
                return icon != null ? icon : (bodyRenderer != null ? bodyRenderer.sprite : null);
            }
        }
        public Color IconColor => (visual != null && visual.GetIcon(_levelIndex) != null) || icon != null || bodyRenderer == null ? Color.white : bodyRenderer.color;

        private void Awake()
        {
            _baseScale = transform.localScale;
            if (sortAsOneObject && GetComponent<SortingGroup>() == null)
            {
                SortingGroup g = gameObject.AddComponent<SortingGroup>();
                g.sortingOrder = sortingOrder;
            }
            RefreshVisual(false);
        }

        public void Initialize(TowerSlot slot, int paid)
        {
            Slot = slot;
            _levelIndex = 0;
            TotalSpent = paid;
            RefreshVisual(false);
            StartConstruction();
            Sfx.At("SFX_Tower_Build_Start", transform.position);
        }

        public void Upgrade(int paid)
        {
            if (!CanUpgrade) return;
            _levelIndex++;
            TotalSpent += paid;
            RefreshVisual(true);
            StartConstruction();
            Sfx.At("SFX_Tower_Upgrade_T" + Mathf.Clamp(_levelIndex, 1, 3), transform.position);
        }

        private void RefreshVisual(bool isUpgrade)
        {
            transform.localScale = _baseScale * (1f + scalePerLevel * _levelIndex);
            if (visual != null) visual.OnLevelChanged(_levelIndex, isUpgrade);
            for (int i = 0; i < levelPips.Length; i++)
                if (levelPips[i] != null) levelPips[i].SetActive(i < _levelIndex);
        }

        private void StartConstruction()
        {
            _buildTimer = constructionTime;
            if (_buildTimer > 0f)
            {
                SetBodyAlpha(0.35f);
                if (visual != null) visual.OnConstruction(0f);
                if (constructionBar != null) constructionBar.SetValue(0f);
            }
        }

        private void SetBodyAlpha(float a)
        {
            if (bodyRenderer == null) return;
            Color c = bodyRenderer.color;
            c.a = a;
            bodyRenderer.color = c;
        }

        private void Update()
        {
            if (!GameManager.IsGameplayRunning || levels.Count == 0) return;

            if (_buildTimer > 0f)
            {
                _buildTimer -= Time.deltaTime;
                float t = constructionTime > 0f ? 1f - Mathf.Clamp01(_buildTimer / constructionTime) : 1f;
                if (constructionBar != null) constructionBar.SetValue(t);
                if (visual != null) visual.OnConstruction(_buildTimer <= 0f ? 1f : t);
                if (_buildTimer <= 0f) { SetBodyAlpha(1f); Sfx.At("SFX_Tower_Build_Done", transform.position); }
                return;
            }

            // ลำแสงหยุดยิงนานพอ -> หยุดเสียงฮัม
            if (_beamLoop != 0)
            {
                _beamIdle += Time.deltaTime;
                if (_beamIdle > Current.attackInterval + 0.35f) Sfx.StopLoop(ref _beamLoop);
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            TowerLevel lv = Current;
            int shotCount = Mathf.Max(1, lv.targetsPerShot);
            FindTargets(lv.range, shotCount);
            if (_targets.Count == 0) return;
            if (extraShotsHitSameTarget && attackType == TowerAttackType.Projectile)
                for (int i = _targets.Count; i < shotCount; i++) _targets.Add(_targets[i % _targets.Count]);

            _timer = lv.attackInterval;

            if (visual != null)
            {
                // มีภาพเคลื่อนไหว: ตั้งท่า (เช่น อ้าปาก) ก่อน แล้วค่อยยิงออก
                List<Unit> shots = new List<Unit>(_targets);
                visual.OnAttackStart(shots);
                float delay = visual.FireDelay;
                if (delay > 0f) { StartCoroutine(FireAfterDelay(shots, delay)); return; }
                for (int i = 0; i < shots.Count; i++) FireShot(i, shots[i], lv);
                return;
            }

            PlayShotSound();
            for (int i = 0; i < _targets.Count; i++)
            {
                if (attackType == TowerAttackType.Beam) FireBeam(_targets[i], lv, FirePosition);
                else FireProjectile(_targets[i], lv, FirePosition);
            }
        }

        private IEnumerator FireAfterDelay(List<Unit> shots, float delay)
        {
            yield return new WaitForSeconds(delay);
            TowerLevel lv = Current;
            for (int i = 0; i < shots.Count; i++)
            {
                Unit t = shots[i];
                // เป้าตายระหว่างตั้งท่า -> หาตัวใหม่ในระยะ
                if (t == null || !t.IsAlive)
                {
                    FindTargets(lv.range, 1);
                    t = _targets.Count > 0 ? _targets[0] : null;
                }
                if (t != null) FireShot(i, t, lv);
            }
        }

        private void PlayShotSound()
        {
            if (SoundKey == "Beam")
            {
                _beamIdle = 0f;
                if (_beamLoop == 0 || AudioManager.Instance == null || !AudioManager.Instance.IsLoopPlaying(_beamLoop))
                    _beamLoop = Sfx.Loop("SFX_Tower_Beam_Loop", 0.55f, transform);
            }
            else Sfx.At(SoundKey == "Bomb" ? "SFX_Tower_Bomb_Shoot" : "SFX_Tower_Orb_Shoot", transform.position, 0.7f);
        }

        private void OnDestroy() => Sfx.StopLoop(ref _beamLoop);

        private void FireShot(int index, Unit target, TowerLevel lv)
        {
            PlayShotSound();
            Vector2 from = visual != null && visual.GetFirePosition(index).HasValue ? visual.GetFirePosition(index).Value : FirePosition;
            if (attackType == TowerAttackType.Beam) FireBeam(target, lv, from);
            else
            {
                Projectile p = FireProjectile(target, lv, from);
                if (p != null && visual != null) visual.OnProjectileSpawned(index, p);
            }
            if (visual != null) visual.OnShotFired(index, target);
        }

        private void FindTargets(float range, int count)
        {
            _targets.Clear();
            Vector2 pos = transform.position;
            for (int n = 0; n < count; n++)
            {
                Unit best = null;
                float bestDist = range;
                for (int i = 0; i < Registry.Enemies.Count; i++)
                {
                    EnemyUnit e = Registry.Enemies[i];
                    if (!Registry.IsValidHostile(e, Team.Player, canHitFlying) || _targets.Contains(e)) continue;
                    float d = Vector2.Distance(pos, e.Position) - e.BodyRadius;
                    if (d <= bestDist) { bestDist = d; best = e; }
                }
                if (best == null) break;
                _targets.Add(best);
            }
        }

        private Vector2 FirePosition => firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        private Projectile FireProjectile(Unit target, TowerLevel lv, Vector2 from)
        {
            if (projectilePrefab == null) { target.TakeDamage(lv.damage, this); return null; }
            Projectile p = Instantiate(projectilePrefab, from, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Projectiles));
            p.Launch(Team.Player, target, lv.damage, canHitFlying, this);
            p.SetAreaRadius(lv.areaRadius);
            if (lv.slowPercent > 0f) p.SetSlow(lv.slowPercent, lv.slowDuration);
            if (lv.poisonPoolChance > 0f && poisonPoolPrefab != null)
            {
                float chance = lv.poisonPoolChance;
                DamageZone pool = poisonPoolPrefab;
                p.OnImpact = pos =>
                {
                    if (UnityEngine.Random.value < chance)
                    {
                        Instantiate(pool, pos, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Effects));
                        Sfx.At("SFX_Tower_Poison_Pool", pos, 0.6f);
                    }
                };
            }
            return p;
        }

        private readonly List<Unit> _chainHit = new List<Unit>();

        private void FireBeam(Unit first, TowerLevel lv, Vector2 from)
        {
            _chainHit.Clear();
            Unit current = first;
            int hops = 1 + Mathf.Max(0, lv.chainTargets);

            for (int h = 0; h < hops && current != null; h++)
            {
                if (beamPrefab != null)
                {
                    BeamEffect b = Instantiate(beamPrefab, GameManager.GetFolder(SpawnFolder.Effects));
                    b.Play(from, current.Position);
                }
                if (h > 0) Sfx.At("SFX_Tower_Beam_Chain", current.Position, 0.6f);
                current.TakeDamage(lv.damage, this);
                if (lv.slowPercent > 0f) current.ApplySlow(lv.slowPercent, lv.slowDuration);
                _chainHit.Add(current);
                from = current.Position;
                current = FindChainTarget(from);
            }
        }

        private Unit FindChainTarget(Vector2 from)
        {
            Unit best = null;
            float bestDist = chainRange;
            for (int i = 0; i < Registry.Enemies.Count; i++)
            {
                EnemyUnit e = Registry.Enemies[i];
                if (!Registry.IsValidHostile(e, Team.Player, canHitFlying) || _chainHit.Contains(e)) continue;
                float d = Vector2.Distance(from, e.Position);
                if (d <= bestDist) { bestDist = d; best = e; }
            }
            return best;
        }

        private void OnDrawGizmosSelected()
        {
            if (levels.Count == 0) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, Current.range);
            if (blockPlayer)
            {
                Gizmos.color = Color.green;
                Vector3 c = FootprintCenter;
                Vector3 prev = c + new Vector3(footprintSize.x * 0.5f, 0f, 0f);
                for (int i = 1; i <= 32; i++)
                {
                    float a = i / 32f * Mathf.PI * 2f;
                    Vector3 p = c + new Vector3(Mathf.Cos(a) * footprintSize.x * 0.5f, Mathf.Sin(a) * footprintSize.y * 0.5f, 0f);
                    Gizmos.DrawLine(prev, p);
                    prev = p;
                }
            }
        }
    }
}
