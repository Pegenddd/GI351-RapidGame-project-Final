using UnityEngine;
using NecroDefense.Characters.Player;
using NecroDefense.Core;
using NecroDefense.Combat;

namespace NecroDefense.Skills.Active
{
    /// <summary>
    /// บอลเวทมน (Magic Bolt / Dark Bolt).
    /// Auto-fires magical homing dark bolts at nearest enemies.
    /// Lv.1: ยิงบอลเวท 1 ลูกใส่ศัตรูใกล้สุด
    /// Lv.2: ยิงบอลเวท 2 ลูกพร้อมกัน
    /// Lv.3: บอลเวทสามารถโจมตีทะลุศัตรูได้ 1 ตัว (Pierce = 2)
    /// Lv.4: ยิงบอลเวท 3 ลูก และเพิ่มดาเมจ
    /// </summary>
    public class MagicBoltSkill : ActiveSkillBase
    {
        private int _boltCount = 1;
        private int _pierceCount = 1;
        private float _damageBonus = 0f;

        public MagicBoltSkill() : base("magic_bolt", "บอลเวทมน (Magic Bolt)", 1.0f, 15f)
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "ยิงบอลเวท 1 ลูกใส่ศัตรูใกล้สุด";
                case 2: return "ยิงบอลเวท 2 ลูกพร้อมกัน";
                case 3: return "บอลเวทสามารถโจมตีทะลุศัตรูได้ 1 ตัว";
                case 4: return "ยิงบอลเวท 3 ลูก และเพิ่มดาเมจ +40%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            switch (level)
            {
                case 1:
                    _boltCount = 1;
                    _pierceCount = 1;
                    _damageBonus = 0f;
                    break;
                case 2:
                    _boltCount = 2;
                    break;
                case 3:
                    _pierceCount = 2;
                    break;
                case 4:
                    _boltCount = 3;
                    _damageBonus = 0.4f;
                    break;
            }
        }

        protected override void Fire(PlayerNecromancer player, PlayerSkillManager manager)
        {
            if (player == null || !player.IsAlive) return;

            // Find nearest enemies
            var enemies = UnityEngine.Object.FindObjectsByType<Characters.Enemies.EnemyUnit>();
            if (enemies == null || enemies.Length == 0) return;

            // Sort enemies by distance
            System.Array.Sort(enemies, (a, b) =>
            {
                if (a == null) return 1;
                if (b == null) return -1;
                float distA = Vector2.Distance(player.Position, a.Position);
                float distB = Vector2.Distance(player.Position, b.Position);
                return distA.CompareTo(distB);
            });

            float dmg = CalculateDamage(player, manager, 1f + _damageBonus);
            int shotsFired = 0;

            for (int i = 0; i < enemies.Length && shotsFired < _boltCount; i++)
            {
                var enemy = enemies[i];
                if (enemy != null && enemy.IsAlive && enemy.IsValidTarget)
                {
                    Vector2 dir = ((Vector2)enemy.Position - (Vector2)player.Position).normalized;
                    if (dir == Vector2.zero) dir = Vector2.right;

                    // Slight angle spread if shooting multiple bolts at same target
                    if (shotsFired > 0)
                    {
                        float angleOffset = (shotsFired % 2 == 1) ? 15f * ((shotsFired + 1) / 2) : -15f * (shotsFired / 2);
                        dir = Quaternion.Euler(0, 0, angleOffset) * dir;
                    }

                    SpawnBolt(player, dir, dmg, enemy);
                    shotsFired++;
                }
            }

            // If not enough enemies, fire remaining in forward/random direction
            while (shotsFired < _boltCount)
            {
                float randomAngle = UnityEngine.Random.Range(0f, 360f);
                Vector2 dir = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle));
                SpawnBolt(player, dir, dmg, null);
                shotsFired++;
            }
        }

        private void SpawnBolt(PlayerNecromancer player, Vector2 dir, float dmg, ITargetable target)
        {
            GameObject boltObj = Utils.ProceduralVisuals.CreateProjectileGameObject("DarkBolt", Color.white);
            boltObj.transform.position = player.Position;

            if (boltObj.TryGetComponent<HomingProjectile>(out var homing))
            {
                homing.EnsureVisuals();
                homing.SetPierce(_pierceCount);
                homing.Initialize(dir, dmg, DamageType.Necrotic, Faction.Player, player);
                if (target != null)
                {
                    homing.SetTarget(target);
                }
            }
            else if (boltObj.TryGetComponent<Projectile>(out var proj))
            {
                proj.EnsureVisuals();
                proj.SetPierce(_pierceCount);
                proj.Initialize(dir, dmg, DamageType.Necrotic, Faction.Player, player);
            }
        }
    }
}

