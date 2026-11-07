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
        [Header("Minion Info")]
        [SerializeField] private string displayName = "Minion";
        [Tooltip("รูปที่ใช้ใน UI (ว่าง = ใช้รูปของ Body)")]
        [SerializeField] private Sprite icon;
        [Tooltip("Mana ที่ใช้อัญเชิญ")]
        [SerializeField] private int manaCost = 10;
        [Tooltip("สังเวยให้คริสตัลแล้วฟื้น HP ฐานกี่ % (5 = 5%)")]
        [SerializeField] private float sacrificeHealPercent = 5f;

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

        private float _baseMaxHealth;
        private Vector2 _holdPosition;
        private float _formationAngle;
        private MinionCommand _command = MinionCommand.SelfDefense;
        private Vector2 _moveTarget;
        public float DetectionRange => aggroRange;
        /// <summary>Prefab ต้นแบบ (ใช้นับจำนวนกองทัพ และส่งกลับเข้ากระเป๋าตอนตาย)</summary>
        public MinionUnit SourcePrefab { get; set; }

        public override Team Team => Team.Player;
        public string DisplayName => displayName;
        public int ManaCost => manaCost;
        public Sprite Icon => icon != null ? icon : (bodyRenderer != null ? bodyRenderer.sprite : null);
        public MinionCommand Command => _command;
        public float SacrificeHealPercent => sacrificeHealPercent;

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

        /// <summary>ศัตรูใกล้สุดในวงมองเห็น (และไม่เกินระยะ leash จากจุดยืน)</summary>
        private Unit FindInSight(Vector2 center)
        {
            Unit u = Registry.FindNearestHostile(center, Team, aggroRange, canAttackFlying);
            if (u == null) return null;
            // กำลังไล่อยู่แล้วแต่ศัตรูหนีไกลเกินจุดยืน -> ปล่อย
            if (chaseLeash > 0f && Vector2.Distance(_holdPosition, u.Position) > aggroRange + chaseLeash) return null;
            return u;
        }

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
                    Unit u = Registry.FindNearestHostile(center, Team, aggroRange, canAttackFlying);
                    return u;
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

        protected override void OnDeath()
        {
            StopMoving();
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
