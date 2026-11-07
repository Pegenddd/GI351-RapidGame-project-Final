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
    /// - ลากไปปล่อยที่คริสตัล = สังเวย (Sacrifice Heal)
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
        [SerializeField] private Color placingColor = new Color(0.55f, 0.35f, 0.85f, 1f);

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
            if (icon != null) { icon.sprite = e.minionPrefab.Icon; icon.preserveAspect = true; }
            if (tooltip != null)
            {
                int owned = gm.Minions.GetOwned(e.minionPrefab), cap = gm.Minions.GetCap(e.minionPrefab);
                tooltip.Set(e.minionPrefab.DisplayName,
                    "Army " + owned + " / " + cap + "   (in bag " + e.count + ")\n" +
                    e.minionPrefab.ManaCost + " Mana to summon. Dead minions return to the bag.\n" +
                    "Click (or key), then left-click the field to place\nDrag onto Main Base: sacrifice (+" + e.minionPrefab.SacrificeHealPercent + "% HP)");
            }
            if (countText != null)
            {
                countText.text = "x" + e.count;
                // กองทัพชนิดนี้เต็มแล้ว -> ตัวเลขสีทอง
                countText.color = gm.Minions.HasRoom(e.minionPrefab) ? Color.white : new Color(1f, 0.85f, 0.3f);
            }
            if (costText != null) costText.text = e.minionPrefab.ManaCost.ToString();
            if (keyText != null) keyText.text = UIUtil.KeyLabel(e.summonKey);
            if (canvasGroup != null) canvasGroup.alpha = e.count > 0 ? 1f : 0.45f;
            if (background != null)
                background.color = gm.Minions.PlacingIndex == bagIndex ? placingColor : normalColor;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging || eventData.button != PointerEventData.InputButton.Left) return;
            if (!GameManager.IsGameplayRunning) return;
            GameManager.Instance.Minions.BeginPlacement(bagIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
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
            if (!GameManager.IsGameplayRunning) return;

            // ปล่อยบน UI = ยกเลิก
            if (eventData.pointerCurrentRaycast.gameObject != null) return;

            Vector2 world = PlayerSkills.GetMouseWorld();
            Crystal crystal = Registry.Crystal;
            if (crystal != null && Vector2.Distance(world, crystal.Position) <= crystal.SacrificeDropRadius)
                GameManager.Instance.Minions.TrySacrifice(bagIndex);
            else
                GameManager.Instance.Minions.TrySummon(bagIndex, world);
        }
    }
}
