using UnityEngine;
using UnityEngine.UI;
using NecroDefense.Base;
using NecroDefense.Characters.Player;
using NecroDefense.Managers;

namespace NecroDefense.UI
{
    public class GameHUDCanvas : MonoBehaviour
    {
        [Header("Player & World References")]
        [SerializeField] private PlayerNecromancer player;
        [SerializeField] private BaseCrystal baseCrystal;
        [SerializeField] private WaveManager waveManager;
        private Skills.PlayerProgression _progression;
        private System.Collections.Generic.List<Skills.SkillBase> _currentChoices;

        [Header("Top-Left Status UI Elements")]
        [SerializeField] private GameObject statusPanel;
        [SerializeField] private Slider citadelHpSlider;
        [SerializeField] private Text citadelHpText;
        [SerializeField] private Slider playerHpSlider;
        [SerializeField] private Text playerHpText;
        [SerializeField] private Slider soulsSlider;
        [SerializeField] private Text soulsText;
        [SerializeField] private Slider expSlider;
        [SerializeField] private Text expText;
        [SerializeField] private Text waveStatsText;

        [Header("Wave Control UI")]
        [Tooltip("ปุ่มสำหรับให้ผู้เล่นกดข้ามเวลาพักและเรียกเวฟถัดไป (Skip Wave)")]
        [SerializeField] private Button skipWaveButton;

        [Header("Game Over / Victory Panel")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private Text gameOverTitleText;
        [SerializeField] private Text gameOverStatsText;
        [SerializeField] private Button restartButton;

        [Header("Level Up Panel (Dynamic Generation - Dual Text)")]
        [SerializeField] private GameObject levelUpPanel;
        [SerializeField] private Text levelUpTitleText;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject skillCardPrefab;

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<PlayerNecromancer>();
            if (baseCrystal == null) baseCrystal = FindAnyObjectByType<BaseCrystal>();
            if (waveManager == null) waveManager = FindAnyObjectByType<WaveManager>();

            if (player != null)
            {
                _progression = player.GetComponent<Skills.PlayerProgression>();
                if (_progression != null)
                {
                    _progression.OnShowLevelUpMenu += choices => {
                        _currentChoices = choices;
                        ShowLevelUpUI(true);
                    };
                    _progression.OnCloseLevelUpMenu += () => {
                        _currentChoices = null;
                        ShowLevelUpUI(false);
                    };
                }
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(() => {
                    if (GameManager.Instance != null) GameManager.Instance.RestartGame();
                });
            }

            // ผูกฟังก์ชันกดปุ่ม Skip Wave เข้ากับ WaveManager โดยอัตโนมัติ
            if (skipWaveButton != null)
            {
                skipWaveButton.onClick.AddListener(() => {
                    if (waveManager != null)
                    {
                        waveManager.SkipToNextWave();
                    }
                });
            }

            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (levelUpPanel != null) levelUpPanel.SetActive(false);
        }

        private void Update()
        {
            UpdateStatusDisplay();
            CheckGameStates();
            UpdateSkipWaveButtonUI();
        }

        private void UpdateStatusDisplay()
        {
            if (baseCrystal != null)
            {
                float hp = baseCrystal.CurrentHealth;
                float max = baseCrystal.MaxHealth;
                if (citadelHpSlider != null) { citadelHpSlider.maxValue = max; citadelHpSlider.value = hp; }
                if (citadelHpText != null) citadelHpText.text = $"CITADEL HP: {(int)hp} / {(int)max}";
            }

            if (player != null)
            {
                float pHp = player.CurrentHealth;
                float pMax = player.MaxHealth;
                if (playerHpSlider != null) { playerHpSlider.maxValue = pMax; playerHpSlider.value = pHp; }
                if (playerHpText != null) playerHpText.text = $"MAGE HP: {(int)pHp} / {(int)pMax}";

                int souls = player.CurrentSouls;
                int maxSouls = player.MaxSouls;
                if (soulsSlider != null) { soulsSlider.maxValue = maxSouls; soulsSlider.value = souls; }
                if (soulsText != null) soulsText.text = $"SOULS: {souls} / {maxSouls}";

                if (_progression != null)
                {
                    int exp = _progression.CurrentExp;
                    int nextExp = _progression.ExpToNextLevel;
                    if (expSlider != null) { expSlider.maxValue = Mathf.Max(1, nextExp); expSlider.value = exp; }
                    if (expText != null) expText.text = $"LV.{_progression.CurrentLevel} EXP: {exp} / {nextExp}";
                }
            }

            int wave = waveManager != null ? waveManager.CurrentWave : 1;
            int kills = GameManager.Instance != null ? GameManager.Instance.TotalKills : 0;
            float time = GameManager.Instance != null ? GameManager.Instance.SurvivalTimer : 0f;
            if (waveStatsText != null)
            {
                waveStatsText.text = $"WAVE: {wave} | KILLS: {kills} | TIME: {time:F0}s";
            }
        }

