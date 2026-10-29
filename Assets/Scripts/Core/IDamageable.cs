using System;
using UnityEngine;

namespace NecroDefense.Core
{
    /// <summary>
    /// Abstraction for any entity that can receive damage and be healed.
    /// Demonstrates the Abstraction and Interface Segregation principles.
    /// </summary>
    public interface IDamageable
    {
        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsAlive { get; }

        event Action<DamageInfo> OnDamaged;
        event Action OnDied;
        event Action<float, float> OnHealthChanged; // current, max

        void TakeDamage(DamageInfo damageInfo);
        void Heal(float amount);
    }
}

