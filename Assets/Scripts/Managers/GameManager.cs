using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using NecroDefense.Base;
using NecroDefense.Characters.Player;
using NecroDefense.Characters.Enemies;
using NecroDefense.Characters.Minions;
using NecroDefense.Core;

namespace NecroDefense.Managers
{
    public enum GameState
    {
        Playing,
        GameOver,
        Victory
    }

    /// <summary>
    /// Master Game Manager & Central Control Panel.
    /// Every gameplay variable (Player, Citadel, Enemies, Minions, Waves, Pacing)
    /// can be tuned directly in this single Inspector component!
    /// Demonstrates Singleton Pattern, Facade Pattern, and Observer Pattern.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("=== ⚙️ MASTER CONTROL PANEL ===")]
        [Tooltip("If checked, all stats configured below in GameManager will override scene components at runtime")]
        [SerializeField] private bool overrideAllSettings = true;

        [Header("=== 1. SCENE REFERENCES ===")]
        [SerializeField] private BaseCrystal baseCrystal;
        [SerializeField] private PlayerNecromancer player;
        [SerializeField] private WaveSpawner waveSpawner;

        [Header("=== 2. PLAYER NECROMANCER ===")]
        [Tooltip("Maximum health pool of the Necromancer")]
        [SerializeField] private float playerMaxHealth = 100f;
        [Tooltip("Move speed in units per second")]
        [SerializeField] private float playerMoveSpeed = 3.5f;
        [Tooltip("Dark Bolt spell base damage")]
        [SerializeField] private float playerBoltPower = 15f;
        [Tooltip("Attack cooldown interval (seconds between Dark Bolt casts)")]
        [SerializeField] private float playerAttackInterval = 0.8f;
        [Tooltip("Range within which Dark Bolt targets enemies")]
        [SerializeField] private float playerSpellRange = 8.5f;
        [Tooltip("Maximum soul essence capacity")]
        [SerializeField] private int playerMaxSouls = 100;
        [Tooltip("Initial soul essence at start")]
        [SerializeField] private int playerStartingSouls = 20;
        [Tooltip("Radius around Necromancer to resurrect fallen corpses")]
        [SerializeField] private float playerResurrectRadius = 5f;
        [Tooltip("Size scale of the Dark Bolt magical ball")]
        [SerializeField] private float playerBoltScale = 1.3f;

        [Header("=== 3. CITADEL CORE (DEFENSIVE BASE) ===")]
        [Tooltip("Citadel Core hit points")]
        [SerializeField] private float citadelMaxHealth = 1000f;
        [Tooltip("Interval between defensive healing pulses to allied minions (seconds)")]
        [SerializeField] private float citadelPulseInterval = 5f;
        [Tooltip("Health points restored to nearby allied minions per pulse")]
        [SerializeField] private float citadelPulseHeal = 5f;
        [Tooltip("Radius of Citadel defensive aura affecting minions")]
        [SerializeField] private float citadelAuraRadius = 6f;

        [Header("=== 4. ENEMY: ZOMBIE (MELEE SWARM) ===")]
        [SerializeField] private float zombieMaxHealth = 40f;
        [SerializeField] private float zombieMoveSpeed = 1.8f;
        [SerializeField] private float zombieAttackPower = 8f;
        [SerializeField] private float zombieAttackRange = 1.4f;
        [SerializeField] private float zombieAttackInterval = 1.0f;
        [SerializeField] private int zombieSoulYield = 2;
        // (ตัด zombieResurrectCost ออก เพื่อให้ดึงค่าจาก Inspector ของตัวศัตรูโดยตรง ไม่โดนทับ)

        [Header("=== 5. ENEMY: SKELETON ARCHER (RANGED) ===")]
        [SerializeField] private float archerMaxHealth = 30f;
        [SerializeField] private float archerMoveSpeed = 1.4f;
        [SerializeField] private float archerAttackPower = 7f;
        [SerializeField] private float archerAttackRange = 6.5f;
        [SerializeField] private float archerAttackInterval = 1.3f;
        [SerializeField] private int archerSoulYield = 3;

        [Header("=== 6. MINION: ALLIED ZOMBIE (MELEE GUARDIAN) ===")]
        [SerializeField] private float minionZombieMaxHealth = 60f;
        [SerializeField] private float minionZombieMoveSpeed = 2.2f;
        [SerializeField] private float minionZombieAttackPower = 12f;
        [SerializeField] private float minionZombieAttackRange = 1.4f;
        [SerializeField] private float minionZombieAttackInterval = 0.9f;

