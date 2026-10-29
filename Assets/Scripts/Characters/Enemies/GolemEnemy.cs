using UnityEngine;
using NecroDefense.Core;

namespace NecroDefense.Characters.Enemies
{
    /// <summary>
    /// Golem Enemy: Heavy frontline brute.
    /// Demonstrates:
    /// - Polymorphic Damage Mitigation: Takes reduced physical damage through armor.
    /// - Polymorphic Attack: Ground slam with Area of Effect.
    /// </summary>
    public class GolemEnemy : EnemyUnit
    {
        [Header("Golem Armor & Slam")]
        [Tooltip("Flat damage subtracted from every incoming hit (minimum 1 damage always dealt)")]
        [SerializeField] private float armorFlatReduction = 3f;
        [Tooltip("Radius of the ground slam AoE attack")]
        [SerializeField] private float slamRadius = 2.0f;
        [Tooltip("Fraction of knockback force applied after armor reduction (e.g. 0.3 = 30% of source knockback)")]
        [Range(0f, 1f)]
        [SerializeField] private float knockbackResistance = 0.3f;

        public override void TakeDamage(DamageInfo damageInfo)
        {
            // Polymorphic armor reduction
            float reducedDamage = Mathf.Max(1f, damageInfo.Amount - armorFlatReduction);
            DamageInfo modifiedInfo = new DamageInfo(reducedDamage, damageInfo.Type, damageInfo.Attacker, damageInfo.HitDirection, damageInfo.KnockbackForce * knockbackResistance);
            base.TakeDamage(modifiedInfo);
        }

        protected override void ExecuteAttack(ITargetable target)
        {
            // Ground Slam AOE
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(Position, slamRadius);
            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<IDamageable>(out var damageable) &&
                    col.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team != Team && targetable.IsValidTarget)
                    {
                        Vector2 knockbackDir = (targetable.Position - Position).normalized;
                        DamageInfo info = new DamageInfo(AttackPower, DamageType.Physical, this, knockbackDir, 4f);
                        damageable.TakeDamage(info);
                    }
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, slamRadius);
        }
    }
}

