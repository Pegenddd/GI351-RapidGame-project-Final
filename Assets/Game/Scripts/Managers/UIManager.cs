using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ตัวกลางของ UI: เก็บอ้างอิงทุกแผงบน Canvas และเปิด/ปิดหน้าต่างตามสถานะเกม
    /// (ตัว UI จริงทั้งหมดอยู่ใต้ "Canvas_Gameplay" ใน Hierarchy ปรับตำแหน่ง/สี/ขนาดได้อิสระ)
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("HUD (แสดงตลอด)")]
        [SerializeField] private PlayerStatusUI playerStatus;
        [SerializeField] private ResourceUI resources;
        [SerializeField] private WaveUI wave;
        [SerializeField] private SkillBarUI skillBar;
        [SerializeField] private MinionBagUI minionBag;
        [SerializeField] private MessageUI message;
        [Tooltip("วิญญาณบินเข้ากระเป๋าตอนชุบสำเร็จ")]
        [SerializeField] private SoulFlyUI soulFly;

        [Header("Popups (ซ่อนไว้ก่อน)")]
        [Tooltip("เมนูวงกลมรอบแท่นป้อม")]
        [SerializeField] private TowerPanelUI towerPanel;
        [Tooltip("เมนูวงกลมรอบมินเนียนที่คลิก")]
        [SerializeField] private CommandPanelUI commandPanel;
        [SerializeField] private LevelUpPanelUI levelUpPanel;
        [SerializeField] private PausePanelUI pausePanel;
        [SerializeField] private GameOverPanelUI gameOverPanel;

        public PlayerStatusUI PlayerStatus => playerStatus;
        public ResourceUI Resources => resources;
        public WaveUI Wave => wave;
        public SkillBarUI SkillBar => skillBar;
        public MinionBagUI MinionBag => minionBag;
        public CommandPanelUI CommandPanel => commandPanel;
        public TowerPanelUI TowerPanel => towerPanel;
        public SoulFlyUI SoulFly => soulFly;
        public LevelUpPanelUI LevelUpPanel => levelUpPanel;

        private void Start()
        {
            if (towerPanel != null) towerPanel.Hide();
            if (commandPanel != null) commandPanel.Hide();
            if (levelUpPanel != null) levelUpPanel.Hide();
            if (pausePanel != null) pausePanel.Hide();
            if (gameOverPanel != null) gameOverPanel.Hide();
            if (GameManager.Instance != null) GameManager.Instance.StateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.StateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState state)
        {
            if (pausePanel != null)
            {
                if (state == GameState.Paused) pausePanel.Show();
                else pausePanel.Hide();
            }

            if (state == GameState.Victory || state == GameState.Defeat)
            {
                if (towerPanel != null) towerPanel.Hide();
                if (commandPanel != null) commandPanel.Hide();
                if (levelUpPanel != null) levelUpPanel.Hide();
                if (gameOverPanel != null) gameOverPanel.Show(state == GameState.Victory);
            }
        }

        public void ShowMessage(string text)
        {
            if (message != null) message.Show(text);
        }
    }
}
