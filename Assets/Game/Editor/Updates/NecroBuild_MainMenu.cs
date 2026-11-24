using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// เมนู Tools > NecroDefense > Build Main Menu Scene
    /// 1) สร้างซีน Assets/Scenes/MainMenu.unity (ภาพปก + ปุ่ม PLAY / HOW TO PLAY / SETTINGS / CREDITS / QUIT)
    /// 2) ตั้ง Build Settings: MainMenu เป็นซีนแรก, Map2 เปิดใช้
    /// 3) เพิ่มปุ่ม "Main Menu" ในหน้า Pause และหน้าจบเกมของ Map2
    /// สร้างซ้ำได้ (จะถามก่อนเขียนทับ)
    /// </summary>
    public static class NecroBuild_MainMenu
    {
        private const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string GameScenePath = "Assets/Scenes/Map2.unity";
        private const string CoverPath = "Assets/Game/Art/Menu/CoverGame.png";
        private const string UiArtDir = "Assets/Game/Art/UI";
        private const string FontMatDir = "Assets/TextMesh Pro/Resources/Fonts & Materials/";

        private static readonly Color CoverRed = new Color(0.663f, 0.145f, 0.141f, 1f); // แดงเดียวกับภาพปก
        private static Sprite _card, _barFrame, _barFill, _handle, _softDisc, _vignette, _divider;
        private static Material _shadowMat;

        [MenuItem("Tools/NecroDefense/Build Main Menu Scene")]
        public static void Run()
        {
            bool exists = File.Exists(MenuScenePath);
            if (!EditorUtility.DisplayDialog("NecroDefense - Main Menu",
                    (exists ? "ซีน MainMenu มีอยู่แล้ว จะสร้างใหม่ทับ\n\n" : "") +
                    "• สร้างซีน " + MenuScenePath + "\n" +
                    "• ตั้ง Build Settings ให้ MainMenu เป็นซีนแรก → Map2\n" +
                    "• เพิ่มปุ่ม Main Menu ในหน้า Pause / จบเกม ของ Map2\n\n" +
                    "แนะนำให้ commit git ก่อนกด", exists ? "Rebuild" : "Build", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            List<string> log = new List<string>();
            try
            {
                EditorUtility.DisplayProgressBar("NecroDefense", "Importing assets...", 0.1f);
                NecroUpdate_UIAudio.ImportAssets(log);
                ImportCover(log);
                LoadAssets();

                EditorUtility.DisplayProgressBar("NecroDefense", "Building MainMenu scene...", 0.4f);
                BuildMenuScene(log);

                EditorUtility.DisplayProgressBar("NecroDefense", "Build Settings...", 0.6f);
                UpdateBuildSettings(log);

                EditorUtility.DisplayProgressBar("NecroDefense", "Adding Main Menu buttons to Map2...", 0.8f);
                PatchGameScene(log);

                EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string summary = string.Join("\n", log);
            Debug.Log("<b>[NecroDefense] Main Menu</b>\n" + summary);
            EditorUtility.DisplayDialog("Main Menu done", summary + "\n\nเปิดซีน MainMenu แล้ว กด Play ได้เลย", "OK");
        }

        // ================================================================== assets

        private static void ImportCover(List<string> log)
        {
            TextureImporter imp = AssetImporter.GetAtPath(CoverPath) as TextureImporter;
            if (imp == null) { log.Add("!! ไม่พบภาพปก " + CoverPath); return; }
            if (imp.textureType == TextureImporterType.Sprite && !imp.mipmapEnabled && imp.maxTextureSize >= 2048) return;
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = 2048;
            imp.textureCompression = TextureImporterCompression.CompressedHQ;
            imp.SaveAndReimport();
            log.Add("Imported cover image as Sprite");
        }

        private static Sprite Spr(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(UiArtDir + "/" + n + ".png");

        private static void LoadAssets()
        {
            _card = Spr("UI_Card"); _barFrame = Spr("UI_BarFrame"); _barFill = Spr("UI_BarFill"); _handle = Spr("UI_Handle");
            _softDisc = Spr("UI_SoftDisc"); _vignette = Spr("UI_Vignette"); _divider = Spr("UI_Divider");
            _shadowMat = AssetDatabase.LoadAssetAtPath<Material>(FontMatDir + "LiberationSans SDF - Drop Shadow.mat");
        }

        // ================================================================== scene

        private static void BuildMenuScene(List<string> log)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // กล้อง + EventSystem
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.AddComponent<AudioListener>();

            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            // Canvas
            GameObject cGo = new GameObject("Canvas_Menu", typeof(RectTransform));
            cGo.layer = 5;
            Canvas canvas = cGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = cGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            cGo.AddComponent<GraphicRaycaster>();
            Transform root = cGo.transform;

            // ---- พื้นหลัง
            // ขนาดจริงตามจอจะถูกจัดตอนเล่นโดย MenuBackgroundFX (ชิดซ้าย)
            RectTransform bgFit = BU.Place(BU.UI("Background", root), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            RectTransform bg = BU.Stretch(BU.UI("Cover", bgFit));
            Image bgImg = BU.Img(bg, Color.white, false, AssetDatabase.LoadAssetAtPath<Sprite>(CoverPath));
            bgImg.preserveAspect = false;

            RectTransform ash = BU.Stretch(BU.UI("Ash", root));
            RectTransform redPulse = BU.Stretch(BU.UI("RedPulse", root));
            Image redPulseImg = BU.Img(redPulse, new Color(CoverRed.r, CoverRed.g, CoverRed.b, 0f), false, _vignette);
            RectTransform vig = BU.Stretch(BU.UI("Vignette", root));
            BU.Img(vig, new Color(0f, 0f, 0f, 0.55f), false, _vignette);

            // คำโปรยใต้ชื่อเกม (อยู่ในกล่องแดงของภาพปก)
            // ImageSpace = กรอบขนาดเท่าภาพปก (จัดโดย MenuBackgroundFX) ใช้วางของให้ตรงกับภาพทุกสัดส่วนจอ
            RectTransform imageSpace = BU.Place(BU.UI("ImageSpace", root), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(1920f, 1080f));
            TextMeshProUGUI tag = BU.Text("Tagline", imageSpace, "RAISE THE DEAD  ·  HOLD THE LINE", 24f, Img(805f, 402f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(640f, 36f), TextAlignmentOptions.Center, UITheme.Paper);
            tag.textWrappingMode = TextWrappingModes.NoWrap;
            tag.fontStyle = FontStyles.Bold;
            tag.characterSpacing = 6f;
            Shadow(tag);

            // ---- กลุ่มปุ่มหลัก
            RectTransform main = BU.Stretch(BU.UI("Main", imageSpace));
            CanvasGroup mainGroup = main.gameObject.AddComponent<CanvasGroup>();
            RectTransform list = BU.Place(BU.UI("Buttons", main), Img(625f, 464f), new Vector2(0f, 1f), Vector2.zero, new Vector2(470f, 420f));

            string[] labels = { "PLAY", "HOW TO PLAY", "SETTINGS", "CREDITS", "QUIT" };
            string[] hints =
            {
                "Begin the night. Defend the Base until the last wave falls.",
                "Goal and controls.",
                "Master / Music / SFX / UI volume.",
                "The people who raised this game from the grave.",
                "Leave the crypt.",
            };
            MenuButton[] buttons = new MenuButton[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                buttons[i] = MakeButton(list, labels[i].Replace(" ", "") + "_Button", labels[i], new Vector2(0f, -i * 82f), new Vector2(470f, 70f), i == 0 ? 48f : 38f);
                buttons[i].hint = hints[i];
            }

            TextMeshProUGUI hint = BU.Text("Hint_Text", main, "", 22f, Img(625f, 925f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(760f, 34f),
                TextAlignmentOptions.Left, UITheme.Bone);
            hint.fontStyle = FontStyles.Italic;
            Shadow(hint);

            TextMeshProUGUI footer = BU.Text("Footer_Text", root, "GI351 Rapid Game  ·  Final Project", 18f, new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(-28f, 20f), new Vector2(600f, 28f), TextAlignmentOptions.Right, new Color(1f, 1f, 1f, 0.55f));
            footer.characterSpacing = 3f;
            Shadow(footer);

            // ---- หน้าต่างย่อย
            List<MenuButton> backs = new List<MenuButton>();
            RectTransform howTo = MakePanel(root, "HowToPlay_Panel", "HOW TO PLAY", new Vector2(1040f, 680f), backs, out RectTransform howBody);
            FillHowTo(howBody);
            RectTransform settings = MakePanel(root, "Settings_Panel", "SETTINGS", new Vector2(640f, 470f), backs, out RectTransform setBody);
            BuildVolumeRows(setBody);
            RectTransform credits = MakePanel(root, "Credits_Panel", "CREDITS", new Vector2(760f, 600f), backs, out RectTransform credBody);
            FillCredits(credBody);

            // ---- แดงวาบ / ดำ
            RectTransform flashRt = BU.Stretch(BU.UI("Flash", root));
            Image flash = BU.Img(flashRt, new Color(CoverRed.r, CoverRed.g, CoverRed.b, 0f), false);
            flashRt.gameObject.SetActive(false);
            RectTransform fadeRt = BU.Stretch(BU.UI("Fade", root));
            Image fade = BU.Img(fadeRt, Color.black, true);

            // ---- สคริปต์
            MenuBackgroundFX fx = cGo.AddComponent<MenuBackgroundFX>();
            fx.background = bg;
            fx.fitRoot = bgFit;
            fx.imageSpace = new[] { imageSpace };
            fx.alignX = 0f; // ชิดซ้าย: ชื่อเกมในภาพไม่ตก
            fx.ashRoot = ash;
            fx.ashSprite = _softDisc;
            fx.redPulse = redPulseImg;

            MainMenuUI ui = cGo.AddComponent<MainMenuUI>();
            ui.gameSceneName = Path.GetFileNameWithoutExtension(GameScenePath);
            ui.playButton = buttons[0];
            ui.howToButton = buttons[1];
            ui.settingsButton = buttons[2];
            ui.creditsButton = buttons[3];
            ui.quitButton = buttons[4];
            ui.howToPanel = howTo;
            ui.settingsPanel = settings;
            ui.creditsPanel = credits;
            ui.backButtons = backs.ToArray();
            ui.mainGroup = mainGroup;
            ui.hintText = hint;
            ui.pressHint = tag;
            ui.fade = fade;
            ui.flash = flash;
            ui.backgroundFX = fx;
            buttons[0].clickSound = ""; // PLAY ใช้เสียง SFX_Menu_Start แทน

            Directory.CreateDirectory(Path.GetDirectoryName(MenuScenePath));
            EditorSceneManager.SaveScene(scene, MenuScenePath);
            log.Add("Built " + MenuScenePath);
        }

        /// <summary>จุดบนภาพปก (พิกเซล 1920x1080 นับจากซ้ายบน) → anchor</summary>
        private static Vector2 Img(float x, float y) => new Vector2(x / 1920f, 1f - y / 1080f);

        private static void Shadow(TMP_Text t)
        {
            if (_shadowMat != null) t.fontSharedMaterial = _shadowMat;
        }

        /// <summary>ปุ่มเมนู: Root (รับเมาส์) > Visual (ขยาย) > Slab แดง / Tick / Underline / Label</summary>
        private static MenuButton MakeButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, float fontSize)
        {
            RectTransform rt = BU.Place(BU.UI(name, parent), new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size);
            BU.Img(rt, new Color(0f, 0f, 0f, 0f), true);

            RectTransform visual = BU.UI("Visual", rt);
            visual.anchorMin = Vector2.zero; visual.anchorMax = Vector2.one; visual.pivot = new Vector2(0.06f, 0.5f);
            visual.offsetMin = Vector2.zero; visual.offsetMax = Vector2.zero;

            RectTransform line = BU.UI("Underline", visual);
            line.anchorMin = new Vector2(0f, 0f); line.anchorMax = new Vector2(0.72f, 0f); line.pivot = new Vector2(0f, 0f);
            line.offsetMin = new Vector2(0f, 2f); line.offsetMax = new Vector2(0f, 4f);
            BU.Img(line, new Color(1f, 1f, 1f, 0.16f), false);

            RectTransform slab = BU.UI("Slab", visual);
            slab.anchorMin = new Vector2(0f, 0.06f); slab.anchorMax = new Vector2(1f, 0.94f); slab.pivot = new Vector2(0f, 0.5f);
            slab.offsetMin = Vector2.zero; slab.offsetMax = Vector2.zero;
            Image slabImg = BU.Img(slab, CoverRed, false);

            RectTransform tick = BU.Place(BU.UI("Tick", visual), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(7f, size.y * 0.55f));
            Image tickImg = BU.Img(tick, new Color(0.55f, 0.55f, 0.56f, 1f), false);

            TextMeshProUGUI label = BU.Text("Label", visual, text, fontSize, Vector2.zero, new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero,
                TextAlignmentOptions.Left, UITheme.Bone);
            label.rectTransform.anchorMin = Vector2.zero; label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = new Vector2(30f, 0f); label.rectTransform.offsetMax = new Vector2(0f, 0f);
            label.fontStyle = FontStyles.Bold;
            label.characterSpacing = 4f;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            Shadow(label);

            MenuButton mb = rt.gameObject.AddComponent<MenuButton>();
            mb.visual = visual;
            mb.slab = slab;
            mb.slabImage = slabImg;
            mb.slabColor = CoverRed;
            mb.tick = tick;
            mb.tickImage = tickImg;
            mb.label = label;
            return mb;
        }

        private static RectTransform MakePanel(Transform root, string name, string title, Vector2 size, List<MenuButton> backs, out RectTransform body)
        {
            RectTransform panel = BU.Stretch(BU.UI(name, root));
            BU.Img(panel, new Color(0f, 0f, 0f, 0.72f), true);

            RectTransform card = BU.Place(BU.UI("Card", panel), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), size);
            Image cardImg = BU.Img(card, _card != null ? Color.white : UITheme.Panel, true, _card);
            if (_card != null) cardImg.type = Image.Type.Sliced;
            UIPopIn pop = card.gameObject.AddComponent<UIPopIn>();
            pop.duration = 0.22f;
            pop.startScale = 0.88f;

            // หัวข้อ: แถบแดงแบบเดียวกับภาพปก
            RectTransform head = BU.Place(BU.UI("Header", card), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(-18f, -6f), new Vector2(380f, 64f));
            BU.Img(head, CoverRed, false);
            TextMeshProUGUI t = BU.StretchText("Title", head, title, 36f, TextAlignmentOptions.Left, UITheme.Paper, 0f);
            t.rectTransform.offsetMin = new Vector2(26f, 0f);
            t.fontStyle = FontStyles.Bold;
            t.characterSpacing = 6f;
            Shadow(t);

            body = BU.UI("Body", card);
            body.anchorMin = Vector2.zero; body.anchorMax = Vector2.one; body.pivot = new Vector2(0.5f, 0.5f);
            body.offsetMin = new Vector2(46f, 96f); body.offsetMax = new Vector2(-46f, -66f);

            MenuButton back = MakeButton(card, "Back_Button", "BACK", Vector2.zero, new Vector2(220f, 56f), 30f);
            RectTransform brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0f, 0f);
            brt.pivot = new Vector2(0f, 0f);
            brt.anchoredPosition = new Vector2(40f, 26f);
            back.hoverScale = 1.1f;
            backs.Add(back);

            panel.gameObject.SetActive(false);
            return panel;
        }

        private static void FillHowTo(RectTransform body)
        {
            Color red = new Color(0.95f, 0.3f, 0.3f);

            // ---- ซ้าย: เป้าหมาย
            Heading(body, "Goal_Title", "GOAL", new Vector2(0f, 0f), red);
            TextMeshProUGUI goal = BU.Text("Goal_Text", body,
                "Survive every wave and keep your <b>Base</b> alive.\n\n" +
                "Fallen enemies leave corpses. <b>Raise them</b> into minions that fight for you.\n\n" +
                "Build <b>towers</b> on empty slots, then level up to choose new skills.\n\n" +
                "If the Base or the Necromancer falls, the night is lost.",
                22f, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -44f), new Vector2(380f, 440f), TextAlignmentOptions.TopLeft, UITheme.Bone);
            goal.textWrappingMode = TextWrappingModes.Normal;
            goal.lineSpacing = 4f;

            RectTransform div = BU.Place(BU.UI("Divider", body), new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(418f, -4f), new Vector2(2f, 470f));
            BU.Img(div, new Color(1f, 1f, 1f, 0.18f), false);

            // ---- ขวา: ปุ่มควบคุม (คอลัมน์ปุ่ม ชิดขวา | คอลัมน์การกระทำ ชิดซ้าย บรรทัดตรงกัน)
            Heading(body, "Controls_Title", "CONTROLS", new Vector2(450f, 0f), red);
            string[,] rows =
            {
                { "WASD", "Move" },
                { "Mouse wheel", "Zoom" },
                { "Click corpse", "Raise Dead" },
                { "Q", "Mass Raise (nearby corpses)" },
                { "R", "Change summon batch" },
                { "Click slot / tower", "Build menu" },
                { "Click minion", "Command menu" },
                { "Right-click", "Teleport" },
                { "E", "Arcane Nova" },
                { "Tab", "Minion Bag" },
                { "1 - 5", "Pick minion, click to place" },
                { "Esc", "Pause / cancel" },
            };
            string keys = "", acts = "";
            for (int i = 0; i < rows.GetLength(0); i++)
            {
                keys += (i > 0 ? "\n" : "") + rows[i, 0];
                acts += (i > 0 ? "\n" : "") + rows[i, 1];
            }
            TextMeshProUGUI k = BU.Text("Controls_Keys", body, keys, 20f, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(450f, -44f), new Vector2(180f, 440f),
                TextAlignmentOptions.TopRight, UITheme.Paper);
            k.fontStyle = FontStyles.Bold;
            TextMeshProUGUI a = BU.Text("Controls_Actions", body, acts, 20f, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(646f, -44f), new Vector2(310f, 440f),
                TextAlignmentOptions.TopLeft, UITheme.Muted);
            foreach (TextMeshProUGUI t in new[] { k, a })
            {
                t.textWrappingMode = TextWrappingModes.NoWrap;
                t.lineSpacing = 12f;
            }
        }

        private static void Heading(RectTransform body, string name, string text, Vector2 pos, Color color)
        {
            TextMeshProUGUI h = BU.Text(name, body, text, 24f, new Vector2(0f, 1f), new Vector2(0f, 1f), pos, new Vector2(300f, 32f), TextAlignmentOptions.TopLeft, color);
            h.fontStyle = FontStyles.Bold;
            h.characterSpacing = 4f;
            RectTransform line = BU.Place(BU.UI(name + "_Line", body), new Vector2(0f, 1f), new Vector2(0f, 1f), pos + new Vector2(0f, -32f), new Vector2(60f, 3f));
            BU.Img(line, color, false);
        }

        private static void FillCredits(RectTransform body)
        {
            string red = "#" + ColorUtility.ToHtmlStringRGB(new Color(0.95f, 0.3f, 0.3f));
            TextMeshProUGUI t = BU.StretchText("Credits_Text", body,
                "<size=44><b>NecroDefense</b></size>\n" +
                "<color=#9E9EA0>GI351 Rapid Game Development  ·  Final Project</color>\n\n" +
                "<color=" + red + "><b>TEAM</b></color>\n" +
                "Tine  —  Game Design · UI · Audio\n" +
                "Member 2  —  Role\n" +
                "Member 3  —  Role\n" +
                "Member 4  —  Role\n\n" +
                "<color=#9E9EA0>Made with Unity 6</color>",
                24f, TextAlignmentOptions.Top, UITheme.Bone, 0f);
            t.textWrappingMode = TextWrappingModes.Normal;
            t.lineSpacing = 8f;
        }

        /// <summary>แถบปรับเสียง 4 ช่อง (ใช้ VolumeSliderUI ตัวเดียวกับหน้า Pause ค่าจะจำข้ามซีน)</summary>
        private static void BuildVolumeRows(RectTransform body)
        {
            string[] names = { "Master", "Music", "SFX", "UI" };
            AudioChannel[] ch = { AudioChannel.Master, AudioChannel.Music, AudioChannel.SFX, AudioChannel.UI };
            for (int i = 0; i < 4; i++)
            {
                RectTransform row = BU.Place(BU.UI("Volume_" + names[i], body), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f - i * 62f), new Vector2(520f, 48f));
                TextMeshProUGUI label = BU.Text("Label", row, names[i].ToUpper(), 26f, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(130f, 40f),
                    TextAlignmentOptions.Left, UITheme.Bone);
                label.fontStyle = FontStyles.Bold;
                label.characterSpacing = 3f;
                TextMeshProUGUI value = BU.Text("Value", row, "100%", 24f, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(80f, 40f),
                    TextAlignmentOptions.Right, UITheme.Muted);
                Shadow(label); Shadow(value);

                GameObject sGo = DefaultControls.CreateSlider(new DefaultControls.Resources { standard = _barFrame, background = _barFrame, knob = _handle });
                sGo.name = "Slider";
                sGo.transform.SetParent(row, false);
                foreach (Transform tr in sGo.GetComponentsInChildren<Transform>(true)) tr.gameObject.layer = 5;
                RectTransform srt = (RectTransform)sGo.transform;
                BU.Place(srt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140f, 0f), new Vector2(290f, 22f));
                Slider slider = sGo.GetComponent<Slider>();
                slider.navigation = new Navigation { mode = Navigation.Mode.None };
                Image bgI = srt.Find("Background").GetComponent<Image>();
                bgI.type = Image.Type.Sliced;
                bgI.color = Color.white;
                Image fill = srt.Find("Fill Area/Fill").GetComponent<Image>();
                if (_barFill != null) fill.sprite = _barFill;
                fill.type = Image.Type.Simple;
                fill.color = CoverRed;
                RectTransform fillArea = (RectTransform)srt.Find("Fill Area");
                fillArea.offsetMin = new Vector2(3f, 3f); fillArea.offsetMax = new Vector2(-3f, -3f);
                fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;
                Image knob = srt.Find("Handle Slide Area/Handle").GetComponent<Image>();
                knob.color = Color.white;
                knob.rectTransform.sizeDelta = new Vector2(28f, 0f);
                ColorBlock cb = slider.colors;
                cb.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                cb.highlightedColor = Color.white;
                cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
                slider.colors = cb;

                VolumeSliderUI v = row.gameObject.AddComponent<VolumeSliderUI>();
                v.channel = ch[i];
                v.slider = slider;
                v.valueText = value;
            }
        }

        // ================================================================== build settings

        private static void UpdateBuildSettings(List<string> log)
        {
            List<EditorBuildSettingsScene> list = EditorBuildSettings.scenes.ToList();
            list.RemoveAll(s => s.path == MenuScenePath);
            list.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
            int gi = list.FindIndex(s => s.path == GameScenePath);
            if (gi < 0) list.Add(new EditorBuildSettingsScene(GameScenePath, true));
            else list[gi] = new EditorBuildSettingsScene(GameScenePath, true);
            EditorBuildSettings.scenes = list.ToArray();
            log.Add("Build Settings: MainMenu (0) → Map2");
        }

        // ================================================================== Map2: ปุ่ม Main Menu

        private static void PatchGameScene(List<string> log)
        {
            if (!File.Exists(GameScenePath)) { log.Add("!! ไม่พบ " + GameScenePath + " (ข้ามการเพิ่มปุ่ม Main Menu)"); return; }
            Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
            int added = 0;
            foreach (GameObject r in scene.GetRootGameObjects())
            {
                foreach (PausePanelUI p in r.GetComponentsInChildren<PausePanelUI>(true))
                    if (AddMenuButton(p)) { added++; log.Add("Map2: Pause panel → Main Menu button"); }
                foreach (GameOverPanelUI g in r.GetComponentsInChildren<GameOverPanelUI>(true))
                    if (AddMenuButton(g)) { added++; log.Add("Map2: Game Over panel → Main Menu button"); }
            }
            if (added == 0) log.Add("Map2: ปุ่ม Main Menu มีอยู่แล้ว / ไม่พบหน้า Pause");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>ก๊อปปุ่ม Restart เป็นปุ่ม Main Menu ไว้ใต้กัน แล้วเลื่อนของที่อยู่ข้างล่างลง</summary>
        private static bool AddMenuButton(Component panel)
        {
            SerializedObject so = new SerializedObject(panel);
            SerializedProperty menuProp = so.FindProperty("menuButton");
            Button restart = so.FindProperty("restartButton")?.objectReferenceValue as Button;
            if (menuProp == null || restart == null) return false;
            if (menuProp.objectReferenceValue != null) return false;

            RectTransform orig = (RectTransform)restart.transform;
            RectTransform parent = (RectTransform)orig.parent;
            Transform existing = parent.Find("MainMenu_Button");
            GameObject dup = existing != null ? existing.gameObject : Object.Instantiate(orig.gameObject, parent);
            dup.name = "MainMenu_Button";
            Button b = dup.GetComponent<Button>();
            b.onClick.RemoveAllListeners();
            TMP_Text label = dup.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = "Main Menu";

            if (existing == null)
            {
                float shift = orig.sizeDelta.y + 15f;
                RectTransform d = (RectTransform)dup.transform;
                d.SetSiblingIndex(orig.GetSiblingIndex() + 1);
                d.anchoredPosition = orig.anchoredPosition - new Vector2(0f, shift);
                foreach (RectTransform c in parent)
                {
                    if (c == orig || c == d) continue;
                    if (c.anchorMin == orig.anchorMin && c.anchorMax == orig.anchorMax && c.anchoredPosition.y < orig.anchoredPosition.y - 1f)
                        c.anchoredPosition -= new Vector2(0f, shift);
                }
                if (parent.anchorMin == parent.anchorMax) parent.sizeDelta += new Vector2(0f, shift);
            }

            menuProp.objectReferenceValue = b;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
            return true;
        }
    }
}
