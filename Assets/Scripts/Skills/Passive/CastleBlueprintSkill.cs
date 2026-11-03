using UnityEngine;
using NecroDefense.Characters.Player;
using NecroDefense.Base;

namespace NecroDefense.Skills.Passive
{
    /// <summary>
    /// Castle Blueprint.
    /// Increases Citadel Core (Defensive Base) Max HP.
    /// Lv.1: Max HP ของปราสาทหลักเพิ่ม 20%
    /// Lv.2: เพิ่ม 40%
    /// Lv.3: เพิ่ม 60%
    /// </summary>
    public class CastleBlueprintSkill : PassiveSkillBase
    {
        private float _baseCitadelHp = 1000f;
        private bool _capturedBaseHp = false;

        public CastleBlueprintSkill() : base("castle_blueprint", "Castle Blueprint")
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "Max HP ของปราสาทหลัก (Citadel) เพิ่มขึ้น +20%";
                case 2: return "Max HP ของปราสาทหลัก (Citadel) เพิ่มขึ้น +40%";
                case 3: return "Max HP ของปราสาทหลัก (Citadel) เพิ่มขึ้น +60%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            var baseCrystal = UnityEngine.Object.FindAnyObjectByType<BaseCrystal>();
            if (baseCrystal == null) return;

            if (!_capturedBaseHp)
            {
                _baseCitadelHp = baseCrystal.MaxHealth;
                _capturedBaseHp = true;
            }

            float bonus = 0f;
            if (level == 1) bonus = 0.20f;
            else if (level == 2) bonus = 0.40f;
            else if (level == 3) bonus = 0.60f;

            baseCrystal.ApplyMaxHealthBonus(bonus, _baseCitadelHp);
        }

        public override void ResetSkill()
        {
            base.ResetSkill();
            _capturedBaseHp = false;
        }
    }
}

