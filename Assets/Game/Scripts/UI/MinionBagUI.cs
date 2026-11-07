using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// กระเป๋ามินเนียน (มุมซ้ายล่าง) - กด Tab หรือคลิกปุ่มกระเป๋าเพื่อเปิด/ปิด
    /// </summary>
    public class MinionBagUI : MonoBehaviour
    {
        [Header("Toggle")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private Key toggleKey = Key.Tab;
        [Tooltip("แผงที่เปิด/ปิด (มีช่องมินเนียนอยู่ข้างใน)")]
        [SerializeField] private GameObject panel;
        [SerializeField] private bool startOpen = false;
        [Tooltip("ตัวเลขจำนวนมินเนียนทั้งหมดในกระเป๋า (บนปุ่ม)")]
        [SerializeField] private TMP_Text totalCountText;

        [Header("Slots")]
        [SerializeField] private List<MinionBagSlotUI> slots = new List<MinionBagSlotUI>();
        [SerializeField] private TMP_Text activeCountText;

        [Header("Drag")]
        [Tooltip("รูปที่ตามเมาส์ตอนลากช่องมินเนียน")]
        [SerializeField] private Image dragGhost;

        public bool IsOpen => panel != null && panel.activeSelf;

        private float _pulse;

        /// <summary>จุดที่วิญญาณบินไปหา: ช่องของมินเนียนนั้น (ถ้ากระเป๋าเปิด) ไม่งั้นปุ่มกระเป๋า</summary>
        public RectTransform GetSoulTarget(int bagIndex)
        {
            if (IsOpen)
                foreach (MinionBagSlotUI s in slots)
                    if (s != null && s.BagIndex == bagIndex) return (RectTransform)s.transform;
            return toggleButton != null ? (RectTransform)toggleButton.transform : (RectTransform)transform;
        }

        /// <summary>ปุ่มกระเป๋าเด้งตอนวิญญาณเข้า</summary>
        public void Pulse() => _pulse = 1f;

        private void Awake()
        {
            if (toggleButton != null) toggleButton.onClick.AddListener(Toggle);
        }

        private void Start()
        {
            foreach (MinionBagSlotUI s in slots) if (s != null) s.Bind(this);
            if (dragGhost != null) dragGhost.gameObject.SetActive(false);
            SetOpen(startOpen);
        }

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool open)
        {
            if (panel != null) panel.SetActive(open);
        }

        private void Update()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;

            Keyboard kb = Keyboard.current;
            if (kb != null && toggleKey != Key.None && kb[toggleKey].wasPressedThisFrame && GameManager.IsGameplayRunning) Toggle();

            if (toggleButton != null)
            {
                _pulse = Mathf.Max(0f, _pulse - Time.unscaledDeltaTime * 4f);
                toggleButton.transform.localScale = Vector3.one * (1f + 0.18f * Mathf.Sin(_pulse * Mathf.PI));
            }

            int total = 0;
            foreach (MinionBagEntry e in gm.Minions.Bag) total += e.count;
            if (totalCountText != null) totalCountText.text = total.ToString();

            if (!IsOpen) return;
            foreach (MinionBagSlotUI s in slots) if (s != null) s.Refresh();
            if (activeCountText != null) activeCountText.text = gm.Minions.ActiveCount + "/" + gm.Minions.MaxActive;
        }

        public void BeginDragGhost(MinionBagSlotUI slot, Vector2 screenPos)
        {
            if (dragGhost == null) return;
            var bag = GameManager.Instance.Minions.Bag;
            if (slot.BagIndex < 0 || slot.BagIndex >= bag.Count || bag[slot.BagIndex].minionPrefab == null) return;
            MinionUnit prefab = bag[slot.BagIndex].minionPrefab;
            dragGhost.sprite = prefab.Icon;
            dragGhost.preserveAspect = true;
            dragGhost.color = new Color(1f, 1f, 1f, 0.85f);
            dragGhost.gameObject.SetActive(true);
            MoveDragGhost(screenPos);
        }

        public void MoveDragGhost(Vector2 screenPos)
        {
            if (dragGhost != null) dragGhost.rectTransform.position = screenPos;
        }

        public void EndDragGhost()
        {
            if (dragGhost != null) dragGhost.gameObject.SetActive(false);
        }
    }
}
