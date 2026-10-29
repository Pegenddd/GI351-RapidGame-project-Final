using UnityEngine;
using NecroDefense.Core;

namespace NecroDefense.Characters.Minions
{
    /// <summary>
    /// Resurrected Golem: Undead Titan Barricade.
    /// Acts as a heavy defensive wall protecting the Citadel Core.
    /// Demonstrates Polymorphic Armor Mitigation and Area Stun Slam.
    /// </summary>
    public class GolemMinion : MinionUnit
    {
        [Header("Undead Titan Perks")]
        [Tooltip("Flat damage reduction from enemy attacks (minimum 1 damage always dealt)")]
        [SerializeField] private float armorFlatReduction = 4f;
        [Tooltip("Area radius of ground slam attack")]
        [SerializeField] private float groundSlamRadius = 2.5f;
        [Tooltip("Knockback force multiplier after damage reduction (0=none, 1=full)")]
        [Range(0f, 1f)]
        [SerializeField] private float knockbackResistance = 0.2f;

        public override void TakeDamage(DamageInfo damageInfo)
        {
            // Polymorphic armor reduction: heavily shields incoming damage
            float reducedDamage = Mathf.Max(1f, damageInfo.Amount - armorFlatReduction);
            DamageInfo modifiedInfo = new DamageInfo(reducedDamage, damageInfo.Type, damageInfo.Attacker, damageInfo.HitDirection, damageInfo.KnockbackForce * knockbackResistance);
            base.TakeDamage(modifiedInfo);
        }

        protected override void ExecuteAttack(ITargetable target)
        {
            // Ground Slam AOE affecting nearby hostile monsters
            Collider2D[] colliders = Physics2D.OverlapCircleAll(Position, groundSlamRadius);
            foreach (var col in colliders)
            {
                if (col.TryGetComponent<IDamageable>(out var damageable) &&
                    col.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team == Faction.Enemy && targetable.IsValidTarget)
                    {
                        Vector2 knockback = (targetable.Position - Position).normalized;
                        DamageInfo info = new DamageInfo(AttackPower, DamageType.Physical, this, knockback, 5f);
                        damageable.TakeDamage(info);
                    }
                }
            }
        }
    }
}

