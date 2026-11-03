using NecroDefense.Characters.Player;

namespace NecroDefense.Skills.Passive
{
    /// <summary>
    /// Soul Efficiency.
    /// Reduces soul cost for resurrecting fallen corpses and base actions.
    /// Lv.1: ลดมานาที่ใช้ชุบชีวิตและอัปเกรดป้อม 10%
    /// Lv.2: ลดลง 20%
    /// Lv.3: ลดลง 30%
    /// </summary>
    public class SoulEfficiencySkill : PassiveSkillBase
    {
        public SoulEfficiencySkill() : base("soul_efficiency", "Soul Efficiency", SkillCategory.SpecialPassive)
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "ลดวิญญาณที่ใช้ชุบชีวิตและอัปเกรดป้อม 10%";
                case 2: return "ลดวิญญาณที่ใช้ชุบชีวิตและอัปเกรดป้อม 20%";
                case 3: return "ลดวิญญาณที่ใช้ชุบชีวิตและอัปเกรดป้อม 30%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            float discount = 0f;
            if (level == 1) discount = 0.10f;
            else if (level == 2) discount = 0.20f;
            else if (level == 3) discount = 0.30f;

            if (manager != null)
            {
                manager.SetSoulCostDiscount(discount);
            }
        }
    }
}

