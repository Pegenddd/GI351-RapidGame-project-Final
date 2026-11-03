using System.Collections.Generic;
using UnityEngine;
using NecroDefense.Characters.Player;
using NecroDefense.Skills.CombatEntities;

namespace NecroDefense.Skills.Active
{
    /// <summary>
    /// วงแหวนกะโหลก (Skull Orbit).
    /// Creates floating undead skulls that orbit the player, damaging enemies on contact.
    /// Lv.1: มี 1 กะโหลกหมุนรอบตัว
    /// Lv.2: ความเร็วในการหมุนรอบตัวเพิ่มขึ้น 20%
    /// Lv.3: เพิ่มจำนวนเป็น 2 กะโหลก
    /// Lv.4: ขยายระยะความกว้างของวงแหวน 30% และดาเมจเพิ่ม
    /// </summary>
    public class SkullOrbitSkill : ActiveSkillBase
    {
        private int _skullCount = 1;
        private float _orbitSpeed = 160f;
        private float _orbitRadius = 2.0f;
        private float _damageBonus = 0f;

        private readonly List<OrbitingSkullEntity> _activeSkulls = new List<OrbitingSkullEntity>();

        public SkullOrbitSkill() : base("skull_orbit", "วงแหวนกะโหลก (Skull Orbit)", 9999f, 12f)
        {
        }

        public override string GetLevelDescription(int level)
        {
            switch (level)
            {
                case 1: return "มี 1 กะโหลกหมุนรอบตัว คอยป้องกันและโจมตีศัตรู";
                case 2: return "ความเร็วในการหมุนรอบตัวเพิ่มขึ้น 20%";
                case 3: return "เพิ่มจำนวนเป็น 2 กะโหลกหมุนรอบตัว";
                case 4: return "ขยายระยะความกว้างของวงแหวน 30% และดาเมจเพิ่ม +50%";
                default: return "";
            }
        }

        protected override void ApplyLevel(int level, PlayerNecromancer player, PlayerSkillManager manager)
        {
            switch (level)
            {
                case 1:
                    _skullCount = 1;
                    _orbitSpeed = 160f;
                    _orbitRadius = 2.0f;
                    _damageBonus = 0f;
                    break;
                case 2:
                    _orbitSpeed = 192f; // +20%
                    break;
                case 3:
                    _skullCount = 2;
                    break;
                case 4:
                    _orbitRadius = 2.6f; // +30%
                    _damageBonus = 0.5f; // +50%
                    break;
            }

            RebuildSkulls(player, manager);
        }

        public override void UpdateSkill(float deltaTime, PlayerNecromancer player, PlayerSkillManager manager)
        {
            if (!IsUnlocked || player == null || !player.IsAlive) return;

            // Ensure skulls are alive
            _activeSkulls.RemoveAll(s => s == null);
            if (_activeSkulls.Count < _skullCount)
            {
                RebuildSkulls(player, manager);
            }
        }

        protected override void Fire(PlayerNecromancer player, PlayerSkillManager manager)
        {
            // Passive orbital weapon, no periodic fire needed
        }

        private void RebuildSkulls(PlayerNecromancer player, PlayerSkillManager manager)
        {
            if (player == null) return;

            // Clear old skulls
            foreach (var s in _activeSkulls)
            {
                if (s != null) UnityEngine.Object.Destroy(s.gameObject);
            }
            _activeSkulls.Clear();

            float dmg = CalculateDamage(player, manager, 1f + _damageBonus);
            float angleStep = 360f / _skullCount;

            for (int i = 0; i < _skullCount; i++)
            {
                float angle = i * angleStep;
                GameObject skullObj = new GameObject($"OrbitingSkull_{i}");
                var comp = skullObj.AddComponent<OrbitingSkullEntity>();
                comp.Initialize(player.transform, angle, _orbitRadius, _orbitSpeed, dmg, player);
                _activeSkulls.Add(comp);
            }
        }

        public override void ResetSkill()
        {
            base.ResetSkill();
            foreach (var s in _activeSkulls)
            {
                if (s != null) UnityEngine.Object.Destroy(s.gameObject);
            }
            _activeSkulls.Clear();
        }
    }
}

