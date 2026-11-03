using System;
using UnityEngine;
using NecroDefense.Characters.Player;

namespace NecroDefense.Skills
{
    /// <summary>
    /// Abstract Base Class for all Vampire Survivors-style skills and weapons.
    /// Demonstrates OOP Principles:
    /// - Abstraction: Common contract for active and passive skill logic.
    /// - Encapsulation: Protected state with public read-only accessors.
    /// - Polymorphism: Virtual and abstract methods for level-up application and update execution.
    /// </summary>
    public abstract class SkillBase
    {
        public string SkillId { get; }
        public string DisplayName { get; }
        public SkillCategory Category { get; }
        public int MaxLevel { get; }
        public int CurrentLevel { get; protected set; }

        public bool IsUnlocked => CurrentLevel > 0;
        public bool IsMaxLevel => CurrentLevel >= MaxLevel;

        public event Action<SkillBase, int> OnSkillLevelChanged;

        protected SkillBase(string skillId, string displayName, SkillCategory category, int maxLevel)
        {
            SkillId = skillId;
            DisplayName = displayName;
            Category = category;
            MaxLevel = maxLevel;
            CurrentLevel = 0;
        }

        /// <summary>
        /// Returns specific description text for a given level.
        /// </summary>
        public abstract string GetLevelDescription(int level);

        /// <summary>
        /// Description of the next upcoming upgrade level.
        /// </summary>
        public virtual string GetNextLevelDescription()
        {
            if (IsMaxLevel) return "MAX LEVEL REACHED";
            return GetLevelDescription(CurrentLevel + 1);
        }

        /// <summary>
        /// Upgrades this skill by +1 level and applies stat changes/spawns.
        /// </summary>
        public virtual void Upgrade(PlayerNecromancer player, PlayerSkillManager manager)
        {
            if (IsMaxLevel) return;

            CurrentLevel++;
            ApplyLevel(CurrentLevel, player, manager);
            OnSkillLevelChanged?.Invoke(this, CurrentLevel);
        }

        /// <summary>
        /// Polymorphic method implemented by each skill to apply changes at the given level.
        /// </summary>
        protected abstract void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager);

        /// <summary>
        /// Periodic update for active weapons (cooldown timers, orbital movement, etc.).
        /// </summary>
        public virtual void UpdateSkill(float deltaTime, PlayerNecromancer player, PlayerSkillManager manager)
        {
        }

        /// <summary>
        /// Resets level back to 0 (or initial state) on new run.
        /// </summary>
        public virtual void ResetSkill()
        {
            CurrentLevel = 0;
        }
    }
}

