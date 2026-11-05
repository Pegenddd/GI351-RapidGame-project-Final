using UnityEngine;
using UnityEngine.UI;
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
    /// Includes Hybrid Skip Wave Button integration.
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
        private WaveManager _waveManager;
        private Skills.PlayerProgression _progression;
        private System.Collections.Generic.List<Skills.SkillBase> _currentChoices;
        private GUIStyle _cardBtnStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _subStyle;
        private GUIStyle _boxStyle;
        private GUIStyle _btnStyle;
        private GUIStyle _skipBtnStyle; // สไตล์สำหรับปุ่ม Skip Wave
        private Texture2D _boxBgTex;
        private Texture2D _btnNormalTex;
        private Texture2D _btnHoverTex;
        private Texture2D _skipBtnNormalTex;
        private Texture2D _skipBtnHoverTex;
        private bool _stylesInitialized = false;

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerNecromancer>();
            _baseCrystal = FindAnyObjectByType<BaseCrystal>();
            _waveManager = FindAnyObjectByType<WaveManager>();
            if (_player != null)
            {
                _progression = _player.GetComponent<Skills.PlayerProgression>();
                if (_progression != null)
                {
                    _progression.OnShowLevelUpMenu += choices => _currentChoices = choices;
                    _progression.OnCloseLevelUpMenu += () => _currentChoices = null;
                }
            }
        }

        private void Update()
        {
            if (_waveManager == null) _waveManager = FindAnyObjectByType<WaveManager>();

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
            key1 = Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(Keycode.Keypad1);
            key2 = Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(Keycode.Keypad2);
            key3 = Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(Keycode.Keypad3);
#endif
            if (_progression != null && _progression.IsLevelingUp)
            {
                if (key1 && _currentChoices != null && _currentChoices.Count > 0) _progression.SelectUpgrade(_currentChoices[0]);
                else if (key2 && _currentChoices != null && _currentChoices.Count > 1) _progression.SelectUpgrade(_currentChoices[1]);
                else if (key3 && _currentChoices != null && _currentChoices.Count > 2) _progression.SelectUpgrade(_currentChoices[2]);
                return;
            }

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

            // Skip Wave Button Textures (Inverse style to stand out when ready)
            _skipBtnNormalTex = MakeBorderedTex(16, 16, Color.white, Color.white);
            _skipBtnHoverTex = MakeBorderedTex(16, 16, Color.black, Color.white);

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

            _skipBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { background = _skipBtnNormalTex, textColor = Color.black },
                hover = { background = _skipBtnHoverTex, textColor = Color.white },
                active = { background = _skipBtnHoverTex, textColor = Color.white }
            };

            _cardBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 11,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 8, 8),
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
            GUILayout.BeginArea(new Rect(15, 15, 270, 235), _boxStyle);
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

                // Souls Bar
                float soulPct = (float)_player.CurrentSouls / _player.MaxSouls;
                DrawBar($"SOULS ({_player.CurrentSouls}/{_player.MaxSouls})", _player.CurrentSouls, _player.MaxSouls, soulPct);

                // EXP Bar
                if (_progression != null)
                {
                    float expPct = _progression.ExpToNextLevel > 0 ? (float)_progression.CurrentExp / _progression.ExpToNextLevel : 0f;
                    DrawBar($"LV.{_progression.CurrentLevel} EXP ({_progression.CurrentExp}/{_progression.ExpToNextLevel})", _progression.CurrentExp, _progression.ExpToNextLevel, expPct);
                }
            }

            GUILayout.Space(2);
            // Wave & Stats
            int wave = _waveManager != null ? _waveManager.CurrentWave : 1;
            int kills = GameManager.Instance != null ? GameManager.Instance.TotalKills : 0;
            float time = GameManager.Instance != null ? GameManager.Instance.SurvivalTimer : 0f;
            GUILayout.Label($"WAVE: {wave} | KILLS: {kills} | TIME: {time:F0}s", _subStyle);

            // --- SKIP WAVE BUTTON (แสดงขึ้นมาเฉพาะตอนที่มอนสเตอร์สปอนครบแล้ว) ---
            if (_waveManager != null && _waveManager.CanSkipWave)
            {
                GUILayout.Space(6);
                if (GUILayout.Button(">> SKIP / NEXT WAVE <<", _skipBtnStyle, GUILayout.Height(28)))
                {
                    _waveManager.SkipToNextWave();
                }
            }

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

            // --- Vampire Survivors Level Up Choice Screen ---
            if (_progression != null && _progression.IsLevelingUp && _currentChoices != null && _currentChoices.Count > 0)
            {
                DrawLevelUpScreen();
            }
        }

        private void DrawLevelUpScreen()
        {
            float w = 540;
            float h = 42 + _currentChoices.Count * 76;
            Rect rect = new Rect((Screen.width - w) / 2, (Screen.height - h) / 2, w, h);

            GUILayout.BeginArea(rect, _boxStyle);
            GUILayout.Space(6);

            GUIStyle titleStyle = new GUIStyle(_titleStyle)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label($"★ LEVEL UP! (LEVEL {_progression.CurrentLevel}) ★", titleStyle);

            GUIStyle tipStyle = new GUIStyle(_subStyle)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter
            };
            GUILayout.Label("SELECT AN UPGRADE [CLICK OR PRESS 1 - 3]", tipStyle);
            GUILayout.Space(6);

            for (int i = 0; i < _currentChoices.Count; i++)
            {
                var choice = _currentChoices[i];
                string hotkey = $"[{i + 1}]";
                string status = choice.CurrentLevel == 0 ? "[NEW!]" : $"Lv.{choice.CurrentLevel} -> Lv.{choice.CurrentLevel + 1}";
                string categoryTag = $"[{choice.Category.ToString().ToUpper()}]";

                string cardText = $"{hotkey}  {choice.DisplayName}  {status}  {categoryTag}\n" +
                                  $"     {choice.GetNextLevelDescription()}";

                if (GUILayout.Button(cardText, _cardBtnStyle, GUILayout.Height(62)))
                {
                    _progression.SelectUpgrade(choice);
                    break;
                }
                GUILayout.Space(4);
            }

            GUILayout.EndArea();
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