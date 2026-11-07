using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace NecroDefense
{
    /// <summary>
    /// ตัวควบคุมหลักของซีน Gameplay
    /// Manager ย่อยทั้งหมดเป็น "ลูก" ของ GameObject นี้ใน Hierarchy:
    ///   GameManager
    ///    ├ EconomyManager   (Souls / Mana)
    ///    ├ WaveManager      (เวฟศัตรู 12 เวฟ)
    ///    ├ MinionManager    (Minion Bag / อัญเชิญ / คำสั่ง)
    ///    ├ TowerManager     (สร้าง/อัปเกรด/ขายป้อม)
    ///    ├ LevelUpManager   (XP / การ์ดเลเวลอัป)
    ///    └ UIManager        (ตัวกลางคุม UI บน Canvas)
    /// สคริปต์อื่นเรียกใช้ผ่าน GameManager.Instance.Economy / .Waves / .Minions ...
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Sub Managers (ลูกของ GameManager)")]
        [SerializeField] private EconomyManager economy;
        [SerializeField] private WaveManager waves;
        [SerializeField] private MinionManager minions;
        [SerializeField] private TowerManager towers;
        [SerializeField] private LevelUpManager levelUp;
        [SerializeField] private UIManager ui;

        [Header("Scene References")]
        [SerializeField] private PlayerNecromancer player;
        [SerializeField] private Crystal crystal;
        [SerializeField] private MapBounds bounds;

        [Header("Runtime Folders (ของที่เกิดระหว่างเล่นจะไปอยู่ใต้พวกนี้)")]
        [SerializeField] private Transform enemiesFolder;
        [SerializeField] private Transform minionsFolder;
        [SerializeField] private Transform projectilesFolder;
        [SerializeField] private Transform effectsFolder;
        [SerializeField] private Transform pickupsFolder;

        [Header("Controls")]
        [SerializeField] private Key pauseKey = Key.Escape;

        public GameState State { get; private set; } = GameState.Playing;
        public event Action<GameState> StateChanged;

        public EconomyManager Economy => economy;
        public WaveManager Waves => waves;
        public MinionManager Minions => minions;
        public TowerManager Towers => towers;
        public LevelUpManager LevelUp => levelUp;
        public UIManager UI => ui;
        public PlayerNecromancer Player => player;
        public PlayerSkills PlayerSkills { get; private set; }
        public PlayerAutoAttack PlayerAttack { get; private set; }
        public Crystal Crystal => crystal;
        public MapBounds Bounds => bounds;

        /// <summary>true = กำลังเล่นอยู่ (ไม่ได้ Pause / เลือกการ์ด / จบเกม)</summary>
        public static bool IsGameplayRunning => Instance != null && Instance.State == GameState.Playing;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            if (player != null)
            {
                PlayerSkills = player.GetComponent<PlayerSkills>();
                PlayerAttack = player.GetComponent<PlayerAutoAttack>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Time.timeScale = 1f;
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb[pauseKey].wasPressedThisFrame)
            {
                // Esc ระหว่างเลือกที่วางมินเนียน = ยกเลิกการวาง
                if (State == GameState.Playing && minions != null && minions.IsPlacing) { minions.CancelPlacement(); return; }
                if (State == GameState.Playing) Pause();
                else if (State == GameState.Paused) Resume();
            }
        }

        /// <summary>Transform ที่ใช้เป็นพ่อของของที่ spawn ระหว่างเล่น (ให้ Hierarchy เป็นระเบียบ)</summary>
        public static Transform GetFolder(SpawnFolder folder)
        {
            if (Instance == null) return null;
            switch (folder)
            {
                case SpawnFolder.Enemies: return Instance.enemiesFolder;
                case SpawnFolder.Minions: return Instance.minionsFolder;
                case SpawnFolder.Projectiles: return Instance.projectilesFolder;
                case SpawnFolder.Effects: return Instance.effectsFolder;
                default: return Instance.pickupsFolder;
            }
        }

        // ---------------- State ----------------

        public void Pause() { if (State == GameState.Playing) SetState(GameState.Paused); }
        public void Resume() { if (State == GameState.Paused) SetState(GameState.Playing); }

        public void EnterLevelUp() { if (State == GameState.Playing) SetState(GameState.LevelUp); }
        public void ExitLevelUp() { if (State == GameState.LevelUp) SetState(GameState.Playing); }

        public void Victory()
        {
            if (State == GameState.Victory || State == GameState.Defeat) return;
            SetState(GameState.Victory);
        }

        public string DefeatReason { get; private set; } = "";

        public void Defeat(string reason)
        {
            if (State == GameState.Victory || State == GameState.Defeat) return;
            DefeatReason = reason;
            SetState(GameState.Defeat);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void SetState(GameState newState)
        {
            State = newState;
            Time.timeScale = newState == GameState.Playing ? 1f : 0f;
            StateChanged?.Invoke(newState);
        }
    }
}
