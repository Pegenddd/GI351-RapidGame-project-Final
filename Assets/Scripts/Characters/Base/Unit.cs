using System;
using UnityEngine;
using NecroDefense.Core;

namespace NecroDefense.Characters.Base
{
    /// <summary>
    /// Abstract base class for all combatants (Player, Enemies, Minions).
    /// Demonstrates:
    /// 1. Inheritance (inherits from Entity)
    /// 2. Abstraction & Interface Realization (IDamageable, ITargetable, IAttacker)
    /// 3. Encapsulation (protected fields with public read-only properties)
    /// 4. Polymorphism (virtual methods for Die, TakeDamage, Attack, Move)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public abstract class Unit : Entity, IDamageable, ITargetable, IAttacker
    {
        [Header("Unit Attributes (Editable in Inspector)")]
        [Tooltip("Maximum health pool of this unit")]
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("Movement speed in units per second")]
        [SerializeField] private float moveSpeed = 3f;
        [Tooltip("Base damage dealt by this unit's attacks")]
        [SerializeField] private float attackPower = 10f;
        [Tooltip("Range within which this unit initiates an attack")]
        [SerializeField] private float attackRange = 1.2f;
        [Tooltip("Cooldown interval between consecutive attacks (in seconds)")]
        [SerializeField] private float attackInterval = 1f;
        [Tooltip("Allegiance of this unit (Player or Enemy)")]
        [SerializeField] private Faction team = Faction.Enemy;

        [Header("Visual Feedback (Editable)")]
        [Tooltip("Flash color/alpha when taking damage")]
        [SerializeField] private Color damageFlashColor = new Color(1f, 1f, 1f, 0.2f);
        [Tooltip("Duration of damage flash in seconds")]
        [SerializeField] private float damageFlashDuration = 0.08f;

        protected Color _baseColor = Color.white;
        private Coroutine _flashCoroutine;

        // Encapsulated state fields
        private float _currentHealth;
        protected float _lastAttackTime = -999f;
        protected Rigidbody2D _rb;
        protected Collider2D _col;
        protected SpriteRenderer _spriteRenderer;

        // --- IDamageable Implementation ---
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => _currentHealth > 0f;

        public event Action<DamageInfo> OnDamaged;
        public event Action OnDied;
        public event Action<float, float> OnHealthChanged;

        // --- ITargetable Implementation ---
        public Transform TargetTransform => CachedTransform;
        public Faction Team => team;
        public virtual bool IsValidTarget => IsAlive && gameObject.activeInHierarchy;

        // --- IAttacker Implementation ---
        public float AttackPower => attackPower;
        public float AttackRange => attackRange;
        public float AttackInterval => attackInterval;
        public bool CanAttack => Time.time >= _lastAttackTime + attackInterval;

        // --- Properties to adjust stats cleanly (Encapsulation) ---
        public float MoveSpeed
        {
            get => moveSpeed;
            protected set => moveSpeed = Mathf.Max(0f, value);
        }

        protected override void Awake()
        {
            base.Awake();
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Configure 2D physics defaults
            if (_rb != null)
            {
                _rb.gravityScale = 0f;
                _rb.freezeRotation = true;
            }

            _currentHealth = maxHealth;
        }

        protected virtual void Start()
        {
            if (_spriteRenderer != null)
            {
                _baseColor = _spriteRenderer.color;
                if (_baseColor.a < 0.1f) _baseColor = Color.white;
            }

            // Enemies show floating 1-bit Health Bar for clear visibility and damage feedback
            if (team == Faction.Enemy && GetComponentInChildren<UI.UnitHealthBar2D>() == null)
            {
                var hbObj = new GameObject("HealthBar");
                hbObj.transform.SetParent(transform, false);
                hbObj.AddComponent<UI.UnitHealthBar2D>();
            }

            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// Polymorphic damage reception. Subclasses can override to apply damage reduction, shields, or counters.
        /// </summary>
        public virtual void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive) return;

            float actualDamage = damageInfo.Amount;
            _currentHealth = Mathf.Max(0f, _currentHealth - actualDamage);

            OnDamaged?.Invoke(damageInfo);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            // Apply knockback if any
            if (damageInfo.KnockbackForce > 0f && _rb != null)
            {
                _rb.AddForce(damageInfo.HitDirection.normalized * damageInfo.KnockbackForce, ForceMode2D.Impulse);
            }

            // Visual flash feedback
            TriggerDamageFlash();

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// Encapsulated healing method.
        /// </summary>
        public virtual void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        /// <summary>
        /// Polymorphic death handling. Overridden by Enemies to drop corpses,
        /// and by Player to trigger defeat.
        /// </summary>
        protected virtual void Die()
        {
            OnDied?.Invoke();
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Polymorphic attack method. Overridden by Melee vs Ranged implementations.
        /// </summary>
        public virtual void Attack(ITargetable target)
        {
            if (!CanAttack || target == null || !target.IsValidTarget) return;

            _lastAttackTime = Time.time;
            ExecuteAttack(target);
        }

        /// <summary>
        /// Template method pattern: concrete subclasses implement the specific offensive action.
        /// </summary>
        protected abstract void ExecuteAttack(ITargetable target);

        /// <summary>
        /// Movement logic using Rigidbody2D velocity.
        /// </summary>
        public virtual void MoveTowards(Vector2 targetPosition)
        {
            if (_rb == null || !IsAlive) return;

            Vector2 currentPos = _rb.position;
            Vector2 direction = (targetPosition - currentPos).normalized;
            _rb.linearVelocity = direction * moveSpeed;

            // Flip sprite based on direction
            if (_spriteRenderer != null && direction.x != 0)
            {
                _spriteRenderer.flipX = direction.x < 0;
            }
        }

        public virtual void StopMovement()
        {
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
            }
        }

        protected virtual void TriggerDamageFlash()
        {
            if (_spriteRenderer != null && gameObject.activeInHierarchy)
            {
                if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
                _flashCoroutine = StartCoroutine(DamageFlashCoroutine());
            }
        }

        private System.Collections.IEnumerator DamageFlashCoroutine()
        {
            _spriteRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(damageFlashDuration);
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
            }
            _flashCoroutine = null;
        }

        // Set faction programmatically (e.g., when converting corpse to ally)
        public void SetFaction(Faction newTeam)
        {
            team = newTeam;
        }

        public void SetMaxHealth(float newMax, bool resetCurrent = true)
        {
            maxHealth = newMax;
            if (resetCurrent) _currentHealth = maxHealth;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void SetAttackPower(float newPower) => attackPower = newPower;
        public void SetMoveSpeed(float newSpeed) => moveSpeed = Mathf.Max(0f, newSpeed);
        public void SetAttackRange(float newRange) => attackRange = Mathf.Max(0f, newRange);
        public void SetAttackInterval(float newInterval) => attackInterval = Mathf.Max(0.01f, newInterval);
    }
}

