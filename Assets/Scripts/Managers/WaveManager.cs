using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense.Managers
{
    [System.Serializable]
    public class SubWaveData
    {
        [Tooltip("Prefab ของมอนสเตอร์")]
        public GameObject monsterPrefab;
        [Tooltip("จำนวนตัวที่จะทยอยเกิดในกลุ่มนี้")]
        public int count = 3;
        [Tooltip("หน่วงเวลากี่วินาทีก่อนจะเกิดตัวถัดไป")]
        public float spawnInterval = 1.0f;
        [Tooltip("เวลารอก่อนไป Sub-Wave ถัดไปของเส้นทางนี้")]
        public float delayBeforeNextSubWave = 2.0f;
    }

    [System.Serializable]
    public class PathSpawnData
    {
        [Tooltip("ลาก GameObject แม่ของเส้นทางนี้ (Path Container) มาใส่")]
        public Transform pathContainer;
        [Tooltip("รายการกลุ่มย่อย (Sub-Waves) ที่จะให้ออกมาจากเส้นทางนี้")]
        public List<SubWaveData> subWaves = new List<SubWaveData>();
    }

    [System.Serializable]
    public class WaveData
    {
        [Tooltip("ชื่อเวฟ เช่น Wave 1: Single Lane, Wave 2: Dual Lane Attack")]
        public string waveName = "Wave 1";

        [Tooltip("ในเวฟนี้ จะให้มอนสเตอร์บุกมาจากเส้นทาง (Path Containers) ไหนบ้าง?")]
        public List<PathSpawnData> pathSpawns = new List<PathSpawnData>();

        [Tooltip("เวลารอก่อนขึ้นเวฟถัดไป (กรณีรอมอนสเตอร์ตายหมดแล้วขึ้นให้อัตโนมัติ)")]
        public float delayBeforeNextWave = 3.0f;
    }

    public class WaveManager : MonoBehaviour
    {
        [Header("=== TRUE KINGDOM RUSH HYBRID WAVE CONFIG ===")]
        [SerializeField] private List<WaveData> allWaves = new List<WaveData>();

        private int _currentWaveIndex = 0;
        private bool _isSpawning = false;
        private int _activeEnemiesCount = 0;
        private bool _forceNextWave = false;
        private bool _waveSpawnCompleted = false; // เช็กว่ามอนสเตอร์ในเวฟนี้สปอนออกครบทุกตัวแล้วหรือยัง

        public int CurrentWave => _currentWaveIndex + 1;
        public int TotalWaves => allWaves.Count;
        public bool IsWaveActive => _isSpawning;

        // ปุ่ม Skip จะเปิดใช้งานได้ก็ต่อเมื่อ กำลังเล่นอยู่, สปอนมอนสเตอร์ครบแล้ว, และยังไม่กด Skip ซ้ำ
        public bool CanSkipWave => _isSpawning && _waveSpawnCompleted && !_forceNextWave && _currentWaveIndex < allWaves.Count;

        private void Start()
        {
            if (allWaves != null && allWaves.Count > 0)
            {
                StartCoroutine(RunWaveSequence());
            }
        }

        public void PopulateDefaultWaves()
        {
            if (allWaves == null) allWaves = new List<WaveData>();

            if (allWaves.Count == 0)
            {
                WaveData defaultWave = new WaveData { waveName = "Wave 1" };
                PathSpawnData defaultPath = new PathSpawnData();
                defaultPath.subWaves.Add(new SubWaveData());
                defaultWave.pathSpawns.Add(defaultPath);
                allWaves.Add(defaultWave);
            }
        }

        private IEnumerator RunWaveSequence()
        {
            _isSpawning = true;

            while (_currentWaveIndex < allWaves.Count)
            {
                WaveData currentWave = allWaves[_currentWaveIndex];
                Debug.Log($"<color=cyan>[WaveManager]</color> Starting <b>{currentWave.waveName}</b> (Wave {CurrentWave}/{TotalWaves})");

                _forceNextWave = false;
                _waveSpawnCompleted = false; // เริ่มเวฟใหม่ รีเซ็ตสถานะการสปอน

                List<Coroutine> pathCoroutines = new List<Coroutine>();

                if (currentWave.pathSpawns != null && currentWave.pathSpawns.Count > 0)
                {
                    foreach (var pathSpawn in currentWave.pathSpawns)
                    {
                        if (pathSpawn.pathContainer != null && pathSpawn.subWaves.Count > 0)
                        {
                            Coroutine c = StartCoroutine(ProcessPathSpawn(pathSpawn));
                            pathCoroutines.Add(c);
                        }
                    }
                }

                // รอจนกว่ามอนสเตอร์ของเวฟนี้จะสปอนออกครบทุกตัว
                foreach (var c in pathCoroutines)
                {
                    yield return c;
                }

                // สปอนครบทุกตัวแล้ว เปิดธงให้ปุ่ม Skip ใน HUD เด้งขึ้นมาได้
                _waveSpawnCompleted = true;
                Debug.Log($"<color=green>[WaveManager]</color> {currentWave.waveName} spawned completely! Skip button is now available.");

                // เงื่อนไขจบเวฟเพื่อไปต่อ: 
                // 1. ผู้เล่นกดปุ่ม Skip (_forceNextWave กลายเป็น true) OR 
                // 2. มอนสเตอร์ในฉากตายจนหมดเกลี้ยง (_activeEnemiesCount <= 0)
                yield return new WaitUntil(() => _forceNextWave || _activeEnemiesCount <= 0);

                if (_forceNextWave)
                {
                    Debug.Log("<color=yellow>[WaveManager]</color> Player pressed Skip Wave! Forcing next wave immediately.");
                }
                else
                {
                    Debug.Log($"<color=yellow>[WaveManager]</color> All enemies defeated! Auto-advancing in {currentWave.delayBeforeNextWave} seconds...");
                    yield return new WaitForSeconds(currentWave.delayBeforeNextWave);
                }

                _currentWaveIndex++;
            }

            _isSpawning = false;
            _waveSpawnCompleted = false;
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetVictory("All waves survived!");
            }
        }

        /// <summary>
        /// ฟังก์ชันสำหรับให้ปุ่ม Skip บนหน้าจอเรียกใช้งาน เพื่อข้ามไปเวฟถัดไปทันที
        /// </summary>
        public void SkipToNextWave()
        {
            if (CanSkipWave)
            {
                Debug.Log("<color=yellow>[WaveManager]</color> Skip Wave triggered by player!");
                _forceNextWave = true;
            }
        }

        private IEnumerator ProcessPathSpawn(PathSpawnData pathSpawn)
        {
            Transform container = pathSpawn.pathContainer;
            int childCount = container.childCount;
            Transform[] waypoints = new Transform[childCount];
            for (int i = 0; i < childCount; i++)
            {
                waypoints[i] = container.GetChild(i);
            }

            foreach (var subWave in pathSpawn.subWaves)
            {
                if (subWave.monsterPrefab == null) continue;

                yield return new WaitForSeconds(subWave.spawnInterval);

                for (int i = 0; i < subWave.count; i++)
                {
                    SpawnMonster(subWave.monsterPrefab, waypoints);
                    yield return new WaitForSeconds(subWave.spawnInterval);
                }

                yield return new WaitForSeconds(subWave.delayBeforeNextSubWave);
            }
        }

        private void SpawnMonster(GameObject prefab, Transform[] waypoints)
        {
            Vector3 spawnPos = (waypoints != null && waypoints.Length > 0)
                ? waypoints[0].position
                : transform.position;

            GameObject monsterObj = Instantiate(prefab, spawnPos, Quaternion.identity);
            _activeEnemiesCount++;

            var pathController = monsterObj.GetComponent<Enemies.EnemyPathController>();
            if (pathController != null && waypoints != null)
            {
                pathController.SetWaypoints(waypoints);
            }

            StartCoroutine(TrackEnemyLife(monsterObj));
        }

        private IEnumerator TrackEnemyLife(GameObject monsterObj)
        {
            while (monsterObj != null)
            {
                yield return null;
            }
            _activeEnemiesCount = Mathf.Max(0, _activeEnemiesCount - 1);
        }
    }
}