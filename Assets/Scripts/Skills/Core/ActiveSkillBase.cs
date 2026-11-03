using UnityEngine;
using NecroDefense.Characters.Player;

namespace NecroDefense.Skills
{
    /// <summary>
    /// Abstract Base Class for all automatic weapons (Active Skills).
    /// Handles cooldown management, stat scaling, and activation cycles.
    /// Demonstrates Inheritance & Polymorphic Weapon Execution.
    /// </summary>
    public abstract class ActiveSkillBase : SkillBase
    {
        public float BaseCooldown { get; protected set; }
        public float BaseDamage { get; protected set; }
        public float CooldownTimer { get; protected set; }

        protected ActiveSkillBase(string skillId, string displayName, float baseCooldown, float baseDamage)
            : base(skillId, displayName, SkillCategory.Active, 4)
        {
            BaseCooldown = baseCooldown;
            BaseDamage = baseDamage;
            CooldownTimer = 0f;
        }

        public override void UpdateSkill(float deltaTime, PlayerNecromancer player, PlayerSkillManager manager)
        {
            if (!IsUnlocked || player == null || !player.IsAlive) return;

            float effectiveCooldown = BaseCooldown * (manager != null ? manager.CooldownMultiplier : 1f);
            CooldownTimer += deltaTime;

            if (CooldownTimer >= effectiveCooldown)
            {
                CooldownTimer = 0f;
                Fire(player, manager);
            }
        }

        /// <summary>
        /// Polymorphic attack invocation when the weapon cooldown triggers.
        /// </summary>
        protected abstract void Fire(PlayerNecromancer player, PlayerSkillManager manager);

        /// <summary>
        /// Calculates damage accounting for player attack power and Arcane Focus damage multiplier.
        /// </summary>
        protected float CalculateDamage(PlayerNecromancer player, PlayerSkillManager manager, float multiplier = 1f)
        {
            float playerBase = player != null ? player.AttackPower : 10f;
            float arcaneBonus = manager != null ? manager.DamageMultiplier : 1f;
            return (BaseDamage + playerBase * 0.5f) * arcaneBonus * multiplier;
        }

        public override void ResetSkill()
        {
            base.ResetSkill();
            CooldownTimer = 0f;
        }
    }
}

