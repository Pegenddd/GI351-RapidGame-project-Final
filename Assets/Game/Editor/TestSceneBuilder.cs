using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// เมนู Tools > NecroDefense > Build Test Arena Scene
    /// สร้างซีนเทส Assets/Scenes/TestArena.unity (ทางมอนเลนเดียว -> พื้นที่อิสระ -> ป้อมหลัก) + แผงเทส (F1)
    /// *** ไม่แตะซีน Gameplay และไม่ทับ Prefab / การ์ดที่มีอยู่ ***
    /// </summary>
    public static class TestSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/TestArena.unity";

        private static readonly Color GroundColor = new Color(0.33f, 0.33f, 0.35f);
        private static readonly Color LaneColor = new Color(0.88f, 0.88f, 0.87f);
        private static readonly Color Gold = new Color(1f, 0.85f, 0.35f);

        [MenuItem("Tools/NecroDefense/Build Test Arena Scene")]
        public static void BuildMenu()
        {
            if (System.IO.File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Test Arena", "TestArena.unity มีอยู่แล้ว สร้างใหม่ทับเฉพาะซีนเทสนี้ไหม?\n(ซีน Gameplay / Prefab / การ์ด ไม่ถูกแตะ)", "สร้างใหม่", "ยกเลิก"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                EditorUtility.DisplayProgressBar("NecroDefense", "Building Test Arena...", 0.3f);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BU.CreateSprites();
                GameplayPrefabBuilder.Overwrite = false;   // ใช้ Prefab ที่มีอยู่ ไม่ทับ
                GameplayPrefabBuilder p = new GameplayPrefabBuilder();
                p.BuildAll();

                BuildScene(p);

                BU.EnsureFolder("Assets/Scenes");
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
                AddToBuildSettings();
                Debug.Log("<b>[NecroDefense]</b> Test Arena built: " + ScenePath + "  (กด F1 ในเกมเพื่อเปิด/ปิดแผงเทส)");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        /// <summary>เพิ่มเข้า Build Settings ต่อท้าย (ไม่ย้าย Gameplay) เพื่อให้ปุ่ม Restart ใช้ได้</summary>
        private static void AddToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == ScenePath)) return;
            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ==================================================================

        private static void BuildScene(GameplayPrefabBuilder p)
        {
            GameplaySceneBuilder.MapRefs m = new GameplaySceneBuilder.MapRefs();
            m.follow = GameplaySceneBuilder.BuildCameraAndSystems();

            // ---------- Map: เลนเดียว ซ้าย -> ขวา ----------
            GameObject map = BU.Go("Map (Test Arena)");
            MapBounds bounds = map.AddComponent<MapBounds>();
            new SO(bounds).Set("size", new Vector2(32f, 16f)).Apply();
            m.bounds = bounds;

            GameObject ground = BU.Go("Ground", map.transform);
            BU.Sprite("Ground", ground.transform, BU.Square, GroundColor, new Vector2(36f, 20f), -30);
            BU.Sprite("EnemyLane", ground.transform, BU.Square, LaneColor, new Vector2(19f, 2.6f), -28, new Vector3(-6.5f, 0f, 0f));
            BU.Sprite("FreeZone_Area", ground.transform, BU.Square, LaneColor, new Vector2(9f, 8f), -28, new Vector3(7.5f, 0f, 0f));

            GameObject freeZone = BU.Go("FreeZone (Area)", map.transform, new Vector3(7.5f, 0f, 0f));
            new SO(freeZone.AddComponent<FreeZone>()).Set("size", new Vector2(9f, 8f)).Apply();

            m.crystal = GameplaySceneBuilder.BuildMainBase(p, map.transform, new Vector3(13.5f, 0f, 0f));

            GameObject slots = BU.Go("TowerSlots", map.transform);
            Vector3[] slotPos = { new Vector3(-9f, 2.7f), new Vector3(-9f, -2.7f), new Vector3(-2f, 2.7f), new Vector3(-2f, -2.7f) };
            for (int i = 0; i < slotPos.Length; i++) GameplaySceneBuilder.BuildTowerSlot(slots.transform, i + 1, slotPos[i]);

            GameObject paths = BU.Go("EnemyPaths", map.transform);
            m.west = GameplaySceneBuilder.MakePath("Path_Lane", paths.transform, new Vector3(-15.5f, 0f), new Vector3(-13.5f, 0f),
                new Vector3(-13.5f, 0f), new Vector3(-4f, 0f), new Vector3(3.5f, 0f), new Vector3(11.4f, 0f));

            Transform center = BU.Go("TestSpawn_Center", map.transform, new Vector3(6f, 0f, 0f)).transform;

            m.playerStart = new Vector3(6f, 2f, 0f);
            GameplaySceneBuilder.BuildMapHelpers(map.transform, m);

            // ---------- ผู้เล่น / Manager / UI (เหมือนซีนจริง แต่ไม่มีเวฟ) ----------
            GameObject gmGo = GameplaySceneBuilder.BuildCore(p, m, false);

            // ---------- แผงเทส ----------
            GameObject canvas = GameObject.Find("Canvas_Gameplay");
            DebugTestPanel panel = BuildDebugPanel(canvas.transform, p, m.west, center);
            Selection.activeGameObject = panel.gameObject;
            gmGo.transform.SetSiblingIndex(0);
        }

        // ==================================================================
        // Debug panel UI
        // ==================================================================

        private const float PanelW = 440f, RowH = 34f, Gap = 6f, Pad = 10f;
        private static readonly Vector2 TL = new Vector2(0f, 1f);

        private static float RowY(int row) => -(Pad + row * (RowH + Gap));

        private static TextMeshProUGUI Label(Transform parent, string text, int row, float x, float w, float size = 16f, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            return BU.Text("Label_" + text.Replace(" ", ""), parent, text, size, TL, TL, new Vector2(x, RowY(row)), new Vector2(w, RowH), align, color);
        }

        private static Button Btn(Transform parent, string name, string text, int row, float x, float w)
        {
            return BU.Button(name, parent, text, TL, TL, new Vector2(x, RowY(row)), new Vector2(w, RowH), out _, 15f);
        }

        private static TMP_InputField Input(Transform parent, string name, int row, float x, float w, TMP_InputField.ContentType type)
        {
            GameObject go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            BU.Place((RectTransform)go.transform, TL, TL, new Vector2(x, RowY(row)), new Vector2(w, RowH));
            TMP_InputField f = go.GetComponent<TMP_InputField>();
            f.contentType = type;
            f.pointSize = 16f;
            go.GetComponent<Image>().color = new Color(0.92f, 0.92f, 0.95f);
            return f;
        }

        private static TMP_Dropdown Drop(Transform parent, string name, int row, float x, float w)
        {
            GameObject go = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            BU.Place((RectTransform)go.transform, TL, TL, new Vector2(x, RowY(row)), new Vector2(w, RowH));
            TMP_Dropdown d = go.GetComponent<TMP_Dropdown>();
            if (d.captionText != null) d.captionText.fontSize = 16f;
            if (d.itemText != null) d.itemText.fontSize = 16f;
            RectTransform tpl = d.template;
            if (tpl != null) tpl.sizeDelta = new Vector2(tpl.sizeDelta.x, 240f);
            go.GetComponent<Image>().color = new Color(0.92f, 0.92f, 0.95f);
            return d;
        }

        private static void Header(Transform parent, string text, int row)
        {
            Label(parent, text, row, Pad, PanelW - Pad * 2, 17f, Gold).fontStyle = FontStyles.Bold;
        }

        private static DebugTestPanel BuildDebugPanel(Transform canvas, GameplayPrefabBuilder p, EnemyPath lane, Transform center)
        {
            RectTransform holder = BU.Stretch(BU.UI("TestPanel (F1)", canvas));
            // วางใต้ Tooltip (Tooltip ต้องอยู่บนสุด)
            Transform tooltip = canvas.Find("Tooltip");
            if (tooltip != null) holder.SetSiblingIndex(tooltip.GetSiblingIndex());

            Button toggle = BU.Button("TestPanel_Toggle", holder, "TEST (F1)", TL, TL, new Vector2(72f, -160f), new Vector2(110f, 44f), out _, 16f,
                new Color(0.45f, 0.15f, 0.15f, 1f));

            RectTransform panel = BU.Panel("Panel", holder, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, -30f),
                new Vector2(PanelW, Pad * 2 + 17 * (RowH + Gap)), new Color(0.04f, 0.04f, 0.07f, 0.93f));
            Transform t = panel;
            float x0 = Pad, lw = 64f;

            int r = 0;
            Header(t, "TEST PANEL   (F1 = show / hide)", r++);

            // Souls
            Label(t, "Souls", r, x0, lw);
            TMP_InputField souls = Input(t, "Souls_Input", r, x0 + lw, 100f, TMP_InputField.ContentType.IntegerNumber);
            Button setSouls = Btn(t, "Souls_Set", "Set", r, x0 + lw + 106f, 56f);
            Button souls100 = Btn(t, "Souls_+100", "+100", r, x0 + lw + 168f, 70f);
            Button souls1000 = Btn(t, "Souls_+1000", "+1000", r, x0 + lw + 244f, 106f);
            r++;

            // Mana
            Label(t, "Max MP", r, x0, lw);
            TMP_InputField mana = Input(t, "Mana_Input", r, x0 + lw, 100f, TMP_InputField.ContentType.DecimalNumber);
            Button setMana = Btn(t, "Mana_SetMax", "Set", r, x0 + lw + 106f, 56f);
            Button fillMana = Btn(t, "Mana_Fill", "Fill", r, x0 + lw + 168f, 70f);
            Button infMana = Btn(t, "Mana_Infinite", "Infinite", r, x0 + lw + 244f, 106f);
            r++;

            // God / heal
            Label(t, "God", r, x0, lw);
            Button godPlayer = Btn(t, "God_Player", "Player", r, x0 + lw, 100f);
            Button godBase = Btn(t, "God_Base", "Main Base", r, x0 + lw + 106f, 120f);
            Button healAll = Btn(t, "HealAll", "Heal All", r, x0 + lw + 232f, 118f);
            r++;

            // Speed
            Label(t, "Speed", r, x0, lw);
            string[] speeds = { "0.5x", "1x", "2x", "4x" };
            List<Button> speedBtns = new List<Button>();
            for (int i = 0; i < speeds.Length; i++) speedBtns.Add(Btn(t, "Speed_" + speeds[i], speeds[i], r, x0 + lw + i * 88f, 82f));
            r++;

            // Enemies
            Header(t, "SPAWN ENEMY", r++);
            TMP_Dropdown enemyDrop = Drop(t, "Enemy_Dropdown", r, x0, 250f);
            Label(t, "Count", r, x0 + 262f, 60f);
            TMP_InputField count = Input(t, "Enemy_Count", r, x0 + 322f, 98f, TMP_InputField.ContentType.IntegerNumber);
            r++;
            Label(t, "Spawn at", r, x0, 80f);
            Button atLane = Btn(t, "Spawn_Lane", "Lane start", r, x0 + 80f, 110f);
            Button atCenter = Btn(t, "Spawn_Center", "Free zone", r, x0 + 196f, 110f);
            Button atPlayer = Btn(t, "Spawn_Player", "Near me", r, x0 + 312f, 108f);
            r++;
            Button waveMinus = Btn(t, "Wave_-", "-", r, x0, 40f);
            TextMeshProUGUI waveLabel = Label(t, "Wave 1", r, x0 + 46f, 274f, 16f, null, TextAlignmentOptions.Center);
            waveLabel.name = "Wave_Label";
            Button wavePlus = Btn(t, "Wave_+", "+", r, x0 + 326f, 40f);
            Label(t, "strength", r, x0 + 372f, 60f, 13f, new Color(1f, 1f, 1f, 0.6f));
            r++;
            Button killAll = Btn(t, "KillAll", "Kill all enemies", r, x0, 205f);
            Button clearCorpses = Btn(t, "ClearCorpses", "Clear corpses", r, x0 + 215f, 205f);
            r++;

            // Minions
            Header(t, "MINION BAG", r++);
            TMP_Dropdown minionDrop = Drop(t, "Minion_Dropdown", r, x0, 170f);
            Button add1 = Btn(t, "Minion_+1", "+1", r, x0 + 176f, 60f);
            Button add5 = Btn(t, "Minion_+5", "+5", r, x0 + 242f, 60f);
            Button fill = Btn(t, "Minion_FillCap", "Fill cap", r, x0 + 308f, 112f);
            r++;

            // Level / cards
            Header(t, "LEVEL / CARDS / SKILLS", r++);
            Button lvUp = Btn(t, "LevelUp", "Level Up", r, x0, 130f);
            Button xp = Btn(t, "XP_+10", "+10 XP", r, x0 + 136f, 100f);
            Button resetCd = Btn(t, "ResetCooldowns", "Reset cooldowns", r, x0 + 242f, 178f);
            r++;
            TMP_Dropdown cardDrop = Drop(t, "Card_Dropdown", r, x0, 300f);
            Button giveCard = Btn(t, "GiveCard", "Give card", r, x0 + 306f, 114f);
            r++;

            TextMeshProUGUI status = BU.Text("Status_Text", t, "", 15f, TL, TL, new Vector2(x0, RowY(r)), new Vector2(PanelW - Pad * 2, RowH * 1.6f),
                TextAlignmentOptions.TopLeft, new Color(0.7f, 1f, 0.8f));

            DebugTestPanel ui = holder.gameObject.AddComponent<DebugTestPanel>();
            new SO(ui)
                .Set("lane", lane).Set("centerPoint", center)
                .ObjList("enemyPrefabs", p.EnemySlime, p.EnemySlimeSmall, p.EnemyGoblin, p.EnemyVulture, p.EnemyArcher, p.EnemyGiant)
                .Set("panelRoot", panel.gameObject).Set("panelToggleButton", toggle)
                .Set("soulsInput", souls).Set("setSoulsButton", setSouls).Set("addSouls100Button", souls100).Set("addSouls1000Button", souls1000)
                .Set("manaInput", mana).Set("setMaxManaButton", setMana).Set("fillManaButton", fillMana).Set("infiniteManaButton", infMana)
                .Set("playerGodButton", godPlayer).Set("baseGodButton", godBase).Set("healAllButton", healAll).Set("resetCooldownButton", resetCd)
                .ObjList("timeScaleButtons", speedBtns.Cast<Object>().ToArray())
                .Set("enemyDropdown", enemyDrop).Set("enemyCountInput", count)
                .Set("spawnLaneButton", atLane).Set("spawnCenterButton", atCenter).Set("spawnPlayerButton", atPlayer)
                .Set("waveMinusButton", waveMinus).Set("wavePlusButton", wavePlus).Set("waveLabel", waveLabel)
                .Set("killAllButton", killAll).Set("clearCorpsesButton", clearCorpses)
                .Set("minionDropdown", minionDrop).Set("addMinion1Button", add1).Set("addMinion5Button", add5).Set("fillMinionButton", fill)
                .Set("levelUpButton", lvUp).Set("addXpButton", xp).Set("cardDropdown", cardDrop).Set("giveCardButton", giveCard)
                .Set("statusText", status)
                .Apply();
            return ui;
        }
    }
}
