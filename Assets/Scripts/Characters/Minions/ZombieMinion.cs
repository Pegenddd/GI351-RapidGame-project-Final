using UnityEngine;
using NecroDefense.Core;

namespace NecroDefense.Characters.Minions
{
    /// <summary>
    /// Resurrected Zombie: Undead Ghoul Brawler.
    /// Defends the base with swift claw swipes infused with necrotic poison.
    /// Demonstrates Inheritance and Polymorphic Melee Execution.
    /// </summary>
    public class ZombieMinion : MinionUnit
    {
        [Header("Undead Ghoul Perks")]
        [Tooltip("Bonus necrotic damage dealt with each attack")]
        [SerializeField] private float necroticBonusDamage = 4f;

        protected override void ExecuteAttack(ITargetable target)
        {
            if (target is IDamageable damageable)
            {
                Vector2 hitDir = (target.Position - Position).normalized;
                DamageInfo info = new DamageInfo(AttackPower + necroticBonusDamage, DamageType.Necrotic, this, hitDir, 2f);
                damageable.TakeDamage(info);
            }
        }
    }
}