        [Header("=== 7. MINION: ALLIED ARCHER (RANGED MARKSMAN) ===")]
        [SerializeField] private float minionArcherMaxHealth = 45f;
        [SerializeField] private float minionArcherMoveSpeed = 1.6f;
        [SerializeField] private float minionArcherAttackPower = 10f;
        [SerializeField] private float minionArcherAttackRange = 6.5f;
        [SerializeField] private float minionArcherAttackInterval = 1.1f;

        [Header("=== 8. MINION DIRECTIVES & AI PERIMETER ===")]
        [Tooltip("Patrol radius around Citadel when minion order is Defend Citadel")]
        [SerializeField] private float minionDefendRadius = 5f;
        [Tooltip("Max leash distance before returning when following Necromancer")]
        [SerializeField] private float minionFollowLeash = 3f;
        [Tooltip("Detection radius for hunting enemy monsters")]
        [SerializeField] private float minionDetectionRadius = 12f;

        [Header("=== 9. WAVE & ARENA PERIMETER ===")]
        [Tooltip("Distance from Citadel at which enemies spawn")]
        [SerializeField] private float waveSpawnRadius = 18f;
        [Tooltip("Pause duration between waves (seconds)")]
        [SerializeField] private float timeBetweenWaves = 8f;
        [Tooltip("Prep delay before the first wave starts")]
        [SerializeField] private float initialWaveDelay = 2f;
        [Tooltip("Continue infinite procedural scaling waves after wave list")]
        [SerializeField] private bool infiniteProceduralWaves = true;

        [Header("=== 10. GAME OVER & PACING ===")]
        [Tooltip("Time scale applied on Game Over (0=pause, 1=normal). Dramatic slow-mo effect.")]
        [SerializeField] private float gameOverSlowdownScale = 0.2f;
        [Tooltip("Time scale applied on Victory")]
        [SerializeField] private float victorySlowdownScale = 0.5f;
        [Tooltip("Delay in seconds before triggering Game Over after Citadel falls")]
        [SerializeField] private float gameOverDelay = 0f;

        private GameState _currentState = GameState.Playing;
        private int _totalKills = 0;
        private float _survivalTimer = 0f;

        public bool OverrideAllSettings => overrideAllSettings;
        public bool IsGameActive => _currentState == GameState.Playing;
        public int TotalKills => _totalKills;
        public float SurvivalTimer => _survivalTimer;
        public GameState CurrentState => _currentState;

        public event Action<GameState, string> OnGameStateChanged;
        public event Action<int> OnKillCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (baseCrystal == null) baseCrystal = FindAnyObjectByType<BaseCrystal>();
            if (player == null) player = FindAnyObjectByType<PlayerNecromancer>();
            if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();

            if (baseCrystal != null) baseCrystal.OnDied += HandleBaseDestroyed;
            if (player != null) player.OnDied += HandlePlayerDied;

            ApplyAllSettings();
        }

        private void Update()
        {
            if (IsGameActive)
            {
                _survivalTimer += Time.deltaTime;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && overrideAllSettings)
            {
                ApplyAllSettings();
                UpdateExistingUnits();
            }
        }

        [ContextMenu("⚡ Apply All Settings To Scene Objects Now")]
        public void ApplyAllSettingsToHierarchy()
        {
            if (baseCrystal == null) baseCrystal = FindAnyObjectByType<BaseCrystal>();
            if (player == null) player = FindAnyObjectByType<PlayerNecromancer>();
            if (waveSpawner == null) waveSpawner = FindAnyObjectByType<WaveSpawner>();

            ApplyAllSettings();

            if (player != null) UnityEditor.EditorUtility.SetDirty(player);
            if (baseCrystal != null) UnityEditor.EditorUtility.SetDirty(baseCrystal);
            if (waveSpawner != null) UnityEditor.EditorUtility.SetDirty(waveSpawner);
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log("<color=green>[GameManager]</color> Successfully applied all master settings to scene objects!");
        }
