using NecroDefense.Characters.Player;

namespace NecroDefense.Skills
{
    /// <summary>
    /// Abstract Base Class for passive stat upgrades and permanent modifiers.
    /// Demonstrates Inheritance & Encapsulation.
    /// </summary>
    public abstract class PassiveSkillBase : SkillBase
    {
        protected PassiveSkillBase(string skillId, string displayName, SkillCategory category = SkillCategory.Passive)
            : base(skillId, displayName, category, 3)
        {
        }
    }
}

