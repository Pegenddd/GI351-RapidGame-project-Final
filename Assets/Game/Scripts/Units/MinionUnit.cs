using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// มินเนียนฝ่ายเรา 1 ตัว (Slime / Goblin / Skeleton / Vulture / Giant ใช้สคริปต์เดียวกัน)
    /// แต่ละตัวมีคำสั่งของตัวเอง (คลิกที่ตัวมินเนียน -> Hold Area / Move / Follow Player)
    /// Hold Area (ค่าเริ่มต้น) = ปกป้องบริเวณที่ยืน: ตีศัตรูที่เข้ามาในวงมองเห็น แล้วกลับมายืนที่เดิม
    /// ตายแล้ว -> กลับเข้า Minion Bag
    /// </summary>
    public class MinionUnit : CombatUnit
    {
        [Header("Data (ค่าบาลานซ์)")]
        [Tooltip("ไฟล์ค่าของมินเนียนชนิดนี้ (Data/Units/Minions) - ใส่แล้วเกมจะใช้ค่าในไฟล์แทนค่าใน Prefab ด้านล่าง (ว่าง = ใช้ค่าใน Prefab)")]
        [SerializeField] private MinionStats stats;

        [Header("Minion Info")]
        [SerializeField] private string displayName = "Minion";
        [Tooltip("รูปที่ใช้ใน UI (ว่าง = ใช้รูปของ Body)")]
        [SerializeField] private Sprite icon;
        [Tooltip("Soul ที่ใช้อัญเชิญ")]
        [SerializeField] private int soulCost = 10;

        [Header("Selection")]
        [Tooltip("วงที่โชว์ใต้ตัวตอนถูกเลือก")]
        [SerializeField] private GameObject selectionRing;
        [Tooltip("วงโชว์ระยะมองหาศัตรู ตอนถูกเลือก (ปรับขนาดตาม Detection Range อัตโนมัติ)")]
        [SerializeField] private Transform detectionRing;

        [Header("AI Ranges (วงฟ้าใน Scene View)")]
        [Tooltip("ระยะมองหาศัตรู: ศัตรูต้องเข้ามาในวงนี้ก่อนมินเนียนถึงจะเข้าไปตี (Attack Nearest / Self-Defense / Move)\nโหมด Follow วัดจากตัวผู้เล่น")]
        [SerializeField] private float aggroRange = 5f;
        [Tooltip("ไล่ศัตรูออกไปได้ไกลสุดเท่านี้จากจุดเริ่มไล่ ก่อนยอมกลับ (0 = ไม่จำกัด)")]
        [SerializeField] private float chaseLeash = 8f;
        [Tooltip("Guard Base: ไล่ตีศัตรูที่อยู่ในรัศมีนี้รอบคริสตัล")]
        [SerializeField] private float guardRadius = 6f;
        [Tooltip("Guard Base: ระยะยืนเฝ้ารอบคริสตัล")]
        [SerializeField] private float guardStandDistance = 2.5f;
        [Tooltip("Move: ถึงจุดหมายแล้วเมื่อห่างไม่เกินเท่านี้ (จากนั้นยืนเฝ้าแบบ Self-Defense)")]
        [SerializeField] private float moveArriveDistance = 0.35f;
        [Tooltip("Follow Player: ระยะเดินตามผู้เล่น")]
        [SerializeField] private float followDistance = 1.8f;

        [Header("Split On Death (Slime)")]
        [Tooltip("ตายแล้วแตกเป็นตัวอะไร (ว่าง = ไม่แตก) - ตัวที่แตกออกมาตายหมดแล้ว วิญญาณตัวใหญ่ถึงจะกลับเข้ากระเป๋า 1 ตัว")]
        [SerializeField] private MinionUnit splitPrefab;
        [SerializeField] private int splitCount = 2;
        [SerializeField] private float splitSpread = 0.5f;

        [Header("Crowd (รุมตี / ไม่ดันกัน)")]
        [Tooltip("เดินทะลุศัตรูและมินเนียนด้วยกันได้ (ไม่ดันกันทางฟิสิกส์) - ตอนรุมจะยืนเป็นวงรอบเป้าแทน")]
        [SerializeField] private bool passThroughUnits = true;
        [Tooltip("รุมตีได้ แต่ถ้ามีศัตรูตัวอื่นที่ยังไม่มีใครขวาง เข้ามาในวงมองเห็น จะแบ่งตัวไปขวางมัน")]
        [SerializeField] private bool splitToBlockFreeEnemies = true;
        [Tooltip("ระยะห่างของช่องยืนรอบเป้า (องศา) ตอนรุม - ยิ่งมากยิ่งยืนห่างกัน")]
        [Range(20f, 120f)]
        [SerializeField] private float surroundSpacing = 55f;
        [Tooltip("แรงแยกตัวจากมินเนียนข้างๆ ตอนเดิน (ไม่ให้ซ้อนกันเป็นก้อน) 0 = ปิด")]
        [SerializeField] private float separationStrength = 1f;

        private float _baseMaxHealth;
        private Vector2 _holdPosition;
        private float _formationAngle;
        private MinionCommand _command = MinionCommand.SelfDefense;
        private Vector2 _moveTarget;
        public float DetectionRange => stats != null ? stats.aggroRange : aggroRange;
        public MinionStats Stats => stats;
        public MinionUnit SplitPrefab => splitPrefab;
        public int SplitCount => splitCount;
        public float SplitSpread => splitSpread;
        /// <summary>เป็นตัวที่แตกออกมาจากกลุ่มไหน (null = ตัวปกติ)</summary>
        public MinionSplitGroup SplitGroup { get; set; }

        /// <summary>Prefab ต้นแบบ (ใช้นับจำนวนกองทัพ และส่งกลับเข้ากระเป๋าตอนตาย)</summary>
        public MinionUnit SourcePrefab { get; set; }

        public override Team Team => Team.Player;
        public string DisplayName => stats != null ? stats.displayName : displayName;
        public int SoulCost => stats != null ? stats.soulCost : soulCost;
        public Sprite Icon => icon != null ? icon : (bodyRenderer != null ? bodyRenderer.sprite : null);
        public MinionCommand Command => _command;

        protected override void ApplyStats()
        {
            if (stats == null) return;
            ApplyUnitStats(stats);
            displayName = stats.displayName;
            soulCost = stats.soulCost;
            aggroRange = stats.aggroRange;
            chaseLeash = stats.chaseLeash;
            guardRadius = stats.guardRadius;
            followDistance = stats.followDistance;
        }

        protected override void Awake()
        {
            base.Awake();
            _baseMaxHealth = maxHealth;
            _holdPosition = Position;
            _formationAngle = Random.Range(0f, 360f);
            if (selectionRing != null) selectionRing.SetActive(false);
            if (detectionRing != null) detectionRing.gameObject.SetActive(false);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Registry.Minions.Add(this);
            if (passThroughUnits)
            {
                for (int i = 0; i < Registry.Enemies.Count; i++) IgnoreCollisions(this, Registry.Enemies[i]);
                for (int i = 0; i < Registry.Minions.Count; i++) IgnoreCollisions(this, Registry.Minions[i]);
            }
        }

        // ------------------------------------------------------------------
        // รุมตี / แบ่งตัวไปขวาง
        // ------------------------------------------------------------------

        protected override void OnTargetChanged(Unit oldTarget, Unit newTarget)
        {
            if (oldTarget is EnemyUnit o) o.Attackers.Remove(this);
            // ตัวยิงไกลไม่นับเป็นตัวขวาง (ศัตรูที่โดนยิงอย่างเดียว ยังถือว่าไม่มีใครขวาง)
            if (attackStyle != AttackStyle.Ranged && newTarget is EnemyUnit n && !n.Attackers.Contains(this)) n.Attackers.Add(this);
        }

        /// <summary>
        /// เลือกศัตรูในวง: ตัวที่ยังไม่มีใครขวางก่อน (แบ่งกันไปขวาง) / ถ้าทุกตัวมีคนขวางแล้ว -> ไปรุมตัวที่ใกล้สุด
        /// กำลังตีตัวไหนอยู่ก็ตีต่อ ยกเว้นตัวนั้นมีเพื่อนรุมอยู่แล้ว และมีศัตรูที่ยังไม่มีใครขวางโผล่มา -> แยกไปขวางตัวนั้น
        /// </summary>
        private Unit PickEnemy(Vector2 center, float range, float leashFromHold)
        {
            EnemyUnit current = currentTarget as EnemyUnit;
            bool currentOk = current != null && Registry.IsValidHostile(current, Team, canAttackFlying)
                             && Vector2.Distance(center, current.Position) - current.BodyRadius <= range + 0.5f
                             && (leashFromHold <= 0f || Vector2.Distance(_holdPosition, current.Position) <= aggroRange + leashFromHold);

            EnemyUnit nearest = null, nearestFree = null;
            float dN = float.MaxValue, dF = float.MaxValue;
            for (int i = 0; i < Registry.Enemies.Count; i++)
            {
                EnemyUnit e = Registry.Enemies[i];
                if (!Registry.IsValidHostile(e, Team, canAttackFlying)) continue;
                float dc = Vector2.Distance(center, e.Position) - e.BodyRadius;
                if (dc > range) continue;
                if (leashFromHold > 0f && Vector2.Distance(_holdPosition, e.Position) > aggroRange + leashFromHold) continue;
                float d = Vector2.Distance(Position, e.Position);
                if (d < dN) { dN = d; nearest = e; }
                bool free = e.Attackers.Count == 0 || (e.Attackers.Count == 1 && e.Attackers[0] == this);
                if (free && d < dF) { dF = d; nearestFree = e; }
            }

            if (currentOk)
            {
                // มีเพื่อนรุมตัวนี้อยู่แล้ว + มีศัตรูที่ยังไม่มีใครขวาง -> แบ่งตัวไปขวาง (ทีละตัว เพราะพอไปแล้วตัวนั้นก็ "มีคนขวาง")
                if (splitToBlockFreeEnemies && nearestFree != null && nearestFree != current && current.Attackers.Count > 1)
                    return nearestFree;
                return current;
            }
            if (splitToBlockFreeEnemies && nearestFree != null) return nearestFree;
            return nearest;
        }

        /// <summary>ช่องยืนรอบเป้าตอนรุม: ตัวแรกยืนหน้า ตัวต่อไปสลับซ้าย-ขวา เป็นวงรอบเป้า</summary>
        protected override Vector2? GetAttackSlot(Unit target)
        {
            if (attackStyle == AttackStyle.Ranged) return null;
            if (!(target is EnemyUnit e)) return null;
            int idx = e.Attackers.IndexOf(this);
            if (idx <= 0) return null; // ตัวแรก = ขวางหน้าตรงๆ
            MinionUnit first = e.Attackers[0];
            Vector2 baseDir = first != null ? first.Position - e.Position : Position - e.Position;
            if (baseDir.sqrMagnitude < 0.0001f) baseDir = Vector2.right;
            baseDir.Normalize();

            int step = (idx + 1) / 2;
            float ang = (idx % 2 == 1 ? 1f : -1f) * step * surroundSpacing;
            float ring = 0f;
            while (Mathf.Abs(ang) > 180f) { ang -= Mathf.Sign(ang) * 360f; ring += 0.35f; } // วงเต็ม -> วงนอก
            float rad = ang * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(baseDir.x * Mathf.Cos(rad) - baseDir.y * Mathf.Sin(rad), baseDir.x * Mathf.Sin(rad) + baseDir.y * Mathf.Cos(rad));
            float r = e.BodyRadius + bodyRadius + Mathf.Max(0.05f, attackRange * 0.5f) + ring;
            return e.Position + dir * r;
        }

        /// <summary>แยกตัวจากมินเนียนข้างๆ ตอนเดิน (เดินทะลุกันได้แล้ว เลยใช้แรงนี้กันซ้อนเป็นก้อน)</summary>
        protected override Vector2 GetSteeringOffset(Vector2 moveDir)
        {
            if (separationStrength <= 0f) return Vector2.zero;
            Vector2 push = Vector2.zero;
            for (int i = 0; i < Registry.Minions.Count; i++)
            {
                MinionUnit other = Registry.Minions[i];
                if (other == null || other == this || other.isFlying != isFlying) continue;
                Vector2 away = Position - other.Position;
                float minDist = (bodyRadius + other.BodyRadius) * 1.05f;
                float d = away.magnitude;
                if (d >= minDist) continue;
                away = d < 0.001f ? new Vector2(-moveDir.y, moveDir.x) * (i % 2 == 0 ? 1f : -1f) : away / d;
                push += away * (1f - d / minDist);
            }
            if (push == Vector2.zero) return Vector2.zero;
            return Vector2.ClampMagnitude(push * separationStrength * CurrentMoveSpeed, CurrentMoveSpeed * 0.6f);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Registry.Minions.Remove(this);
        }

        /// <summary>เรียกจาก MinionManager ทุกครั้งที่ตัวคูณอัปเกรดเปลี่ยน</summary>
        public void ApplyMultipliers(float healthMult, float damageMult, float speedMult)
        {
            float ratio = maxHealth > 0f ? currentHealth / maxHealth : 1f;
            maxHealth = _baseMaxHealth * healthMult;
            currentHealth = maxHealth * ratio;
            RefreshHealthBar();
            damageMultiplier = damageMult;
            moveSpeedMultiplier = speedMult;
            attackSpeedMultiplier = speedMult;
        }

        /// <summary>สั่งมินเนียนตัวนี้ (Self-Defense = ยืนเฝ้าจุดที่อยู่ตอนนี้)</summary>
        public void SetCommand(MinionCommand command)
        {
            // เหลือ 3 คำสั่ง: Hold Area (= SelfDefense) / Move / Follow  -> คำสั่งเก่าแปลงเป็น Hold Area
            if (command == MinionCommand.GuardBase || command == MinionCommand.AttackNearest) command = MinionCommand.SelfDefense;
            _command = command;
            if (command == MinionCommand.SelfDefense || command == MinionCommand.AttackNearest) _holdPosition = Position;
        }

        /// <summary>คำสั่ง Move: เดินไปจุดนี้ (ไม่สนศัตรูระหว่างทาง) ถึงแล้วยืนเฝ้าตรงนั้น</summary>
        public void MoveTo(Vector2 destination)
        {
            _command = MinionCommand.MoveTo;
            _moveTarget = destination;
        }

        public void SetSelected(bool selected)
        {
            if (selectionRing != null) selectionRing.SetActive(selected);
            if (detectionRing != null)
            {
                detectionRing.gameObject.SetActive(selected);
                detectionRing.localScale = Vector3.one * aggroRange * 2f;
            }
        }

        /// <summary>ศัตรูในวงมองเห็น (และไม่เกินระยะ leash จากจุดยืน) - แบ่งกันขวาง / รุมตามกติกาใน PickEnemy</summary>
        private Unit FindInSight(Vector2 center) => PickEnemy(center, aggroRange, chaseLeash);

        protected override Unit ChooseTarget()
        {
            switch (_command)
            {
                case MinionCommand.GuardBase:
                {
                    Crystal c = Registry.Crystal;
                    if (c == null) return Registry.FindNearestHostile(Position, Team, aggroRange, canAttackFlying);
                    // ศัตรูที่เข้ามาในเขตป้องกันรอบคริสตัล -> เลือกตัวที่ใกล้เราสุด
                    Unit best = null;
                    float bestDist = float.MaxValue;
                    for (int i = 0; i < Registry.Enemies.Count; i++)
                    {
                        EnemyUnit e = Registry.Enemies[i];
                        if (!Registry.IsValidHostile(e, Team, canAttackFlying)) continue;
                        if (Vector2.Distance(e.Position, c.Position) > guardRadius) continue;
                        float d = Vector2.Distance(Position, e.Position);
                        if (d < bestDist) { bestDist = d; best = e; }
                    }
                    return best;
                }

                case MinionCommand.FollowPlayer:
                {
                    PlayerNecromancer p = Registry.Player;
                    Vector2 center = p != null ? p.Position : Position;
                    return PickEnemy(center, aggroRange, 0f);
                }

                case MinionCommand.AttackNearest:
                    // ตีศัตรูที่เข้ามาในวงมองเห็นเท่านั้น
                    return FindInSight(Position);

                case MinionCommand.MoveTo:
                    // กำลังเดินไปจุดหมาย: ไม่สนศัตรู
                    if (Vector2.Distance(Position, _moveTarget) <= moveArriveDistance)
                    {
                        SetCommand(MinionCommand.SelfDefense);
                        return FindInSight(Position);
                    }
                    return null;

                default: // SelfDefense
                    return FindInSight(Position);
            }
        }

        protected override Vector2? GetIdleDestination()
        {
            Vector2 offset = new Vector2(Mathf.Cos(_formationAngle * Mathf.Deg2Rad), Mathf.Sin(_formationAngle * Mathf.Deg2Rad));
            switch (_command)
            {
                case MinionCommand.GuardBase:
                    Crystal c = Registry.Crystal;
                    if (c == null) return null;
                    return c.Position + offset * (c.BodyRadius + guardStandDistance);

                case MinionCommand.FollowPlayer:
                    PlayerNecromancer p = Registry.Player;
                    if (p == null) return null;
                    return p.Position + offset * followDistance;

                case MinionCommand.AttackNearest:
                    // ไม่มีศัตรูในวง -> ยืนรอตรงที่อยู่ตอนนี้
                    _holdPosition = Position;
                    return null;

                case MinionCommand.MoveTo:
                    return _moveTarget;

                default:
                    return _holdPosition;
            }
        }

        protected override string AttackSoundId
        {
            get
            {
                string k = SoundKey;
                if (k == "MiniSlime") k = "Slime";
                return string.IsNullOrEmpty(k) ? null : "SFX_Minion_Attack_" + k;
            }
        }

        protected override void OnDeath()
        {
            StopMoving();
            Sfx.At("SFX_Minion_Death", Position, 0.8f);
            // วิญญาณกลับเข้ากระเป๋า (นับตอนบินถึง)
            if (GameManager.Instance != null) GameManager.Instance.Minions.OnMinionDied(this);
            Destroy(gameObject);
        }

        /// <summary>วาดวงระยะมองหาศัตรูใน Scene View ตลอด (สีฟ้า)</summary>
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, aggroRange);
            if (_command == MinionCommand.MoveTo && Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _moveTarget);
            }
        }
    }
}
