using NecroDefense.Characters.Player;

namespace NecroDefense.Skills.Passive
{
    /// <summary>
    /// Mana Magnet.
    /// Increases EXP and Soul pickup collection radius.
    /// Lv.1: รัศมีกว้างขึ้น 50%
    /// Lv.2: กว้างขึ้น 100%
    /// Lv.3: กว้างขึ้น 200%
    /// </summary>
    public class ManaMagnetSkill : PassiveSkillBase
    {
        public ManaMagnetSkill() : base("mana_magnet", "Mana Magnet")
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "รัศมีดูดไอเทมและวิญญาณกว้างขึ้น +50%";
                case 2: return "รัศมีดูดไอเทมและวิญญาณกว้างขึ้น +100%";
                case 3: return "รัศมีดูดไอเทมและวิญญาณกว้างขึ้น +200%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            float bonus = 0f;
            if (level == 1) bonus = 0.5f;
            else if (level == 2) bonus = 1.0f;
            else if (level == 3) bonus = 2.0f;

            if (manager != null)
            {
                manager.SetMagnetRadiusBonus(bonus);
            }
        }
    }
}

