using UnityEngine;
using NecroDefense.Core;

namespace NecroDefense.Characters.Minions
{
    /// <summary>
    /// Resurrected Ghost: Spectral Phantom.
    /// Emits a frost/necrotic slow aura around itself, hindering oncoming enemy waves.
    /// Demonstrates Polymorphic Passive Aura & Siphon Execution.
    /// </summary>
    public class GhostMinion : MinionUnit
    {
        [Header("Spectral Aura Settings")]
        [Tooltip("Radius of chilling aura affecting nearby enemies")]
        [SerializeField] private float chillAuraRadius = 3.5f;
        [Tooltip("Interval in seconds between chilling aura damage ticks")]
        [SerializeField] private float chillDamageInterval = 1f;
        [Tooltip("Fraction of AttackPower dealt by the aura per tick (0.4 = 40%)")]
        [Range(0f, 1f)]
        [SerializeField] private float auraDamageMultiplier = 0.4f;

        private float _lastAuraTick;

        protected override void Update()
        {
            base.Update();
            if (!IsAlive) return;

            // Chilling aura tick
            if (Time.time >= _lastAuraTick + chillDamageInterval)
            {
                _lastAuraTick = Time.time;
                TickChillAura();
            }
        }

        private void TickChillAura()
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(Position, chillAuraRadius);
            foreach (var col in colliders)
            {
                if (col.TryGetComponent<IDamageable>(out var damageable) &&
                    col.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team == Faction.Enemy && targetable.IsValidTarget)
                    {
                        DamageInfo info = new DamageInfo(AttackPower * auraDamageMultiplier, DamageType.Necrotic, this);
                        damageable.TakeDamage(info);
                    }
                }
            }
        }

        protected override void ExecuteAttack(ITargetable target)
        {
            if (target is IDamageable damageable)
            {
                DamageInfo info = new DamageInfo(AttackPower, DamageType.Necrotic, this);
                damageable.TakeDamage(info);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.1f, 0.9f, 0.9f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, chillAuraRadius);
        }
    }
}

