using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// คลาสแม่ของ "ยูนิตที่เดินและสู้เองได้" (EnemyUnit, MinionUnit)
    /// วนลูป: เลือกเป้า -> ถ้าอยู่ในระยะก็ตี / ถ้าไม่ถึงก็เดินเข้าไป -> ถ้าไม่มีเป้าก็เดินกลับจุดพัก
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public abstract class CombatUnit : Unit
    {
        [Header("Movement")]
        [Tooltip("ความเร็วเดิน (หน่วย/วินาที)")]
        [SerializeField] protected float moveSpeed = 2f;

        [Header("Attack")]
        [SerializeField] protected AttackStyle attackStyle = AttackStyle.Melee;
        [Tooltip("ดาเมจต่อการโจมตี 1 ครั้ง")]
        [SerializeField] protected float attackDamage = 5f;
        [Tooltip("ระยะโจมตี (วัดจากตัวเราถึงขอบตัวเป้าหมาย)")]
        [SerializeField] protected float attackRange = 0.5f;
        [Tooltip("วินาทีต่อการโจมตี 1 ครั้ง")]
        [SerializeField] protected float attackInterval = 1f;
        [Tooltip("รัศมีทุบพื้น (ใช้เมื่อ Attack Style = MeleeArea)")]
        [SerializeField] protected float areaRadius = 1.5f;
        [Tooltip("กระสุน (ใช้เมื่อ Attack Style = Ranged)")]
        [SerializeField] protected Projectile projectilePrefab;
        [Tooltip("ตีตัวบินได้ไหม")]
        [SerializeField] protected bool canAttackFlying = true;
        [Tooltip("เอฟเฟกต์ตอนทุบพื้น (ไม่ใส่ก็ได้)")]
        [SerializeField] protected RingEffect areaAttackEffect;

        [Header("Sound")]
        [Tooltip("ชนิดเสียง: Slime / MiniSlime / Goblin / Archer / Vulture / Giant (ว่าง = เดาจากชื่อ Prefab)")]
        [SerializeField] protected string soundKey = "";

        protected Rigidbody2D rb;
        private string _resolvedSoundKey;
        private float _stepTimer;

        /// <summary>ชนิดเสียงของยูนิตนี้ (ใช้ประกอบชื่อไฟล์เสียง)</summary>
        public string SoundKey
        {
            get
            {
                if (_resolvedSoundKey != null) return _resolvedSoundKey;
                string n = !string.IsNullOrEmpty(soundKey) ? soundKey : gameObject.name;
                if (n.Contains("SlimeSmall") || n.Contains("MiniSlime") || n.Contains("Mini Slime")) _resolvedSoundKey = "MiniSlime";
                else if (n.Contains("Slime")) _resolvedSoundKey = "Slime";
                else if (n.Contains("Goblin")) _resolvedSoundKey = "Goblin";
                else if (n.Contains("Skeleton") || n.Contains("Archer")) _resolvedSoundKey = "Archer";
                else if (n.Contains("Vulture")) _resolvedSoundKey = "Vulture";
                else if (n.Contains("Giant")) _resolvedSoundKey = "Giant";
                else _resolvedSoundKey = string.IsNullOrEmpty(soundKey) ? "" : soundKey;
                return _resolvedSoundKey;
            }
        }

        /// <summary>ไฟล์เสียงตอนโจมตี (คลาสลูกกำหนด)</summary>
        protected virtual string AttackSoundId => null;
        protected Unit currentTarget;
        protected float attackTimer;

        // ตัวคูณจากการอัปเกรด (มินเนียนใช้)
        protected float damageMultiplier = 1f;
        protected float moveSpeedMultiplier = 1f;
        protected float attackSpeedMultiplier = 1f;

        private static readonly List<Unit> _hitBuffer = new List<Unit>();

        public Unit CurrentTarget => currentTarget;
        public float CurrentMoveSpeed => moveSpeed * moveSpeedMultiplier * SlowMultiplier;
        public float AttackDamage => attackDamage * damageMultiplier;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive) return;

            if (!GameManager.IsGameplayRunning)
            {
                StopMoving();
                return;
            }

            attackTimer -= Time.deltaTime;
            currentTarget = ChooseTarget();

            if (currentTarget != null)
            {
                float edgeDistance = Vector2.Distance(Position, currentTarget.Position) - currentTarget.BodyRadius - bodyRadius;
                if (edgeDistance <= attackRange)
                {
                    StopMoving();
                    FaceTowards(currentTarget.Position);
                    if (attackTimer <= 0f)
                    {
                        PerformAttack(currentTarget);
                        attackTimer = attackInterval / Mathf.Max(0.01f, attackSpeedMultiplier);
                    }
                }
                else
                {
                    MoveTowards(currentTarget.Position);
                }
            }
            else
            {
                TryAttackWhileMoving();
                Vector2? idle = GetIdleDestination();
                if (idle.HasValue && Vector2.Distance(Position, idle.Value) > 0.2f) MoveTowards(idle.Value);
                else StopMoving();
            }
        }

        /// <summary>คลาสลูกบอกว่าจะตีใคร (null = ไม่มีเป้า)</summary>
        protected abstract Unit ChooseTarget();

        /// <summary>ยิงระหว่างเดินได้ไหม (Skeleton Archer) - คลาสลูก override</summary>
        protected virtual void TryAttackWhileMoving() { }

        /// <summary>ใช้ตอนยิงระหว่างเดิน: ตีถ้าคูลดาวน์พร้อม (คืน true ถ้าตี)</summary>
        protected bool AttackIfReady(Unit target)
        {
            if (target == null || attackTimer > 0f) return false;
            PerformAttack(target);
            attackTimer = attackInterval / Mathf.Max(0.01f, attackSpeedMultiplier);
            return true;
        }

        /// <summary>เป้าหมายอยู่ในระยะโจมตีไหม</summary>
        protected bool InAttackRange(Unit target) =>
            target != null && Vector2.Distance(Position, target.Position) - target.BodyRadius - bodyRadius <= attackRange;

        /// <summary>ไม่มีเป้าแล้วจะเดินไปไหน (null = ยืนเฉยๆ)</summary>
        protected virtual Vector2? GetIdleDestination() => null;

        protected void MoveTowards(Vector2 destination)
        {
            Vector2 dir = (destination - Position).normalized;
            Vector2 velocity = dir * CurrentMoveSpeed + GetSteeringOffset(dir);
            if (rb != null) rb.linearVelocity = velocity;
            else transform.position += (Vector3)(velocity * Time.deltaTime);
            FaceTowards(destination);

            // เสียงก้าวหนักของ Giant
            if (SoundKey == "Giant")
            {
                _stepTimer -= Time.deltaTime;
                if (_stepTimer <= 0f)
                {
                    _stepTimer = 0.95f;
                    Sfx.At("SFX_Enemy_Giant_Step", Position, Team == Team.Enemy ? 0.8f : 0.5f);
                    if (Team == Team.Enemy) CameraShake.Shake(0.05f, 0.15f);
                }
            }
        }

        /// <summary>แรงเบี่ยงทิศตอนเดิน (เช่น หลบเพื่อนที่ขวางอยู่) - คลาสลูก override, ค่าเริ่มต้น = ไม่เบี่ยง</summary>
        protected virtual Vector2 GetSteeringOffset(Vector2 moveDir) => Vector2.zero;

        protected void StopMoving()
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }

        protected void FaceTowards(Vector2 point)
        {
            float dx = point.x - Position.x;
            if (Mathf.Abs(dx) <= 0.01f) return;
            bool left = dx < 0f;
            if (visualRoot != null)
            {
                Vector3 sc = visualRoot.localScale;
                sc.x = Mathf.Abs(sc.x) * (left ? -1f : 1f);
                visualRoot.localScale = sc;
            }
            else if (bodyRenderer != null) bodyRenderer.flipX = left;
        }

        protected virtual void PerformAttack(Unit target)
        {
            float dmg = AttackDamage;
            string snd = AttackSoundId;
            if (!string.IsNullOrEmpty(snd)) Sfx.At(snd, Position, Team == Team.Enemy ? 1f : 0.6f);
            if (attackStyle == AttackStyle.MeleeArea && Team == Team.Enemy) CameraShake.Shake(0.15f, 0.25f);
            switch (attackStyle)
            {
                case AttackStyle.Melee:
                    target.TakeDamage(dmg);
                    break;

                case AttackStyle.MeleeArea:
                    Registry.GetHostilesInRadius(Position, areaRadius, Team, canAttackFlying, _hitBuffer);
                    for (int i = 0; i < _hitBuffer.Count; i++) _hitBuffer[i].TakeDamage(dmg);
                    if (areaAttackEffect != null)
                    {
                        RingEffect fx = Instantiate(areaAttackEffect, Position, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Effects));
                        fx.Play(areaRadius);
                    }
                    break;

                case AttackStyle.Ranged:
                    if (projectilePrefab == null)
                    {
                        target.TakeDamage(dmg);
                        break;
                    }
                    Projectile p = Instantiate(projectilePrefab, Position, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Projectiles));
                    p.Launch(Team, target, dmg, canAttackFlying);
                    break;
            }
        }

        protected override void OnDeath()
        {
            StopMoving();
            base.OnDeath();
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, bodyRadius + attackRange);
            if (attackStyle == AttackStyle.MeleeArea)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f);
                Gizmos.DrawWireSphere(transform.position, areaRadius);
            }
        }
    }
}
