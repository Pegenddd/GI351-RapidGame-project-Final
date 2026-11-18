using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// โจมตีอัตโนมัติของผู้เล่น
    /// - ลูกบอลเวทหมุนรอบตัวตลอด (Orbit) ชนศัตรูที่เข้ามาใกล้ = ดาเมจ
    /// - มีศัตรูในระยะ: ลูกบอลที่หมุนอยู่พุ่งออกไปเป็นกระสุน (Magic Bolt) ทีละลูก ใส่ตัวที่ใกล้สุด
    ///   ลูกที่ยิงไปแล้วจะค่อยๆ เกิดใหม่ (Orb Recharge Time)
    /// </summary>
    public class PlayerAutoAttack : MonoBehaviour
    {
        [Header("Magic Bolt (ยิงอัตโนมัติ)")]
        [SerializeField] private Projectile boltPrefab;
        [SerializeField] private float boltDamage = 12f;
        [Tooltip("วินาทีต่อนัด (ใช้เมื่อปิด Orbs Fire As Bolts - ถ้าเปิด ความถี่ยิงขึ้นกับจำนวนลูกบอล / Orb Recharge Time)")]
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

        [Header("Orbs = กระสุน")]
        [Tooltip("มีศัตรูในระยะ: ลูกบอลที่หมุนอยู่พุ่งออกไปเป็นกระสุนทีละลูก (ปิด = แบบเดิม: ลูกบอลหายแล้วยิงจากตัวผู้เล่น)")]
        [SerializeField] private bool orbsFireAsBolts = true;
        [Tooltip("ลูกบอลแต่ละลูกที่ยิงออกไปแล้ว ใช้เวลากี่วินาทีเกิดใหม่ (ทุกลูกยิงได้ของตัวเอง: 4 ลูก = ยิงได้ 4 นัดต่อรอบ)")]
        [SerializeField] private float orbRechargeTime = 1.2f;
        [Tooltip("ระยะห่างระหว่างลูกที่พุ่งออกไปติดกัน (วินาที) - ลูกพร้อมหลายลูกจะยิงต่อกันเป็นชุด")]
        [SerializeField] private float orbFireGap = 0.15f;
        [Tooltip("เวลาที่ลูกบอลค่อยๆ โตขึ้นตอนเกิดใหม่")]
        [SerializeField] private float orbRespawnGrowTime = 0.25f;

        private PlayerNecromancer _player;
        private float _attackTimer;
        private float _orbAngle;
        private float _orbDamageMultiplier = 1f;
        private readonly List<Transform> _orbs = new List<Transform>();
        private readonly List<float> _orbRecharge = new List<float>();   // > 0 = ยิงไปแล้ว กำลังเกิดใหม่
        private readonly List<float> _orbGrow = new List<float>();       // 0..1 ขนาดตอนเกิดใหม่
        private Vector3 _orbBaseScale = Vector3.one;
        private readonly Dictionary<Unit, float> _orbHitTimes = new Dictionary<Unit, float>();

        public int OrbCount => orbCount;

        private void Awake()
        {
            _player = GetComponent<PlayerNecromancer>();
            if (orbHolder == null) orbHolder = transform;
            PlayerStats s = _player != null ? _player.Stats : null;
            if (s != null)
            {
                boltDamage = s.boltDamage;
                attackInterval = s.attackInterval;
                attackRange = s.attackRange;
                orbCount = s.orbCount;
                orbRadius = s.orbRadius;
                orbRotateSpeed = s.orbRotateSpeed;
                orbDamage = s.orbDamage;
                orbHitRadius = s.orbHitRadius;
                orbHitCooldown = s.orbHitCooldown;
                orbsFireAsBolts = s.orbsFireAsBolts;
                orbRechargeTime = s.orbRechargeTime;
                orbFireGap = s.orbFireGap;
            }
        }

        private void Start() => RebuildOrbs();

        private void Update()
        {
            if (_player == null || !_player.IsAlive || !GameManager.IsGameplayRunning) return;

            _attackTimer -= Time.deltaTime;
            Unit target = Registry.FindNearestHostile(_player.Position, Team.Player, attackRange, true);

            if (orbsFireAsBolts && _orbs.Count > 0)
            {
                UpdateOrbs();
                if (target != null && _attackTimer <= 0f)
                {
                    int orb = PickChargedOrb(target.Position);
                    if (orb >= 0)
                    {
                        FireBolt(target, _orbs[orb].position);
                        _orbRecharge[orb] = Mathf.Max(0.01f, orbRechargeTime);
                        _orbGrow[orb] = 0f;
                        _orbs[orb].gameObject.SetActive(false);
                        _attackTimer = orbFireGap;
                    }
                }
                return;
            }

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

        /// <summary>ลูกบอลที่พร้อมยิงและอยู่ใกล้เป้าที่สุด (-1 = ไม่มีลูกพร้อม)</summary>
        private int PickChargedOrb(Vector2 targetPos)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < _orbs.Count; i++)
            {
                if (_orbRecharge[i] > 0f || _orbGrow[i] < 1f) continue;
                float d = Vector2.Distance(_orbs[i].position, targetPos);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        private void FireBolt(Unit target) => FireBolt(target, _player.Position);

        private void FireBolt(Unit target, Vector2 from)
        {
            if (boltPrefab == null) { target.TakeDamage(boltDamage); return; }
            Sfx.At("SFX_Player_Bolt_Cast", from, 0.7f);
            Projectile p = Instantiate(boltPrefab, from, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Projectiles));
            p.Launch(Team.Player, target, boltDamage, true);
        }

        private void UpdateOrbs()
        {
            if (_orbs.Count == 0) return;
            if (_orbHitTimes.Count > 200) _orbHitTimes.Clear(); // ล้างตัวที่ตายไปแล้ว
            _orbAngle += orbRotateSpeed * Time.deltaTime;
            float step = 360f / _orbs.Count;
            float now = Time.time;

            float dt = Time.deltaTime;
            for (int i = 0; i < _orbs.Count; i++)
            {
                float a = (_orbAngle + step * i) * Mathf.Deg2Rad;
                Vector2 pos = _player.Position + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * orbRadius;
                _orbs[i].position = pos;

                // ลูกที่ยิงไปแล้ว: รอเกิดใหม่ แล้วค่อยๆ โตขึ้น
                if (i < _orbRecharge.Count)
                {
                    if (_orbRecharge[i] > 0f)
                    {
                        _orbRecharge[i] -= dt;
                        if (_orbRecharge[i] > 0f) continue;
                        _orbs[i].gameObject.SetActive(true);
                        _orbGrow[i] = 0f;
                    }
                    if (_orbGrow[i] < 1f)
                    {
                        _orbGrow[i] = orbRespawnGrowTime > 0f ? Mathf.Min(1f, _orbGrow[i] + dt / orbRespawnGrowTime) : 1f;
                        _orbs[i].localScale = _orbBaseScale * Mathf.SmoothStep(0f, 1f, _orbGrow[i]);
                    }
                    if (!_orbs[i].gameObject.activeSelf) _orbs[i].gameObject.SetActive(true);
                }

                for (int e = 0; e < Registry.Enemies.Count; e++)
                {
                    EnemyUnit enemy = Registry.Enemies[e];
                    if (!enemy.IsAlive) continue;
                    if (Vector2.Distance(pos, enemy.Position) > orbHitRadius + enemy.BodyRadius) continue;
                    if (_orbHitTimes.TryGetValue(enemy, out float last) && now - last < orbHitCooldown) continue;
                    _orbHitTimes[enemy] = now;
                    Sfx.At("SFX_Player_Orb_Hit", pos, 0.7f);
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
            _orbRecharge.Clear();
            _orbGrow.Clear();
            if (orbPrefab == null) return;
            _orbBaseScale = orbPrefab.transform.localScale;
            for (int i = 0; i < orbCount; i++)
            {
                GameObject orb = Instantiate(orbPrefab, orbHolder);
                orb.name = "OrbitOrb_" + (i + 1);
                _orbs.Add(orb.transform);
                _orbRecharge.Add(0f);
                _orbGrow.Add(1f);
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
