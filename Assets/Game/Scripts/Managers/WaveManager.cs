using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>กลุ่มศัตรู 1 กลุ่มในเวฟ (เช่น Slime 8 ตัว ออกทุก 1 วิ จากทิศเหนือ)</summary>
    [Serializable]
    public class SpawnGroup
    {
        public EnemyUnit enemyPrefab;
        [Min(1)] public int count = 5;
        [Tooltip("วินาทีระหว่างแต่ละตัว")]
        public float spawnInterval = 1f;
        [Tooltip("รอกี่วินาทีหลังเวฟเริ่มก่อนกลุ่มนี้จะออก")]
        public float startDelay = 0f;
        [Tooltip("ออกจากทิศไหน (AllSides = วนครบ 4 ทิศ)")]
        public SpawnSide side = SpawnSide.Random;
    }

    /// <summary>1 เวฟ = หลายกลุ่มที่ออกพร้อมกัน</summary>
    [Serializable]
    public class Wave
    {
        public string waveName = "Wave";
        public List<SpawnGroup> groups = new List<SpawnGroup>();
    }

    /// <summary>ข้อมูลเตือนล่วงหน้า: เวฟหน้าจะมีอะไรมาทางนี้กี่ตัว</summary>
    public class PathPreview
    {
        public EnemyPath path;
        public readonly Dictionary<EnemyUnit, int> counts = new Dictionary<EnemyUnit, int>();
        public int Total { get { int t = 0; foreach (var kv in counts) t += kv.Value; return t; } }
    }

    /// <summary>
    /// ควบคุมเวฟศัตรู: นับถอยหลัง -> ปล่อยศัตรู -> ฆ่าหมด -> นับถอยหลังเวฟถัดไป -> ครบทุกเวฟ = ชนะ
    /// แก้จำนวน/ชนิดศัตรูได้ในลิสต์ Waves ด้านล่าง
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        [Header("Spawn Paths (Map/EnemyPaths) - ว่างได้ถ้าแมพไม่มีทางนั้น")]
        [SerializeField] private EnemyPath pathNorth;
        [SerializeField] private EnemyPath pathEast;
        [SerializeField] private EnemyPath pathSouth;
        [SerializeField] private EnemyPath pathWest;
        [Tooltip("สุ่มกระจายรอบจุดเกิด")]
        [SerializeField] private float spawnScatter = 1.5f;

        [Header("Timing")]
        [Tooltip("นับถอยหลังก่อนเวฟแรก (วินาที)")]
        [SerializeField] private float firstWaveDelay = 10f;
        [Tooltip("พักระหว่างเวฟ (วินาที)")]
        [SerializeField] private float timeBetweenWaves = 12f;

        [Header("Difficulty Scaling (ศัตรูแข็งขึ้นทุกเวฟ)")]
        [Tooltip("ตัวคูณเลือดศัตรูพื้นฐาน (ทั้งเกม)")]
        [SerializeField] private float enemyHealthMultiplier = 1f;
        [Tooltip("ตัวคูณดาเมจศัตรูพื้นฐาน (ทั้งเกม)")]
        [SerializeField] private float enemyDamageMultiplier = 1f;
        [Tooltip("เลือดเพิ่มต่อเวฟ (0.12 = +12% ต่อเวฟ: เวฟ 1 = x1.00, เวฟ 12 = x2.32)")]
        [SerializeField] private float healthGrowthPerWave = 0.12f;
        [Tooltip("ดาเมจเพิ่มต่อเวฟ")]
        [SerializeField] private float damageGrowthPerWave = 0.12f;

        [Header("Waves (GDD: 12 เวฟ)")]
        [SerializeField] private List<Wave> waves = new List<Wave>();

        private int _currentIndex = -1;
        private bool _waveActive;
        private int _runningGroups;
        private float _countdown;
        private int _sideCycle;

        // แผนของเวฟถัดไป (ตัดสินใจทิศล่วงหน้า เพื่อโชว์เตือนผู้เล่นได้)
        private readonly List<List<EnemyPath>> _plannedGroupPaths = new List<List<EnemyPath>>();
        private readonly List<PathPreview> _preview = new List<PathPreview>();
        private int _plannedFor = -1;

        /// <summary>เวฟถัดไปจะมาทางไหนบ้าง (ใช้ตอนนับถอยหลัง)</summary>
        public IReadOnlyList<PathPreview> UpcomingPreview
        {
            get
            {
                EnsurePlan();
                return _preview;
            }
        }

        /// <summary>เวฟปัจจุบัน (1-based, 0 = ยังไม่เริ่ม)</summary>
        public int CurrentWaveNumber => _currentIndex + 1;
        public int TotalWaves => waves.Count;
        public bool IsWaveActive => _waveActive;
        public bool HasMoreWaves => _currentIndex + 1 < waves.Count;
        public float Countdown => Mathf.Max(0f, _countdown);
        public bool IsCountingDown => !_waveActive && HasMoreWaves;
        /// <summary>ตัวคูณเลือดศัตรูของเวฟปัจจุบัน</summary>
        /// <summary>ตัวคูณเลือด/ดาเมจถ้าเป็นเวฟที่ wave (1-based) - ใช้ในซีนเทส</summary>
        public float HealthMultiplierForWave(int wave) => enemyHealthMultiplier * (1f + healthGrowthPerWave * Mathf.Max(0, wave - 1));
        public float DamageMultiplierForWave(int wave) => enemyDamageMultiplier * (1f + damageGrowthPerWave * Mathf.Max(0, wave - 1));

        public float CurrentHealthMultiplier => enemyHealthMultiplier * (1f + healthGrowthPerWave * Mathf.Max(0, _currentIndex));
        /// <summary>ตัวคูณดาเมจศัตรูของเวฟปัจจุบัน</summary>
        public float CurrentDamageMultiplier => enemyDamageMultiplier * (1f + damageGrowthPerWave * Mathf.Max(0, _currentIndex));

        public string CurrentWaveName => _currentIndex >= 0 && _currentIndex < waves.Count ? waves[_currentIndex].waveName : "";

        private void Start() => _countdown = firstWaveDelay;

        private void Update()
        {
            if (!GameManager.IsGameplayRunning) return;

            if (_waveActive)
            {
                if (_runningGroups <= 0 && Registry.Enemies.Count == 0) OnWaveCleared();
            }
            else if (HasMoreWaves)
            {
                _countdown -= Time.deltaTime;
                if (_countdown <= 0f) StartNextWave();
            }
        }

        /// <summary>เริ่มเวฟถัดไปทันที (ปุ่ม "Start Wave" ใน UI เรียกอันนี้)</summary>
        public void StartNextWave()
        {
            if (_waveActive || !HasMoreWaves) return;
            EnsurePlan(); // ใช้ทิศที่วางแผน/เตือนไว้แล้ว
            _currentIndex++;
            _waveActive = true;
            _runningGroups = 0;

            Wave wave = waves[_currentIndex];
            for (int gi = 0; gi < wave.groups.Count; gi++)
            {
                SpawnGroup g = wave.groups[gi];
                if (g.enemyPrefab == null || g.count <= 0) continue;
                _runningGroups++;
                StartCoroutine(SpawnGroupRoutine(g, _plannedGroupPaths[gi]));
            }
            _plannedFor = -1;
            _preview.Clear();
            if (GameManager.Instance != null)
                GameManager.Instance.UI.ShowMessage("Wave " + CurrentWaveNumber + " - " + wave.waveName);
        }

        /// <summary>
        /// วางแผนทิศของเวฟถัดไป: กำหนดทิศ = ทางนั้น / Random = สุ่ม 1 ทางต่อกลุ่ม / AllSides = วนทุกทาง
        /// </summary>
        private void EnsurePlan()
        {
            int next = _waveActive ? _currentIndex : _currentIndex + 1;
            if (_waveActive || next >= waves.Count || _plannedFor == next) return;
            _plannedFor = next;
            _plannedGroupPaths.Clear();
            _preview.Clear();

            foreach (SpawnGroup g in waves[next].groups)
            {
                List<EnemyPath> paths = new List<EnemyPath>();
                if (g.side == SpawnSide.AllSides) paths.AddRange(AvailablePaths());
                else
                {
                    EnemyPath p = GetPath(g.side);
                    if (p != null) paths.Add(p);
                }
                _plannedGroupPaths.Add(paths);

                if (g.enemyPrefab == null || g.count <= 0 || paths.Count == 0) continue;
                for (int i = 0; i < g.count; i++)
                {
                    EnemyPath p = paths[i % paths.Count];
                    PathPreview pv = _preview.Find(x => x.path == p);
                    if (pv == null) { pv = new PathPreview { path = p }; _preview.Add(pv); }
                    pv.counts.TryGetValue(g.enemyPrefab, out int c);
                    pv.counts[g.enemyPrefab] = c + 1;
                }
            }
        }

        private List<EnemyPath> AvailablePaths()
        {
            List<EnemyPath> list = new List<EnemyPath>();
            foreach (EnemyPath p in new[] { pathNorth, pathEast, pathSouth, pathWest })
                if (p != null) list.Add(p);
            return list;
        }

        private IEnumerator SpawnGroupRoutine(SpawnGroup g, List<EnemyPath> paths)
        {
            if (g.startDelay > 0f) yield return new WaitForSeconds(g.startDelay);
            for (int i = 0; i < g.count; i++)
            {
                EnemyPath path = paths != null && paths.Count > 0 ? paths[i % paths.Count] : GetPath(g.side);
                Spawn(g.enemyPrefab, path);
                if (i < g.count - 1) yield return new WaitForSeconds(Mathf.Max(0.05f, g.spawnInterval));
            }
            _runningGroups--;
        }

        private void Spawn(EnemyUnit prefab, EnemyPath path)
        {
            Vector2 pos = (path != null ? path.SpawnPosition : Vector2.zero) + UnityEngine.Random.insideUnitCircle * spawnScatter;
            EnemyUnit e = Instantiate(prefab, pos, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Enemies));
            e.ApplyWaveScaling(CurrentHealthMultiplier, CurrentDamageMultiplier);
            e.SetPath(path);
        }

        private readonly List<EnemyPath> _available = new List<EnemyPath>();

        private EnemyPath GetPath(SpawnSide side)
        {
            switch (side)
            {
                case SpawnSide.North: if (pathNorth != null) return pathNorth; break;
                case SpawnSide.East: if (pathEast != null) return pathEast; break;
                case SpawnSide.South: if (pathSouth != null) return pathSouth; break;
                case SpawnSide.West: if (pathWest != null) return pathWest; break;
            }

            // Random / AllSides / ทิศที่ไม่มีทาง -> เลือกจากทางที่มีอยู่
            _available.Clear();
            foreach (EnemyPath p in new[] { pathNorth, pathEast, pathSouth, pathWest })
                if (p != null) _available.Add(p);
            if (_available.Count == 0) return null;
            if (side == SpawnSide.AllSides)
            {
                _sideCycle = (_sideCycle + 1) % _available.Count;
                return _available[_sideCycle];
            }
            return _available[UnityEngine.Random.Range(0, _available.Count)];
        }

        private void OnWaveCleared()
        {
            _waveActive = false;
            if (!HasMoreWaves)
            {
                if (GameManager.Instance != null) GameManager.Instance.Victory();
                return;
            }
            _countdown = timeBetweenWaves;
            if (GameManager.Instance != null) GameManager.Instance.UI.ShowMessage("Wave " + CurrentWaveNumber + " cleared!");
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            foreach (EnemyPath p in new[] { pathNorth, pathEast, pathSouth, pathWest })
                if (p != null) Gizmos.DrawWireSphere(p.transform.position, spawnScatter);
        }
    }
}