#endif

        public void UpdateExistingUnits()
        {
            if (!Application.isPlaying || !overrideAllSettings) return;

            var enemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsAlive)
                {
                    ApplyEnemyStats(enemy, enemy.EnemyType);
                }
            }

            var minions = FindObjectsByType<MinionUnit>(FindObjectsSortMode.None);
            foreach (var minion in minions)
            {
                if (minion != null && minion.IsAlive)
                {
                    EnemyType type = (minion is ZombieMinion) ? EnemyType.Zombie : EnemyType.SkeletonArcher;
                    ApplyMinionStats(minion, type);
                }
            }
        }

        public void ApplyAllSettings()
        {
            if (!overrideAllSettings) return;

            if (player != null)
            {
                player.ConfigureStats(
                    playerMaxHealth,
                    playerMoveSpeed,
                    playerBoltPower,
                    playerAttackInterval,
                    playerSpellRange,
                    playerMaxSouls,
                    playerStartingSouls,
                    playerResurrectRadius,
                    playerBoltScale
                );
            }

            if (baseCrystal != null)
            {
                baseCrystal.ConfigureStats(
                    citadelMaxHealth,
                    citadelPulseInterval,
                    citadelPulseHeal,
                    citadelAuraRadius
                );
            }

            if (waveSpawner != null)
            {
                waveSpawner.ConfigureStats(
                    waveSpawnRadius,
                    timeBetweenWaves,
                    initialWaveDelay,
                    infiniteProceduralWaves
                );
            }
        }

        public void ApplyEnemyStats(EnemyUnit enemy, EnemyType type)
        {
            if (!overrideAllSettings || enemy == null) return;

            switch (type)
            {
                case EnemyType.Zombie:
                    enemy.SetMaxHealth(zombieMaxHealth);
                    enemy.SetMoveSpeed(zombieMoveSpeed);
                    enemy.SetAttackPower(zombieAttackPower);
                    enemy.SetAttackRange(zombieAttackRange);
                    enemy.SetAttackInterval(zombieAttackInterval);
                    // ตัด SetSoulParameters ออก ปล่อยให้ศัตรูใช้ค่า Resurrection Cost จากหน้า Inspector ของตัวเองโดยตรง
                    break;

                case EnemyType.SkeletonArcher:
                    enemy.SetMaxHealth(archerMaxHealth);
                    enemy.SetMoveSpeed(archerMoveSpeed);
                    enemy.SetAttackPower(archerAttackPower);
                    enemy.SetAttackRange(archerAttackRange);
                    enemy.SetAttackInterval(archerAttackInterval);
                    break;
            }
        }

        public void ApplyMinionStats(MinionUnit minion, EnemyType type)
        {
            if (!overrideAllSettings || minion == null) return;

            minion.ConfigureAIPerimeter(minionDefendRadius, minionFollowLeash, minionDetectionRadius);

            switch (type)
            {
                case EnemyType.Zombie:
                    minion.SetMaxHealth(minionZombieMaxHealth);
                    minion.SetMoveSpeed(minionZombieMoveSpeed);
                    minion.SetAttackPower(minionZombieAttackPower);
                    minion.SetAttackRange(minionZombieAttackRange);
                    minion.SetAttackInterval(minionZombieAttackInterval);
                    break;

                case EnemyType.SkeletonArcher:
                    minion.SetMaxHealth(minionArcherMaxHealth);
                    minion.SetMoveSpeed(minionArcherMoveSpeed);
                    minion.SetAttackPower(minionArcherAttackPower);
                    minion.SetAttackRange(minionArcherAttackRange);
                    minion.SetAttackInterval(minionArcherAttackInterval);
                    break;
            }
        }

        public void RegisterKill()
        {
            _totalKills++;
            OnKillCountChanged?.Invoke(_totalKills);
        }

        private void HandleBaseDestroyed()
        {
            SetGameOver("The Necrotic Citadel was destroyed!");
        }

        private void HandlePlayerDied()
        {
            SetGameOver("The Necromancer has perished!");
        }

        public void SetGameOver(string reason)
        {
            if (!IsGameActive) return;

            if (gameOverDelay > 0f)
            {
                StartCoroutine(DelayedGameOver(reason));
            }
            else
            {
                TriggerGameOver(reason);
            }
        }

        private System.Collections.IEnumerator DelayedGameOver(string reason)
        {
            yield return new WaitForSecondsRealtime(gameOverDelay);
            TriggerGameOver(reason);
        }

        private void TriggerGameOver(string reason)
        {
            _currentState = GameState.GameOver;
            OnGameStateChanged?.Invoke(GameState.GameOver, reason);
            Time.timeScale = gameOverSlowdownScale;
        }

        public void SetVictory(string reason)
        {
            if (!IsGameActive) return;

            _currentState = GameState.Victory;
            OnGameStateChanged?.Invoke(GameState.Victory, reason);
            Time.timeScale = victorySlowdownScale;
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}