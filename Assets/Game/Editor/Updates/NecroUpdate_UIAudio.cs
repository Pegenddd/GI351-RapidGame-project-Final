using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// อัปเดต "ธีม UI ขาว-ดำ + ระบบเสียง" ให้ซีน Gameplay (กดซ้ำได้ ไม่เพิ่มของซ้ำ)
    /// - ตั้งค่า Import ของรูปธีม (Assets/Game/Art/UI) และไฟล์เสียง (Assets/Game/Resources/Audio)
    /// - แปลงสี UI ทั้งหมดเป็นขาว-ดำ-เทา, ใส่กรอบแผง/ช่อง/ปุ่ม/หลอดแบบใหม่, ตัวหนังสือมีเงา
    /// - ใส่เอฟเฟกต์ปุ่ม (ชี้/กด + เสียง), แผงเด้งเข้า, ขอบจอมืดตอน HP ต่ำ
    /// - เพิ่มแถบปรับเสียง Master / Music / SFX / UI ในหน้า Pause
    /// - เพิ่ม AudioManager ใต้ GameManager
    /// ของที่ปรับเองทีหลังใน Inspector ไม่ถูกแตะ ถ้าไม่ได้กดเมนูนี้ซ้ำ
    /// </summary>
    public static class NecroUpdate_UIAudio
    {
        private const string ScenePath = "Assets/Scenes/Gameplay.unity";
        private const string UiArtDir = "Assets/Game/Art/UI";
        private const string SfxDir = "Assets/Game/Resources/Audio/SFX";
        private const string MusicDir = "Assets/Game/Resources/Audio/Music";
        private const string FontMatDir = "Assets/TextMesh Pro/Resources/Fonts & Materials/";

        private static Sprite _panel, _card, _slot, _button, _round, _badge, _barFrame, _barFill, _vignette, _softDisc, _handle, _divider;
        private static Material _shadowMat, _outlineMat;

        [MenuItem("Tools/NecroDefense/Update - UI Theme (Red-White) + Audio")]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog("NecroDefense - UI Theme + Audio",
                    "จะแก้ซีน Gameplay ดังนี้:\n\n" +
                    "• UI ธีมขาว-ดำ-เทา + แดงเลือดเป็นสีเน้น (แบบ Othercide) + กรอบแผง/ปุ่ม/หลอดแบบใหม่\n" +
                    "• เอฟเฟกต์ปุ่ม (ชี้/กด + เสียง), แผงเด้งเข้า, ขอบจอมืดตอน HP ต่ำ\n" +
                    "• แถบปรับเสียง 4 ช่องในหน้า Pause\n" +
                    "• เพิ่ม AudioManager (เพลง + เสียงประกอบ)\n\n" +
                    "แนะนำให้ commit git ก่อนกด", "Apply", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            List<string> log = new List<string>();
            try
            {
                EditorUtility.DisplayProgressBar("NecroDefense", "Importing UI sprites & audio...", 0.2f);
                ImportAssets(log);

                EditorUtility.DisplayProgressBar("NecroDefense", "Updating Gameplay scene...", 0.6f);
                Scene scene = SceneManager.GetActiveScene();
                if (scene.path != ScenePath)
                {
                    if (!System.IO.File.Exists(ScenePath)) { EditorUtility.DisplayDialog("Not found", ScenePath + " not found", "OK"); return; }
                    scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                }
                ApplyToScene(log);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string summary = string.Join("\n", log);
            Debug.Log("<b>[NecroDefense] UI Theme + Audio</b>\n" + summary);
            EditorUtility.DisplayDialog("UI Theme + Audio done", summary.Length > 1500 ? summary.Substring(0, 1500) + "\n..." : summary, "OK");
        }

        // ================================================================== assets

        private static readonly Dictionary<string, int> Borders = new Dictionary<string, int>
        {
            { "UI_Panel", 24 }, { "UI_Card", 40 }, { "UI_Slot", 16 }, { "UI_Button", 18 }, { "UI_Badge", 10 }, { "UI_BarFrame", 8 },
        };

        public static void ImportAssets(List<string> log)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { UiArtDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
                if (imp == null) continue;
                string n = System.IO.Path.GetFileNameWithoutExtension(path);
                Borders.TryGetValue(n, out int b);
                Vector4 border = new Vector4(b, b, b, b);
                if (imp.textureType == TextureImporterType.Sprite && imp.spriteBorder == border && !imp.mipmapEnabled) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Clamp;
                imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                TextureImporterSettings s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                s.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(s);
                imp.spriteBorder = border;
                imp.SaveAndReimport();
                log.Add("Imported UI sprite " + n);
            }

            int sfx = 0, mus = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { SfxDir, MusicDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioImporter imp = AssetImporter.GetAtPath(path) as AudioImporter;
                if (imp == null) continue;
                bool music = path.StartsWith(MusicDir);
                AudioImporterSampleSettings st = imp.defaultSampleSettings;
                AudioClipLoadType want = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
                if (st.loadType == want) continue;
                st.loadType = want;
                st.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.PCM;
                st.quality = 0.6f;
                imp.defaultSampleSettings = st;
                imp.loadInBackground = music;
                imp.SaveAndReimport();
                if (music) mus++; else sfx++;
            }
            if (sfx + mus > 0) log.Add("Audio import settings: " + sfx + " SFX, " + mus + " music");

            _panel = Spr("UI_Panel"); _card = Spr("UI_Card"); _slot = Spr("UI_Slot"); _button = Spr("UI_Button");
            _round = Spr("UI_RoundSlot"); _badge = Spr("UI_Badge"); _barFrame = Spr("UI_BarFrame"); _barFill = Spr("UI_BarFill");
            _vignette = Spr("UI_Vignette"); _softDisc = Spr("UI_SoftDisc"); _handle = Spr("UI_Handle"); _divider = Spr("UI_Divider");
            _shadowMat = AssetDatabase.LoadAssetAtPath<Material>(FontMatDir + "LiberationSans SDF - Drop Shadow.mat");
            _outlineMat = AssetDatabase.LoadAssetAtPath<Material>(FontMatDir + "LiberationSans SDF - Outline.mat");
        }

        private static Sprite Spr(string n) => AssetDatabase.LoadAssetAtPath<Sprite>(UiArtDir + "/" + n + ".png");

        // ================================================================== scene

        /// <summary>แต่งซีนที่เปิดอยู่ (เรียกต่อจาก ImportAssets)</summary>
        public static void ApplyToScene(List<string> log)
        {
            if (_panel == null) ImportAssets(log);
            EnsureAudioManager(log);

            Canvas canvas = null;
            foreach (Canvas c in FindAll<Canvas>())
                if (c.name.StartsWith("Canvas_Gameplay")) { canvas = c; break; }
            if (canvas == null) { log.Add("!! Canvas_Gameplay not found - UI skipped"); return; }
            Transform root = canvas.transform;

            // 1) ทุกอย่างเป็นขาว-ดำ-เทา
            int mono = 0;
            foreach (Graphic g in root.GetComponentsInChildren<Graphic>(true))
            {
                Color m = UITheme.Mono(g.color);
                if (m != g.color) { g.color = m; EditorUtility.SetDirty(g); mono++; }
            }
            log.Add("Converted " + mono + " UI colours to grayscale");

            // 2) แผงหลัก
            foreach (string n in new[] { "TopLeft_PlayerStatus", "TopCenter_Wave", "TopRight_Resources", "BottomRight_Skills", "Bag_Panel", "TowerInfoBar", "InfoBox" })
                SkinPanel(Find(root, n), _panel);
            Transform tip = Find(root, "Tooltip");
            if (tip != null) SkinPanel(tip.Find("Box"), _panel);
            Transform pauseBox = Find(root, "PausePanel") != null ? Find(root, "PausePanel").Find("Box") : null;
            SkinPanel(pauseBox, _card, 40);
            log.Add("Panels re-skinned");

            // 3) หลอด
            foreach (BarUI bar in root.GetComponentsInChildren<BarUI>(true)) SkinBar(bar);
            log.Add("Bars re-skinned (animated + damage trail)");

            // 4) ช่องสกิล / ช่องกระเป๋า / ปุ่มกระเป๋า / ปุ่มวงกลม
            foreach (SkillSlotUI s in root.GetComponentsInChildren<SkillSlotUI>(true))
            {
                Image bg = s.GetComponent<Image>();
                SetSprite(bg, _slot, Image.Type.Sliced, UITheme.SlotTint);
                new SO(s).Set("readyColor", UITheme.SlotTint).Set("unavailableColor", new Color(0.5f, 0.06f, 0.08f, 1f)).Apply();
                IconsBone(s.transform);
            }
            foreach (MinionBagSlotUI s in root.GetComponentsInChildren<MinionBagSlotUI>(true))
            {
                SetSprite(s.GetComponent<Image>(), _slot, Image.Type.Sliced, UITheme.SlotTint);
                new SO(s).Set("normalColor", UITheme.SlotTint).Set("placingColor", UITheme.SlotHighlightTint).Apply();
            }
            Transform bagBtn = Find(root, "Bag_Button");
            if (bagBtn != null) { SetSprite(bagBtn.GetComponent<Image>(), _slot, Image.Type.Sliced, UITheme.SlotTint); IconsBone(bagBtn); }
            Transform help = Find(root, "Help_Button");
            if (help != null) SetSprite(help.GetComponent<Image>(), _round, Image.Type.Simple, UITheme.SlotTint);

            foreach (RingOptionUI o in root.GetComponentsInChildren<RingOptionUI>(true))
            {
                SetSprite(o.GetComponent<Image>(), _round, Image.Type.Simple, UITheme.SlotTint);
                new SO(o).Set("normalColor", UITheme.SlotTint).Set("highlightColor", UITheme.SlotHighlightTint).Set("disabledColor", UITheme.SlotDisabledTint).Apply();
                Transform rim = o.transform.Find("Rim");
                if (rim != null) SetColor(rim.GetComponent<Image>(), new Color(1f, 1f, 1f, 0f)); // ขอบอยู่ในรูปใหม่แล้ว
                Transform check = o.transform.Find("Confirm_Check");
                if (check != null) SetColor(check.GetComponent<Image>(), UITheme.Paper);
                IconsBone(o.transform);
            }
            foreach (string menu in new[] { "TowerPanel (Ring Menu)", "MinionCommandPanel (Ring Menu)" })
            {
                Transform m = Find(root, menu);
                if (m == null) continue;
                Backdrop(m, menu.StartsWith("Tower") ? 330f : 290f);
                Transform ring = m.Find("Ring");
                if (ring != null) SetColor(ring.GetComponent<Image>(), new Color(UITheme.Red.r, UITheme.Red.g, UITheme.Red.b, 0.7f));
            }
            log.Add("Slots / ring menus re-skinned");

            // 5) ป้ายเล็ก (ราคา / จำนวน / ปุ่มลัด)
            foreach (Image img in root.GetComponentsInChildren<Image>(true))
            {
                if (!img.name.EndsWith("_Badge") || img.color.a < 0.05f) continue;
                if (img.sprite != null && img.sprite != _badge) continue;
                float l = Mathf.Min(img.color.r, 0.22f);
                SetSprite(img, _badge, Image.Type.Sliced, new Color(l, l, l, 0.95f));
            }

            // 6) ปุ่มข้อความ
            foreach (Button b in root.GetComponentsInChildren<Button>(true))
            {
                if (b.GetComponentInChildren<TMP_Text>(true) == null || b.GetComponent<RingOptionUI>() || b.GetComponent<UpgradeCardUI>()
                    || b.GetComponent<MinionBagSlotUI>() || b.GetComponent<WaveWarningIndicator>() || b.name == "Bag_Button" || b.name == "Help_Button") continue;
                Image img = b.targetGraphic as Image;
                if (img == null) continue;
                bool primary = b.name == "Restart_Button" && b.transform.parent != null && b.transform.parent.name == "GameOverPanel";
                // ปุ่มปกติ = เทาเข้ม ชี้แล้วเป็นแดง / ปุ่มหลัก (Play Again) = แดงตลอด
                SetSprite(img, _button, Image.Type.Sliced, primary ? UITheme.Red : Color.white);
                foreach (TMP_Text t in b.GetComponentsInChildren<TMP_Text>(true)) { t.color = UITheme.Paper; t.fontStyle = FontStyles.Bold; EditorUtility.SetDirty(t); }
                ColorBlock cb = b.colors;
                cb.normalColor = primary ? new Color(0.9f, 0.9f, 0.9f, 1f) : new Color(0.62f, 0.62f, 0.63f, 1f);
                cb.highlightedColor = primary ? Color.white : UITheme.SlotHighlightTint;
                cb.selectedColor = cb.normalColor;
                cb.pressedColor = primary ? new Color(0.6f, 0.6f, 0.6f, 1f) : new Color(0.5f, 0.05f, 0.07f, 1f);
                cb.fadeDuration = 0.08f;
                b.colors = cb;
                EditorUtility.SetDirty(b);
            }

            // 7) การ์ดเลเวลอัป
            int ci = 0;
            foreach (UpgradeCardUI card in root.GetComponentsInChildren<UpgradeCardUI>(true))
            {
                SetSprite(card.GetComponent<Image>(), _card, Image.Type.Sliced, Color.white);
                Transform strip = card.transform.Find("Category_Strip");
                if (strip != null)
                {
                    RectTransform srt = (RectTransform)strip;
                    srt.sizeDelta = new Vector2(((RectTransform)card.transform).sizeDelta.x - 28f, 40f);
                    srt.anchoredPosition = new Vector2(0f, -14f);
                    SetSprite(strip.GetComponent<Image>(), _badge, Image.Type.Sliced, UITheme.Red);
                    TMP_Text ct = strip.GetComponentInChildren<TMP_Text>(true);
                    if (ct != null) { ct.color = UITheme.Paper; EditorUtility.SetDirty(ct); }
                }
                Transform desc = card.transform.Find("Description_Text");
                if (desc != null) SetColor(desc.GetComponent<TMP_Text>(), new Color(0.82f, 0.82f, 0.82f, 1f));
                UIPopIn pop = Add<UIPopIn>(card.gameObject);
                pop.delay = 0.08f * ci; pop.duration = 0.28f; pop.startScale = 0.7f;
                UIButtonFX fx = Add<UIButtonFX>(card.gameObject);
                fx.hoverScale = 1.045f; fx.clickSound = "";
                ci++;
            }
            Transform lvl = Find(root, "LevelUpPanel");
            if (lvl != null)
            {
                Transform t = lvl.Find("Title_Text");
                if (t != null) { SetColor(t.GetComponent<TMP_Text>(), UITheme.Paper); Divider(lvl, "Title_Divider", new Vector2(0f, -200f), 700f); }
            }

            // 8) เตือนเวฟ
            foreach (WaveWarningIndicator w in root.GetComponentsInChildren<WaveWarningIndicator>(true))
            {
                SetSprite(w.GetComponent<Image>(), _round, Image.Type.Simple, UITheme.Red);
                Transform rim = w.transform.Find("Rim");
                if (rim != null) SetColor(rim.GetComponent<Image>(), new Color(1f, 1f, 1f, 0f));
                Transform arrow = w.transform.Find("ArrowPivot/Arrow");
                if (arrow != null) SetColor(arrow.GetComponent<Image>(), UITheme.RedBright);
                Transform strip = w.transform.Find("Enemy_Strip");
                if (strip != null) SetSprite(strip.GetComponent<Image>(), _badge, Image.Type.Sliced, UITheme.Bone);
                UIButtonFX fx = Add<UIButtonFX>(w.gameObject);
                fx.clickSound = "";
            }

            // 9) ตัวหนังสือ: เงา / ขอบดำ ให้อ่านง่ายบนพื้นขาว-ดำ
            int txt = 0;
            foreach (TMP_Text t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.font == null || !t.font.name.StartsWith("LiberationSans SDF")) continue;
                bool onBar = t.transform.parent != null && t.transform.parent.GetComponent<BarUI>() != null;
                bool big = t.fontSize >= 40f || t.name == "Message_Text";
                Material m = onBar || big ? _outlineMat : _shadowMat;
                if (m != null && t.fontSharedMaterial != m) { t.fontSharedMaterial = m; EditorUtility.SetDirty(t); txt++; }
            }
            Transform msg = Find(root, "Message_Text");
            if (msg != null) SetColor(msg.GetComponent<TMP_Text>(), UITheme.Paper);
            log.Add("Text shadow/outline: " + txt);

            // 10) ไอคอน placeholder ทั้งหมด = ขาวกระดูก
            foreach (Image img in root.GetComponentsInChildren<Image>(true))
            {
                if (img.sprite == null || !img.name.Contains("Icon")) continue;
                string p = AssetDatabase.GetAssetPath(img.sprite);
                if (p.Contains("/Placeholder/")) SetColor(img, new Color(UITheme.Bone.r, UITheme.Bone.g, UITheme.Bone.b, img.color.a));
            }

            // 10b) จุดเน้นสีแดง (แบบ Othercide): หัวใจ HP, กะโหลกเวฟ, หัวข้อจบเกม
            foreach (string n in new[] { "HP_Icon", "Wave_Icon" })
            {
                Transform t = Find(root, n);
                if (t != null) SetColor(t.GetComponent<Image>(), UITheme.Red);
            }
            Transform go = Find(root, "GameOverPanel");
            if (go != null && go.Find("Title_Text") != null) SetColor(go.Find("Title_Text").GetComponent<TMP_Text>(), UITheme.Red);
            foreach (WaveWarningIndicator w in root.GetComponentsInChildren<WaveWarningIndicator>(true))
            {
                Transform skull = w.transform.Find("Skull");
                if (skull != null) SetColor(skull.GetComponent<Image>(), UITheme.Paper);
            }
            foreach (LowHealthVignette v in root.GetComponentsInChildren<LowHealthVignette>(true))
            { v.tint = new Color(0.4f, 0f, 0.02f, 1f); EditorUtility.SetDirty(v); }

            // 11) วิญญาณบิน = ขาว
            foreach (SoulFlyUI s in root.GetComponentsInChildren<SoulFlyUI>(true))
                new SO(s).Set("soulColor", new Color(1f, 1f, 1f, 0.9f)).Apply();

            // 12) เอฟเฟกต์ปุ่ม + แผงเด้งเข้า + ตัวเลขเด้ง
            int fxCount = 0;
            foreach (Button b in root.GetComponentsInChildren<Button>(true))
            {
                bool had = b.GetComponent<UIButtonFX>() != null;
                UIButtonFX fx = Add<UIButtonFX>(b.gameObject);
                if (had) continue;
                fxCount++;
                if (b.GetComponent<RingOptionUI>() || b.name == "StartWave_Button") fx.clickSound = "";
                if (b.name == "Bag_Button") { fx.clickSound = ""; fx.animateScale = false; } // MinionBagUI เด้งปุ่มเองอยู่แล้ว
            }
            log.Add("Button FX added: " + fxCount);

            PopIn(Find(root, "TowerPanel (Ring Menu)"), 0.18f, 0.7f);
            PopIn(Find(root, "MinionCommandPanel (Ring Menu)"), 0.16f, 0.7f);
            PopIn(Find(root, "InfoBox"), 0.16f, 0.9f);
            PopIn(Find(root, "TowerInfoBar"), 0.16f, 0.92f);
            PopIn(Find(root, "Bag_Panel"), 0.16f, 0.92f);
            PopIn(pauseBox, 0.22f, 0.85f);
            PopIn(Find(root, "GameOverPanel"), 0.5f, 0.96f);
            if (tip != null) PopIn(tip.Find("Box"), 0.1f, 0.95f);
            foreach (string n in new[] { "Souls_Text", "Level_Text", "Wave_Text" })
            {
                Transform t = Find(root, n);
                if (t != null) Add<UIValuePunch>(t.gameObject);
            }

            // 13) ขอบจอมืดตอน HP ต่ำ
            if (Find(root, "LowHealthVignette") == null)
            {
                RectTransform v = BU.Stretch(BU.UI("LowHealthVignette", root));
                Image img = BU.Img(v, new Color(0f, 0f, 0f, 0f), false, _vignette);
                img.enabled = false;
                v.gameObject.AddComponent<LowHealthVignette>();
                v.SetSiblingIndex(0);
                log.Add("Added LowHealthVignette");
            }

            // 14) แถบปรับเสียงในหน้า Pause
            if (pauseBox != null) BuildVolumeSliders((RectTransform)pauseBox, log);
        }

        // ================================================================== pieces

        private static void EnsureAudioManager(List<string> log)
        {
            if (FindAll<AudioManager>().Count > 0) return;
            List<GameManager> gms = FindAll<GameManager>();
            GameManager gm = gms.Count > 0 ? gms[0] : null;
            GameObject go = new GameObject("AudioManager");
            if (gm != null) go.transform.SetParent(gm.transform, false);
            go.AddComponent<AudioManager>();
            log.Add("Added AudioManager" + (gm != null ? " under GameManager" : ""));
        }

        private static void SkinPanel(Transform t, Sprite sprite, float minAlpha = 0.9f)
        {
            if (t == null) return;
            Image img = t.GetComponent<Image>();
            if (img == null) return;
            SetSprite(img, sprite, Image.Type.Sliced, Color.white);
        }

        private static void SkinBar(BarUI bar)
        {
            Image frame = bar.GetComponent<Image>();
            SetSprite(frame, _barFrame, Image.Type.Sliced, Color.white);
            SerializedObject so = new SerializedObject(bar);
            Image fill = so.FindProperty("fill").objectReferenceValue as Image;
            string n = bar.name;
            // HP = แดงเลือด, Mana = ขาว, XP = เทา, ฐาน = ขาวกระดูก
            Color c = n.StartsWith("Mana") ? new Color(0.86f, 0.86f, 0.86f, 1f)
                    : n.StartsWith("XP") ? new Color(0.55f, 0.55f, 0.56f, 1f)
                    : n.StartsWith("Base") ? UITheme.Bone
                    : UITheme.Red;
            if (fill != null)
            {
                fill.sprite = _barFill;
                fill.color = c;
                EditorUtility.SetDirty(fill);
            }
            SerializedProperty low = so.FindProperty("lowPulseBelow");
            if (low != null) low.floatValue = n.StartsWith("HP") || n.StartsWith("Base") ? 0.3f : 0f;
            SerializedProperty pulse = so.FindProperty("lowPulseColor");
            if (pulse != null) pulse.colorValue = n.StartsWith("Base") ? UITheme.Red : new Color(1f, 0.55f, 0.55f, 1f);
            SerializedProperty trail = so.FindProperty("useTrail");
            if (trail != null) trail.boolValue = !n.StartsWith("XP");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Backdrop(Transform menu, float size)
        {
            if (menu.Find("Backdrop") != null) return;
            RectTransform rt = BU.Place(BU.UI("Backdrop", menu), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            BU.Img(rt, new Color(0f, 0f, 0f, 0.6f), false, _softDisc);
            rt.SetSiblingIndex(0);
        }

        private static void Divider(Transform parent, string name, Vector2 pos, float width)
        {
            if (parent.Find(name) != null || _divider == null) return;
            RectTransform rt = BU.Place(BU.UI(name, parent), new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), pos, new Vector2(width, 12f));
            BU.Img(rt, new Color(1f, 1f, 1f, 0.7f), false, _divider);
        }

        private static void IconsBone(Transform t)
        {
            Transform i = t.Find("Icon") ?? t.Find("Bag_Icon");
            if (i == null) return;
            Image img = i.GetComponent<Image>();
            if (img == null || img.sprite == null || !AssetDatabase.GetAssetPath(img.sprite).Contains("/Placeholder/")) return;
            SetColor(img, UITheme.Bone);
        }

        private static void PopIn(Transform t, float duration, float startScale)
        {
            if (t == null) return;
            UIPopIn p = Add<UIPopIn>(t.gameObject);
            p.duration = duration;
            p.startScale = startScale;
        }

        private static void BuildVolumeSliders(RectTransform box, List<string> log)
        {
            if (box.Find("Volume_Group") != null) return;
            box.sizeDelta = new Vector2(Mathf.Max(box.sizeDelta.x, 460f), 560f);

            RectTransform group = BU.Place(BU.UI("Volume_Group", box), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -258f), new Vector2(400f, 280f));
            Divider(group, "Divider", new Vector2(0f, 0f), 360f);
            TextMeshProUGUI head = BU.Text("Volume_Title", group, "VOLUME", 22f, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(380f, 30f),
                TextAlignmentOptions.Center, UITheme.Muted);
            head.fontStyle = FontStyles.Bold;
            head.characterSpacing = 8f;
            if (_shadowMat != null) head.fontSharedMaterial = _shadowMat;

            string[] names = { "Master", "Music", "SFX", "UI" };
            AudioChannel[] ch = { AudioChannel.Master, AudioChannel.Music, AudioChannel.SFX, AudioChannel.UI };
            for (int i = 0; i < 4; i++)
            {
                RectTransform row = BU.Place(BU.UI("Volume_" + names[i], group), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -56f - i * 52f), new Vector2(390f, 40f));
                TextMeshProUGUI label = BU.Text("Label", row, names[i], 22f, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(100f, 36f),
                    TextAlignmentOptions.Left, UITheme.Bone);
                TextMeshProUGUI value = BU.Text("Value", row, "100%", 20f, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(64f, 36f),
                    TextAlignmentOptions.Right, UITheme.Muted);
                if (_shadowMat != null) { label.fontSharedMaterial = _shadowMat; value.fontSharedMaterial = _shadowMat; }

                GameObject sGo = DefaultControls.CreateSlider(new DefaultControls.Resources { standard = _barFrame, background = _barFrame, knob = _handle });
                sGo.name = "Slider";
                sGo.transform.SetParent(row, false);
                SetLayer(sGo.transform, 5);
                RectTransform srt = (RectTransform)sGo.transform;
                BU.Place(srt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(108f, 0f), new Vector2(206f, 18f));
                Slider slider = sGo.GetComponent<Slider>();
                slider.navigation = new Navigation { mode = Navigation.Mode.None };
                Image bg = srt.Find("Background").GetComponent<Image>();
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
                Image fill = srt.Find("Fill Area/Fill").GetComponent<Image>();
                fill.sprite = _barFill;
                fill.type = Image.Type.Simple;
                fill.color = UITheme.Bone;
                RectTransform fillArea = (RectTransform)srt.Find("Fill Area");
                fillArea.offsetMin = new Vector2(3f, 3f); fillArea.offsetMax = new Vector2(-3f, -3f);
                RectTransform fillRt = fill.rectTransform;
                fillRt.offsetMin = new Vector2(0f, 0f); fillRt.offsetMax = new Vector2(0f, 0f);
                Image knob = srt.Find("Handle Slide Area/Handle").GetComponent<Image>();
                knob.color = Color.white;
                knob.rectTransform.sizeDelta = new Vector2(26f, 0f);
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
            log.Add("Pause panel: added Master / Music / SFX / UI volume sliders");
        }

        // ================================================================== helpers

        private static List<T> FindAll<T>() where T : Component
        {
            List<T> list = new List<T>();
            foreach (GameObject r in SceneManager.GetActiveScene().GetRootGameObjects())
                list.AddRange(r.GetComponentsInChildren<T>(true));
            return list;
        }

        private static T Add<T>(GameObject go) where T : Component
        {
            T c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            EditorUtility.SetDirty(c);
            return c;
        }

        private static void SetSprite(Image img, Sprite sprite, Image.Type type, Color color)
        {
            if (img == null || sprite == null) return;
            img.sprite = sprite;
            img.type = type;
            img.pixelsPerUnitMultiplier = 1f;
            img.fillCenter = true;
            img.color = color;
            EditorUtility.SetDirty(img);
        }

        private static void SetColor(Graphic g, Color c)
        {
            if (g == null) return;
            g.color = c;
            EditorUtility.SetDirty(g);
        }

        private static void SetLayer(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform c in t) SetLayer(c, layer);
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                Transform r = Find(c, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
