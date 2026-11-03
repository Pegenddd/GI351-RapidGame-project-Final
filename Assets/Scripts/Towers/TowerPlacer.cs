using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using NecroDefense.Base;
using NecroDefense.Characters.Player;

namespace NecroDefense.Towers
{
    /// <summary>
    /// ระบบ Tower Placement แบบ Click-to-Place (รองรับทั้ง Old และ New Input System)
    /// </summary>
    public class TowerPlacer : MonoBehaviour
    {
        [Header("Placement Validation")]
        [Tooltip("ระยะขั้นต่ำจาก BaseCrystal ที่ห้ามสร้างทาวเวอร์")]
        [SerializeField] private float minDistanceFromBase = 3f;

        [Tooltip("ระยะขั้นต่ำระหว่างทาวเวอร์ที่ห้ามสร้างชิดกัน")]
        [SerializeField] private float minDistanceFromOtherTowers = 1.5f;

        [Header("Ghost Visual")]
        [Tooltip("Sorting Layer ที่ Ghost Sprite ควรแสดง")]
        [SerializeField] private int ghostSortingOrder = 20;

        [Tooltip("ความโปร่งใสของ Ghost (0 = โปร่งสนิท, 1 = ทึบสนิท)")]
        [Range(0f, 1f)]
        [SerializeField] private float ghostAlpha = 0.55f;

        // ── Runtime References ─────────────────────────────────────────────
        private GameObject _currentGhost;       // Ghost Preview object
        private GameObject _selectedPrefab;     // Prefab ที่เลือกจาก UI
        private SpriteRenderer _ghostRenderer;     // Renderer ของ Ghost
        private bool _isPlacing = false;   // กำลัง Place อยู่หรือเปล่า

        private BaseCrystal _baseCrystal;        // ฐาน (อ้างอิง)
        private Camera _mainCam;
        private PlayerNecromancer _player;         // สำหรับหักทรัพยากร Soul

        // ──────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _mainCam = Camera.main;
        }

        private void Start()
        {
            _baseCrystal = Object.FindAnyObjectByType<BaseCrystal>();
            _player = Object.FindAnyObjectByType<PlayerNecromancer>();
        }

        private void Update()
        {
            if (!_isPlacing) return;

            Vector2 mouseWorld = GetMouseWorldPosition();

            // ── อัปเดต Ghost ตามตำแหน่งเมาส์ ──────────────────────────────
            if (_currentGhost != null)
            {
                _currentGhost.transform.position = mouseWorld;
                UpdateGhostColor(mouseWorld);
            }

            // ── Mouse 0 → ยืนยันวาง ────────────────────────────────────────
            if (IsLeftMouseButtonPressed())
            {
                TryPlaceTower(mouseWorld);
                return;
            }

            // ── Mouse 1 หรือ ESC → ยกเลิก ──────────────────────────────────
            if (IsRightMouseButtonPressed() || IsEscapePressed())
            {
                CancelPlacement();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Input Helper (รองรับทั้ง Old และ New Input System ป้องกัน Error)
        // ─────────────────────────────────────────────────────────────────
        private Vector2 GetMouseWorldPosition()
        {
            if (_mainCam == null) _mainCam = Camera.main;
            if (_mainCam == null) return Vector2.zero;

            Vector3 screenPos = Vector3.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                Vector2 mouseVal = Mouse.current.position.ReadValue();
                screenPos = new Vector3(mouseVal.x, mouseVal.y, 0f);
            }
#else
            screenPos = Input.mousePosition;
#endif

            Vector3 world = _mainCam.ScreenToWorldPoint(screenPos);
            world.z = 0f;
            return world;
        }

        private bool IsLeftMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        private bool IsRightMouseButtonPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(1);
#endif
        }

        private bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        // ─────────────────────────────────────────────────────────────────
        //  Public API: เรียกจากปุ่ม UI เพื่อเลือก Prefab และเริ่ม Preview
        // ─────────────────────────────────────────────────────────────────
        public void SelectTowerToBuild(GameObject towerPrefab)
        {
            if (towerPrefab == null)
            {
                Debug.LogWarning("[TowerPlacer] SelectTowerToBuild: towerPrefab เป็น null!");
                return;
            }

            if (_isPlacing) CancelPlacement();

            _selectedPrefab = towerPrefab;
            _isPlacing = true;

            SpawnGhost(towerPrefab);
        }

