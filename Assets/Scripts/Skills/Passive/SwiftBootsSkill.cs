using NecroDefense.Characters.Player;

namespace NecroDefense.Skills.Passive
{
    /// <summary>
    /// Swift Boots.
    /// Increases Player movement speed.
    /// Lv.1: ความเร็วการเดินเพิ่ม 10%
    /// Lv.2: เพิ่ม 20%
    /// Lv.3: เพิ่ม 30%
    /// </summary>
    public class SwiftBootsSkill : PassiveSkillBase
    {
        public SwiftBootsSkill() : base("swift_boots", "Swift Boots")
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "ความเร็วการเคลื่อนที่เพิ่มขึ้น +10%";
                case 2: return "ความเร็วการเคลื่อนที่เพิ่มขึ้น +20%";
                case 3: return "ความเร็วการเคลื่อนที่เพิ่มขึ้น +30%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            float bonus = 0f;
            if (level == 1) bonus = 0.10f;
            else if (level == 2) bonus = 0.20f;
            else if (level == 3) bonus = 0.30f;

            if (manager != null)
            {
                manager.SetMoveSpeedBonus(bonus);
            }
        }
    }
}

