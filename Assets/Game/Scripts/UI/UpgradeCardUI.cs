using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>การ์ด 1 ใบในหน้าต่างเลเวลอัป</summary>
    public class UpgradeCardUI : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image categoryStrip;
        [SerializeField] private TMP_Text categoryText;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text stacksText;

        private UpgradeCard _card;
        private Action<UpgradeCard> _onPick;

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(() => { if (_card != null) _onPick?.Invoke(_card); });
        }

        public void Bind(UpgradeCard card, int currentStacks, Action<UpgradeCard> onPick)
        {
            _card = card;
            _onPick = onPick;
            gameObject.SetActive(card != null);
            if (card == null) return;
            if (categoryText != null) categoryText.text = card.category.ToString().ToUpper();
            if (categoryStrip != null) categoryStrip.color = card.cardColor;
            if (titleText != null) titleText.text = card.title;
            if (descriptionText != null) descriptionText.text = card.description;
            if (stacksText != null)
                stacksText.text = card.maxStacks > 0 ? "Owned " + currentStacks + "/" + card.maxStacks : "Owned " + currentStacks + " (no limit)";
        }
    }
}
