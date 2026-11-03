using NecroDefense.Characters.Player;

namespace NecroDefense.Skills.Passive
{
    /// <summary>
    /// Haste Scroll.
    /// Reduces weapon attack cooldowns.
    /// Lv.1: คูลดาวน์อาวุธโจมตีลดลง 10%
    /// Lv.2: ลดลง 20%
    /// Lv.3: ลดลง 30%
    /// </summary>
    public class HasteScrollSkill : PassiveSkillBase
    {
        public HasteScrollSkill() : base("haste_scroll", "Haste Scroll")
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "คูลดาวน์อาวุธโจมตีทั้งหมดลดลง 10%";
                case 2: return "คูลดาวน์อาวุธโจมตีทั้งหมดลดลง 20%";
                case 3: return "คูลดาวน์อาวุธโจมตีทั้งหมดลดลง 30%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            float reduction = 0f;
            if (level == 1) reduction = 0.10f;
            else if (level == 2) reduction = 0.20f;
            else if (level == 3) reduction = 0.30f;

            if (manager != null)
            {
                manager.SetCooldownReduction(reduction);
            }
        }
    }
}

