using System.IO;
using UnityEditor;
using UnityEngine;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// ใส่รูปวาดป้อม Magic Orb + ภาพเคลื่อนไหว ลงใน Prefab ป้อมเดิม (ไม่ลบค่าอื่นใน Prefab / ไม่แตะซีน)
    /// เมนู: Tools > NecroDefense > Updates > Apply Magic Orb Tower Art
    /// กดซ้ำได้: จะสร้างเฉพาะ Visual_MagicOrb ใหม่ (ตำแหน่ง/ขนาดของ Visual_MagicOrb ที่ปรับไว้จะถูกเก็บไว้)
    /// </summary>
    public static class NecroUpdate_MagicOrbArt
    {
        private const string ArtDir = "Assets/Game/Art/Towers/MagicOrb";
        private const string PrefabPath = "Assets/Game/Prefabs/Towers/Tower_MagicOrb.prefab";
        private const float PPU = 420f;          // พิกเซลต่อหน่วย (รูปวาด 1024px)
        private const float OriginX = 512f;       // จุดในรูปที่ตรงกับพื้นใต้ป้อม
        private const float OriginY = 940f;
        private const string VisualName = "Visual_MagicOrb";

        [MenuItem("Tools/NecroDefense/Updates/Apply Magic Orb Tower Art")]
        public static void Apply()
        {
            if (!File.Exists(PrefabPath)) { EditorUtility.DisplayDialog("Magic Orb Art", "ไม่เจอ " + PrefabPath, "OK"); return; }
            ImportSprites();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Tower tower = root.GetComponent<Tower>();
                if (tower == null) { Debug.LogError("Tower_MagicOrb ไม่มี Tower component"); return; }

                // เก็บตำแหน่ง/ขนาดที่ผู้ใช้ปรับไว้ (ถ้าเคยกดแล้ว)
                Vector3 keepPos = new Vector3(0f, -0.45f, 0f), keepScale = Vector3.one;
                Transform old = root.transform.Find(VisualName);
                if (old != null) { keepPos = old.localPosition; keepScale = old.localScale; Object.DestroyImmediate(old.gameObject); }

                GameObject vis = new GameObject(VisualName);
                vis.transform.SetParent(root.transform, false);
                vis.transform.localPosition = keepPos;
                vis.transform.localScale = keepScale;

                GameObject[] levels = new GameObject[4];
                // Level 0: ฐาน + กะโหลกกลาง
                levels[0] = Level(vis.transform, 0);
                Part(levels[0].transform, "Base", "MagicOrb_Base0", 512f, 699f, 2, idleBreathe: 0.012f);
                Skull(levels[0].transform, "Skull", 512f, 293f, 399f, 370f, "C", 0f);
                // Level 1: + หินโค้ง 2 ข้าง
                levels[1] = Level(vis.transform, 1);
                Part(levels[1].transform, "Base", "MagicOrb_Base1", 512f, 699f, 2, idleBreathe: 0.012f);
                Part(levels[1].transform, "Stones", "MagicOrb_Stones1", 512f, 271f, 3, bob: 0.03f, sway: 1.5f, phase: 0.25f);
                Skull(levels[1].transform, "Skull", 512f, 293f, 399f, 370f, "C", 0f);
                // Level 2: กะโหลกคู่
                levels[2] = Level(vis.transform, 2);
                Part(levels[2].transform, "Base", "MagicOrb_Base2", 512f, 666.5f, 2, idleBreathe: 0.012f);
                Skull(levels[2].transform, "Skull_L", 390f, 273f, 379f, 350f, "L", 0f);
                Skull(levels[2].transform, "Skull_R", 634f, 272f, 378f, 349f, "R", 0.5f);
                // Level 3: กะโหลกคู่ + เปลวไฟกลาง
                levels[3] = Level(vis.transform, 3);
                Part(levels[3].transform, "Base", "MagicOrb_Base3", 512f, 666.5f, 2, idleBreathe: 0.012f);
                // หินโค้งรอบกะโหลกคู่ (แยกบน/ล่าง ลอยคนละจังหวะ)
                Part(levels[3].transform, "Stone_L_Top", "MagicOrb_StoneL_Top", 263f, 195f, 3, bob: 0.035f, sway: 2.5f, phase: 0.1f);
                Part(levels[3].transform, "Stone_L_Bottom", "MagicOrb_StoneL_Bot", 259f, 339f, 3, bob: 0.025f, sway: 2f, phase: 0.35f);
                Part(levels[3].transform, "Stone_R_Top", "MagicOrb_StoneR_Top", 761f, 194f, 3, bob: 0.035f, sway: 2.5f, phase: 0.6f);
                Part(levels[3].transform, "Stone_R_Bottom", "MagicOrb_StoneR_Bot", 765f, 338f, 3, bob: 0.025f, sway: 2f, phase: 0.85f);
                // ลูกไฟกลาง: สั่นตลอด + พองขึ้นตอนกะโหลกยิง
                Part(levels[3].transform, "Flame", "MagicOrb_Flame3", 512f, 287f, 3, bob: 0.015f, flicker: 0.09f, attackPulse: 0.7f);
                Skull(levels[3].transform, "Skull_L", 390f, 273f, 379f, 350f, "L", 0f);
                Skull(levels[3].transform, "Skull_R", 634f, 272f, 378f, 349f, "R", 0.5f);
                for (int i = 1; i < 4; i++) levels[i].SetActive(false);

                // แสงวาบตอนสร้าง/อัปเกรดเสร็จ
                SpriteRenderer flash = Sprite(vis.transform, "FlashGlow", Load("MagicOrb_Glow"), new Vector3(0f, 0.9f, 0f), 7);
                flash.color = new Color(1f, 1f, 1f, 0f); // ขาว = ฝั่งเรา (เกมใช้แค่ ขาว-ดำ-แดง)
                flash.enabled = false;

                MagicOrbTowerVisual v = vis.AddComponent<MagicOrbTowerVisual>();
                SerializedObject so = new SerializedObject(v);
                SetArray(so.FindProperty("levelRoots"), levels);
                SetArray(so.FindProperty("levelIcons"), new Object[] { Load("MagicOrb_Full0"), Load("MagicOrb_Full1"), Load("MagicOrb_Full2"), Load("MagicOrb_Full3") });
                so.FindProperty("flashGlow").objectReferenceValue = flash;
                so.ApplyModifiedPropertiesWithoutUndo();

                // ผูกกับ Tower: ใช้ Visual แทน Body รูปเดิม, รูปวาดโตเองตามเลเวลแล้วจึงไม่ต้องขยาย
                SerializedObject ts = new SerializedObject(tower);
                ts.FindProperty("visual").objectReferenceValue = v;
                ts.FindProperty("scalePerLevel").floatValue = 0f;
                ts.ApplyModifiedPropertiesWithoutUndo();

                Transform body = root.transform.Find("Body");
                if (body != null && body.TryGetComponent(out SpriteRenderer bodySr)) bodySr.enabled = false;

                // ดาวเลเวล / หลอดก่อสร้าง: ย้ายขึ้นเหนือรูปใหม่ (เฉพาะถ้ายังอยู่ตำแหน่งเดิม)
                MoveIfDefault(root.transform.Find("LevelPips"), 1.45f, 1.7f);
                MoveIfDefault(root.transform.Find("ConstructionBar"), 1.2f, 2.0f);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                RecolorProjectile();
                Debug.Log("[NecroDefense] Magic Orb art applied -> " + PrefabPath);
                EditorUtility.DisplayDialog("Magic Orb Art", "ใส่รูป + ภาพเคลื่อนไหวป้อม Magic Orb เรียบร้อย\n\nปรับท่าทางได้ที่ Prefab Tower_MagicOrb > Visual_MagicOrb\n(กะโหลก = TowerSkullPart, หิน/ไฟ/ฐาน = TowerIdleMotion)", "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ------------------------------------------------------------------

        private const string ProjectilePath = "Assets/Game/Prefabs/Projectiles/Proj_MagicOrb.prefab";

        /// <summary>กระสุนป้อม: เปลี่ยนจากฟ้าเป็นขาว (โทนฝั่งเรา) - แตะแค่สีของรูป</summary>
        private static void RecolorProjectile()
        {
            if (!File.Exists(ProjectilePath)) return;
            GameObject p = PrefabUtility.LoadPrefabContents(ProjectilePath);
            try
            {
                SpriteRenderer sr = p.GetComponent<SpriteRenderer>();
                if (sr == null) return;
                sr.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                PrefabUtility.SaveAsPrefabAsset(p, ProjectilePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(p); }
        }

        private static Vector3 Px(float cx, float cy) => new Vector3((cx - OriginX) / PPU, (OriginY - cy) / PPU, 0f);

        private static GameObject Level(Transform parent, int i)
        {
            GameObject go = new GameObject("Level_" + i);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Part(Transform parent, string name, string sprite, float cx, float cy, int order,
            float idleBreathe = 0f, float bob = 0f, float sway = 0f, float flicker = 0f, float phase = 0f, float attackPulse = 0f)
        {
            SpriteRenderer sr = Sprite(parent, name, Load(sprite), Px(cx, cy), order);
            if (idleBreathe == 0f && bob == 0f && sway == 0f && flicker == 0f && attackPulse == 0f) return;
            TowerIdleMotion m = sr.gameObject.AddComponent<TowerIdleMotion>();
            SerializedObject so = new SerializedObject(m);
            so.FindProperty("bobHeight").floatValue = bob;
            so.FindProperty("swayAngle").floatValue = sway;
            so.FindProperty("breathe").floatValue = idleBreathe;
            so.FindProperty("flicker").floatValue = flicker;
            so.FindProperty("phase").floatValue = phase;
            so.FindProperty("attackPulse").floatValue = attackPulse;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>กะโหลก: จุดหมุนอยู่กลางหัว / กรามลูก / จุดปาก + แสงในปาก</summary>
        private static void Skull(Transform parent, string name, float cx, float cy, float jawCy, float mouthCy, string tag, float phase)
        {
            GameObject rig = new GameObject(name);
            rig.transform.SetParent(parent, false);
            rig.transform.localPosition = Px(cx, cy);

            Sprite(rig.transform, "Cranium", Load("MagicOrb_Skull" + tag + "_Skull"), Vector3.zero, 5);
            SpriteRenderer jaw = Sprite(rig.transform, "Jaw", Load("MagicOrb_Skull" + tag + "_Jaw"), new Vector3(0f, (cy - jawCy) / PPU, 0f), 6);

            GameObject mouth = new GameObject("Mouth");
            mouth.transform.SetParent(rig.transform, false);
            mouth.transform.localPosition = new Vector3(0f, (cy - mouthCy) / PPU, 0f);
            SpriteRenderer glow = Sprite(mouth.transform, "MouthGlow", Load("MagicOrb_Glow"), Vector3.zero, 4);
            glow.color = new Color(1f, 1f, 1f, 0f);
            glow.enabled = false;

            TowerSkullPart part = rig.AddComponent<TowerSkullPart>();
            SerializedObject so = new SerializedObject(part);
            so.FindProperty("jaw").objectReferenceValue = jaw.transform;
            so.FindProperty("mouth").objectReferenceValue = mouth.transform;
            so.FindProperty("mouthGlow").objectReferenceValue = glow;
            so.FindProperty("glowColor").colorValue = Color.white;
            so.FindProperty("phase").floatValue = phase;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Vector3 localPos, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        private static Sprite Load(string file)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + file + ".png");
            if (s == null) Debug.LogWarning("[NecroDefense] ไม่เจอรูป " + ArtDir + "/" + file + ".png");
            return s;
        }

        private static void SetArray(SerializedProperty p, Object[] items)
        {
            p.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }

        private static void MoveIfDefault(Transform t, float defaultY, float newY)
        {
            if (t == null || Mathf.Abs(t.localPosition.y - defaultY) > 0.001f) return;
            Vector3 p = t.localPosition;
            p.y = newY;
            t.localPosition = p;
        }

        /// <summary>ตั้งค่า Import ให้รูปในโฟลเดอร์ป้อม (Sprite, PPU, ไม่มี mipmap)</summary>
        private static void ImportSprites()
        {
            AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                float ppu = path.Contains("Glow") ? 128f : PPU;
                bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single
                             || !Mathf.Approximately(ti.spritePixelsPerUnit, ppu) || ti.mipmapEnabled || !ti.alphaIsTransparency;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = ppu;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.SaveAndReimport();
            }
        }
    }
}
