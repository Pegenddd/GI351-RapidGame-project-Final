using UnityEngine;
using NecroDefense.Core;

namespace NecroDefense.Characters.Enemies
{
    /// <summary>
    /// Ghost Enemy: Fast, spectral phantasm.
    /// Demonstrates:
    /// - Polymorphic Evasion: Chance to phase through attacks.
    /// - Polymorphic Attack: Life leeching touch.
    /// </summary>
    public class GhostEnemy : EnemyUnit
    {
        [Header("Ghost Phasing")]
        [Tooltip("Probability (0-1) of phasing through an incoming attack entirely")]
        [Range(0f, 1f)]
        [SerializeField] private float evasionChance = 0.25f;

        [Header("Ghost Life Steal")]
        [Tooltip("Fraction of attack damage restored as HP on each hit (0=none, 1=full leech)")]
        [Range(0f, 1f)]
        [SerializeField] private float lifeStealRatio = 0.5f;

        public override void TakeDamage(DamageInfo damageInfo)
        {
            // Polymorphic evasion: Chance to completely ignore damage by phasing
            if (Random.value < evasionChance)
            {
                // Phased through attack
                return;
            }

            base.TakeDamage(damageInfo);
        }

        protected override void ExecuteAttack(ITargetable target)
        {
            if (target is IDamageable damageable)
            {
                DamageInfo info = new DamageInfo(AttackPower, DamageType.Necrotic, this, Vector2.zero, 0f);
                damageable.TakeDamage(info);

                // Life steal
                Heal(AttackPower * lifeStealRatio);
            }
        }
    }
}