        // ─────────────────────────────────────────────────────────────────
        //  Ghost Preview
        // ─────────────────────────────────────────────────────────────────
        private void SpawnGhost(GameObject prefab)
        {
            _currentGhost = Instantiate(prefab, GetMouseWorldPosition(), Quaternion.identity);
            _currentGhost.name = "TowerGhost_Preview";

            foreach (var col in _currentGhost.GetComponentsInChildren<Collider2D>())
                col.enabled = false;

            if (_currentGhost.TryGetComponent<TowerBehavior>(out var tb))
                tb.enabled = false;

            _ghostRenderer = _currentGhost.GetComponentInChildren<SpriteRenderer>();
            if (_ghostRenderer != null)
            {
                _ghostRenderer.sortingOrder = ghostSortingOrder;
                SetGhostColor(Color.white);
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Validation & Color Update
        // ─────────────────────────────────────────────────────────────────
        private void UpdateGhostColor(Vector2 position)
        {
            if (_ghostRenderer == null) return;

            bool canPlace = IsValidPlacement(position);
            SetGhostColor(canPlace ? Color.white : Color.black);
        }

        private void SetGhostColor(Color baseColor)
        {
            if (_ghostRenderer == null) return;
            _ghostRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, ghostAlpha);
        }

        private bool IsValidPlacement(Vector2 position)
        {
            if (_baseCrystal != null)
            {
                float distFromBase = Vector2.Distance(position, (Vector2)_baseCrystal.Position);
                if (distFromBase < minDistanceFromBase) return false;
            }

            Collider2D[] nearby = Physics2D.OverlapCircleAll(position, minDistanceFromOtherTowers);
            foreach (var col in nearby)
            {
                if (_currentGhost != null && col.transform.IsChildOf(_currentGhost.transform)) continue;
                if (col.gameObject == _currentGhost) continue;

                if (col.CompareTag("Tower") || col.GetComponent<TowerBehavior>() != null)
                    return false;
            }

            return true;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Try Place: ยืนยันการวาง Tower จริง
        // ─────────────────────────────────────────────────────────────────
        private void TryPlaceTower(Vector2 position)
        {
            if (!IsValidPlacement(position))
            {
                Debug.Log("<color=red>[TowerPlacer]</color> ตำแหน่งนี้วางไม่ได้!");
                return;
            }

            int cost = GetSelectedTowerCost();
            if (_player != null && !_player.SpendSouls(cost))
            {
                Debug.Log($"<color=yellow>[TowerPlacer]</color> Soul ไม่พอ! ต้องการ {cost} Souls");
                return;
            }

            GameObject tower = Instantiate(_selectedPrefab, position, Quaternion.identity);
            tower.name = _selectedPrefab.name;
            tower.tag = "Tower"; // กำหนด Tag ให้เป็น Tower เพื่อใช้เช็กระยะรอบหน้า

            if (tower.TryGetComponent<TowerBehavior>(out var tb))
                tb.enabled = true;

            Debug.Log($"<color=lime>[TowerPlacer]</color> สร้าง Tower '{tower.name}' ที่ {position} (ใช้ {cost} Souls)");

            DestroyGhost();
            _isPlacing = false;
            _selectedPrefab = null;
        }

        private void CancelPlacement()
        {
            DestroyGhost();
            _isPlacing = false;
            _selectedPrefab = null;
            Debug.Log("<color=grey>[TowerPlacer]</color> ยกเลิกการวาง Tower");
        }

        public void CancelPlacementPublic() => CancelPlacement();

        public bool IsPlacing => _isPlacing;

        private void DestroyGhost()
        {
            if (_currentGhost != null)
            {
                Destroy(_currentGhost);
                _currentGhost = null;
                _ghostRenderer = null;
            }
        }

        private int GetSelectedTowerCost()
        {
            if (_selectedPrefab == null) return 0;
            if (_selectedPrefab.TryGetComponent<TowerBehavior>(out var tb))
                return tb.BuildCost;
            return 0;
        }

        private void OnDrawGizmosSelected()
        {
            if (_baseCrystal != null)
            {
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.15f);
                Gizmos.DrawWireSphere(_baseCrystal.Position, minDistanceFromBase);
            }
        }
    }
}