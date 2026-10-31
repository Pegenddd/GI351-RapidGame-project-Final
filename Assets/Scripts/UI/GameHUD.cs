using UnityEngine;
using NecroDefense.Base;
using NecroDefense.Characters.Player;
using NecroDefense.Characters.Minions;
using NecroDefense.Managers;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace NecroDefense.UI
{
    /// <summary>
    /// In-game Heads-Up Display (HUD) in 2-Color Palette (1-bit Black & White).
    /// Pure Black backgrounds, 1px White borders, and Pure White text/bars.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("2-Color HUD Customization (Inspector)")]
        [Tooltip("Toggle HUD display")]
        [SerializeField] private bool showHUD = true;
        [Tooltip("Color of Citadel Core HP bar")]
        [SerializeField] private Color citadelBarColor = Color.white;
        [Tooltip("Color of Necromancer HP bar")]
        [SerializeField] private Color playerBarColor = Color.white;
        [Tooltip("Color of Soul Essence bar")]
        [SerializeField] private Color soulBarColor = Color.white;
        [Tooltip("Color of HUD title text")]
        [SerializeField] private Color titleTextColor = Color.white;

        private PlayerNecromancer _player;
        private BaseCrystal _baseCrystal;
        private WaveSpawner _waveSpawner;
        private GUIStyle _titleStyle;
        private GUIStyle _subStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _btnStyle;
        private Texture2D _boxBgTex;
        private Texture2D _btnNormalTex;
        private Texture2D _btnHoverTex;
        private bool _stylesInitialized = false;

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerNecromancer>();
            _baseCrystal = FindAnyObjectByType<BaseCrystal>();
            _waveSpawner = FindAnyObjectByType<WaveSpawner>();
        }

        private void Update()
        {
            // Minion command hotkeys [1], [2], [3]
            bool key1 = false, key2 = false, key3 = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                key1 = Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame;
                key2 = Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame;
                key3 = Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame;
            }
#else
            key1 = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1);
            key2 = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2);
            key3 = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3);
