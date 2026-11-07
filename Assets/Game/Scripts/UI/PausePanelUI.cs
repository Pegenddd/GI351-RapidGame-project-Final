using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>หน้าต่าง Pause (กด Esc)</summary>
    public class PausePanelUI : MonoBehaviour
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(() => GameManager.Instance.Resume());
            if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
