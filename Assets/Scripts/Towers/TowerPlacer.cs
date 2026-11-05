using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using NecroDefense.Base;
using NecroDefense.Characters.Player;

namespace NecroDefense.Towers
{
    /// <summary>
    /// ระบบ Tower Placement แบบ Click-to-Place (วางเฉพาะบน TowerBaseSlot โดยอ้างอิงตำแหน่ง BuildPosition)
    /// </summary>
    public class TowerPlacer : MonoBehaviour
    {
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

        private Camera _mainCam;
        private PlayerNecromancer _player;         // สำหรับหักทรัพยากร Soul
        private TowerBaseSlot _targetedSlot;       // แท่นวางป้อมที่เมาส์ชี้อยู่ปัจจุบัน

        // ──────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _mainCam = Camera.main;
        }

        private void Start()
        {
            _player = Object.FindAnyObjectByType<PlayerNecromancer>();
        }

        private void Update()
        {
            if (!_isPlacing) return;

            Vector2 mouseWorld = GetMouseWorldPosition();

            // ── อัปเดต Ghost ตามตำแหน่งเมาส์ ──────────────────────────────
            if (_currentGhost != null)
            {
                _targetedSlot = GetTargetedSlotUnderMouse(mouseWorld);

                if (_targetedSlot != null && !_targetedSlot.IsOccupied)
                {
                    // สแนป Ghost ไปที่ตำแหน่งขยับขึ้นบน (GetBuildPosition)
                    _currentGhost.transform.position = _targetedSlot.GetBuildPosition();
                    SetGhostColor(Color.white); // วางได้
                }
                else
                {
                    _currentGhost.transform.position = mouseWorld;
                    SetGhostColor(Color.black); // วางไม่ได้
                }
            }

            // ── Mouse 0 → ยืนยันวาง ────────────────────────────────────────
            if (IsLeftMouseButtonPressed())
            {
                TryPlaceTower();
                return;
            }

            // ── Mouse 1 หรือ ESC → ยกเลิก ──────────────────────────────────
            if (IsRightMouseButtonPressed() || IsEscapePressed())
            {
                CancelPlacement();
            }
        }

        // ─────────────────────────────────────────────────────────────────
        //  Input Helper 
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
        //  Public API: เลือก Prefab และเริ่ม Preview
        // ─────────────────────────────────────────────────────────────────
        public void SelectTowerToBuild(GameObject towerPrefab)
        {
            if (towerPrefab == null) return;
            if (_isPlacing) CancelPlacement();

            _selectedPrefab = towerPrefab;
            _isPlacing = true;
            SpawnGhost(towerPrefab);
        }

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

        private void SetGhostColor(Color baseColor)
        {
            if (_ghostRenderer == null) return;
            _ghostRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, ghostAlpha);
        }

        private TowerBaseSlot GetTargetedSlotUnderMouse(Vector2 worldPos)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                if (hit.TryGetComponent<TowerBaseSlot>(out var slot))
                {
                    return slot;
                }
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────────
        //  Try Place: ยืนยันการวาง Tower บน Slot
        // ─────────────────────────────────────────────────────────────────
        private void TryPlaceTower()
        {
            if (_targetedSlot == null || _targetedSlot.IsOccupied)
            {
                Debug.Log("<color=red>[TowerPlacer]</color> ต้องวางบนแท่นสร้างป้อม (Tower Base) ที่ว่างอยู่เท่านั้น!");
                return;
            }

            int cost = GetSelectedTowerCost();
            if (_player != null && !_player.SpendSouls(cost))
            {
                Debug.Log($"<color=yellow>[TowerPlacer]</color> Soul ไม่พอ! ต้องการ {cost} Souls");
                return;
            }

            // สร้างป้อมที่ตำแหน่ง GetBuildPosition() เพื่อให้ลอยขึ้นไปอยู่บนแท่น
            Vector3 placePos = _targetedSlot.GetBuildPosition();
            GameObject tower = Instantiate(_selectedPrefab, placePos, Quaternion.identity);
            tower.name = _selectedPrefab.name;
            tower.tag = "Tower";

            if (tower.TryGetComponent<TowerBehavior>(out var tb))
                tb.enabled = true;

            _targetedSlot.AssignTower(tower);

            Debug.Log($"<color=lime>[TowerPlacer]</color> สร้าง Tower บนแท่นสำเร็จ! (ใช้ {cost} Souls)");

            DestroyGhost();
            _isPlacing = false;
            _selectedPrefab = null;
            _targetedSlot = null;
        }

        private void CancelPlacement()
        {
            DestroyGhost();
            _isPlacing = false;
            _selectedPrefab = null;
            _targetedSlot = null;
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
    }
}