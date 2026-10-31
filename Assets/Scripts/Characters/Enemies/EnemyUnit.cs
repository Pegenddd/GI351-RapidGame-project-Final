using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;
using NecroDefense.Combat.Strategies;

namespace NecroDefense.Characters.Enemies
{
    /// <summary>
    /// Abstract base class for all enemy monsters.
    /// Demonstrates:
    /// - Inheritance: Inherits from Unit.
    /// - Abstraction & Strategy Pattern: Uses ITargetingStrategy for dynamic target selection.
    /// - Polymorphic Death: Overrides Die() to spawn a Corpse and grant souls.
    /// </summary>
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

        protected ITargetingStrategy _targetingStrategy;
        protected ITargetable _currentTarget;

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
            _targetingStrategy = new TargetBasePriorityStrategy();

            if (_spriteRenderer != null && _spriteRenderer.sprite == null)
            {
                _spriteRenderer.sprite = Utils.ProceduralVisuals.GetEnemySprite(enemyType);
                _spriteRenderer.color = Color.gray;
            }
        }

        protected virtual void Update()
        {
            if (!IsAlive) return;
            UpdateAI();
        }

        protected virtual void UpdateAI()
        {
            if (_currentTarget == null || !_currentTarget.IsValidTarget)
            {
                _currentTarget = _targetingStrategy.FindTarget(this, 30f);
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
                StopMovement();
            }
        }

        protected override void Die()
        {
            SpawnCorpse();
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
                // ส่งค่า resurrectionCost และสีเทา (Color.gray) ไปยังซากศพ
                corpse.Initialize(enemyType, resurrectionCost, _spriteRenderer != null ? _spriteRenderer.sprite : null, Color.gray);
            }
        }

        private void AwardSoulsToPlayer()
        {
            var player = Object.FindAnyObjectByType<Player.PlayerNecromancer>();
            if (player != null && player.IsAlive)
            {
                player.AddSouls(soulYield);
            }
        }
    }
}