using System;
using System.Collections.Generic;
using UnityEngine;
using NecroDefense.Characters.Player;

namespace NecroDefense.Skills
{
    /// <summary>
    /// Vampire Survivors style EXP & Level Progression System.
    /// Handles Level, EXP acquisition, Magnet Radius, and Level-Up event dispatching.
    /// </summary>
    [RequireComponent(typeof(PlayerNecromancer), typeof(PlayerSkillManager))]
    public class PlayerProgression : MonoBehaviour
    {
        [Header("Progression Settings (Inspector)")]
        [SerializeField] private int startingLevel = 1;
        [SerializeField] private int baseExpRequirement = 5;
        [SerializeField] private float expGrowthRate = 1.35f;
        [SerializeField] private float baseMagnetRadius = 2.5f;

        private int _currentLevel;
        private int _currentExp;
        private int _expToNextLevel;
        private int _pendingLevelUps = 0;

        private PlayerNecromancer _player;
        private PlayerSkillManager _skillManager;

        public int CurrentLevel => _currentLevel;
        public int CurrentExp => _currentExp;
        public int ExpToNextLevel => _expToNextLevel;
        public bool IsLevelingUp => _pendingLevelUps > 0;

        public float MagnetRadius
        {
            get
            {
                float bonus = _skillManager != null ? _skillManager.MagnetRadiusBonus : 0f;
                return baseMagnetRadius * (1f + bonus);
            }
        }

        public event Action<int, int, int> OnExpChanged; // currentExp, expToNext, level
        public event Action<List<SkillBase>> OnShowLevelUpMenu;
        public event Action OnCloseLevelUpMenu;

        private void Awake()
        {
            _player = GetComponent<PlayerNecromancer>();
            _skillManager = GetComponent<PlayerSkillManager>();

            _currentLevel = startingLevel;
            _currentExp = 0;
            _expToNextLevel = CalculateExpRequirement(_currentLevel);
        }

        private void Start()
        {
            OnExpChanged?.Invoke(_currentExp, _expToNextLevel, _currentLevel);
        }

        public void AddExp(int amount)
        {
            if (amount <= 0) return;

            _currentExp += amount;

            while (_currentExp >= _expToNextLevel)
            {
                _currentExp -= _expToNextLevel;
                _currentLevel++;
                _expToNextLevel = CalculateExpRequirement(_currentLevel);
                _pendingLevelUps++;
            }

            OnExpChanged?.Invoke(_currentExp, _expToNextLevel, _currentLevel);

            if (_pendingLevelUps > 0 && Time.timeScale > 0f)
            {
                TriggerNextLevelUp();
            }
        }

        private int CalculateExpRequirement(int level)
        {
            return Mathf.RoundToInt(baseExpRequirement * Mathf.Pow(expGrowthRate, level - 1));
        }

        private void TriggerNextLevelUp()
        {
            var choices = _skillManager.GetRandomUpgradeChoices(3);
            if (choices == null || choices.Count == 0)
            {
                // All skills maxed out!
                _pendingLevelUps = 0;
                Time.timeScale = 1.0f;
                OnCloseLevelUpMenu?.Invoke();
                return;
            }

            // Pause the game for level-up selection
            Time.timeScale = 0f;
            OnShowLevelUpMenu?.Invoke(choices);
        }

        /// <summary>
        /// Called when the player clicks or presses 1/2/3 to choose an upgrade.
        /// </summary>
        public void SelectUpgrade(SkillBase chosenSkill)
        {
            if (chosenSkill != null && _skillManager != null)
            {
                _skillManager.ApplyUpgrade(chosenSkill);
            }

            _pendingLevelUps--;

            if (_pendingLevelUps > 0)
            {
                // Another level up waiting in queue
                TriggerNextLevelUp();
            }
            else
            {
                // Finished leveling up
                Time.timeScale = 1.0f;
                OnCloseLevelUpMenu?.Invoke();
            }
        }

        public void ResetProgression()
        {
            _currentLevel = startingLevel;
            _currentExp = 0;
            _pendingLevelUps = 0;
            _expToNextLevel = CalculateExpRequirement(_currentLevel);
            Time.timeScale = 1.0f;
            OnExpChanged?.Invoke(_currentExp, _expToNextLevel, _currentLevel);
        }
    }
}

