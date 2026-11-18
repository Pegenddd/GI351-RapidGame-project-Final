using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// ช่องมินเนียน 1 ช่องใน Minion Bag
    /// - คลิก / ปุ่มลัด  = เข้าโหมดเลือกจุดวาง แล้วคลิกซ้ายในสนาม (คลิกขวา/Esc ยกเลิก)
    /// - ลากไปปล่อยในสนาม = อัญเชิญตรงนั้น
    /// </summary>
    public class MinionBagSlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("ช่องนี้ = ลำดับที่เท่าไรใน MinionManager > Bag (เริ่ม 0)")]
        [SerializeField] private int bagIndex;
        [Tooltip("รูปมินเนียน")]
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private TMP_Text keyText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TooltipTrigger tooltip;
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = new Color(0.16f, 0.16f, 0.21f, 0.98f);
        [SerializeField] private Color placingColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Locked (ยังไม่เคยได้มินเนียนชนิดนี้)")]
        [Tooltip("ซ่อนรูป / ชื่อ / ราคา จนกว่าจะได้มินเนียนชนิดนี้ครั้งแรก (ไม่สปอยผู้เล่น)")]
        [SerializeField] private bool hideUntilUnlocked = true;
        [Tooltip("ข้อความแทนรูปตอนยังล็อก")]
        [SerializeField] private string lockedSymbol = "?";
        [SerializeField] private Color lockedSymbolColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        [SerializeField] private float lockedSymbolSize = 46f;
        [Tooltip("ความจางของช่องตอนยังล็อก")]
        [Range(0f, 1f)]
        [SerializeField] private float lockedAlpha = 0.6f;

        private TextMeshProUGUI _lockedLabel;
        private bool _locked;

        private MinionBagUI _owner;

        public int BagIndex => bagIndex;

        public void Bind(MinionBagUI owner) => _owner = owner;

        public void Refresh()
        {
            GameManager gm = GameManager.Instance;
            if (gm == null) return;
            var bag = gm.Minions.Bag;
            bool valid = bagIndex >= 0 && bagIndex < bag.Count && bag[bagIndex].minionPrefab != null;
            if (!valid)
            {
                if (countText != null) countText.text = "";
                if (costText != null) costText.text = "";
                if (keyText != null) keyText.text = "";
                if (canvasGroup != null) canvasGroup.alpha = 0.3f;
                return;
            }
            MinionBagEntry e = bag[bagIndex];
            _locked = hideUntilUnlocked && !gm.Minions.IsUnlocked(e.minionPrefab);
            SetLockedLook(_locked);
            if (_locked)
            {
                if (tooltip != null) tooltip.Set("???", "Not unlocked yet.\nRaise this kind of enemy to unlock it.");
                if (countText != null) countText.text = "";
                if (costText != null) costText.text = "";
                if (keyText != null) keyText.text = UIUtil.KeyLabel(e.summonKey);
                if (canvasGroup != null) canvasGroup.alpha = lockedAlpha;
                if (background != null) background.color = normalColor;
                return;
            }
            if (icon != null) { icon.sprite = e.minionPrefab.Icon; icon.preserveAspect = true; }
            if (tooltip != null)
            {
                int owned = gm.Minions.GetOwned(e.minionPrefab), cap = gm.Minions.GetCap(e.minionPrefab);
                tooltip.Set(e.minionPrefab.DisplayName,
                    "Army " + owned + " / " + cap + "   (in bag " + e.count + ")\n" +
                    e.minionPrefab.ManaCost + " Mana to summon. Dead minions return to the bag.\n" +
                    "Click (or key): summon around you (R = how many)\nDrag onto the field: summon there");
            }
            if (countText != null)
            {
                countText.text = "x" + e.count;
                // กองทัพชนิดนี้เต็มแล้ว -> ตัวเลขสีทอง
                countText.color = gm.Minions.HasRoom(e.minionPrefab) ? Color.white : new Color(0.9f, 0.15f, 0.12f); // เต็มเพดาน = แดง
            }
            if (costText != null) costText.text = e.minionPrefab.ManaCost.ToString();
            if (keyText != null) keyText.text = UIUtil.KeyLabel(e.summonKey);
            if (canvasGroup != null) canvasGroup.alpha = e.count > 0 ? 1f : 0.45f;
            if (background != null)
                background.color = gm.Minions.PlacingIndex == bagIndex ? placingColor : normalColor;
        }

        /// <summary>สลับหน้าตาช่อง: ล็อก = ซ่อนรูป แสดง "?" / ปลดแล้ว = รูปมินเนียน</summary>
        private void SetLockedLook(bool locked)
        {
            if (icon != null) icon.enabled = !locked;
            if (locked && _lockedLabel == null && icon != null)
            {
                GameObject go = new GameObject("Locked_Label", typeof(RectTransform));
                go.transform.SetParent(icon.transform.parent, false);
                RectTransform rt = (RectTransform)go.transform;
                RectTransform ir = icon.rectTransform;
                rt.anchorMin = ir.anchorMin; rt.anchorMax = ir.anchorMax; rt.pivot = ir.pivot;
                rt.anchoredPosition = ir.anchoredPosition; rt.sizeDelta = ir.sizeDelta;
                _lockedLabel = go.AddComponent<TextMeshProUGUI>();
                _lockedLabel.alignment = TextAlignmentOptions.Center;
                _lockedLabel.fontStyle = FontStyles.Bold;
                _lockedLabel.raycastTarget = false;
            }
            if (_lockedLabel != null)
            {
                _lockedLabel.gameObject.SetActive(locked);
                _lockedLabel.text = lockedSymbol;
                _lockedLabel.color = lockedSymbolColor;
                _lockedLabel.fontSize = lockedSymbolSize;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (_locked) return;
            if (eventData.dragging || eventData.button != PointerEventData.InputButton.Left) return;
            if (!GameManager.IsGameplayRunning) return;
            GameManager.Instance.Minions.SummonFromSlot(bagIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_locked) { eventData.pointerDrag = null; return; }
            if (GameManager.Instance != null && GameManager.Instance.Minions.IsPlacing) GameManager.Instance.Minions.CancelPlacement();
            if (_owner != null) _owner.BeginDragGhost(this, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_owner != null) _owner.MoveDragGhost(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_owner != null) _owner.EndDragGhost();
            if (_locked) return;
            if (!GameManager.IsGameplayRunning) return;

            // ปล่อยบน UI = ยกเลิก
            if (eventData.pointerCurrentRaycast.gameObject != null) return;

            Vector2 world = PlayerSkills.GetMouseWorld();
            GameManager.Instance.Minions.TrySummon(bagIndex, world);
        }
    }
}
