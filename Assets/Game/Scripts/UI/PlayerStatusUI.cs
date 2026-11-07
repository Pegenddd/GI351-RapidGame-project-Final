using TMPro;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>มุมซ้ายบน: HP / Mana / XP / Level ของผู้เล่น</summary>
    public class PlayerStatusUI : MonoBehaviour
    {
        [SerializeField] private BarUI healthBar;
        [SerializeField] private BarUI manaBar;
        [SerializeField] private BarUI xpBar;
        [SerializeField] private TMP_Text levelText;

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.Player != null && healthBar != null) healthBar.SetValue(gm.Player.CurrentHealth, gm.Player.MaxHealth);
            if (manaBar != null) manaBar.SetValue(gm.Economy.Mana, gm.Economy.MaxMana);

            LevelUpManager lv = gm.LevelUp;
            if (xpBar != null)
            {
                if (lv.IsMaxLevel) xpBar.SetValue(1f, 1f);
                else xpBar.SetValue(lv.CurrentXp, lv.XpToNextLevel);
            }
            if (levelText != null) levelText.text = lv.IsMaxLevel ? "Lv MAX" : "Lv " + lv.Level;
        }
    }
}
