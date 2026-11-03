using NecroDefense.Characters.Player;

namespace NecroDefense.Skills.Passive
{
    /// <summary>
    /// Giant's Heart.
    /// Increases Player Necromancer Max HP.
    /// Lv.1: Max HP ของ Necromancer เพิ่ม 20%
    /// Lv.2: เพิ่ม 40%
    /// Lv.3: เพิ่ม 60%
    /// </summary>
    public class GiantsHeartSkill : PassiveSkillBase
    {
        private float _previousBonus = 0f;

        public GiantsHeartSkill() : base("giants_heart", "Giant's Heart")
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "Max HP ของ Necromancer เพิ่มขึ้น +20%";
                case 2: return "Max HP ของ Necromancer เพิ่มขึ้น +40%";
                case 3: return "Max HP ของ Necromancer เพิ่มขึ้น +60%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            float bonus = 0f;
            if (level == 1) bonus = 0.20f;
            else if (level == 2) bonus = 0.40f;
            else if (level == 3) bonus = 0.60f;

            if (player != null)
            {
                // Calculate base max HP without previous bonus
                float baseHp = player.MaxHealth / (1f + _previousBonus);
                float newMaxHp = baseHp * (1f + bonus);
                float hpDifference = newMaxHp - player.MaxHealth;

                player.SetMaxHealth(newMaxHp, false);
                if (hpDifference > 0)
                {
                    player.Heal(hpDifference);
                }
            }

            _previousBonus = bonus;
        }

        public override void ResetSkill()
        {
            base.ResetSkill();
            _previousBonus = 0f;
        }
    }
}

