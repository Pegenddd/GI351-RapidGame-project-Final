using System.IO;
using UnityEditor;
using UnityEngine;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// ใส่รูปวาดป้อม Explosive (สไลม์แช่หม้อต้ม) + ภาพเคลื่อนไหว ลงใน Prefab ป้อมเดิม (ไม่ลบค่าอื่น / ไม่แตะซีน)
    /// เมนู: Tools > NecroDefense > Updates > Apply Explosive Tower Art
    /// - Tower_Explosive: เพิ่ม Visual_Explosive (กดซ้ำได้ ตำแหน่ง/ขนาดของ Visual_Explosive ที่ปรับไว้จะถูกเก็บไว้)
    /// - Proj_Explosive: ลอยโค้ง (สไลม์กระโดด) + สไลม์แตกกระจายตอนตก (แตะแค่ค่าการบินโค้ง / ของที่เกิดตอนตก)
    /// - FX_SlimeSplat: สร้างใหม่ถ้ายังไม่มี
    /// </summary>
    public static class NecroUpdate_ExplosiveArt
    {
        private const string ArtDir = "Assets/Game/Art/Towers/Explosive";
        private const string PrefabPath = "Assets/Game/Prefabs/Towers/Tower_Explosive.prefab";
        private const string ProjectilePath = "Assets/Game/Prefabs/Projectiles/Proj_Explosive.prefab";
        private const string SplatPath = "Assets/Game/Prefabs/Effects/FX_SlimeSplat.prefab";
        private const float PPU = 420f;           // พิกเซลต่อหน่วย (เท่าป้อม Magic Orb)
        private const float OriginX = 512f;        // จุดในรูปวาด (1024px) ที่ตรงกับพื้นใต้ป้อม
        private const float OriginY = 940f;
        private const string VisualName = "Visual_Explosive";

        // ตำแหน่งชิ้นส่วนในรูปวาด 1024px (กลางรูป x, y) - ได้จากการตัดรูปของแต่ละเลเวล
        private static readonly float[] PotY = { 721.5f, 682.5f, 635.5f, 635.5f };
        private static readonly float[] LavaY = { 625f, 533.5f, 421.5f, 421.5f };
        private static readonly float[] SlimeY = { 627f, 519f, 442f, 424.5f };       // กลางตัวสไลม์ (เต็มตัว)
        private static readonly float[] SlimeLava = { 641f, 539f, 465f, 464f };     // ระดับผิวลาวา = จุดหมุนสไลม์
        // ลาวา/ขอบหม้อด้านหน้า ที่บังครึ่งล่างของสไลม์ (ตัดตามแนวโค้งจริงของรูปวาด)
        private static readonly float[] FrontX = { 512.5f, 512f, 512.5f, 512f };
        private static readonly float[] FrontY = { 681.5f, 575.5f, 500.5f, 505.5f };
        private static readonly float[] BubblesY = { 0f, 0f, 426.5f, 426.5f };

        [MenuItem("Tools/NecroDefense/Updates/Apply Explosive Tower Art")]
        public static void Apply()
        {
            if (!File.Exists(PrefabPath)) { EditorUtility.DisplayDialog("Explosive Art", "ไม่เจอ " + PrefabPath, "OK"); return; }
            ImportSprites();
            GameObject splat = EnsureSplatPrefab();
            UpdateProjectile(splat);

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Tower tower = root.GetComponent<Tower>();
                if (tower == null) { Debug.LogError("Tower_Explosive ไม่มี Tower component"); return; }

                Vector3 keepPos = new Vector3(0f, -0.45f, 0f), keepScale = Vector3.one;
                Transform old = root.transform.Find(VisualName);
                if (old != null) { keepPos = old.localPosition; keepScale = old.localScale; Object.DestroyImmediate(old.gameObject); }

                GameObject vis = new GameObject(VisualName);
                vis.transform.SetParent(root.transform, false);
                vis.transform.localPosition = keepPos;
                vis.transform.localScale = keepScale;

                GameObject[] levels = new GameObject[4];
                for (int i = 0; i < 4; i++)
                {
                    GameObject lv = new GameObject("Level_" + i);
                    lv.transform.SetParent(vis.transform, false);
                    levels[i] = lv;

                    Part(lv.transform, "Base", "Explosive_Base", 512f, 848f, 2);
                    Part(lv.transform, "Pot", "Explosive_Pot" + i, 512.5f, PotY[i], 3);
                    Part(lv.transform, "Lava", "Explosive_Lava" + i, 512.5f, LavaY[i], 4);
                    if (BubblesY[i] > 0f)
                        Part(lv.transform, "Bubbles", "Explosive_Bubbles" + i, 511f, BubblesY[i], 5, bob: 0.012f, flicker: 0.15f, flickerSpeed: 6f, attackPulse: 0.2f);
                    Slime(lv.transform, i);
                    Part(lv.transform, "LavaFront", "Explosive_LavaFront" + i, FrontX[i], FrontY[i], 9);
                    Part(lv.transform, "Fire", "Explosive_Fire", 512.5f, 786f, 10, flicker: 0.07f, flickerSpeed: 7f, attackPulse: 0.18f);
                    if (i > 0) lv.SetActive(false);
                }

                SlimePotTowerVisual v = vis.AddComponent<SlimePotTowerVisual>();
                SerializedObject so = new SerializedObject(v);
                SetArray(so.FindProperty("levelRoots"), levels);
                SetArray(so.FindProperty("levelIcons"), new Object[] { Load("Explosive_Full0"), Load("Explosive_Full1"), Load("Explosive_Full2"), Load("Explosive_Full3") });
                so.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject ts = new SerializedObject(tower);
                ts.FindProperty("visual").objectReferenceValue = v;
                ts.FindProperty("scalePerLevel").floatValue = 0f;
                ts.ApplyModifiedPropertiesWithoutUndo();

                Transform body = root.transform.Find("Body");
                if (body != null && body.TryGetComponent(out SpriteRenderer bodySr)) bodySr.enabled = false;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[NecroDefense] Explosive art applied -> " + PrefabPath);
                EditorUtility.DisplayDialog("Explosive Art",
                    "ใส่รูป + ภาพเคลื่อนไหวป้อม Explosive เรียบร้อย\n\n" +
                    "ปรับท่าทางได้ที่ Prefab Tower_Explosive > Visual_Explosive\n" +
                    "(สไลม์ = TowerSlimePart, หม้อ/ลาวา/ไฟ/ฟอง = TowerIdleMotion)\n" +
                    "ความโค้งตอนกระโดด = Proj_Explosive > Arc", "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ------------------------------------------------------------------

        private static Vector3 Px(float cx, float cy) => new Vector3((cx - OriginX) / PPU, (OriginY - cy) / PPU, 0f);

        private static void Part(Transform parent, string name, string sprite, float cx, float cy, int order,
            float bob = 0f, float flicker = 0f, float flickerSpeed = 8f, float attackPulse = 0f)
        {
            SpriteRenderer sr = Sprite(parent, name, Load(sprite), Px(cx, cy), order);
            if (bob == 0f && flicker == 0f && attackPulse == 0f) return;
            TowerIdleMotion m = sr.gameObject.AddComponent<TowerIdleMotion>();
            SerializedObject so = new SerializedObject(m);
            so.FindProperty("bobHeight").floatValue = bob;
            so.FindProperty("flicker").floatValue = flicker;
            so.FindProperty("flickerSpeed").floatValue = flickerSpeed;
            so.FindProperty("attackPulse").floatValue = attackPulse;
            so.FindProperty("phase").floatValue = Random.value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>สไลม์เต็มตัว: จุดหมุนอยู่ที่ผิวลาวา ครึ่งล่างถูก LavaFront บังไว้ (เหมือนแช่อยู่ในหม้อ)</summary>
        private static void Slime(Transform parent, int level)
        {
            GameObject rig = new GameObject("Slime");
            rig.transform.SetParent(parent, false);
            rig.transform.localPosition = Px(512.5f, SlimeLava[level]);

            Vector3 bodyPos = new Vector3(0f, (SlimeLava[level] - SlimeY[level]) / PPU, 0f);
            SpriteRenderer sr = Sprite(rig.transform, "SlimeBody", Load("Explosive_Slime" + level), bodyPos, 6);

            // ก่อนกระโดด: ตาโกรธ + สีแดงไล่จากล่างขึ้นบน (TowerSlimePart ตัดรูปแดงเป็นชั้นให้เอง)
            SpriteRenderer eyes = Sprite(rig.transform, "AngryEyes", Load("Explosive_SlimeAngryEyes" + level), bodyPos, 7);
            eyes.enabled = false;
            SpriteRenderer red = Sprite(rig.transform, "RedFill", null, bodyPos, 8);
            red.enabled = false;

            TowerSlimePart part = rig.AddComponent<TowerSlimePart>();
            SerializedObject so = new SerializedObject(part);
            so.FindProperty("inPotRenderer").objectReferenceValue = sr;
            so.FindProperty("fullBodySprite").objectReferenceValue = Load("Explosive_Slime" + level);
            so.FindProperty("angryEyes").objectReferenceValue = eyes;
            so.FindProperty("redFill").objectReferenceValue = red;
            so.FindProperty("angryBodySprite").objectReferenceValue = Load("Explosive_SlimeRed" + level);
            so.FindProperty("phase").floatValue = Random.value;
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

        /// <summary>สไลม์แตกกระจายตอนตก (สร้างครั้งเดียว ถ้ามีแล้วไม่แตะ)</summary>
        private static GameObject EnsureSplatPrefab()
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(SplatPath);
            if (existing != null) return existing;
            GameObject go = new GameObject("FX_SlimeSplat");
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Load("Explosive_Slime0");
            sr.sortingOrder = 8;
            SpriteSplatEffect fx = go.AddComponent<SpriteSplatEffect>();
            SerializedObject so = new SerializedObject(fx);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.ApplyModifiedPropertiesWithoutUndo();
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, SplatPath);
            Object.DestroyImmediate(go);
            return saved;
        }

        /// <summary>กระสุนป้อม: ลอยโค้งแบบกระโดด + สไลม์แตกตอนตก (ค่าอื่นของกระสุนไม่แตะ)</summary>
        private static void UpdateProjectile(GameObject splat)
        {
            if (!File.Exists(ProjectilePath)) return;
            GameObject p = PrefabUtility.LoadPrefabContents(ProjectilePath);
            try
            {
                Projectile proj = p.GetComponent<Projectile>();
                if (proj == null) return;
                SerializedObject so = new SerializedObject(proj);
                so.FindProperty("arcHeight").floatValue = 0.9f;
                so.FindProperty("arcHeightPerDistance").floatValue = 0.15f;
                so.FindProperty("rotateToDirection").boolValue = false;
                so.FindProperty("impactSpawn").objectReferenceValue = splat;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(p, ProjectilePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(p); }
        }

        private static void ImportSprites()
        {
            AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                float ppu = PPU;
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
