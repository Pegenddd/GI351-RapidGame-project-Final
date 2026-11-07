using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// XP + เลเวลอัปแบบ Roguelite: เลเวลอัป -> เกมหยุด -> สุ่มการ์ด 3 ใบให้เลือก 1
    /// การ์ดทั้งหมดอยู่ใน Assets/Game/Data/UpgradeCards (แก้ค่า/เพิ่มใบใหม่ได้)
    /// </summary>
    public class LevelUpManager : MonoBehaviour
    {
        [Header("XP Curve")]
        [Tooltip("เลเวลสูงสุด (GDD = 30)")]
        [SerializeField] private int maxLevel = 30;
        [Tooltip("XP ที่ต้องใช้จาก Lv1 -> Lv2")]
        [SerializeField] private int baseXpToLevel = 8;
        [Tooltip("XP ที่ต้องใช้เพิ่มขึ้นต่อเลเวล")]
        [SerializeField] private int xpIncreasePerLevel = 5;

        [Header("Cards")]
        [SerializeField] private List<UpgradeCard> cardPool = new List<UpgradeCard>();
        [SerializeField] private int choicesPerLevel = 3;

        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }
        public int XpToNextLevel => baseXpToLevel + (Level - 1) * xpIncreasePerLevel;
        public bool IsMaxLevel => Level >= maxLevel;
        public int MaxLevel => maxLevel;

        private readonly Dictionary<UpgradeCard, int> _stacks = new Dictionary<UpgradeCard, int>();
        private int _pendingLevelUps;
        private bool _choosing;

        public int GetStacks(UpgradeCard card) => card != null && _stacks.TryGetValue(card, out int s) ? s : 0;

        public IReadOnlyList<UpgradeCard> CardPool => cardPool;

        /// <summary>ซีนเทส: เลเวลอัปทันที 1 ครั้ง (เปิดหน้าเลือกการ์ด)</summary>
        public void DebugLevelUp()
        {
            if (IsMaxLevel) return;
            AddXp(Mathf.Max(1, XpToNextLevel - CurrentXp));
        }

        /// <summary>ซีนเทส: ได้การ์ดใบนี้ทันที (ไม่ต้องเลเวลอัป)</summary>
        public void DebugGiveCard(UpgradeCard card)
        {
            if (card == null) return;
            _stacks[card] = GetStacks(card) + 1;
            ApplyCard(card);
        }

        public void AddXp(int amount)
        {
            if (IsMaxLevel || amount <= 0) return;
            CurrentXp += amount;
            while (!IsMaxLevel && CurrentXp >= XpToNextLevel)
            {
                CurrentXp -= XpToNextLevel;
                Level++;
                _pendingLevelUps++;
            }
            if (IsMaxLevel) CurrentXp = 0;
            TryShowChoices();
        }

        private void TryShowChoices()
        {
            if (_choosing || _pendingLevelUps <= 0) return;
            if (!GameManager.IsGameplayRunning) return; // รอจนกลับมาเล่นต่อ

            List<UpgradeCard> choices = PickRandomCards(choicesPerLevel);
            if (choices.Count == 0) { _pendingLevelUps = 0; return; }

            _choosing = true;
            GameManager.Instance.EnterLevelUp();
            GameManager.Instance.UI.LevelUpPanel.Show(Level, choices, OnCardPicked);
        }

        private void Update()
        {
            // ถ้าเลเวลอัปตอน Pause อยู่ จะโชว์การ์ดเมื่อกลับมาเล่น
            if (!_choosing && _pendingLevelUps > 0) TryShowChoices();
        }

        private List<UpgradeCard> PickRandomCards(int count)
        {
            List<UpgradeCard> available = new List<UpgradeCard>();
            foreach (UpgradeCard c in cardPool)
                if (c != null && !c.effect.ToString().StartsWith("Removed") && (c.maxStacks <= 0 || GetStacks(c) < c.maxStacks)
                    && Level >= c.minLevel && (c.requires == null || GetStacks(c.requires) > 0))
                    available.Add(c);

            List<UpgradeCard> result = new List<UpgradeCard>();
            while (result.Count < count && available.Count > 0)
            {
                int i = Random.Range(0, available.Count);
                result.Add(available[i]);
                available.RemoveAt(i);
            }
            return result;
        }

        private void OnCardPicked(UpgradeCard card)
        {
            _stacks[card] = GetStacks(card) + 1;
            ApplyCard(card);
            _pendingLevelUps--;
            _choosing = false;
            GameManager.Instance.UI.LevelUpPanel.Hide();
            GameManager.Instance.ExitLevelUp();
            TryShowChoices();
        }

        /// <summary>ผลของการ์ดแต่ละแบบ (เพิ่มการ์ดชนิดใหม่ = เพิ่ม CardEffect + case ที่นี่)</summary>
        private void ApplyCard(UpgradeCard card)
        {
            GameManager gm = GameManager.Instance;
            switch (card.effect)
            {
                case CardEffect.NecromancyMastery: gm.PlayerSkills.AddRaiseChance(card.value1); break;
                case CardEffect.TeleportMastery: gm.PlayerSkills.ReduceTeleportCooldown(card.value1); break;
                case CardEffect.NovaBoost: gm.PlayerSkills.BoostNova(card.value1, card.value2); break;
                case CardEffect.OrbitalMagic: gm.PlayerAttack.UpgradeOrbital(Mathf.RoundToInt(card.value1), card.value2); break;
                case CardEffect.MinionPower: gm.Minions.AddDamagePercent(card.value1); break;
                case CardEffect.MinionHaste: gm.Minions.AddSpeedPercent(card.value1); break;
                case CardEffect.MinionVitality: gm.Minions.AddHealthPercent(card.value1); break;
                case CardEffect.ManaFont: gm.Economy.UpgradeMana(card.value1, card.value2); break;
                case CardEffect.BaseArchitect:
                    gm.Crystal.AddMaxHealth(card.value1);
                    gm.Crystal.AddArmor(card.value2);
                    break;
                case CardEffect.SwiftBoots: gm.Player.AddMoveSpeedPercent(card.value1); break;
                case CardEffect.SoulHarvester: gm.Towers.ApplyDiscount(card.value1); break;

                case CardEffect.RaiseRadius: gm.PlayerSkills.AddMassRaiseRadius(card.value1); break;
                case CardEffect.RaiseCooldown: gm.PlayerSkills.ReduceMassRaiseCooldown(card.value1); break;
                case CardEffect.ArmySize: gm.Minions.AddArmyCapacity(Mathf.RoundToInt(card.value1)); break;
            }
            gm.UI.ShowMessage("Picked: " + card.title);
        }
    }
}
