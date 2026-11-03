using UnityEngine;
using NecroDefense.Characters.Player;
using NecroDefense.Skills.CombatEntities;

namespace NecroDefense.Skills.Active
{
    /// <summary>
    /// เคียวกระดูก (Bone Scythe).
    /// Boomerang scythe that flies outward and returns, slicing through monsters.
    /// Lv.1: ปล่อย 1 เล่ม (ดาเมจขาไปขากลับดาเมจเท่ากัน มีคูลดาวน์)
    /// Lv.2: ดาเมจตอนขากลับแรงขึ้น
    /// Lv.3: ขนาด Hitbox ของเคียวใหญ่ขึ้น
    /// Lv.4: ปล่อยเคียว 2 เล่ม (พุ่งออกไปด้านหน้าและด้านหลังพร้อมกัน)
    /// </summary>
    public class BoneScytheSkill : ActiveSkillBase
    {
        private float _returnDamageMultiplier = 1.0f;
        private float _hitboxScale = 1.0f;
        private bool _dualScythes = false;

        public BoneScytheSkill() : base("bone_scythe", "เคียวกระดูก (Bone Scythe)", 2.4f, 18f)
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "ปล่อยเคียว 1 เล่ม (ดาเมจขาไปขากลับเท่ากัน มีคูลดาวน์)";
                case 2: return "ดาเมจตอนขากลับแรงขึ้น +75%";
                case 3: return "ขนาด Hitbox ของเคียวใหญ่ขึ้น 50%";
                case 4: return "ปล่อยเคียว 2 เล่ม (พุ่งออกไปด้านหน้าและด้านหลังพร้อมกัน)";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            switch (level)
            {
                case 1:
                    _returnDamageMultiplier = 1.0f;
                    _hitboxScale = 1.0f;
                    _dualScythes = false;
                    break;
                case 2:
                    _returnDamageMultiplier = 1.75f;
                    break;
                case 3:
                    _hitboxScale = 1.5f;
                    break;
                case 4:
                    _dualScythes = true;
                    break;
            }
        }

        protected override void Fire(PlayerNecromancer player, PlayerSkillManager manager)
        {
            if (player == null || !player.IsAlive) return;

            // Determine forward direction: either moving direction or facing direction
            Vector2 forwardDir = Vector2.right;
            var sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.flipX) forwardDir = Vector2.left;

            // Or look for nearest enemy to aim
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
                            forwardDir = ((Vector2)e.Position - (Vector2)player.Position).normalized;
                        }
                    }
                }
            }

            float outDmg = CalculateDamage(player, manager, 1f);
            float retDmg = outDmg * _returnDamageMultiplier;

            SpawnScythe(player, forwardDir, outDmg, retDmg);

            // Lv.4: Dual scythes (front and back)
            if (_dualScythes)
            {
                SpawnScythe(player, -forwardDir, outDmg, retDmg);
            }
        }

        private void SpawnScythe(PlayerNecromancer player, Vector2 dir, float outDmg, float retDmg)
        {
            GameObject scytheObj = new GameObject("Projectile_BoneScythe");
            scytheObj.transform.position = player.Position;

            var comp = scytheObj.AddComponent<BoneScytheProjectile>();
            comp.Initialize(dir, outDmg, retDmg, _hitboxScale, player.transform, player);
        }
    }
}

