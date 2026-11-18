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

        protected Rigidbody2D rb;
        protected Unit currentTarget;
        protected float attackTimer;

        // ตัวคูณจากการอัปเกรด (มินเนียนใช้)
        protected float damageMultiplier = 1f;
        protected float moveSpeedMultiplier = 1f;
        protected float attackSpeedMultiplier = 1f;

        private static readonly List<Unit> _hitBuffer = new List<Unit>();

        /// <summary>เรียกทุกครั้งที่โจมตี (ใช้เล่นท่าตี)</summary>
        public event System.Action Attacked;

        public Unit CurrentTarget => currentTarget;
        public float CurrentMoveSpeed => moveSpeed * moveSpeedMultiplier * SlowMultiplier;
        public float AttackDamage => attackDamage * damageMultiplier;

        protected override void Awake()
        {
            base.Awake();
            rb = GetComponent<Rigidbody2D>();
        }

        /// <summary>คัดลอกค่าพื้นฐาน (เลือด / เดิน / โจมตี) จากไฟล์ Data</summary>
        protected void ApplyUnitStats(UnitStats s)
        {
            if (s == null) return;
            maxHealth = s.maxHealth;
            armor = s.armor;
            isFlying = s.isFlying;
            moveSpeed = s.moveSpeed;
            attackStyle = s.attackStyle;
            attackDamage = s.attackDamage;
            attackRange = s.attackRange;
            attackInterval = s.attackInterval;
            areaRadius = s.areaRadius;
            canAttackFlying = s.canAttackFlying;
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
            SetTarget(ChooseTarget());

            if (currentTarget != null)
            {
                float edgeDistance = Vector2.Distance(Position, currentTarget.Position) - currentTarget.BodyRadius - bodyRadius;
                Vector2? slot = GetAttackSlot(currentTarget);
                if (edgeDistance <= attackRange)
                {
                    // รุมตี: ขยับเข้าช่องของตัวเองรอบเป้า (ไม่ยืนซ้อน/ไม่ดันกัน) ระหว่างนั้นตีไปด้วยได้
                    if (slot.HasValue && Vector2.Distance(Position, slot.Value) > SlotTolerance) MoveTowards(slot.Value);
                    else StopMoving();
                    FaceTowards(currentTarget.Position);
                    if (attackTimer <= 0f)
                    {
                        PerformAttack(currentTarget);
                        attackTimer = attackInterval / Mathf.Max(0.01f, attackSpeedMultiplier);
                    }
                }
                else
                {
                    MoveTowards(slot ?? currentTarget.Position);
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

        /// <summary>เปลี่ยนเป้า (แจ้งคลาสลูกเมื่อเป้าเปลี่ยน)</summary>
        protected void SetTarget(Unit target)
        {
            if (target == currentTarget) return;
            Unit old = currentTarget;
            currentTarget = target;
            OnTargetChanged(old, target);
        }

        /// <summary>เป้าเปลี่ยน (มินเนียนใช้ลงชื่อ/ถอนชื่อว่ากำลังสู้กับศัตรูตัวไหน)</summary>
        protected virtual void OnTargetChanged(Unit oldTarget, Unit newTarget) { }

        /// <summary>จุดยืนตอนรุมตีรอบเป้า (null = เดินเข้าหาเป้าตรงๆ)</summary>
        protected virtual Vector2? GetAttackSlot(Unit target) => null;

        /// <summary>ห่างจากช่องยืนไม่เกินเท่านี้ถือว่าถึงแล้ว</summary>
        protected virtual float SlotTolerance => 0.2f;

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
            Attacked?.Invoke();
            float dmg = AttackDamage;
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
            SetTarget(null);
            base.OnDeath();
        }

        protected override void OnDisable()
        {
            SetTarget(null);
            base.OnDisable();
        }

        /// <summary>ปิดการชนทางฟิสิกส์ระหว่าง 2 ยูนิต (เดินทะลุกันได้ ไม่ดันกัน)</summary>
        protected static void IgnoreCollisions(Component a, Component b)
        {
            if (a == null || b == null || a == b) return;
            Collider2D[] ca = a.GetComponentsInChildren<Collider2D>();
            Collider2D[] cb = b.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < ca.Length; i++)
                for (int k = 0; k < cb.Length; k++)
                    if (!ca[i].isTrigger && !cb[k].isTrigger) Physics2D.IgnoreCollision(ca[i], cb[k], true);
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
