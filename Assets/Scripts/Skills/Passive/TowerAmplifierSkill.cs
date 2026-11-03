using UnityEngine;
using NecroDefense.Characters.Player;
using NecroDefense.Base;

namespace NecroDefense.Skills.Passive
{
    /// <summary>
    /// Tower Amplifier.
    /// Increases Citadel defensive aura/range and grants projectile pierce.
    /// Lv.1: ระยะยิงป้อมไกลขึ้น 15%
    /// Lv.2: ไกลขึ้น 30%
    /// Lv.3: ไกลขึ้น 50% และกระสุนป้อมทะลุศัตรูได้ 1 ตัว
    /// </summary>
    public class TowerAmplifierSkill : PassiveSkillBase
    {
        private float _baseAuraRadius = 6f;
        private bool _capturedBaseRadius = false;

        public TowerAmplifierSkill() : base("tower_amplifier", "Tower Amplifier", SkillCategory.SpecialPassive)
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "ระยะยิง/ออร่าของปราสาทหลักไกลขึ้น +15%";
                case 2: return "ระยะยิง/ออร่าของปราสาทหลักไกลขึ้น +30%";
                case 3: return "ระยะยิง/ออร่าไกลขึ้น +50% และกระสุนป้อมทะลุศัตรูได้ 1 ตัว";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            var baseCrystal = UnityEngine.Object.FindAnyObjectByType<BaseCrystal>();
            if (baseCrystal != null)
            {
                if (!_capturedBaseRadius)
                {
                    _baseAuraRadius = baseCrystal.AuraRadius;
                    _capturedBaseRadius = true;
                }

                float bonus = 0f;
                if (level == 1) bonus = 0.15f;
                else if (level == 2) bonus = 0.30f;
                else if (level == 3) bonus = 0.50f;

                baseCrystal.ApplyAuraRadiusBonus(bonus, _baseAuraRadius);
            }

            if (manager != null)
            {
                int pierce = (level >= 3) ? 1 : 0;
                manager.SetTowerPierceBonus(pierce);
            }
        }

        public override void ResetSkill()
        {
            base.ResetSkill();
            _capturedBaseRadius = false;
        }
    }
}