#endif
            if (key1) BroadcastMinionOrder(MinionOrder.DefendBase);
            if (key2) BroadcastMinionOrder(MinionOrder.FollowNecromancer);
            if (key3) BroadcastMinionOrder(MinionOrder.FreeHunt);
        }

        private void BroadcastMinionOrder(MinionOrder order)
        {
            var minions = FindObjectsByType<MinionUnit>(FindObjectsSortMode.None);
            foreach (var m in minions)
            {
                m.SetOrder(order);
            }
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            // 1-bit box background: Black with 1px White border
            _boxBgTex = MakeBorderedTex(16, 16, Color.black, Color.white);
            _btnNormalTex = MakeBorderedTex(16, 16, Color.black, Color.white);
            _btnHoverTex = MakeBorderedTex(16, 16, Color.white, Color.black);

            _boxStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = _boxBgTex },
                padding = new RectOffset(8, 8, 8, 8)
            };

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = titleTextColor }
            };

            _subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                normal = { textColor = Color.white }
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { background = _btnNormalTex, textColor = Color.white },
                hover = { background = _btnHoverTex, textColor = Color.black },
                active = { background = _btnHoverTex, textColor = Color.black }
            };

            _stylesInitialized = true;
        }

        private Texture2D MakeBorderedTex(int width, int height, Color inner, Color border)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isBorder = (x == 0 || x == width - 1 || y == 0 || y == height - 1);
                    tex.SetPixel(x, y, isBorder ? border : inner);
                }
            }
            tex.Apply();
            return tex;
        }

        private void OnGUI()
        {
            if (!showHUD) return;
            InitStyles();

            // --- Top Status Panel ---
            GUILayout.BeginArea(new Rect(15, 15, 270, 195), _boxStyle);
            GUILayout.Label("[*] NECROMANCER DEFENSE", _titleStyle);
            GUILayout.Space(2);

            // Citadel Base Health
            if (_baseCrystal != null)
            {
                float pct = _baseCrystal.CurrentHealth / _baseCrystal.MaxHealth;
                DrawBar("CITADEL HP", _baseCrystal.CurrentHealth, _baseCrystal.MaxHealth, pct);
            }

            // Player Necromancer Health
            if (_player != null)
            {
                float pct = _player.CurrentHealth / _player.MaxHealth;
                DrawBar("MAGE HP", _player.CurrentHealth, _player.MaxHealth, pct);

                // Souls Bar (เปิดใช้งานและส่งค่าถูกต้องแล้ว)
                float soulPct = (float)_player.CurrentSouls / _player.MaxSouls;
                DrawBar($"SOULS ({_player.CurrentSouls}/{_player.MaxSouls})", _player.CurrentSouls, _player.MaxSouls, soulPct);
            }

            GUILayout.Space(2);
            // Wave & Stats
            int wave = _waveSpawner != null ? _waveSpawner.CurrentWave : 1;
            int kills = GameManager.Instance != null ? GameManager.Instance.TotalKills : 0;
            float time = GameManager.Instance != null ? GameManager.Instance.SurvivalTimer : 0f;
            GUILayout.Label($"WAVE: {wave} | KILLS: {kills} | TIME: {time:F0}s", _subStyle);

            GUILayout.EndArea();

            // --- Bottom Controls & Strategy Order Panel ---
            GUILayout.BeginArea(new Rect(15, Screen.height - 85, 450, 75), _boxStyle);
            GUILayout.Label("COMMANDS:", _titleStyle);
            GUILayout.Label("[WASD]: Move | [Click]: Cast Bolt | [Space/R-Click]: Resurrect", _subStyle);
            GUILayout.Label("Orders: [1] Defend  [2] Follow  [3] Free Hunt", _subStyle);
            GUILayout.EndArea();

            // --- Game Over / Victory Screen ---
            if (GameManager.Instance != null && (GameManager.Instance.CurrentState == GameState.GameOver || GameManager.Instance.CurrentState == GameState.Victory))
            {
                bool isWin = GameManager.Instance.CurrentState == GameState.Victory;
                float w = 350;
                float h = 180;
                Rect rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);
                GUILayout.BeginArea(rect, _boxStyle);
                GUILayout.Space(10);
                GUIStyle bannerStyle = new GUIStyle(_titleStyle)
                {
                    fontSize = 20,
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                GUILayout.Label(isWin ? "[+] VICTORY [+]" : "[X] GAME OVER [X]", bannerStyle);
                GUILayout.Space(5);
                GUIStyle statStyle = new GUIStyle(_subStyle) { alignment = TextAnchor.MiddleCenter };
                GUILayout.Label($"Waves Survived: {wave} | Foes Slain: {kills}", statStyle);
                GUILayout.Space(10);

                if (GUILayout.Button(">> PLAY AGAIN <<", _btnStyle, GUILayout.Height(35)))
                {
                    GameManager.Instance.RestartGame();
                }
                GUILayout.EndArea();
            }
        }

        private void DrawBar(string label, float current, float max, float pct, float soulPos = -1f)
        {
            float targetPct = soulPos >= 0f ? soulPos : pct;
            GUILayout.Label($"{label}: {current:F0} / {max:F0}", _subStyle);
            Rect r = GUILayoutUtility.GetRect(240, 10);

            // 1-bit crisp border: 1px white border outline
            GUI.color = Color.white;
            GUI.DrawTexture(r, Texture2D.whiteTexture);

            // Black interior
            Rect inner = new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2);
            GUI.color = Color.black;
            GUI.DrawTexture(inner, Texture2D.whiteTexture);

            // Solid White fill
            float fillWidth = (inner.width) * Mathf.Clamp01(targetPct);
            if (fillWidth > 0)
            {
                Rect fillR = new Rect(inner.x, inner.y, fillWidth, inner.height);
                GUI.color = Color.white;
                GUI.DrawTexture(fillR, Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            GUILayout.Space(1);
        }
    }
}