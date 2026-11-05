using NecroDefense.Base;
using NecroDefense.Characters.Base;
using NecroDefense.Characters.Player;
using NecroDefense.Core;
using NecroDefense.Enemies;
using UnityEngine;

namespace NecroDefense.Characters.Enemies
{
    [RequireComponent(typeof(EnemyPathController))]
    public abstract class EnemyUnit : Unit
    {
        [Header("Enemy Classification & Loot (Editable in Inspector)")]
        [Tooltip("Classification type of this enemy")]
        [SerializeField] private EnemyType enemyType = EnemyType.Zombie;
        [Tooltip("Amount of soul essence granted to player upon defeat")]
        [SerializeField] private int soulYield = 2;
        [Tooltip("Amount of souls required to resurrect this corpse into a minion")]
        [SerializeField] private int resurrectionCost = 20;
        [Tooltip("Corpse prefab spawned upon death (leave empty for procedural fallback)")]
        [SerializeField] private GameObject corpsePrefab;

        protected ITargetable _currentTarget;
        protected EnemyPathController _pathController;

        public EnemyType EnemyType => enemyType;
        public int SoulYield => soulYield;
        public int ResurrectionCost => resurrectionCost;

        public void SetSoulParameters(int yield, int cost)
        {
            soulYield = Mathf.Max(0, yield);
            resurrectionCost = Mathf.Max(0, cost);
        }

        protected override void Awake()
        {
            base.Awake();
            SetFaction(Faction.Enemy);

            _pathController = GetComponent<EnemyPathController>();

            if (_spriteRenderer != null && _spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = Utils.ProceduralVisuals.GetEnemySprite(enemyType);
                _spriteRenderer.color = Color.black;
            }
        }

        protected virtual void Update()
        {
            if (!IsAlive) return;
            UpdateAI();
        }

        protected virtual void UpdateAI()
        {
            // เช็กก่อนว่ายังอยู่ในช่วงเดินตาม Waypoint ของทางเดิน Tower Defense หรือไม่
            if (_pathController != null && !_pathController.IsInArena)
            {
                // ถ้ายังอยู่ในทางเดิน ให้บังคับเดินตามทางไปเรื่อยๆ จนกว่าจะพ้นจุดสุดท้าย
                bool reachedEnd;
                Vector2 nextTargetPos = _pathController.GetNextMovementTarget(Position, MoveSpeed, out reachedEnd);
                MoveTowards(nextTargetPos);
                return; // จบการทำงานรอบนี้ ไม่ไปเช็กตีผู้เล่น/ป้อมกลางทาง
            }

            // === ถ้าพ้นทางเดินแล้ว (เข้าสู่ลานกว้าง / Arena โหมดอิสระ) ถึงจะเริ่มหาเป้าหมายและโจมตี ===

            // ถ้ายังไม่มีเป้าหมาย หรือเป้าหมายเดิมตาย/ใช้การไม่ได้แล้ว ให้สุ่มหาใหม่
            if (_currentTarget == null || !_currentTarget.IsValidTarget)
            {
                _currentTarget = FindRandomTarget(30f);
            }

            if (_currentTarget != null && _currentTarget.IsValidTarget)
            {
                float distance = float.MaxValue;
                if (_col != null && _currentTarget.TargetTransform.TryGetComponent<Collider2D>(out var targetCol))
                {
                    ColliderDistance2D colDist = _col.Distance(targetCol);
                    distance = colDist.isOverlapped ? 0f : colDist.distance;
                }
                else
                {
                    distance = Vector2.Distance(Position, _currentTarget.Position);
                }

                if (distance <= AttackRange)
                {
                    StopMovement();
                    Attack(_currentTarget);
                }
                else
                {
                    MoveTowards(_currentTarget.Position);
                }
            }
            else
            {
                // ถ้าอยู่ในลานกว้างแต่ไม่มีเป้าหมาย ให้เดินไปยังตำแหน่งสุ่มในลานกว้างต่อ
                bool reachedEnd;
                Vector2 roamTarget = _pathController.GetNextMovementTarget(Position, MoveSpeed, out reachedEnd);
                MoveTowards(roamTarget);
            }
        }

        /// <summary>
        /// ค้นหาและสุ่มเลือกระหว่าง Player (ผู้เล่น) หรือ BaseCrystal (ฐานหลัก) ในระยะที่กำหนด
        /// </summary>
        protected virtual ITargetable FindRandomTarget(float maxRange)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(Position, maxRange);
            System.Collections.Generic.List<ITargetable> validTargets = new System.Collections.Generic.List<ITargetable>();

            foreach (var hit in hits)
            {
                if (!hit.TryGetComponent<ITargetable>(out var targetable)) continue;

                // เช็กว่าเป็นฝ่ายผู้เล่นและใช้งานได้
                if (targetable.Team == Faction.Player && targetable.IsValidTarget)
                {
                    // กรองเอาเฉพาะ Player หรือ BaseCrystal เท่านั้น
                    if (targetable is PlayerNecromancer || targetable is BaseCrystal)
                    {
                        validTargets.Add(targetable);
                    }
                }
            }

            // ถ้าเจอเป้าหมาย ให้สุ่มเลือกมา 1 ตัว
            if (validTargets.Count > 0)
            {
                int randomIndex = Random.Range(0, validTargets.Count);
                return validTargets[randomIndex];
            }

            return null;
        }

        protected override void Die()
        {
            SpawnCorpse();
            SpawnExpOrb();
            AwardSoulsToPlayer();
            base.Die();
        }

        protected virtual void SpawnCorpse()
        {
            GameObject corpseObj;
            if (corpsePrefab != null)
            {
                corpseObj = Instantiate(corpsePrefab, Position, Quaternion.identity);
            }
            else
            {
                corpseObj = Utils.ProceduralVisuals.CreateCorpseGameObject(enemyType, resurrectionCost);
                corpseObj.transform.position = Position;
            }

            if (corpseObj.TryGetComponent<Corpse>(out var corpse))
            {
                corpse.Initialize(enemyType, resurrectionCost, _spriteRenderer != null ? _spriteRenderer.sprite : null, Color.black);
            }
        }

        protected virtual void SpawnExpOrb()
        {
            GameObject orb = new GameObject($"ExpOrb_{enemyType}");
            orb.transform.position = Position;
            var expComp = orb.AddComponent<Skills.CombatEntities.ExperienceOrb>();
            int expAmount = (enemyType == EnemyType.Zombie) ? 1 : 2;
            expComp.Initialize(expAmount);
        }

        private void AwardSoulsToPlayer()
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerNecromancer>();
            if (player != null && player.IsAlive)
            {
                player.AddSouls(soulYield);
            }
        }
    }
}