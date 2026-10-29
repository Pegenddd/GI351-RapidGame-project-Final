using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;
using NecroDefense.Combat.Strategies;
using NecroDefense.Base;

namespace NecroDefense.Characters.Minions
{
    public enum MinionOrder
    {
        DefendBase,
        FollowNecromancer,
        FreeHunt
    }

    /// <summary>
    /// Abstract base class for all resurrected undead allies.
    /// Demonstrates:
    /// - Inheritance: Inherits from Unit.
    /// - Polymorphic Role & Attack Behaviors.
    /// - AI Strategy Switching: Changes behavior according to MinionOrder.
    /// </summary>
    public abstract class MinionUnit : Unit
    {
        [Header("Minion Directives")]
        [Tooltip("Default directive for this minion")]
        [SerializeField] private MinionOrder currentOrder = MinionOrder.DefendBase;
        [Tooltip("Radius around Citadel that this minion will patrol when defending")]
        [SerializeField] private float defendPerimeterRadius = 5f;
        [Tooltip("Max distance before returning to player when following")]
        [SerializeField] private float followLeashDistance = 3f;
        [Tooltip("Detection radius for hunting enemy monsters")]
        [SerializeField] private float enemyDetectionRadius = 12f;
        [Tooltip("Color tint applied to this resurrected minion (Pure White for 2-color palette)")]
        [SerializeField] private Color undeadColorTint = Color.white;

        protected ITargetingStrategy _targetingStrategy;
        protected ITargetable _currentTarget;
        protected BaseCrystal _baseCrystal;
        protected Player.PlayerNecromancer _player;

        public MinionOrder CurrentOrder => currentOrder;

        public void ConfigureAIPerimeter(float defendRadius, float followDistance, float detectRadius)
        {
            defendPerimeterRadius = defendRadius;
            followLeashDistance = followDistance;
            enemyDetectionRadius = detectRadius;
        }

        protected override void Awake()
        {
            base.Awake();
            SetFaction(Faction.Player);
            _targetingStrategy = new TargetNearestHostileStrategy();
        }

        protected override void Start()
        {
            base.Start();
            _baseCrystal = Object.FindAnyObjectByType<BaseCrystal>();
            _player = Object.FindAnyObjectByType<Player.PlayerNecromancer>();

            // Auto-assign 2-color palette sprite if prefab sprite is missing
            if (_spriteRenderer != null && _spriteRenderer.sprite == null)
            {
                // Derive enemy type from component type polymorphically
                EnemyType sourceType = EnemyType.Zombie;
                if (this is ZombieMinion)        sourceType = EnemyType.Zombie;
                else if (this is SkeletonArcherMinion) sourceType = EnemyType.SkeletonArcher;
                else if (this is GolemMinion)    sourceType = EnemyType.Golem;
                else if (this is GhostMinion)    sourceType = EnemyType.Ghost;

                _spriteRenderer.sprite = Utils.ProceduralVisuals.GetMinionSprite(sourceType);
                _spriteRenderer.color = Color.white;
            }

            ApplyUndeadAppearance();
        }

        protected virtual void ApplyUndeadAppearance()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = undeadColorTint;
            }
        }

        protected virtual void Update()
        {
            if (!IsAlive) return;

            UpdateMinionAI();
        }

        protected virtual void UpdateMinionAI()
        {
            // 1. Search for hostiles (Enemies)
            if (_currentTarget == null || !_currentTarget.IsValidTarget)
            {
                _currentTarget = _targetingStrategy.FindTarget(this, enemyDetectionRadius);
            }

            // 2. If hostile found, engage!
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
                return;
            }

            // 3. If idle, execute Order (Defend Base, Follow Player)
            HandleIdleOrder();
        }

        private void HandleIdleOrder()
        {
            switch (currentOrder)
            {
                case MinionOrder.DefendBase:
                    if (_baseCrystal != null)
                    {
                        float dist = Vector2.Distance(Position, _baseCrystal.Position);
                        if (dist > defendPerimeterRadius)
                        {
                            MoveTowards(_baseCrystal.Position);
                        }
                        else
                        {
                            StopMovement();
                        }
                    }
                    break;

                case MinionOrder.FollowNecromancer:
                    if (_player != null && _player.IsAlive)
                    {
                        float dist = Vector2.Distance(Position, _player.Position);
                        if (dist > followLeashDistance)
                        {
                            MoveTowards(_player.Position);
                        }
                        else
                        {
                            StopMovement();
                        }
                    }
                    break;

                case MinionOrder.FreeHunt:
                    StopMovement();
                    break;
            }
        }

        public void SetOrder(MinionOrder order)
        {
            currentOrder = order;
        }

        /// <summary>
        /// Polymorphic minion death: dissolves back into the ether.
        /// </summary>
        protected override void Die()
        {
            // Dissolve effect / dust
            base.Die();
            Destroy(gameObject);
        }
    }
}

