using UnityEngine;
using NecroDefense.Characters.Player;
using NecroDefense.Skills.CombatEntities;

namespace NecroDefense.Skills.Active
{
    /// <summary>
    /// หนามเงา (Shadow Spikes).
    /// Spawns erupting shadow spike pillars from the ground dealing AoE damage.
    /// Lv.1: สุ่ม 1 เสาหนามแบบ AOE มีคูลดาวน์
    /// Lv.2: สุ่มเสาหนามขึ้นมาพร้อมกัน 2 ต้นในระยะ
    /// Lv.3: ลดระยะเวลาคูลดาวน์ลงเหลือ 2 วินาที
    /// Lv.4: รัศมี AOE ของเสากว้างขึ้น 50% และดาเมจเพิ่ม
    /// </summary>
    public class ShadowSpikesSkill : ActiveSkillBase
    {
        private int _pillarCount = 1;
        private float _aoeRadius = 1.8f;
        private float _damageBonus = 0f;

        public ShadowSpikesSkill() : base("shadow_spikes", "หนามเงา (Shadow Spikes)", 3.5f, 22f)
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "สุ่ม 1 เสาหนามแบบ AOE ใต้ศัตรู มีคูลดาวน์";
                case 2: return "สุ่มเสาหนามขึ้นมาพร้อมกัน 2 ต้นในระยะ";
                case 3: return "ลดระยะเวลาคูลดาวน์ลงเหลือ 2 วินาที";
                case 4: return "รัศมี AOE ของเสากว้างขึ้น 50% และดาเมจเพิ่ม +50%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            switch (level)
            {
                case 1:
                    _pillarCount = 1;
                    BaseCooldown = 3.5f;
                    _aoeRadius = 1.8f;
                    _damageBonus = 0f;
                    break;
                case 2:
                    _pillarCount = 2;
                    break;
                case 3:
                    BaseCooldown = 2.0f;
                    break;
                case 4:
                    _aoeRadius = 2.7f; // +50%
                    _damageBonus = 0.5f; // +50%
                    break;
            }
        }

        protected override void Fire(PlayerNecromancer player, PlayerSkillManager manager)
        {
            if (player == null || !player.IsAlive) return;

            var enemies = UnityEngine.Object.FindObjectsByType<Characters.Enemies.EnemyUnit>();
            float dmg = CalculateDamage(player, manager, 1f + _damageBonus);
            float maxCastRange = 9.0f;

            for (int i = 0; i < _pillarCount; i++)
            {
                Vector2 targetPos;

                // Pick random enemy within range if available
                if (enemies != null && enemies.Length > 0)
                {
                    var validEnemies = System.Array.FindAll(enemies, e => e != null && e.IsAlive && Vector2.Distance(player.Position, e.Position) <= maxCastRange);
                    if (validEnemies.Length > 0)
                    {
                        var picked = validEnemies[UnityEngine.Random.Range(0, validEnemies.Length)];
                        targetPos = (Vector2)picked.Position + UnityEngine.Random.insideUnitCircle * 0.5f;
                    }
                    else
                    {
                        targetPos = (Vector2)player.Position + UnityEngine.Random.insideUnitCircle * 5f;
                    }
                }
                else
                {
                    targetPos = (Vector2)player.Position + UnityEngine.Random.insideUnitCircle * 5f;
                }

                SpawnSpikePillar(targetPos, dmg, _aoeRadius, player);
            }
        }

        private void SpawnSpikePillar(Vector2 pos, float dmg, float radius, PlayerNecromancer player)
        {
            GameObject spikeObj = new GameObject("Pillar_ShadowSpike");
            spikeObj.transform.position = pos;

            var entity = spikeObj.AddComponent<ShadowSpikeEntity>();
            entity.Initialize(dmg, radius, player);
        }
    }
}

