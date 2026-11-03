using UnityEngine;
using NecroDefense.Characters.Player;
using NecroDefense.Skills.CombatEntities;

namespace NecroDefense.Skills.Active
{
    /// <summary>
    /// เลเซอร์ (Death Laser / Necro Beam).
    /// Piercing beam sweeping through monsters in front of the Necromancer.
    /// Lv.1: ยิง 1 เส้นกวาดด้านหน้า 45 องศา
    /// Lv.2: มุมกวาดกว้างขึ้นเป็น 90 องศา
    /// Lv.3: ดาเมจเพิ่มขึ้น ทุกๆ 0.5 วินาที
    /// Lv.4: เพิ่มเลเซอร์เป็น 2 เส้น (กวาดซ้ายและขวาสวนทางกัน)
    /// </summary>
    public class DeathLaserSkill : ActiveSkillBase
    {
        private float _sweepAngle = 45f;
        private float _damageBonus = 0f;
        private bool _dualLasers = false;

        public DeathLaserSkill() : base("death_laser", "เลเซอร์ (Death Laser)", 3.2f, 25f)
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "ยิง 1 เส้นกวาดด้านหน้า 45 องศา ทะลวงศัตรูทั้งหมด";
                case 2: return "มุมกวาดกว้างขึ้นเป็น 90 องศา";
                case 3: return "ดาเมจเพิ่มขึ้น +50% ต่อ 0.5 วินาที";
                case 4: return "เพิ่มเลเซอร์เป็น 2 เส้น (กวาดซ้ายและขวาสวนทางกัน)";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            switch (level)
            {
                case 1:
                    _sweepAngle = 45f;
                    _damageBonus = 0f;
                    _dualLasers = false;
                    break;
                case 2:
                    _sweepAngle = 90f;
                    break;
                case 3:
                    _damageBonus = 0.5f;
                    break;
                case 4:
                    _dualLasers = true;
                    break;
            }
        }

        protected override void Fire(PlayerNecromancer player, PlayerSkillManager manager)
        {
            if (player == null || !player.IsAlive) return;

            // Facing direction
            float baseAngle = 0f;
            var sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.flipX) baseAngle = 180f;

            // Check if nearest enemy exists to aim towards
            var enemies = UnityEngine.Object.FindObjectsByType<Characters.Enemies.EnemyUnit>();
            if (enemies != null && enemies.Length > 0)
            {
                float closestDist = float.MaxValue;
                foreach (var e in enemies)
                {
                    if (e != null && e.IsAlive && e.IsValidTarget)
                    {
                        float d = Vector2.Distance(player.Position, e.Position);
                        if (d < closestDist)
                        {
                            closestDist = d;
                            Vector2 dir = (e.Position - player.Position).normalized;
                            baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        }
                    }
                }
            }

            float dmg = CalculateDamage(player, manager, 1f + _damageBonus);
            float halfSweep = _sweepAngle * 0.5f;
            float duration = 0.55f;

            // Primary beam: sweeps -halfSweep to +halfSweep
            SpawnLaser(player, baseAngle - halfSweep, baseAngle + halfSweep, duration, dmg);

            // Lv.4: Dual lasers sweeping in opposite directions
            if (_dualLasers)
            {
                SpawnLaser(player, baseAngle + halfSweep, baseAngle - halfSweep, duration, dmg);
            }
        }

        private void SpawnLaser(PlayerNecromancer player, float startAng, float endAng, float duration, float dmg)
        {
            GameObject laserObj = new GameObject("Beam_DeathLaser");
            laserObj.transform.position = player.Position;

            var beam = laserObj.AddComponent<LaserBeamSweep>();
            beam.Initialize(player.transform, startAng, endAng, duration, dmg, player);
        }
    }
}

