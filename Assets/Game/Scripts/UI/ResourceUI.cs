using TMPro;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>มุมขวาบน: Souls + HP ฐานหลัก + เวลาฮีลครั้งถัดไป</summary>
    public class ResourceUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text soulsText;
        [SerializeField] private BarUI crystalBar;
        [SerializeField] private TMP_Text crystalHealText;

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            if (soulsText != null) soulsText.text = gm.Economy.Souls.ToString();
            if (gm.Crystal != null)
            {
                if (crystalBar != null) crystalBar.SetValue(gm.Crystal.CurrentHealth, gm.Crystal.MaxHealth);
                if (crystalHealText != null) crystalHealText.text = Mathf.CeilToInt(gm.Crystal.HealTimeRemaining) + "s";
            }
        }
    }
}
