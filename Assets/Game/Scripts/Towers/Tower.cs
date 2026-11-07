using System;
using System.Collections.Generic;
using UnityEngine;

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
        [Tooltip("ระยะกระโดดของลำแสง (Chain)")]
        [SerializeField] private float chainRange = 3f;
        [SerializeField] private DamageZone poisonPoolPrefab;

        [Header("Levels (0 = สร้าง, 1-3 = อัปเกรด)")]
        [SerializeField] private List<TowerLevel> levels = new List<TowerLevel>();

        [Header("Construction")]
        [Tooltip("เวลาก่อสร้าง/อัปเกรด (วินาที) ระหว่างนี้ป้อมยังไม่ยิง")]
        [SerializeField] private float constructionTime = 1f;
        [SerializeField] private WorldHealthBar constructionBar;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [Tooltip("รูปที่ใช้ในเมนูสร้างป้อม (ว่าง = ใช้รูปของ Body)")]
        [SerializeField] private Sprite icon;
        [Tooltip("ป้อมใหญ่ขึ้นเท่านี้ต่อเลเวล")]
        [SerializeField] private float scalePerLevel = 0.08f;
        [Tooltip("ดาวบอกเลเวล (เปิดทีละอันตาม Tier)")]
        [SerializeField] private GameObject[] levelPips = new GameObject[0];

        private int _levelIndex;
        private float _timer;
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
        public bool IsConstructing => _buildTimer > 0f;
        public Sprite Icon => icon != null ? icon : (bodyRenderer != null ? bodyRenderer.sprite : null);
        public Color IconColor => icon != null || bodyRenderer == null ? Color.white : bodyRenderer.color;

        private void Awake()
        {
            _baseScale = transform.localScale;
            RefreshVisual();
        }

        public void Initialize(TowerSlot slot, int paid)
        {
            Slot = slot;
            _levelIndex = 0;
            TotalSpent = paid;
            RefreshVisual();
            StartConstruction();
        }

        public void Upgrade(int paid)
        {
            if (!CanUpgrade) return;
            _levelIndex++;
            TotalSpent += paid;
            RefreshVisual();
            StartConstruction();
        }

        private void RefreshVisual()
        {
            transform.localScale = _baseScale * (1f + scalePerLevel * _levelIndex);
            for (int i = 0; i < levelPips.Length; i++)
                if (levelPips[i] != null) levelPips[i].SetActive(i < _levelIndex);
        }

        private void StartConstruction()
        {
            _buildTimer = constructionTime;
            if (_buildTimer > 0f)
            {
                SetBodyAlpha(0.35f);
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
                if (_buildTimer <= 0f) SetBodyAlpha(1f);
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            TowerLevel lv = Current;
            FindTargets(lv.range, Mathf.Max(1, lv.targetsPerShot));
            if (_targets.Count == 0) return;

            for (int i = 0; i < _targets.Count; i++)
            {
                if (attackType == TowerAttackType.Beam) FireBeam(_targets[i], lv);
                else FireProjectile(_targets[i], lv);
            }
            _timer = lv.attackInterval;
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

        private void FireProjectile(Unit target, TowerLevel lv)
        {
            if (projectilePrefab == null) { target.TakeDamage(lv.damage, this); return; }
            Projectile p = Instantiate(projectilePrefab, FirePosition, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Projectiles));
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
                        Instantiate(pool, pos, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Effects));
                };
            }
        }

        private readonly List<Unit> _chainHit = new List<Unit>();

        private void FireBeam(Unit first, TowerLevel lv)
        {
            _chainHit.Clear();
            Vector2 from = FirePosition;
            Unit current = first;
            int hops = 1 + Mathf.Max(0, lv.chainTargets);

            for (int h = 0; h < hops && current != null; h++)
            {
                if (beamPrefab != null)
                {
                    BeamEffect b = Instantiate(beamPrefab, GameManager.GetFolder(SpawnFolder.Effects));
                    b.Play(from, current.Position);
                }
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
        }
    }
}