        private void UpdateSkipWaveButtonUI()
        {
            if (skipWaveButton != null && waveManager != null)
            {
                // เปิดปุ่มให้กดได้เฉพาะช่วงเวลาที่เวฟสปอนเสร็จแล้วและกำลังรอให้ผู้เล่นกดข้าม (CanSkipWave)
                bool canSkip = waveManager.CanSkipWave;
                if (skipWaveButton.gameObject.activeSelf != canSkip)
                {
                    skipWaveButton.gameObject.SetActive(canSkip);
                }
            }
        }

        private void CheckGameStates()
        {
            if (GameManager.Instance != null)
            {
                bool isGameOver = GameManager.Instance.CurrentState == GameState.GameOver || GameManager.Instance.CurrentState == GameState.Victory;
                if (gameOverPanel != null && gameOverPanel.activeSelf != isGameOver)
                {
                    gameOverPanel.SetActive(isGameOver);
                    if (isGameOver)
                    {
                        bool isWin = GameManager.Instance.CurrentState == GameState.Victory;
                        if (gameOverTitleText != null) gameOverTitleText.text = isWin ? "[+] VICTORY [+]" : "[X] GAME OVER [X]";
                        int wave = waveManager != null ? waveManager.CurrentWave : 1;
                        int kills = GameManager.Instance.TotalKills;
                        if (gameOverStatsText != null) gameOverStatsText.text = $"Waves Survived: {wave} | Foes Slain: {kills}";
                    }
                }
            }
        }

        private void ShowLevelUpUI(bool show)
        {
            if (levelUpPanel != null) levelUpPanel.SetActive(show);

            if (show && _currentChoices != null)
            {
                if (levelUpTitleText != null && _progression != null)
                    levelUpTitleText.text = $"★ LEVEL UP! (LEVEL {_progression.CurrentLevel}) ★";

                if (cardContainer != null)
                {
                    foreach (Transform child in cardContainer)
                    {
                        Destroy(child.gameObject);
                    }
                }

                for (int i = 0; i < _currentChoices.Count; i++)
                {
                    if (skillCardPrefab == null || cardContainer == null) break;

                    GameObject cardObj = Instantiate(skillCardPrefab, cardContainer);
                    Button cardButton = cardObj.GetComponent<Button>();

                    Text[] texts = cardObj.GetComponentsInChildren<Text>();
                    Text titleText = null;
                    Text descText = null;

                    foreach (var t in texts)
                    {
                        if (t.gameObject.name.Contains("Title")) titleText = t;
                        else if (t.gameObject.name.Contains("Desc")) descText = t;
                    }

                    if (titleText == null && texts.Length > 0) titleText = texts[0];
                    if (descText == null && texts.Length > 1) descText = texts[1];

                    var choice = _currentChoices[i];
                    string status = choice.CurrentLevel == 0 ? "[NEW!]" : $"Lv.{choice.CurrentLevel} -> Lv.{choice.CurrentLevel + 1}";

                    if (titleText != null)
                    {
                        titleText.text = $"{choice.DisplayName} {status}";
                    }

                    if (descText != null)
                    {
                        descText.text = choice.GetNextLevelDescription();
                    }

                    int index = i;
                    if (cardButton != null)
                    {
                        cardButton.onClick.RemoveAllListeners();
                        cardButton.onClick.AddListener(() => {
                            if (_progression != null && _currentChoices != null && index < _currentChoices.Count)
                            {
                                _progression.SelectUpgrade(_currentChoices[index]);
                            }
                        });
                    }
                }
            }
        }
    }
}