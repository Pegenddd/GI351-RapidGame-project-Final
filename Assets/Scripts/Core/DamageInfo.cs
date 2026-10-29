using UnityEngine;

namespace NecroDefense.Core
{
    /// <summary>
    /// Encapsulates all data related to a damage event.
    /// Demonstrates OOP Encapsulation and Abstraction (references ITargetable instead of concrete class).
    /// </summary>
    public readonly struct DamageInfo
    {
        public float Amount { get; }
        public DamageType Type { get; }
        public ITargetable Attacker { get; }
        public Vector2 HitDirection { get; }
        public float KnockbackForce { get; }

        public DamageInfo(float amount, DamageType type, ITargetable attacker = null, Vector2 hitDirection = default, float knockbackForce = 0f)
        {
            Amount = Mathf.Max(0f, amount);
            Type = type;
            Attacker = attacker;
            HitDirection = hitDirection;
            KnockbackForce = knockbackForce;
        }
    }
}
