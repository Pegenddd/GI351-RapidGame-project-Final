using System;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;

namespace NecroDefense.Base
{
    /// <summary>
    /// The Tower Defense objective: The Necromancer's Dark Citadel / Soul Core.
    /// Implements IDamageable and ITargetable.
    /// Demonstrates OOP Abstraction and Interface Realization.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BaseCrystal : Entity, IDamageable, ITargetable
    {
        [Header("Citadel Core Settings (Editable in Inspector)")]
        [Tooltip("Maximum hit points of the Citadel")]
        [SerializeField] private float maxHealth = 1000f;
        [Tooltip("Seconds between defensive healing pulses to allied minions")]
        [SerializeField] private float pulseHealInterval = 5f;
        [Tooltip("Amount of HP restored to allied minions per pulse")]
        [SerializeField] private float pulseHealAmount = 5f;
        [Tooltip("Radius of Citadel defensive aura affecting minions")]
        [SerializeField] private float auraRadius = 6f;

        [Header("Visual Feedback (Editable in Inspector)")]
        [Tooltip("Flash color when Citadel takes damage")]
        [SerializeField] private Color damageFlashColor = new Color(1f, 1f, 1f, 0.2f);
        [Tooltip("Duration of damage flash in seconds")]
        [SerializeField] private float damageFlashDuration = 0.08f;

        private float _currentHealth;
        private float _lastHealPulse;
        private SpriteRenderer _spriteRenderer;

        // IDamageable implementation
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsAlive => _currentHealth > 0f;

        public event Action<DamageInfo> OnDamaged;
        public event Action OnDied;
        public event Action<float, float> OnHealthChanged;

        // ITargetable implementation
        public Transform TargetTransform => CachedTransform;
        public Faction Team => Faction.Player;
        public bool IsValidTarget => IsAlive;

        public float AuraRadius => auraRadius;

        protected override void Awake()
        {
            base.Awake();
            _currentHealth = maxHealth;
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        private void Start()
        {
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        private void Update()
        {
            // Defensive Aura: Periodically heals nearby resurrected minions
            if (Time.time >= _lastHealPulse + pulseHealInterval)
            {
                _lastHealPulse = Time.time;
                PulseRegeneration();
            }
        }

        public void TakeDamage(DamageInfo damageInfo)
        {
            if (!IsAlive) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - damageInfo.Amount);
            OnDamaged?.Invoke(damageInfo);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);

            if (_spriteRenderer != null)
            {
                StartCoroutine(DamageFlash());
            }

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        public void ConfigureStats(float hp, float healInterval, float healAmount, float radius)
        {
            maxHealth = hp;
            _currentHealth = maxHealth;
            pulseHealInterval = healInterval;
            pulseHealAmount = healAmount;
            auraRadius = radius;
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        }

        private void Die()
        {
            OnDied?.Invoke();
            // Trigger Citadel collapse
        }

        private void PulseRegeneration()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(Position, auraRadius);
            foreach (var col in colliders)
            {
                if (col.gameObject == gameObject) continue; // Only heal allied minions, not the Citadel core itself!

                if (col.TryGetComponent<IDamageable>(out var damageable) && 
                    col.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team == Faction.Player && damageable.IsAlive)
                    {
                        damageable.Heal(pulseHealAmount);
                    }
                }
            }
        }

        private System.Collections.IEnumerator DamageFlash()
        {
            if (_spriteRenderer == null) yield break;
            _spriteRenderer.color = damageFlashColor;
            yield return new WaitForSeconds(damageFlashDuration);
            if (_spriteRenderer != null) _spriteRenderer.color = Color.white;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, auraRadius);
        }
    }
}

