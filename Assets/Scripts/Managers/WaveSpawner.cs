using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Factories;
using NecroDefense.Base;

namespace NecroDefense.Managers
{
    [System.Serializable]
    public struct WaveConfig
    {
        public int waveNumber;
        [Tooltip("Number of melee Zombie monsters in this wave")]
        public int zombieCount;
        [Tooltip("Number of ranged Skeleton Archer monsters in this wave")]
        public int archerCount;
        [Tooltip("Interval between individual spawns")]
        public float spawnInterval;
        [Tooltip("Health scaling multiplier for this wave")]
        public float healthMultiplier;
        [Tooltip("Damage scaling multiplier for this wave")]
        public float damageMultiplier;
    }

    /// <summary>
    /// Manages the spawning of enemy waves around the defensive arena.
    /// Demonstrates Observer Pattern via Wave Events.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Header("Spawn Perimeter")]
        [Tooltip("Distance from Citadel at which enemies spawn")]
        [SerializeField] private float spawnRadius = 18f;
        [Tooltip("Pause duration between waves in seconds")]
        [SerializeField] private float timeBetweenWaves = 8f;
        [Tooltip("Delay before the very first wave (gives player time to prepare)")]
        [SerializeField] private float initialWaveDelay = 2f;

        [Header("Wave Progression (Editable in Inspector)")]
        [Tooltip("If true, after all custom waves are beaten, infinite scaled waves continue")]
        [SerializeField] private bool infiniteProceduralWavesAfter = true;
        [Tooltip("Configure specific waves here, or leave empty for automatic procedural waves")]
        [SerializeField] private List<WaveConfig> customWaves = new List<WaveConfig>();

        private int _currentWaveIndex = 0;
        private bool _isSpawningWave = false;
        private int _activeEnemiesCount = 0;
        private BaseCrystal _baseCrystal;

        public int CurrentWave => _currentWaveIndex;
        public int ActiveEnemies => _activeEnemiesCount;
        public bool IsSpawningWave => _isSpawningWave;

        public void ConfigureStats(float radius, float wavePause, float initialDelay, bool infinite)
        {
            spawnRadius = radius;
            timeBetweenWaves = wavePause;
            initialWaveDelay = initialDelay;
            infiniteProceduralWavesAfter = infinite;
        }

        public event Action<int> OnWaveStarted;
        public event Action<int> OnWaveCompleted;

        private void Reset()
        {
            PopulateDefaultWaves();
        }

        public void PopulateDefaultWaves()
        {
            customWaves = new List<WaveConfig>
            {
                new WaveConfig { waveNumber = 1, zombieCount = 8,  archerCount = 0,  spawnInterval = 1.2f, healthMultiplier = 1.0f, damageMultiplier = 1.0f },
                new WaveConfig { waveNumber = 2, zombieCount = 12, archerCount = 4,  spawnInterval = 1.0f, healthMultiplier = 1.1f, damageMultiplier = 1.05f },
                new WaveConfig { waveNumber = 3, zombieCount = 18, archerCount = 6,  spawnInterval = 0.9f, healthMultiplier = 1.2f, damageMultiplier = 1.1f },
                new WaveConfig { waveNumber = 4, zombieCount = 24, archerCount = 10, spawnInterval = 0.8f, healthMultiplier = 1.35f, damageMultiplier = 1.2f },
                new WaveConfig { waveNumber = 5, zombieCount = 34, archerCount = 14, spawnInterval = 0.6f, healthMultiplier = 1.5f, damageMultiplier = 1.3f }
            };
        }

        private void Start()
        {
            _baseCrystal = UnityEngine.Object.FindAnyObjectByType<BaseCrystal>();
            StartCoroutine(WaveLoop());
        }

        private IEnumerator WaveLoop()
        {
            yield return new WaitForSeconds(initialWaveDelay); // Brief prep time

            while (GameManager.Instance == null || GameManager.Instance.IsGameActive)
            {
                if (!infiniteProceduralWavesAfter && _currentWaveIndex >= customWaves.Count)
                {
                    GameManager.Instance?.SetVictory("All waves repelled! The Citadel stands victorious!");
                    yield break;
                }

                _currentWaveIndex++;
                WaveConfig currentConfig = GetWaveConfig(_currentWaveIndex);

                OnWaveStarted?.Invoke(_currentWaveIndex);
                _isSpawningWave = true;

                yield return StartCoroutine(SpawnWaveRoutine(currentConfig));
                _isSpawningWave = false;

                // Wait until active enemies are cleared
                while (_activeEnemiesCount > 0)
                {
                    yield return new WaitForSeconds(0.5f);
                }

                OnWaveCompleted?.Invoke(_currentWaveIndex);
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        private IEnumerator SpawnWaveRoutine(WaveConfig config)
        {
            List<EnemyType> spawnQueue = new List<EnemyType>();

            // Strictly 2 enemy archetypes: Zombie (Melee Swarm) and SkeletonArcher (Ranged Attacker)
            for (int i = 0; i < config.zombieCount; i++) spawnQueue.Add(EnemyType.Zombie);
            for (int i = 0; i < config.archerCount; i++) spawnQueue.Add(EnemyType.SkeletonArcher);

            // Shuffle spawn queue
            for (int i = 0; i < spawnQueue.Count; i++)
            {
                int r = UnityEngine.Random.Range(i, spawnQueue.Count);
                var temp = spawnQueue[i];
                spawnQueue[i] = spawnQueue[r];
                spawnQueue[r] = temp;
            }

            foreach (var enemyType in spawnQueue)
            {
                SpawnEnemy(enemyType, config.healthMultiplier, config.damageMultiplier);
                yield return new WaitForSeconds(config.spawnInterval);
            }
        }

        private void SpawnEnemy(EnemyType type, float hpMult, float dmgMult)
        {
            Vector3 center = (_baseCrystal != null) ? _baseCrystal.Position : Vector3.zero;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 spawnPos = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * spawnRadius;

            if (EnemyFactory.Instance != null)
            {
                var enemy = EnemyFactory.Instance.CreateEnemy(type, spawnPos, hpMult, dmgMult);
                if (enemy != null)
                {
                    _activeEnemiesCount++;
                    enemy.OnDied += () =>
                    {
                        _activeEnemiesCount = Mathf.Max(0, _activeEnemiesCount - 1);
                        GameManager.Instance?.RegisterKill();
                    };
                }
            }
        }

        private WaveConfig GetWaveConfig(int waveNum)
        {
            // If defined in custom list, use it
            if (waveNum - 1 < customWaves.Count)
            {
                return customWaves[waveNum - 1];
            }

            // Otherwise procedural algorithmic scaling for infinite waves (Zombie + Archer only)
            return new WaveConfig
            {
                waveNumber = waveNum,
                zombieCount = 8 + (waveNum * 5),
                archerCount = Mathf.Max(0, (waveNum - 1) * 3),
                spawnInterval = Mathf.Max(0.25f, 1.2f - (waveNum * 0.05f)),
                healthMultiplier = 1f + (waveNum - 1) * 0.15f,
                damageMultiplier = 1f + (waveNum - 1) * 0.10f
            };
        }
    }
}

