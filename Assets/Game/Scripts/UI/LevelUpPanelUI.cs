using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>หน้าต่างเลือกการ์ดตอนเลเวลอัป (เกมหยุดระหว่างเลือก)</summary>
    public class LevelUpPanelUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private List<UpgradeCardUI> cards = new List<UpgradeCardUI>();

        public void Show(int newLevel, List<UpgradeCard> choices, Action<UpgradeCard> onPick)
        {
            gameObject.SetActive(true);
            if (titleText != null) titleText.text = "LEVEL UP!  Lv " + newLevel + "  -  Choose 1 card";
            LevelUpManager lv = GameManager.Instance.LevelUp;
            for (int i = 0; i < cards.Count; i++)
            {
                UpgradeCard c = i < choices.Count ? choices[i] : null;
                if (cards[i] != null) cards[i].Bind(c, lv.GetStacks(c), onPick);
            }
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
