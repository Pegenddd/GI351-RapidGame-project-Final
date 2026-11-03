using NecroDefense.Characters.Player;

namespace NecroDefense.Skills.Passive
{
    /// <summary>
    /// Arcane Focus.
    /// Increases damage of all weapons and attacks.
    /// Lv.1: พลังโจมตีของอาวุธทุกชนิดเพิ่ม 15%
    /// Lv.2: เพิ่ม 30%
    /// Lv.3: เพิ่ม 50%
    /// </summary>
    public class ArcaneFocusSkill : PassiveSkillBase
    {
        public ArcaneFocusSkill() : base("arcane_focus", "Arcane Focus")
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "พลังโจมตีของอาวุธและเวทมนตร์ทุกชนิดเพิ่มขึ้น +15%";
                case 2: return "พลังโจมตีของอาวุธและเวทมนตร์ทุกชนิดเพิ่มขึ้น +30%";
                case 3: return "พลังโจมตีของอาวุธและเวทมนตร์ทุกชนิดเพิ่มขึ้น +50%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            float bonus = 0f;
            if (level == 1) bonus = 0.15f;
            else if (level == 2) bonus = 0.30f;
            else if (level == 3) bonus = 0.50f;

            if (manager != null)
            {
                manager.SetDamageMultiplier(1f + bonus);
            }
        }
    }
}

