using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>กลางบน: เวฟปัจจุบัน / นับถอยหลัง / ปุ่มเริ่มเวฟทันที</summary>
    public class WaveUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button startWaveButton;

        private void Awake()
        {
            if (startWaveButton != null)
                startWaveButton.onClick.AddListener(() => { if (GameManager.Instance != null) GameManager.Instance.Waves.StartNextWave(); });
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            WaveManager w = gm.Waves;

            if (waveText != null) waveText.text = "Wave " + Mathf.Max(w.CurrentWaveNumber, 0) + " / " + w.TotalWaves;

            if (statusText != null)
            {
                if (w.IsWaveActive) statusText.text = "Enemies: " + Registry.Enemies.Count;
                else if (w.IsCountingDown) statusText.text = "Next wave in " + Mathf.CeilToInt(w.Countdown) + "s";
                else statusText.text = "";
            }

            if (startWaveButton != null && startWaveButton.gameObject.activeSelf != w.IsCountingDown)
                startWaveButton.gameObject.SetActive(w.IsCountingDown);
        }
    }
}
