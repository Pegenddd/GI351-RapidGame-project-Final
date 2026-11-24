using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>หน้าต่าง Pause (กด Esc)</summary>
    public class PausePanelUI : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;   // กลับหน้าเมนูหลัก

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(() => GameManager.Instance.Resume());
            if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            if (menuButton != null) menuButton.onClick.AddListener(GameFlow.ToMainMenu);
        }

        public void Show()
        {
            if (!gameObject.activeSelf) Sfx.UI("SFX_UI_Pause");
            gameObject.SetActive(true);
        }
        public void Hide() => gameObject.SetActive(false);
    }
}
