using NecroDefense.Characters.Player;

namespace NecroDefense.Skills.Passive
{
    /// <summary>
    /// Minion Frenzy.
    /// Resurrected allied minions move and attack faster.
    /// Lv.1: ลูกน้องสีขาวเดินและตีเร็วขึ้น 15%
    /// Lv.2: เร็วขึ้น 30%
    /// Lv.3: เร็วขึ้น 50%
    /// </summary>
    public class MinionFrenzySkill : PassiveSkillBase
    {
        public MinionFrenzySkill() : base("minion_frenzy", "Minion Frenzy", SkillCategory.SpecialPassive)
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "ลูกน้องสีขาวเดินและโจมตีเร็วขึ้น +15%";
                case 2: return "ลูกน้องสีขาวเดินและโจมตีเร็วขึ้น +30%";
                case 3: return "ลูกน้องสีขาวเดินและโจมตีเร็วขึ้น +50%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            float speedMultiplier = 1f;
            float attackSpeedMultiplier = 1f;

            if (level == 1)
            {
                speedMultiplier = 1.15f;
                attackSpeedMultiplier = 1.15f;
            }
            else if (level == 2)
            {
                speedMultiplier = 1.30f;
                attackSpeedMultiplier = 1.30f;
            }
            else if (level == 3)
            {
                speedMultiplier = 1.50f;
                attackSpeedMultiplier = 1.50f;
            }

            if (manager != null)
            {
                manager.SetMinionFrenzy(speedMultiplier, attackSpeedMultiplier);
            }
        }
    }
}

