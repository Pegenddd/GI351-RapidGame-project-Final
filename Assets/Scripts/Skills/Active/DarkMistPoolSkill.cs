using UnityEngine;
using NecroDefense.Characters.Player;
using NecroDefense.Skills.CombatEntities;

namespace NecroDefense.Skills.Active
{
    /// <summary>
    /// บ่อหมอกมืด (Dark Mist Pool).
    /// Drops lingering necrotic mist puddles behind the player as they move.
    /// Lv.1: ทิ้งบ่อเมื่อเดินผ่าน (ดาเมจ 5 ต่อ 0.5 วินาที, บ่ออยู่ได้ 3 วินาที)
    /// Lv.2: บ่อคงอยู่บนพื้นนานขึ้นเป็น 5 วินาที
    /// Lv.3: รัศมีของบ่อกว้างขึ้น 30%
    /// Lv.4: ทิ้งบ่อได้ถี่ขึ้น (คูลดาวน์ลดลง) และดาเมจเพิ่มเป็น 10
    /// </summary>
    public class DarkMistPoolSkill : ActiveSkillBase
    {
        private float _poolDuration = 3.0f;
        private float _poolRadius = 1.0f;
        private float _damagePerTick = 5.0f;
        private Vector3 _lastDropPosition = Vector3.negativeInfinity;

        public DarkMistPoolSkill() : base("mist_pool", "บ่อหมอกมืด (Dark Mist Pool)", 1.2f, 5f)
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "ทิ้งบ่อหมอกเมื่อเดินผ่าน (ดาเมจ 5 ต่อ 0.5 วินาที, บ่ออยู่ได้ 3 วินาที)";
                case 2: return "บ่อคงอยู่บนพื้นนานขึ้นเป็น 5 วินาที";
                case 3: return "รัศมีของบ่อกว้างขึ้น 30%";
                case 4: return "ทิ้งบ่อได้ถี่ขึ้น (คูลดาวน์ลดลง 40%) และดาเมจเพิ่มเป็น 10";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            switch (level)
            {
                case 1:
                    BaseCooldown = 1.2f;
                    _poolDuration = 3.0f;
                    _poolRadius = 1.0f;
                    _damagePerTick = 5.0f;
                    break;
                case 2:
                    _poolDuration = 5.0f;
                    break;
                case 3:
                    _poolRadius = 1.3f; // +30%
                    break;
                case 4:
                    BaseCooldown = 0.7f; // Faster drop
                    _damagePerTick = 10.0f;
                    break;
            }
        }

        protected override void Fire(PlayerNecromancer player, PlayerSkillManager manager)
        {
            if (player == null || !player.IsAlive) return;

            // Only drop if player has moved at least a small distance or on timer
            float moved = Vector2.Distance(player.Position, _lastDropPosition);
            if (moved < 0.4f && _lastDropPosition != Vector3.negativeInfinity) return;

            _lastDropPosition = player.Position;

            float arcaneMult = manager != null ? manager.DamageMultiplier : 1f;
            float dmg = _damagePerTick * arcaneMult;

            GameObject poolObj = new GameObject("Ground_DarkMistPool");
            poolObj.transform.position = player.Position;

            var entity = poolObj.AddComponent<MistPoolEntity>();
            entity.Initialize(dmg, _poolDuration, _poolRadius, player);
        }
    }
}

