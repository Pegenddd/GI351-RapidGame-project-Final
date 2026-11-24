using UnityEngine;
using UnityEngine.SceneManagement;

namespace NecroDefense
{
    /// <summary>ย้ายระหว่างหน้าเมนูกับซีนเกม (เรียกจากปุ่ม Main Menu ในหน้า Pause / จบเกม)</summary>
    public static class GameFlow
    {
        public const string MainMenuScene = "MainMenu";

        public static void ToMainMenu()
        {
            Time.timeScale = 1f;
            if (!Application.CanStreamedLevelBeLoaded(MainMenuScene))
            {
                Debug.LogWarning("[NecroDefense] ซีน " + MainMenuScene + " ยังไม่อยู่ใน Build Settings (กดเมนู Tools > NecroDefense > Build Main Menu Scene)");
                return;
            }
            SceneManager.LoadScene(MainMenuScene);
        }
    }
}
