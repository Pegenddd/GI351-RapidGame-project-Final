using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ศัตรู 1 ตัว (Slime / Goblin / Vulture / Giant / Skeleton Archer ใช้สคริปต์นี้ตัวเดียว
    /// ต่างกันแค่ค่าใน Inspector ของ Prefab)
    /// </summary>
    public class EnemyUnit : CombatUnit
    {
        [Header("Enemy Info")]
        [SerializeField] private string displayName = "Enemy";

        [Header("Targeting (ติ๊กว่าให้ตีอะไรได้บ้าง)")]
        [Tooltip("ตี Necromancer ได้ไหม")]
        [SerializeField] private bool attackPlayer = true;
        [Tooltip("ตีมินเนียนได้ไหม")]
        [SerializeField] private bool attackMinions = true;
        [Tooltip("ตีป้อมหลักได้ไหม")]
        [SerializeField] private bool attackMainBase = true;
        [Tooltip("เป้าหมายหลัก: Nearest = ตัวใกล้สุด / Player / Minions / MainBase")]
        [SerializeField] private TargetPriority priority = TargetPriority.Nearest;
        [Tooltip("ระยะมองเห็นเป้า (Player / Minion)")]
        [SerializeField] private float aggroRange = 5f;
        [Tooltip("ระยะไล่เป้าหมายหลัก (Player / Minions) - ไกลกว่านี้จะไม่ไล่")]
        [SerializeField] private float priorityChaseRange = 9f;

        [Header("Lane Behaviour (บนทางมอน ก่อนเข้าพื้นที่อิสระ)")]
        [Tooltip("ระหว่างเดินบนทางมอน สนแค่มินเนียน (ไม่สนผู้เล่น/ป้อมหลัก) จนกว่าจะเข้าพื้นที่อิสระ")]
        [SerializeField] private bool onlyMinionsOnLane = true;
        [Tooltip("เดินไปยิงไปได้ (Skeleton Archer): ระหว่างเดินถ้ามีอะไรในระยะยิงจะยิงโดยไม่หยุดเดิน")]
        [SerializeField] private bool shootWhileWalking = false;

        [Header("Path (เดินตามทางมอน)")]
        [Tooltip("เดินตาม Waypoints ของทางที่เกิดก่อนเข้าตีฐาน (ตัวบินจะไม่สนทาง บินตรงเลย)")]
        [SerializeField] private bool followPath = true;
        [Tooltip("เข้าใกล้ Waypoint เท่านี้ถือว่าถึงแล้ว")]
        [SerializeField] private float waypointReachDistance = 0.5f;

        [Header("Crowd (กันศัตรูเดนติดกันเอง)")]
        [Tooltip("เดินทะลุศัตรูด้วยกันได้ (ไม่ชนกันทางฟิสิกส์ = ไม่ดันกันจนติด) แต่ยังชนผู้เล่น/มินเนียน/ป้อมตามปกติ")]
        [SerializeField] private bool passThroughOtherEnemies = true;
        [Tooltip("แรงแยกตัวจากศัตรูข้างๆ ไม่ให้ซ้อนทับกันเป็นก้อนเดียว (0 = ปิด)")]
        [SerializeField] private float separationStrength = 1.2f;
        [Tooltip("ถ้าเดินไม่เข้าใกล้ Waypoint เลยนานเท่านี้ (วินาที) ให้ข้ามไปจุดถัดไป")]
        [SerializeField] private float stuckSkipTime = 1.5f;

        [Header("Rewards")]
        [Tooltip("Souls ที่ได้เมื่อฆ่า")]
        [SerializeField] private int soulReward = 2;
        [Tooltip("XP ที่ดรอป (เป็นลูกแก้วให้เดินไปเก็บ)")]
        [SerializeField] private int xpReward = 1;
        [SerializeField] private XpOrb xpOrbPrefab;

        [Header("Raise Dead (ชุบชีวิต)")]
        [Tooltip("ซากศพที่ดรอปตอนตาย")]
        [SerializeField] private Corpse corpsePrefab;
        [Tooltip("ถ้าชุบสำเร็จจะได้มินเนียนตัวไหนเข้า Minion Bag")]
        [SerializeField] private MinionUnit minionPrefab;
        [Tooltip("โอกาสชุบชีวิตสำเร็จ (0.3 = 30%)")]
        [Range(0f, 1f)]
        [SerializeField] private float raiseChance = 0.3f;

        [Header("Split On Death (Slime)")]
        [Tooltip("ตายแล้วแตกเป็นตัวอะไร (ว่าง = ไม่แตก)")]
        [SerializeField] private EnemyUnit splitPrefab;
        [SerializeField] private int splitCount = 2;
        [SerializeField] private float splitSpread = 0.5f;

        private EnemyPath _path;
        private int _waypointIndex;

        public override Team Team => Team.Enemy;
        public string DisplayName => displayName;
        public MinionUnit MinionPrefab => minionPrefab;
        public Sprite Icon => bodyRenderer != null ? bodyRenderer.sprite : null;

        protected override void OnEnable()
        {
            base.OnEnable();
            Registry.Enemies.Add(this);
            if (passThroughOtherEnemies) IgnoreOtherEnemies();
        }

        /// <summary>ปิดการชนทางฟิสิกส์ระหว่างศัตรูตัวนี้กับศัตรูตัวอื่นทั้งหมด (ต้นเหตุที่ดันกันจนติด)</summary>
        private void IgnoreOtherEnemies()
        {
            Collider2D[] mine = GetComponentsInChildren<Collider2D>();
            if (mine.Length == 0) return;
            for (int i = 0; i < Registry.Enemies.Count; i++)
            {
                EnemyUnit other = Registry.Enemies[i];
                if (other == null || other == this) continue;
                Collider2D[] theirs = other.GetComponentsInChildren<Collider2D>();
                for (int a = 0; a < mine.Length; a++)
                    for (int b = 0; b < theirs.Length; b++)
                        if (!mine[a].isTrigger && !theirs[b].isTrigger) Physics2D.IgnoreCollision(mine[a], theirs[b], true);
            }
        }

        /// <summary>แยกตัวออกจากศัตรูที่ซ้อนอยู่ใกล้ๆ (ดันออกด้านข้างเป็นหลัก จะได้ไม่ไปขวางทางเดิน)</summary>
        protected override Vector2 GetSteeringOffset(Vector2 moveDir)
        {
            if (separationStrength <= 0f) return Vector2.zero;
            Vector2 push = Vector2.zero;
            for (int i = 0; i < Registry.Enemies.Count; i++)
            {
                EnemyUnit other = Registry.Enemies[i];
                if (other == null || other == this || other.isFlying != isFlying) continue;
                Vector2 away = Position - other.Position;
                float minDist = (bodyRadius + other.BodyRadius) * 1.1f;
                float d = away.magnitude;
                if (d >= minDist) continue;
                if (d < 0.001f) away = new Vector2(-moveDir.y, moveDir.x) * (Registry.Enemies.IndexOf(this) > i ? 1f : -1f);
                else away /= d;
                push += away * (1f - d / minDist);
            }
            if (push == Vector2.zero) return Vector2.zero;
            // ไม่ให้แรงแยกตัวดันถอยหลัง: ตัดส่วนที่สวนทางเดินออก
            float back = Vector2.Dot(push, moveDir);
            if (back < 0f) push -= moveDir * back;
            return Vector2.ClampMagnitude(push * separationStrength * CurrentMoveSpeed, CurrentMoveSpeed * 0.7f);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Registry.Enemies.Remove(this);
        }

        private float _healthScale = 1f, _damageScale = 1f;

        /// <summary>WaveManager เรียกตอนเกิด: ศัตรูแข็งขึ้นตามเวฟ (1.24 = +24%)</summary>
        public void ApplyWaveScaling(float healthMultiplier, float damageMultiplier)
        {
            _healthScale = healthMultiplier;
            _damageScale = damageMultiplier;
            SetMaxHealthAndFill(maxHealth * healthMultiplier);
            this.damageMultiplier = damageMultiplier;
        }

        /// <summary>WaveManager เรียกตอนเกิด: ให้เดินตามทางนี้</summary>
        public void SetPath(EnemyPath path)
        {
            _path = path;
            _waypointIndex = 0;
        }

        /// <summary>เกิดกลางทาง (ซีนเทส): เริ่มเดินจาก Waypoint ที่อยู่ข้างหน้าที่ใกล้สุด</summary>
        public void SetPathFromHere(EnemyPath path)
        {
            _path = path;
            _waypointIndex = 0;
            if (path == null || path.Count == 0) return;
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < path.Count; i++)
            {
                float d = Vector2.Distance(Position, path.GetPoint(i));
                if (d < bestD) { bestD = d; best = i; }
            }
            // ถ้าเลย waypoint ที่ใกล้สุดไปแล้ว ให้ไปจุดถัดไป
            if (best + 1 < path.Count &&
                Vector2.Distance(Position, path.GetPoint(best + 1)) < Vector2.Distance(path.GetPoint(best), path.GetPoint(best + 1)))
                best++;
            _waypointIndex = best;
        }

        private bool IsOnPath => followPath && !isFlying && _path != null && _waypointIndex < _path.Count;

        /// <summary>เข้าพื้นที่อิสระแล้วหรือยัง (เข้าแล้วถือว่าเข้าตลอด) - ตัวบินไม่สนทาง ถือว่าเข้าแล้ว</summary>
        public bool ReachedFreeZone
        {
            get
            {
                if (!_reachedFreeZone && (isFlying || FreeZone.Instance == null || FreeZone.Instance.Contains(Position)))
                    _reachedFreeZone = true;
                return _reachedFreeZone;
            }
        }
        private bool _reachedFreeZone;

        protected override Unit ChooseTarget()
        {
            AdvanceWaypoint();
            Crystal crystal = Registry.Crystal;
            Unit baseTarget = attackMainBase && crystal != null && crystal.IsAlive ? crystal : null;

            // ----- บนทางมอน: สนแค่มินเนียน -----
            if (onlyMinionsOnLane && !ReachedFreeZone)
                return attackMinions ? FindNearest(false, true, aggroRange) : null;

            // ----- เป้าหมายหลัก -----
            switch (priority)
            {
                case TargetPriority.Player:
                    if (attackPlayer)
                    {
                        Unit p = FindNearest(true, false, priorityChaseRange);
                        if (p != null) return p;
                    }
                    break;
                case TargetPriority.Minions:
                    if (attackMinions)
                    {
                        Unit m = FindNearest(false, true, priorityChaseRange);
                        if (m != null) return m;
                    }
                    break;
                case TargetPriority.MainBase:
                    // วิ่งไปป้อมหลัก แต่ถ้ามีอะไรขวางในระยะประชิดก็ตีก่อน
                    Unit blocker = FindNearest(attackPlayer, attackMinions, attackRange + 0.3f);
                    if (blocker != null) return blocker;
                    if (baseTarget != null) return IsOnPath ? null : baseTarget;
                    break;
            }

            // ----- รองลงมา: ตัวใกล้สุดในระยะมองเห็น -----
            Unit near = FindNearest(attackPlayer, attackMinions, aggroRange);
            if (near != null) return near;

            if (IsOnPath) return null;                  // เดินตามทางต่อ
            if (baseTarget != null) return baseTarget;  // ทางจบแล้ว -> ป้อมหลัก

            // ตีป้อมหลักไม่ได้ (เช่น Vulture) -> ไล่ตัวที่ใกล้สุดทั้งแมพ
            return FindNearest(attackPlayer, attackMinions, float.MaxValue);
        }

        private float _bestWaypointDist = float.MaxValue, _stuckTimer;

        private void AdvanceWaypoint()
        {
            if (!IsOnPath) return;
            Vector2 wp = _path.GetPoint(_waypointIndex);
            float d = Vector2.Distance(Position, wp);
            bool reached = d <= waypointReachDistance;

            // เลยจุดไปแล้ว (เช่น โดนเบียดออกข้างทาง): ใกล้จุดถัดไปมากกว่าระยะจากจุดนี้ไปจุดถัดไป
            if (!reached && _waypointIndex + 1 < _path.Count)
            {
                Vector2 next = _path.GetPoint(_waypointIndex + 1);
                if (Vector2.Distance(Position, next) < Vector2.Distance(wp, next) && d <= waypointReachDistance * 3f) reached = true;
            }

            // เดินไม่เข้าใกล้จุดเลยนานเกิน stuckSkipTime (ตอนที่ไม่ได้หยุดตีอะไรอยู่) -> ข้ามจุดนี้
            if (!reached && stuckSkipTime > 0f && currentTarget == null)
            {
                if (d < _bestWaypointDist - 0.05f) { _bestWaypointDist = d; _stuckTimer = 0f; }
                else if ((_stuckTimer += Time.deltaTime) >= stuckSkipTime) reached = true;
            }

            if (reached)
            {
                _waypointIndex++;
                _bestWaypointDist = float.MaxValue;
                _stuckTimer = 0f;
            }
        }

        protected override Vector2? GetIdleDestination()
        {
            if (IsOnPath) return _path.GetPoint(_waypointIndex);
            return null;
        }

        /// <summary>Skeleton Archer: เดินไปยิงไป (ยิงอะไรก็ได้ที่ติ๊กไว้ที่อยู่ในระยะยิง)</summary>
        protected override void TryAttackWhileMoving()
        {
            if (!shootWhileWalking || attackTimer > 0f) return;
            Unit t = FindNearest(attackPlayer, attackMinions, attackRange + bodyRadius);
            if (t == null && attackMainBase && Registry.Crystal != null && Registry.Crystal.IsAlive && InAttackRange(Registry.Crystal))
                t = Registry.Crystal;
            if (t != null) AttackIfReady(t);
        }

        private Unit FindNearest(bool players, bool minions, float range)
        {
            Unit best = null;
            float bestDist = range;

            if (players)
            {
                PlayerNecromancer player = Registry.Player;
                if (Registry.IsValidHostile(player, Team, canAttackFlying))
                {
                    float d = Vector2.Distance(Position, player.Position) - player.BodyRadius;
                    if (d <= bestDist) { bestDist = d; best = player; }
                }
            }
            if (minions)
            {
                for (int i = 0; i < Registry.Minions.Count; i++)
                {
                    MinionUnit m = Registry.Minions[i];
                    if (!Registry.IsValidHostile(m, Team, canAttackFlying)) continue;
                    float d = Vector2.Distance(Position, m.Position) - m.BodyRadius;
                    if (d <= bestDist) { bestDist = d; best = m; }
                }
            }
            return best;
        }

        protected override void OnDeath()
        {
            StopMoving();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.Economy.AddSouls(soulReward);

                // XP orb
                if (xpOrbPrefab != null && xpReward > 0)
                {
                    XpOrb orb = Instantiate(xpOrbPrefab, Position, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Pickups));
                    orb.SetValue(xpReward);
                }

                // Soul Capture (Magic Orb Tower Tier 3): โดนป้อมฆ่า -> มีโอกาสเข้ากระเป๋าทันที
                bool captured = false;
                Tower killer = LastTowerAttacker;
                if (killer != null && minionPrefab != null && GameManager.Instance.Minions.HasRoom(minionPrefab)
                    && Random.value < killer.SoulCaptureChance)
                {
                    GameManager.Instance.Minions.RaiseIntoBag(minionPrefab, Position);
                    captured = true;
                }

                // ซากศพสำหรับ Raise Dead
                if (!captured && corpsePrefab != null && minionPrefab != null)
                {
                    Corpse c = Instantiate(corpsePrefab, Position, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Pickups));
                    c.Setup(displayName, minionPrefab, raiseChance);
                    // ศพหน้าตาเหมือนศัตรูตัวนั้น (ล้มนอน + สีเทา)
                    if (bodyRenderer != null)
                        c.SetLook(bodyRenderer.sprite, bodyRenderer.transform.lossyScale, visualRoot != null && visualRoot.localScale.x < 0f);
                }

                // แตกตัว (Slime)
                if (splitPrefab != null)
                {
                    for (int i = 0; i < splitCount; i++)
                    {
                        Vector2 offset = Random.insideUnitCircle * splitSpread;
                        EnemyUnit child = Instantiate(splitPrefab, Position + offset, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Enemies));
                        child.ApplyWaveScaling(_healthScale, _damageScale); // ลูกแข็งเท่าเวฟของแม่
                        child.SetPath(_path);
                        child._waypointIndex = _waypointIndex;
                    }
                }
            }

            Destroy(gameObject);
        }

        protected override void OnDrawGizmosSelected()
        {
            base.OnDrawGizmosSelected();
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aggroRange);
        }
    }
}
