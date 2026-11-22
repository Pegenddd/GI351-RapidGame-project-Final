using TMPro;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>มุมซ้ายบน: HP / Soul / XP / Level ของผู้เล่น</summary>
    public class PlayerStatusUI : MonoBehaviour
    {
        [SerializeField] private BarUI healthBar;
        [SerializeField] private BarUI soulBar;
        [SerializeField] private BarUI xpBar;
        [SerializeField] private TMP_Text levelText;
        [Tooltip("บังคับ override สีหลอด Soul จากค่า soulColor ด้านล่างนี้ (ถ้าติ๊กออก จะใช้สีที่ตั้งไว้ใน BarUI / Image ใน Hierarchy โดยตรง)")]
        [SerializeField] private bool overrideSoulColor = false;
        [Tooltip("สีหลอด Soul (ม่วง) - มีผลเฉพาะเมื่อติ๊ก overrideSoulColor")]
        [SerializeField] private Color soulColor = new Color(0.72f, 0.45f, 1f, 1f);

        private void Awake()
        {
            if (soulBar != null)
            {
                soulBar.SetTitle("Soul");
                if (overrideSoulColor)
                {
                    soulBar.SetBaseColor(soulColor);
                }
            }
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.Player != null && healthBar != null) healthBar.SetValue(gm.Player.CurrentHealth, gm.Player.MaxHealth);
            if (soulBar != null) soulBar.SetValue(gm.Economy.Soul, gm.Economy.MaxSoul);

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
