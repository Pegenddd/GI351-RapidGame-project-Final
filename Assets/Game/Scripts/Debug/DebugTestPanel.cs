using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// แผงเทสเกม (ใช้ในซีน TestArena เท่านั้น) - กด F1 เพื่อเปิด/ปิด
    /// ตั้ง Souls / Mana, เสกศัตรู, เพิ่มมินเนียน, เลเวลอัป, ให้การ์ด, God Mode, เร่งเวลา ฯลฯ
    /// ค่าเริ่มต้นตอนกด Play ตั้งได้ในหัวข้อ "Start Settings"
    /// </summary>
    public class DebugTestPanel : MonoBehaviour
    {
        [Header("Start Settings (ตอนกด Play)")]
        [SerializeField] private int startSouls = 1000;
        [SerializeField] private float startMaxMana = 100f;
        [SerializeField] private bool startInfiniteMana = false;
        [SerializeField] private bool startPlayerGod = true;
        [SerializeField] private bool startBaseGod = true;
        [Tooltip("ความแข็งศัตรูที่เสกออกมา = เหมือนเวฟที่เท่าไร")]
        [SerializeField] private int startEnemyWave = 1;
        [SerializeField] private bool startPanelOpen = true;

        [Header("Scene")]
        [Tooltip("ทางมอน (ศัตรูที่เสกจะเดินตามทางนี้)")]
        [SerializeField] private EnemyPath lane;
        [Tooltip("จุดเสก 'Center' (กลางพื้นที่อิสระ)")]
        [SerializeField] private Transform centerPoint;
        [Tooltip("ศัตรูที่เลือกเสกได้ (ลำดับ = ใน Dropdown)")]
        [SerializeField] private List<EnemyUnit> enemyPrefabs = new List<EnemyUnit>();

        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Button panelToggleButton;
        [SerializeField] private Key toggleKey = Key.F1;

        [Header("Resources")]
        [SerializeField] private TMP_InputField soulsInput;
        [SerializeField] private Button setSoulsButton;
        [SerializeField] private Button addSouls100Button;
        [SerializeField] private Button addSouls1000Button;
        [SerializeField] private TMP_InputField manaInput;
        [SerializeField] private Button setMaxManaButton;
        [SerializeField] private Button fillManaButton;
        [SerializeField] private Button infiniteManaButton;

        [Header("Cheats")]
        [SerializeField] private Button playerGodButton;
        [SerializeField] private Button baseGodButton;
        [SerializeField] private Button healAllButton;
        [SerializeField] private Button resetCooldownButton;
        [SerializeField] private List<Button> timeScaleButtons = new List<Button>();
        [SerializeField] private List<float> timeScales = new List<float> { 0.5f, 1f, 2f, 4f };

        [Header("Enemies")]
        [SerializeField] private TMP_Dropdown enemyDropdown;
        [SerializeField] private TMP_InputField enemyCountInput;
        [SerializeField] private Button spawnLaneButton;
        [SerializeField] private Button spawnCenterButton;
        [SerializeField] private Button spawnPlayerButton;
        [SerializeField] private Button waveMinusButton;
        [SerializeField] private Button wavePlusButton;
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private Button killAllButton;
        [SerializeField] private Button clearCorpsesButton;

        [Header("Minions")]
        [SerializeField] private TMP_Dropdown minionDropdown;
        [SerializeField] private Button addMinion1Button;
        [SerializeField] private Button addMinion5Button;
        [SerializeField] private Button fillMinionButton;

        [Header("Level / Cards")]
        [SerializeField] private Button levelUpButton;
        [SerializeField] private Button addXpButton;
        [SerializeField] private TMP_Dropdown cardDropdown;
        [SerializeField] private Button giveCardButton;

        [Header("Status")]
        [SerializeField] private TMP_Text statusText;

        [Header("Colors")]
        [SerializeField] private Color onColor = new Color(0.2f, 0.55f, 0.25f, 1f);
        [SerializeField] private Color offColor = new Color(0.22f, 0.22f, 0.28f, 1f);

        private bool _infiniteMana, _playerGod, _baseGod;
        private float _timeScale = 1f;
        private int _enemyWave = 1;
        private float _fps;

        private GameManager GM => GameManager.Instance;

        private void Start()
        {
            _infiniteMana = startInfiniteMana;
            _playerGod = startPlayerGod;
            _baseGod = startBaseGod;
            _enemyWave = Mathf.Max(1, startEnemyWave);
            if (GM != null)
            {
                GM.Economy.SetSouls(startSouls);
                GM.Economy.SetMaxMana(startMaxMana);
                GM.Economy.FillMana();
            }
            if (soulsInput != null) soulsInput.text = startSouls.ToString();
            if (manaInput != null) manaInput.text = startMaxMana.ToString("0");
            if (enemyCountInput != null) enemyCountInput.text = "1";
            if (panelRoot != null) panelRoot.SetActive(startPanelOpen);

            FillDropdowns();
            Wire();
            RefreshToggles();
        }

        private void FillDropdowns()
        {
            if (enemyDropdown != null)
            {
                enemyDropdown.ClearOptions();
                List<string> names = new List<string>();
                foreach (EnemyUnit e in enemyPrefabs) names.Add(e != null ? e.name.Replace("Enemy_", "") : "-");
                enemyDropdown.AddOptions(names);
            }
            if (minionDropdown != null && GM != null)
            {
                minionDropdown.ClearOptions();
                List<string> names = new List<string>();
                foreach (MinionBagEntry b in GM.Minions.Bag) names.Add(b.minionPrefab != null ? b.minionPrefab.DisplayName : "-");
                minionDropdown.AddOptions(names);
            }
            if (cardDropdown != null && GM != null)
            {
                cardDropdown.ClearOptions();
                List<string> names = new List<string>();
                foreach (UpgradeCard c in GM.LevelUp.CardPool) names.Add(c != null ? c.title : "-");
                cardDropdown.AddOptions(names);
            }
        }

        private static void On(Button b, UnityEngine.Events.UnityAction a) { if (b != null) b.onClick.AddListener(a); }

        private void Wire()
        {
            On(panelToggleButton, TogglePanel);

            On(setSoulsButton, () => { if (int.TryParse(soulsInput.text, out int v)) GM.Economy.SetSouls(v); });
            On(addSouls100Button, () => GM.Economy.AddSouls(100));
            On(addSouls1000Button, () => GM.Economy.AddSouls(1000));
            On(setMaxManaButton, () => { if (float.TryParse(manaInput.text, out float v)) { GM.Economy.SetMaxMana(v); GM.Economy.FillMana(); } });
            On(fillManaButton, () => GM.Economy.FillMana());
            On(infiniteManaButton, () => { _infiniteMana = !_infiniteMana; RefreshToggles(); });

            On(playerGodButton, () => { _playerGod = !_playerGod; RefreshToggles(); });
            On(baseGodButton, () => { _baseGod = !_baseGod; RefreshToggles(); });
            On(healAllButton, HealAll);
            On(resetCooldownButton, () => { if (GM.PlayerSkills != null) GM.PlayerSkills.DebugResetCooldowns(); });
            for (int i = 0; i < timeScaleButtons.Count && i < timeScales.Count; i++)
            {
                float s = timeScales[i];
                On(timeScaleButtons[i], () => { _timeScale = s; RefreshToggles(); });
            }

            On(spawnLaneButton, () => Spawn(lane != null ? lane.SpawnPosition : Vector2.zero));
            On(spawnCenterButton, () => Spawn(centerPoint != null ? (Vector2)centerPoint.position : Vector2.zero));
            On(spawnPlayerButton, () => Spawn(GM.Player != null ? GM.Player.Position + new Vector2(-3f, 0f) : Vector2.zero));
            On(waveMinusButton, () => { _enemyWave = Mathf.Max(1, _enemyWave - 1); RefreshToggles(); });
            On(wavePlusButton, () => { _enemyWave = Mathf.Min(99, _enemyWave + 1); RefreshToggles(); });
            On(killAllButton, KillAllEnemies);
            On(clearCorpsesButton, ClearCorpses);

            On(addMinion1Button, () => AddMinion(1));
            On(addMinion5Button, () => AddMinion(5));
            On(fillMinionButton, () => { MinionUnit m = SelectedMinion(); if (m != null) GM.Minions.DebugFillToCap(m); });

            On(levelUpButton, () => GM.LevelUp.DebugLevelUp());
            On(addXpButton, () => GM.LevelUp.AddXp(10));
            On(giveCardButton, GiveCard);
        }

        private void Update()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb[toggleKey].wasPressedThisFrame) TogglePanel();
            if (GM == null) return;

            if (_infiniteMana) GM.Economy.FillMana();
            if (GM.Player != null) GM.Player.Invincible = _playerGod;
            if (GM.Crystal != null) GM.Crystal.Invincible = _baseGod;
            if (GM.State == GameState.Playing) Time.timeScale = _timeScale;

            _fps = Mathf.Lerp(_fps, 1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime), 0.05f);
            if (statusText != null)
                statusText.text = "Enemies " + Registry.Enemies.Count + "   Minions " + Registry.Minions.Count +
                                  "   Corpses " + Registry.Corpses.Count + "\nLv " + GM.LevelUp.Level +
                                  "   XP " + GM.LevelUp.CurrentXp + "/" + GM.LevelUp.XpToNextLevel +
                                  "   FPS " + Mathf.RoundToInt(_fps);
        }

        private void TogglePanel()
        {
            if (panelRoot != null) panelRoot.SetActive(!panelRoot.activeSelf);
        }

        private void RefreshToggles()
        {
            Paint(infiniteManaButton, _infiniteMana);
            Paint(playerGodButton, _playerGod);
            Paint(baseGodButton, _baseGod);
            for (int i = 0; i < timeScaleButtons.Count && i < timeScales.Count; i++)
                Paint(timeScaleButtons[i], Mathf.Approximately(timeScales[i], _timeScale));
            if (waveLabel != null && GM != null)
                waveLabel.text = "Wave " + _enemyWave + "  (HP x" + GM.Waves.HealthMultiplierForWave(_enemyWave).ToString("0.00") + ")";
        }

        private void Paint(Button b, bool on)
        {
            if (b != null && b.targetGraphic != null) b.targetGraphic.color = on ? onColor : offColor;
        }

        // ---------------- Actions ----------------

        private void Spawn(Vector2 at)
        {
            if (enemyDropdown == null || enemyPrefabs.Count == 0) return;
            EnemyUnit prefab = enemyPrefabs[Mathf.Clamp(enemyDropdown.value, 0, enemyPrefabs.Count - 1)];
            if (prefab == null) return;
            int count = 1;
            if (enemyCountInput != null) int.TryParse(enemyCountInput.text, out count);
            count = Mathf.Clamp(count, 1, 200);

            float hp = GM.Waves.HealthMultiplierForWave(_enemyWave), dmg = GM.Waves.DamageMultiplierForWave(_enemyWave);
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = at + Random.insideUnitCircle * Mathf.Min(2.5f, 0.4f + count * 0.08f);
                EnemyUnit e = Instantiate(prefab, pos, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Enemies));
                e.ApplyWaveScaling(hp, dmg);
                e.SetPathFromHere(lane);
            }
        }

        private void KillAllEnemies()
        {
            List<EnemyUnit> list = new List<EnemyUnit>(Registry.Enemies);
            foreach (EnemyUnit e in list) if (e != null) e.DebugKill();
        }

        private void ClearCorpses()
        {
            List<Corpse> list = new List<Corpse>(Registry.Corpses);
            foreach (Corpse c in list) if (c != null) c.Consume();
        }

        private void HealAll()
        {
            if (GM.Player != null) GM.Player.HealPercent(1f);
            if (GM.Crystal != null) GM.Crystal.HealPercent(1f);
            foreach (MinionUnit m in Registry.Minions) m.HealPercent(1f);
        }

        private MinionUnit SelectedMinion()
        {
            if (minionDropdown == null) return null;
            var bag = GM.Minions.Bag;
            int i = minionDropdown.value;
            return i >= 0 && i < bag.Count ? bag[i].minionPrefab : null;
        }

        private void AddMinion(int amount)
        {
            MinionUnit m = SelectedMinion();
            if (m != null) GM.Minions.AddToBag(m, amount, "Test");
        }

        private void GiveCard()
        {
            if (cardDropdown == null) return;
            var pool = GM.LevelUp.CardPool;
            int i = cardDropdown.value;
            if (i >= 0 && i < pool.Count && pool[i] != null) GM.LevelUp.DebugGiveCard(pool[i]);
        }
    }
}
