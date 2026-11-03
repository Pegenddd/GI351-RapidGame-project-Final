using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using NecroDefense.Characters.Player;
using NecroDefense.Towers;

namespace NecroDefense.UI
{
    public class TowerShopHUDCanvas : MonoBehaviour
    {
        [Header("Tower Prefabs")]
        [SerializeField] private GameObject aoeBlastPrefab;
        [SerializeField] private GameObject slowTargetPrefab;
        [SerializeField] private GameObject goblinSpawnerPrefab;

        [Header("References")]
        [SerializeField] private Towers.TowerPlacer towerPlacer;

        [Header("Canvas UI Elements")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Button toggleShopButton;
        [SerializeField] private Text toggleButtonText;

        [Header("Tower Shop Slots (3 Slots: AoE, Slow, Goblin)")]
        [SerializeField] private TowerSlotUI[] towerSlots;

        [Header("Placing Banner")]
        [SerializeField] private GameObject placingBannerPanel;
        [SerializeField] private Text placingBannerText;

        [System.Serializable]
        public struct TowerSlotUI
        {
            public Button slotButton;
            public Image towerIconImage; // สำหรับใส่รูปไอคอน
            public Text costText;        // สำหรับโชว์มานา/Soul ที่ใช้
        }

        private PlayerNecromancer _player;
        private int _selectedSlot = -1;
        private bool _isShopOpen = true;

        private GameObject[] _prefabs;
        private int[] _costs;

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerNecromancer>();
            if (towerPlacer == null)
                towerPlacer = FindAnyObjectByType<Towers.TowerPlacer>();

            _prefabs = new GameObject[] { aoeBlastPrefab, slowTargetPrefab, goblinSpawnerPrefab };
            _costs = new int[3];

            for (int i = 0; i < 3; i++)
            {
                _costs[i] = GetCost(_prefabs[i]);
            }

            if (toggleShopButton != null)
            {
                // ลบ Listener เก่ากันเหนียว แล้วผูกใหม่
                toggleShopButton.onClick.RemoveAllListeners();
                toggleShopButton.onClick.AddListener(ToggleShop);
            }

            for (int i = 0; i < towerSlots.Length; i++)
            {
                int index = i;
                if (towerSlots[i].slotButton != null)
                {
                    towerSlots[i].slotButton.onClick.RemoveAllListeners();
                    towerSlots[i].slotButton.onClick.AddListener(() => SelectSlot(index));
                }
            }

            if (placingBannerPanel != null) placingBannerPanel.SetActive(false);

            // กำหนดสถานะเริ่มต้นให้ตรงกับตัวแปร
            if (shopPanel != null) shopPanel.SetActive(_isShopOpen);
            UpdateToggleText();

            UpdateShopDisplay();
        }

        private int GetCost(GameObject prefab)
        {
            if (prefab != null && prefab.TryGetComponent<Towers.TowerBehavior>(out var tb))
                return tb.BuildCost;
            return 0;
        }

        private void Update()
        {
            if (IsEscapePressed() && _selectedSlot >= 0)
            {
                CancelPlacement();
            }

            UpdateShopDisplay();
        }

        private bool IsEscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                   (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1);
#endif
        }

        private void ToggleShop()
        {
            _isShopOpen = !_isShopOpen;

            if (shopPanel != null)
            {
                shopPanel.SetActive(_isShopOpen);
            }

            UpdateToggleText();
            Debug.Log($"<color=cyan>[TowerShop]</color> Shop State: {(_isShopOpen ? "OPEN" : "CLOSED")}");
        }

        private void UpdateToggleText()
        {
            if (toggleButtonText != null)
            {
                toggleButtonText.text = _isShopOpen ? "[-] TOWER SHOP" : "[+] TOWER SHOP";
            }
        }

        private void UpdateShopDisplay()
        {
            int playerSouls = _player != null ? _player.CurrentSouls : 0;

            for (int i = 0; i < towerSlots.Length; i++)
            {
                var slotUI = towerSlots[i];
                if (i >= _prefabs.Length) continue;

                bool hasPrefab = (_prefabs[i] != null);
                bool canAfford = (playerSouls >= _costs[i]);

                if (slotUI.costText != null)
                {
                    slotUI.costText.text = $"{_costs[i]}";
                }

                if (slotUI.slotButton != null)
                {
                    slotUI.slotButton.interactable = hasPrefab && canAfford;
                }
            }

            bool isPlacing = (_selectedSlot >= 0);
            if (placingBannerPanel != null) placingBannerPanel.SetActive(isPlacing);
            if (isPlacing && placingBannerText != null)
            {
                placingBannerText.text = $"COST: {_costs[_selectedSlot]} SOULS";
            }
        }

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= _prefabs.Length) return;
            if (_prefabs[index] == null) return;
            if (_player != null && _player.CurrentSouls < _costs[index]) return;

            if (_selectedSlot == index)
            {
                CancelPlacement();
                return;
            }

            _selectedSlot = index;
            towerPlacer?.SelectTowerToBuild(_prefabs[index]);
        }

        private void CancelPlacement()
        {
            _selectedSlot = -1;
            towerPlacer?.CancelPlacementPublic();
        }
    }
}