using System;
using System.Collections.Generic;
using UnityEngine;
using NecroDefense.Characters.Player;
using NecroDefense.Skills.Active;
using NecroDefense.Skills.Passive;

namespace NecroDefense.Skills
{
    /// <summary>
    /// Master Skill & Arsenal Manager for the Player Necromancer.
    /// Manages all Active Auto-Weapons, Passive Upgrades, and Stat Modifiers.
    /// Demonstrates Facade Pattern, Strategy Pattern, and Modular OOP Design.
    /// </summary>
    public class PlayerSkillManager : MonoBehaviour
    {
        private PlayerNecromancer _player;
        private readonly List<SkillBase> _allSkills = new List<SkillBase>();
        private readonly Dictionary<string, SkillBase> _skillsById = new Dictionary<string, SkillBase>();

        // Stat Modifiers
        public float DamageMultiplier { get; private set; } = 1.0f;
        public float CooldownMultiplier { get; private set; } = 1.0f;
        public float MoveSpeedBonus { get; private set; } = 0f;
        public float MagnetRadiusBonus { get; private set; } = 0f;
        public float MinionSpeedMultiplier { get; private set; } = 1.0f;
        public float MinionAttackSpeedMultiplier { get; private set; } = 1.0f;
        public int TowerPierceBonus { get; private set; } = 0;
        public float SoulCostDiscount { get; private set; } = 0f;

        private float _basePlayerMoveSpeed = 3.5f;

        public IReadOnlyList<SkillBase> AllSkills => _allSkills;

        public event Action<SkillBase, int> OnSkillUpgraded;

        private void Awake()
        {
            _player = GetComponent<PlayerNecromancer>();
            RegisterAllSkills();
        }

        private void Start()
        {
            if (_player != null)
            {
                _basePlayerMoveSpeed = _player.MoveSpeed;
            }

            // Start with Magic Bolt Lv.1 unlocked so the player can fight immediately!
            if (_skillsById.TryGetValue("magic_bolt", out var magicBolt) && !magicBolt.IsUnlocked)
            {
                magicBolt.Upgrade(_player, this);
            }
        }

        private void Update()
        {
            if (_player == null || !_player.IsAlive) return;

            // Only update skills when game is not paused
            if (Time.timeScale <= 0f) return;

            float dt = Time.deltaTime;
            for (int i = 0; i < _allSkills.Count; i++)
            {
                var skill = _allSkills[i];
                if (skill.IsUnlocked)
                {
                    skill.UpdateSkill(dt, _player, this);
                }
            }
        }

        private void RegisterAllSkills()
        {
            _allSkills.Clear();
            _skillsById.Clear();

            // 1. กลุ่มสกิลอัตโนมัติ (Active Skills)
            AddSkill(new MagicBoltSkill());
            AddSkill(new BoneScytheSkill());
            AddSkill(new ShadowSpikesSkill());
            AddSkill(new SkullOrbitSkill());
            AddSkill(new DeathLaserSkill());
            AddSkill(new DarkMistPoolSkill());

            // 2. กลุ่มสกิลติดตัวเสริมพลัง (Passive Skills)
            AddSkill(new ManaMagnetSkill());
            AddSkill(new HasteScrollSkill());
            AddSkill(new SwiftBootsSkill());
            AddSkill(new GiantsHeartSkill());
            AddSkill(new CastleBlueprintSkill());
            AddSkill(new ArcaneFocusSkill());

            // 3. กลุ่มสกิลอื่นๆ (Special Passives)
            AddSkill(new MinionFrenzySkill());
            AddSkill(new TowerAmplifierSkill());
            AddSkill(new SoulEfficiencySkill());
        }

        private void AddSkill(SkillBase skill)
        {
            _allSkills.Add(skill);
            _skillsById[skill.SkillId] = skill;
        }

        public SkillBase GetSkill(string id)
        {
            _skillsById.TryGetValue(id, out var skill);
            return skill;
        }

        /// <summary>
        /// Selects random upgrade options from skills that haven't reached max level yet.
        /// </summary>
        public List<SkillBase> GetRandomUpgradeChoices(int count = 3)
        {
            List<SkillBase> available = new List<SkillBase>();
            foreach (var s in _allSkills)
            {
                if (!s.IsMaxLevel)
                {
                    available.Add(s);
                }
            }

            // Shuffle available
            for (int i = 0; i < available.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, available.Count);
                var temp = available[i];
                available[i] = available[r];
                available[r] = temp;
            }

            int pickCount = Mathf.Min(count, available.Count);
            return available.GetRange(0, pickCount);
        }

        /// <summary>
        /// Applies the chosen upgrade and updates player statistics.
        /// </summary>
        public void ApplyUpgrade(SkillBase skill)
        {
            if (skill == null || skill.IsMaxLevel) return;

            skill.Upgrade(_player, this);
            OnSkillUpgraded?.Invoke(skill, skill.CurrentLevel);
        }

        // --- Setter callbacks from passives ---

        public void SetDamageMultiplier(float mult)
        {
            DamageMultiplier = Mathf.Max(0.1f, mult);
        }

        public void SetCooldownReduction(float reduction)
        {
            CooldownMultiplier = Mathf.Clamp(1.0f - reduction, 0.2f, 1.0f);
        }

        public void SetMoveSpeedBonus(float bonus)
        {
            MoveSpeedBonus = bonus;
            if (_player != null)
            {
                _player.SetMoveSpeed(_basePlayerMoveSpeed * (1f + MoveSpeedBonus));
            }
        }

        public void SetMagnetRadiusBonus(float bonus)
        {
            MagnetRadiusBonus = bonus;
        }

        public void SetMinionFrenzy(float speedMult, float attackMult)
        {
            MinionSpeedMultiplier = speedMult;
            MinionAttackSpeedMultiplier = attackMult;

            // Apply immediately to all live minions
            var minions = UnityEngine.Object.FindObjectsByType<Characters.Minions.MinionUnit>();
            foreach (var m in minions)
            {
                if (m != null && m.IsAlive)
                {
                    // Slightly increase minion base stats dynamically
                    m.SetMoveSpeed(m.MoveSpeed * speedMult);
                    m.SetAttackInterval(m.AttackInterval / attackMult);
                }
            }
        }

        public void SetTowerPierceBonus(int pierce)
        {
            TowerPierceBonus = pierce;
        }

        public void SetSoulCostDiscount(float discount)
        {
            SoulCostDiscount = Mathf.Clamp(discount, 0f, 0.5f);
        }

        public void ResetAllSkills()
        {
            DamageMultiplier = 1.0f;
            CooldownMultiplier = 1.0f;
            MoveSpeedBonus = 0f;
            MagnetRadiusBonus = 0f;
            MinionSpeedMultiplier = 1.0f;
            MinionAttackSpeedMultiplier = 1.0f;
            TowerPierceBonus = 0;
            SoulCostDiscount = 0f;

            foreach (var s in _allSkills)
            {
                s.ResetSkill();
            }

            // Reactivate Magic Bolt Lv.1
            if (_skillsById.TryGetValue("magic_bolt", out var magicBolt))
            {
                magicBolt.Upgrade(_player, this);
            }
        }
    }
}

