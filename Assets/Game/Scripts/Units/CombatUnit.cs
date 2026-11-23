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
        [Tooltip("ปล่อยกระสุนหลังเริ่มท่าตีกี่วินาที (ให้ตรงกับจังหวะขว้าง/ยิงในท่าตี) 0 = ทันที")]
        [SerializeField] protected float projectileDelay = 0f;
        [Tooltip("จุดปล่อยกระสุน เทียบกับตัว (x กลับด้านตามทิศที่หัน) เช่น มือที่ชูเหนือหัว")]
        [SerializeField] protected Vector2 projectileSpawnOffset = Vector2.zero;
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

        [Header("Facing (หันซ้าย-ขวา)")]
        [Tooltip("หันซ้าย-ขวาตามทิศที่เดิน/ตี (ปิด = หันตามรูปที่วาดตลอด ไม่กลับด้าน)")]
        [SerializeField] protected bool flipToFace = true;
        [Tooltip("ไม่หันกลับถ้าเป้า/จุดหมายอยู่เกือบตรงขึ้น-ลง (กันหันซ้าย-ขวาสลับไปมาตอนเดินทางตั้ง) - ยิ่งมากยิ่งหันยาก")]
        [SerializeField] protected float faceDeadZone = 0.35f;
        [Tooltip("หันกลับได้ไม่ถี่กว่านี้ (วินาที)")]
        [SerializeField] protected float faceMinHoldTime = 0.3f;
        private float _lastFaceFlipTime = -10f;

        protected void FaceTowards(Vector2 point)
        {
            if (!flipToFace)
            {
                if (visualRoot != null && visualRoot.localScale.x < 0f)
                {
                    Vector3 s = visualRoot.localScale; s.x = -s.x; visualRoot.localScale = s;
                }
                else if (visualRoot == null && bodyRenderer != null) bodyRenderer.flipX = false;
                return;
            }
            float dx = point.x - Position.x;
            float dy = point.y - Position.y;
            // จุดหมายเกือบตรงขึ้น-ลง หรือเพิ่งหันไป -> หันเหมือนเดิม
            if (Mathf.Abs(dx) <= Mathf.Max(0.05f, Mathf.Abs(dy) * faceDeadZone)) return;
            bool left = dx < 0f;
            bool nowLeft = visualRoot != null ? visualRoot.localScale.x < 0f : (bodyRenderer != null && bodyRenderer.flipX);
            if (left == nowLeft) return;
            if (Time.time - _lastFaceFlipTime < faceMinHoldTime) return;
            _lastFaceFlipTime = Time.time;
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
                    if (projectileDelay > 0f) StartCoroutine(FireProjectileLater(target, dmg));
                    else FireProjectile(target, dmg);
                    break;
            }
        }

        private System.Collections.IEnumerator FireProjectileLater(Unit target, float dmg)
        {
            yield return new WaitForSeconds(projectileDelay);
            if (!IsAlive || target == null || !target.IsAlive) yield break;
            FireProjectile(target, dmg);
        }

        private void FireProjectile(Unit target, float dmg)
        {
            bool left = visualRoot != null ? visualRoot.localScale.x < 0f : (bodyRenderer != null && bodyRenderer.flipX);
            Vector2 off = projectileSpawnOffset;
            if (left) off.x = -off.x;
            Projectile p = Instantiate(projectilePrefab, Position + off, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Projectiles));
            p.Launch(Team, target, dmg, canAttackFlying);
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
