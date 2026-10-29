using UnityEngine;
using NecroDefense.Core;

namespace NecroDefense.Characters.Enemies
{
    /// <summary>
    /// Zombie Enemy: Standard melee swarm unit.
    /// Demonstrates Inheritance and Polymorphic Melee Execution.
    /// </summary>
    public class ZombieEnemy : EnemyUnit
    {
        [Header("Zombie Attributes")]
        [Tooltip("Multiplier applied to base attack power for each bite (1.0 = 100%)")]
        [Range(0.5f, 3f)]
        [SerializeField] private float biteDamageMultiplier = 1.0f;

        protected override void ExecuteAttack(ITargetable target)
        {
            if (target is IDamageable damageable)
            {
                Vector2 hitDir = (target.Position - Position).normalized;
                DamageInfo info = new DamageInfo(AttackPower * biteDamageMultiplier, DamageType.Physical, this, hitDir, 1.5f);
                damageable.TakeDamage(info);
            }
        }
    }
}

