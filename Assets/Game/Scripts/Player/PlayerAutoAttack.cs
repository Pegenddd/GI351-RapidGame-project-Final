using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// โจมตีอัตโนมัติของผู้เล่น
    /// - มีศัตรูในระยะ: ยิงลูกบอลเวท (Magic Bolt) ใส่ตัวที่ใกล้สุด
    /// - ไม่มีศัตรูในระยะ: ลูกบอลเวทหมุนรอบตัว (Orbit) ทำดาเมจศัตรูที่เข้ามาใกล้
    /// </summary>
    public class PlayerAutoAttack : MonoBehaviour
    {
        [Header("Magic Bolt (ยิงอัตโนมัติ)")]
        [SerializeField] private Projectile boltPrefab;
        [SerializeField] private float boltDamage = 12f;
        [Tooltip("วินาทีต่อนัด")]
        [SerializeField] private float attackInterval = 0.6f;
        [Tooltip("ระยะหาเป้าหมาย")]
        [SerializeField] private float attackRange = 7f;

        [Header("Orbit Mode (ตอนไม่มีศัตรูในระยะ)")]
        [Tooltip("รูปลูกบอลที่หมุนรอบตัว")]
        [SerializeField] private GameObject orbPrefab;
        [Tooltip("GameObject ลูกที่เก็บลูกบอลหมุน (ว่าง = ใช้ตัวเอง)")]
        [SerializeField] private Transform orbHolder;
        [SerializeField] private int orbCount = 2;
        [SerializeField] private float orbRadius = 1.4f;
        [Tooltip("องศาต่อวินาที")]
        [SerializeField] private float orbRotateSpeed = 220f;
        [SerializeField] private float orbDamage = 6f;
        [Tooltip("รัศมีชนของลูกบอลหมุน")]
        [SerializeField] private float orbHitRadius = 0.35f;
        [Tooltip("ศัตรูตัวเดิมโดนซ้ำได้ทุกกี่วินาที")]
        [SerializeField] private float orbHitCooldown = 0.5f;

        private PlayerNecromancer _player;
        private float _attackTimer;
        private float _orbAngle;
        private float _orbDamageMultiplier = 1f;
        private readonly List<Transform> _orbs = new List<Transform>();
        private readonly Dictionary<Unit, float> _orbHitTimes = new Dictionary<Unit, float>();

        public int OrbCount => orbCount;

        private void Awake()
        {
            _player = GetComponent<PlayerNecromancer>();
            if (orbHolder == null) orbHolder = transform;
        }

        private void Start() => RebuildOrbs();

        private void Update()
        {
            if (_player == null || !_player.IsAlive || !GameManager.IsGameplayRunning) return;

            _attackTimer -= Time.deltaTime;
            Unit target = Registry.FindNearestHostile(_player.Position, Team.Player, attackRange, true);

            bool orbitMode = target == null;
            SetOrbsVisible(orbitMode);

            if (!orbitMode)
            {
                if (_attackTimer <= 0f)
                {
                    FireBolt(target);
                    _attackTimer = attackInterval;
                }
            }
            else
            {
                UpdateOrbs();
            }
        }

        private void FireBolt(Unit target)
        {
            if (boltPrefab == null) { target.TakeDamage(boltDamage); return; }
            Projectile p = Instantiate(boltPrefab, _player.Position, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Projectiles));
            p.Launch(Team.Player, target, boltDamage, true);
        }

        private void UpdateOrbs()
        {
            if (_orbs.Count == 0) return;
            if (_orbHitTimes.Count > 200) _orbHitTimes.Clear(); // ล้างตัวที่ตายไปแล้ว
            _orbAngle += orbRotateSpeed * Time.deltaTime;
            float step = 360f / _orbs.Count;
            float now = Time.time;

            for (int i = 0; i < _orbs.Count; i++)
            {
                float a = (_orbAngle + step * i) * Mathf.Deg2Rad;
                Vector2 pos = _player.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * orbRadius;
                _orbs[i].position = pos;

                for (int e = 0; e < Registry.Enemies.Count; e++)
                {
                    EnemyUnit enemy = Registry.Enemies[e];
                    if (!enemy.IsAlive) continue;
                    if (Vector2.Distance(pos, enemy.Position) > orbHitRadius + enemy.BodyRadius) continue;
                    if (_orbHitTimes.TryGetValue(enemy, out float last) && now - last < orbHitCooldown) continue;
                    _orbHitTimes[enemy] = now;
                    enemy.TakeDamage(orbDamage * _orbDamageMultiplier);
                }
            }
        }

        private void SetOrbsVisible(bool visible)
        {
            for (int i = 0; i < _orbs.Count; i++)
                if (_orbs[i].gameObject.activeSelf != visible) _orbs[i].gameObject.SetActive(visible);
        }

        private void RebuildOrbs()
        {
            foreach (Transform t in _orbs) if (t != null) Destroy(t.gameObject);
            _orbs.Clear();
            if (orbPrefab == null) return;
            for (int i = 0; i < orbCount; i++)
            {
                GameObject orb = Instantiate(orbPrefab, orbHolder);
                orb.name = "OrbitOrb_" + (i + 1);
                _orbs.Add(orb.transform);
            }
        }

        /// <summary>การ์ด Orbital Magic: +จำนวนลูก และ +% ดาเมจ</summary>
        public void UpgradeOrbital(int extraOrbs, float damagePercent)
        {
            orbCount += extraOrbs;
            _orbDamageMultiplier += damagePercent;
            RebuildOrbs();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.7f, 0.3f, 1f);
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Gizmos.DrawWireSphere(transform.position, orbRadius);
        }
    }
}
