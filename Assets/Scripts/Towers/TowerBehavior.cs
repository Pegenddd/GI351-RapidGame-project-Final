using System.Collections;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;
using NecroDefense.Characters.Minions;
using NecroDefense.Characters.Player;

namespace NecroDefense.Towers
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Enum: TowerType
    //  ประเภทของทาวเวอร์ที่ผู้เล่นสามารถสร้างได้
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// ประเภทของทาวเวอร์ทั้ง 3 แบบ
    /// </summary>
    public enum TowerType
    {
        /// <summary>ทาวเวอร์ระเบิด AoE — โจมตีศัตรูในวงรัศมีรอบเป้าหมาย</summary>
        AoEBlast,

        /// <summary>ทาวเวอร์ชะลอ — โจมตีเป้าหมายเดี่ยวและลดความเร็ว</summary>
        SlowTarget,

        /// <summary>ทาวเวอร์สร้าง Minion — สร้างก็อบลินออกมาช่วยสู้ตาม Cooldown</summary>
        GoblinSpawner,
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Class: TowerBehavior
    //  ตัว Tower หลักที่รับผิดชอบ AI / Logic ของแต่ละ TowerType
    //  แนบกับ Prefab ของทาวเวอร์แต่ละประเภท
    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// พฤติกรรมหลักของทาวเวอร์ทั้ง 3 ประเภท
    /// Demonstrates:
    /// - Enum-driven Polymorphism ผ่าน switch
    /// - Coroutine-based Attack Cooldown
    /// - Physics2D.OverlapCircleAll สำหรับหาเป้าหมาย
    /// </summary>
    public class TowerBehavior : MonoBehaviour
    {
        // ── Inspector Settings ─────────────────────────────────────────────
        [Header("Tower Identity")]
        [Tooltip("ประเภทของทาวเวอร์นี้")]
        [SerializeField] private TowerType towerType = TowerType.AoEBlast;

        [Header("Combat Stats")]
        [Tooltip("ระยะโจมตีของทาวเวอร์ (หน่วย: units)")]
        [SerializeField] private float attackRange = 5f;

        [Tooltip("ระยะเวลา Cooldown ระหว่างการโจมตีแต่ละครั้ง (วินาที)")]
        [SerializeField] private float attackCooldown = 2f;

        [Tooltip("ค่าความเสียหายต่อครั้งที่โจมตี")]
        [SerializeField] private float attackDamage = 15f;

        [Header("Build Cost")]
        [Tooltip("ค่าใช้จ่าย Soul ในการสร้างทาวเวอร์นี้")]
        [SerializeField] private int buildCost = 30;

        // ── AoEBlast Settings ──────────────────────────────────────────────
        [Header("AoE Blast Settings (AoEBlast only)")]
        [Tooltip("รัศมีระเบิด AoE รอบเป้าหมาย")]
        [SerializeField] private float aoeExplosionRadius = 3f;

        [Tooltip("Prefab เอฟเฟกต์ระเบิด (ไม่บังคับ — จะสร้างแบบ procedural ถ้าไม่ได้ตั้งค่า)")]
        [SerializeField] private GameObject aoeExplosionVfxPrefab;

        // ── SlowTarget Settings ────────────────────────────────────────────
        [Header("Slow Target Settings (SlowTarget only)")]
        [Tooltip("เปอร์เซ็นต์ลดความเร็วของศัตรูที่ถูกยิง (0.0–1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float slowPercent = 0.4f;

        [Tooltip("ระยะเวลาที่ศัตรูถูกชะลอ (วินาที)")]
        [SerializeField] private float slowDuration = 3f;

        [Tooltip("Prefab กระสุนที่ยิงออก (ไม่บังคับ)")]
        [SerializeField] private GameObject projectilePrefab;

        // ── GoblinSpawner Settings ─────────────────────────────────────────
        [Header("Goblin Spawner Settings (GoblinSpawner only)")]
        [Tooltip("Prefab ของก็อบลิน Minion ที่จะสร้างออกมา")]
        [SerializeField] private GameObject goblinMinionPrefab;

        [Tooltip("จำนวนก็อบลินสูงสุดที่ Tower นี้รักษาไว้พร้อมกัน")]
        [SerializeField] private int maxGoblins = 3;

        [Tooltip("ออฟเซ็ตรัศมีที่ก็อบลิน Spawn ออกมา")]
        [SerializeField] private float spawnRadius = 1.2f;

        // ── Private Runtime State ──────────────────────────────────────────
        private float    _lastAttackTime = -999f;
        private int      _activeGoblinCount = 0;
        private SpriteRenderer _spriteRenderer;

        // ── Public Properties ──────────────────────────────────────────────
        public TowerType Type       => towerType;
        public float     AttackRange => attackRange;
        public float     Cooldown   => attackCooldown;
        public int       BuildCost  => buildCost;

        // ──────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            gameObject.tag = "Tower";
        }

        private void Update()
        {
            if (Time.time < _lastAttackTime + attackCooldown) return;

            switch (towerType)
            {
                case TowerType.AoEBlast:     UpdateAoEBlast();     break;
                case TowerType.SlowTarget:   UpdateSlowTarget();   break;
                case TowerType.GoblinSpawner: UpdateGoblinSpawner(); break;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  AoEBlast: หาศัตรูที่ใกล้ที่สุด แล้วระเบิด AoE รอบตำแหน่งนั้น
        // ─────────────────────────────────────────────────────────────────
        private void UpdateAoEBlast()
        {
            ITargetable closestEnemy = FindClosestEnemy();
            if (closestEnemy == null) return;

            _lastAttackTime = Time.time;
            TriggerAoEExplosion(closestEnemy.Position);
        }

        private void TriggerAoEExplosion(Vector2 center)
        {
            // ─ Visual Effect ─
            if (aoeExplosionVfxPrefab != null)
            {
                Instantiate(aoeExplosionVfxPrefab, center, Quaternion.identity);
            }
            else
            {
                SpawnProceduralWave(center, aoeExplosionRadius);
            }

            // ─ Deal AoE Damage ─
            Collider2D[] hits = Physics2D.OverlapCircleAll(center, aoeExplosionRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable) &&
                    hit.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team == Faction.Enemy && targetable.IsValidTarget)
                    {
                        Vector2 dir = ((Vector2)targetable.Position - center).normalized;
                        var info = new DamageInfo(attackDamage, DamageType.Necrotic, null, dir, 1f);
                        damageable.TakeDamage(info);
                    }
                }
            }

            Debug.Log($"<color=orange>[AoEBlast]</color> ระเบิดที่ {center} รัศมี {aoeExplosionRadius}u");
        }

        // ─────────────────────────────────────────────────────────────────
        //  SlowTarget: ยิงกระสุน + หน่วงความเร็วศัตรูที่ใกล้ที่สุด
        // ─────────────────────────────────────────────────────────────────
        private void UpdateSlowTarget()
        {
            ITargetable closestEnemy = FindClosestEnemy();
            if (closestEnemy == null) return;

            _lastAttackTime = Time.time;

            // ─ Visual: Spawn projectile หรือ Line เส้นกระสุน ─
            if (projectilePrefab != null)
            {
                var proj = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
                var dir = ((Vector2)closestEnemy.Position - (Vector2)transform.position).normalized;
                if (proj.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.linearVelocity = dir * 8f;
                }
            }
            else
            {
                // Procedural: เส้นยิงชั่วคราว
                StartCoroutine(DrawShootLine(transform.position, closestEnemy.Position));
            }

            // ─ Deal Damage + Apply Slow ─
            if (closestEnemy is IDamageable damageable)
            {
                Vector2 hitDir = ((Vector2)closestEnemy.Position - (Vector2)transform.position).normalized;
                var info = new DamageInfo(attackDamage, DamageType.Void, null, hitDir, 0f);
                damageable.TakeDamage(info);
            }

            // ─ Apply Slow via MonoBehaviour ─
            if (closestEnemy.TargetTransform.TryGetComponent<Unit>(out var targetUnit))
            {
                StartCoroutine(ApplySlowEffect(targetUnit, slowPercent, slowDuration));
            }

            Debug.Log($"<color=cyan>[SlowTarget]</color> ยิงชะลอศัตรูที่ {closestEnemy.Position}");
        }

        /// <summary>
        /// ลดความเร็วของยูนิตแล้วคืนค่าหลังจาก slowDuration วินาที
        /// </summary>
        private IEnumerator ApplySlowEffect(Unit unit, float slowFraction, float duration)
        {
            if (unit == null || !unit.IsAlive) yield break;

            float originalSpeed = unit.MoveSpeed;
            unit.SetMoveSpeed(originalSpeed * (1f - slowFraction));

            yield return new WaitForSeconds(duration);

            if (unit != null && unit.IsAlive)
            {
                unit.SetMoveSpeed(originalSpeed);
            }
        }

        /// <summary>
        /// วาดเส้นยิงชั่วคราว (Procedural Line Renderer แบบ 1-frame)
        /// </summary>
        private IEnumerator DrawShootLine(Vector2 from, Vector2 to)
        {
            var lineObj = new GameObject("TowerShootLine");
            var lr = lineObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, from);
            lr.SetPosition(1, to);
            lr.startWidth = 0.05f;
            lr.endWidth   = 0.05f;
            lr.material   = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = Color.white;
            lr.endColor   = Color.white;
            lr.sortingOrder = 10;

            yield return new WaitForSeconds(0.08f);
            Destroy(lineObj);
        }

        // ─────────────────────────────────────────────────────────────────
        //  GoblinSpawner: สร้างก็อบลิน Minion ถ้าจำนวนต่ำกว่า maxGoblins
        // ─────────────────────────────────────────────────────────────────
        private void UpdateGoblinSpawner()
        {
            if (_activeGoblinCount >= maxGoblins) return;
            if (goblinMinionPrefab == null)
            {
                Debug.LogWarning("[GoblinSpawner] goblinMinionPrefab ยังไม่ได้ตั้งค่าใน Inspector!");
                return;
            }

            _lastAttackTime = Time.time;

            // เลือกจุด Spawn รอบทาวเวอร์แบบสุ่ม
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector2 spawnOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;
            Vector3 spawnPos = transform.position + (Vector3)spawnOffset;

            GameObject goblinObj = Instantiate(goblinMinionPrefab, spawnPos, Quaternion.identity);
            _activeGoblinCount++;

            // ฟังเมื่อก็อบลิน Minion ตาย เพื่อลดตัวนับ
            if (goblinObj.TryGetComponent<MinionUnit>(out var minion))
            {
                minion.OnDied += OnGoblinDied;
            }

            Debug.Log($"<color=green>[GoblinSpawner]</color> Spawn ก็อบลิน #{_activeGoblinCount}/{maxGoblins} ที่ {spawnPos}");
        }

        private void OnGoblinDied()
        {
            _activeGoblinCount = Mathf.Max(0, _activeGoblinCount - 1);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Utility: หาศัตรูที่ใกล้ที่สุดในระยะ attackRange
        // ─────────────────────────────────────────────────────────────────
        private ITargetable FindClosestEnemy()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, attackRange);
            ITargetable closest = null;
            float minDist = float.MaxValue;

            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<ITargetable>(out var targetable)) continue;
                if (targetable.Team != Faction.Enemy || !targetable.IsValidTarget) continue;

                float dist = Vector2.Distance((Vector2)transform.position, (Vector2)targetable.Position);
                if (dist < minDist)
                {
                    minDist  = dist;
                    closest  = targetable;
                }
            }
            return closest;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Procedural Wave Visual (เหมือนกับ PlayerNecromancer)
        // ─────────────────────────────────────────────────────────────────
        private void SpawnProceduralWave(Vector2 center, float radius)
        {
            var waveObj = new GameObject("TowerAoeWave");
            waveObj.transform.position = center;
            waveObj.transform.localScale = Vector3.zero;

            var sr = waveObj.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color  = Color.white;
            sr.sortingOrder = 12;

            var wave = waveObj.AddComponent<Characters.Player.AoeWaveVisual>();
            wave.Initialize(radius * 2f, 0.35f);
        }

        private Sprite CreateCircleSprite()
        {
            int size = 64;
            var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = size / 2f;
            float r      = center - 2f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, (d <= r && d >= r - 4f) ? Color.white : new Color(0,0,0,0));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Gizmo: แสดง Attack Range ใน Scene View
        // ─────────────────────────────────────────────────────────────────
        private void OnDrawGizmosSelected()
        {
            // Attack Range
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, attackRange);

            // AoE Explosion Radius (เฉพาะ AoEBlast)
            if (towerType == TowerType.AoEBlast)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, aoeExplosionRadius);
            }

            // Spawn radius (เฉพาะ GoblinSpawner)
            if (towerType == TowerType.GoblinSpawner)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.3f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, spawnRadius);
            }
        }
    }
}
