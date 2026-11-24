using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// หน้าเมนูหลัก (ซีน MainMenu)
    /// PLAY → เข้าซีนเกม (ค่าเริ่มต้น Map2) | HOW TO PLAY | SETTINGS (ปรับเสียง) | CREDITS | QUIT
    /// Esc = ปิดหน้าต่างย่อย
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        public static MainMenuUI Instance { get; private set; }

        [Header("ซีนที่จะเข้าเมื่อกด PLAY (ต้องอยู่ใน Build Settings)")]
        public string gameSceneName = "Map2";

        [Header("ปุ่ม")]
        public MenuButton playButton;
        public MenuButton howToButton;
        public MenuButton settingsButton;
        public MenuButton creditsButton;
        public MenuButton quitButton;

        [Header("หน้าต่างย่อย")]
        public RectTransform howToPanel;
        public RectTransform settingsPanel;
        public RectTransform creditsPanel;
        public MenuButton[] backButtons;

        [Header("อื่นๆ")]
        public CanvasGroup mainGroup;
        public TMP_Text hintText;
        public TMP_Text pressHint;          // ข้อความกระพริบ
        public Image fade;                  // ดำ (เปิด/ปิดหน้า)
        public Image flash;                 // แดง (ตอนกด PLAY)
        public MenuBackgroundFX backgroundFX;
        public string music = "MUS_Menu";
        public string startSound = "SFX_Menu_Start";
        public string backSound = "SFX_Menu_Back";
        public string openSound = "SFX_UI_Confirm";

        private RectTransform _open;
        private bool _loading;
        private float _hintAlpha;
        private string _hintTarget = "";

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;
            if (howToPanel != null) howToPanel.gameObject.SetActive(false);
            if (settingsPanel != null) settingsPanel.gameObject.SetActive(false);
            if (creditsPanel != null) creditsPanel.gameObject.SetActive(false);

            Hook(playButton, Play);
            Hook(howToButton, () => Open(howToPanel));
            Hook(settingsButton, () => Open(settingsPanel));
            Hook(creditsButton, () => Open(creditsPanel));
            Hook(quitButton, Quit);
            if (backButtons != null)
                foreach (MenuButton b in backButtons)
                    if (b != null) { b.clickSound = backSound; b.onClick += CloseOpen; }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static void Hook(MenuButton b, System.Action a)
        {
            if (b != null) b.onClick += a;
        }

        private void Start()
        {
            AudioManager am = AudioManager.Get();
            if (am != null && !string.IsNullOrEmpty(music)) am.PlayMusic(music, true);

            // ปุ่มไถลเข้าทีละอัน
            MenuButton[] order = { playButton, howToButton, settingsButton, creditsButton, quitButton };
            for (int i = 0; i < order.Length; i++)
                if (order[i] != null) order[i].PlayEntrance(0.45f + i * 0.08f);

            if (fade != null) StartCoroutine(FadeRoutine(1f, 0f, 0.8f));
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            Keyboard k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame && _open != null && !_loading)
            {
                Sfx.UI(backSound);
                CloseOpen();
            }

            // คำอธิบายปุ่มด้านล่าง: จางออก → เปลี่ยนข้อความ → จางเข้า
            if (hintText != null)
            {
                bool same = hintText.text == _hintTarget;
                float target = same && !string.IsNullOrEmpty(_hintTarget) ? 1f : 0f;
                _hintAlpha = Mathf.MoveTowards(_hintAlpha, target, dt * 6f);
                if (!same && _hintAlpha <= 0.001f) hintText.text = _hintTarget;
                Color c = hintText.color; c.a = _hintAlpha; hintText.color = c;
            }

            if (pressHint != null)
            {
                Color c = pressHint.color;
                c.a = 0.8f + 0.18f * Mathf.Sin(Time.unscaledTime * 2.2f);
                pressHint.color = c;
            }
        }

        public void ShowHint(string text) => _hintTarget = text ?? "";

        // ------------------------------------------------------------ panels

        private void Open(RectTransform panel)
        {
            if (panel == null || _loading) return;
            if (_open != null) _open.gameObject.SetActive(false);
            _open = panel;
            panel.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(openSound)) Sfx.UI(openSound, 0.7f);
            SetMainInteractable(false);
            ShowHint("");
        }

        private void CloseOpen()
        {
            if (_open == null) return;
            _open.gameObject.SetActive(false);
            _open = null;
            SetMainInteractable(true);
        }

        private void SetMainInteractable(bool on)
        {
            if (mainGroup != null)
            {
                mainGroup.interactable = on;
                mainGroup.blocksRaycasts = on;
                mainGroup.alpha = on ? 1f : 0.25f;
            }
            MenuButton[] all = { playButton, howToButton, settingsButton, creditsButton, quitButton };
            foreach (MenuButton b in all) if (b != null) b.Interactable = on;
        }

        // ------------------------------------------------------------ play / quit

        private void Play()
        {
            if (_loading) return;
            _loading = true;
            SetMainInteractable(false);
            if (mainGroup != null) mainGroup.alpha = 1f;
            StartCoroutine(PlayRoutine());
        }

        private IEnumerator PlayRoutine()
        {
            if (!string.IsNullOrEmpty(startSound)) Sfx.UI(startSound);
            if (backgroundFX != null) backgroundFX.Punch(0.07f);

            // แดงวาบ
            if (flash != null)
            {
                flash.gameObject.SetActive(true);
                for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.35f)
                {
                    Color c = flash.color; c.a = Mathf.Lerp(0.75f, 0f, t); flash.color = c;
                    yield return null;
                }
                Color e = flash.color; e.a = 0f; flash.color = e;
            }

            AsyncOperation op = null;
            if (Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                op = SceneManager.LoadSceneAsync(gameSceneName);
                if (op != null) op.allowSceneActivation = false;
            }
            else Debug.LogError("[NecroDefense] ซีน '" + gameSceneName + "' ไม่อยู่ใน Build Settings");

            if (fade != null) yield return FadeRoutine(0f, 1f, 0.7f);
            AudioManager am = AudioManager.Get();
            if (am != null) am.PlayMusic("", false); // จางเพลงออก

            if (op == null) { _loading = false; if (fade != null) yield return FadeRoutine(1f, 0f, 0.4f); SetMainInteractable(true); yield break; }
            while (op.progress < 0.9f) yield return null;
            op.allowSceneActivation = true;
        }

        private IEnumerator FadeRoutine(float from, float to, float time)
        {
            fade.gameObject.SetActive(true);
            fade.raycastTarget = true;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, time))
            {
                Color c = fade.color; c.a = Mathf.Lerp(from, to, t * t * (3f - 2f * t)); fade.color = c;
                yield return null;
            }
            Color e = fade.color; e.a = to; fade.color = e;
            if (to <= 0f) fade.gameObject.SetActive(false);
        }

        private void Quit()
        {
            if (_loading) return;
            _loading = true;
            StartCoroutine(QuitRoutine());
        }

        private IEnumerator QuitRoutine()
        {
            if (fade != null) yield return FadeRoutine(0f, 1f, 0.5f);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
