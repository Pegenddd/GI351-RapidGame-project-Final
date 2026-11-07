using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>หน้าต่างจบเกม (ชนะ / แพ้)</summary>
    public class GameOverPanelUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text detailText;
        [SerializeField] private Button restartButton;
        [SerializeField] private string victoryTitle = "VICTORY";
        [SerializeField] private string defeatTitle = "DEFEAT";

        private void Awake()
        {
            if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
        }

        public void Show(bool victory)
        {
            gameObject.SetActive(true);
            GameManager gm = GameManager.Instance;
            if (titleText != null) titleText.text = victory ? victoryTitle : defeatTitle;
            if (detailText != null)
            {
                string reason = victory ? "All " + gm.Waves.TotalWaves + " waves cleared!" : gm.DefeatReason;
                detailText.text = reason + "\nReached wave " + gm.Waves.CurrentWaveNumber + "  |  Level " + gm.LevelUp.Level;
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
