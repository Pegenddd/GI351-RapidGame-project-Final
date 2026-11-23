using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// ตกแต่งซีน Map2 ด้วยของตกแต่งที่วาดมา (ต้นไม้ / พุ่มไม้ / หิน / ตอไม้) + พื้นหลังโทนมืด
    /// เมนู: Tools > NecroDefense > Updates > Decorate Map2
    /// - พื้นฝั่งศัตรู = พื้นหลังมืด (BlackGround1) ปูต่อกันทั้งแมพ / ฝั่งเรา + ทางมอน = ขาวเหมือนเดิม
    /// - ทางมอน + พื้นที่ฝั่งเรา มีขอบเข้มบางๆ ให้ดูเป็นทางเดินชัดขึ้น
    /// - ป่าด้านบน 2 แถว (แถวหลังมืดกว่า = มีมิติ) / พุ่มไม้ขอบล่าง / ของกระจายในพื้นที่ (ไม่ทับทางมอน จุดสร้างป้อม ฐาน)
    /// - ต้นไม้ไหวเบาๆ (PropSway) + เงาใต้ของทุกชิ้น / เรียงหน้า-หลังตามแกน Y กับยูนิต
    /// - ของเดิม (Ground_Gray, Decorations_Map2) แค่ปิดไว้ / กดซ้ำได้ (สร้างใหม่ทับเฉพาะ Decor_Map2)
    /// ย้าย / ลบ / เพิ่ม ชิ้นไหนก็ได้อิสระใน Scene (อยู่ใต้ Map > Decor_Map2)
    /// </summary>
    public static class NecroUpdate_Map2Decor
    {
        private const string ScenePath = "Assets/Scenes/Map2.unity";
        private const string ArtDir = "Assets/Game/Art/Map";
        private const string PropDir = ArtDir + "/Props";
        private const string GroundFile = ArtDir + "/Ground_Dark.jpg";
        private const string ShadowFile = "Assets/Game/Art/Placeholder/Circle.png";
        private const string RootName = "Decor_Map2";
        private const float PropPPU = 140f;

        private static readonly Color BorderColor = new Color(0.55f, 0.55f, 0.55f, 1f);
        private const float BorderWidth = 0.14f;

        private struct P
        {
            public string kind; public float x, y, scale, tint; public bool flip;
            public P(string kind, float x, float y, float scale, float tint, bool flip)
            { this.kind = kind; this.x = x; this.y = y; this.scale = scale; this.tint = tint; this.flip = flip; }
        }

        // ตำแหน่งของตกแต่ง (จุดเท้า) - คำนวณไม่ให้ทับทางมอน / จุดสร้างป้อม / ฐาน / จุดเกิดผู้เล่น
        private static readonly P[] Props =
        {
            new P("Tree_2", -20.23f, 10.60f, 1.01f, 0.55f, false),
            new P("Tree_3", -19.21f, 10.65f, 1.01f, 0.55f, true),
            new P("Tree_1", -17.37f, 10.43f, 1.11f, 0.55f, false),
            new P("Tree_3", -15.25f, 10.69f, 1.08f, 0.55f, false),
            new P("Tree_1", -14.12f, 10.47f, 1.02f, 0.55f, true),
            new P("Tree_1", -12.24f, 10.69f, 1.04f, 0.55f, true),
            new P("Tree_3", -10.34f, 10.50f, 1.14f, 0.55f, true),
            new P("Tree_3", -8.14f, 10.58f, 1.05f, 0.55f, true),
            new P("Tree_1", -6.38f, 10.66f, 1.18f, 0.55f, false),
            new P("Tree_1", -4.98f, 10.61f, 1.15f, 0.55f, true),
            new P("Tree_1", -2.68f, 10.44f, 1.11f, 0.55f, false),
            new P("Tree_2", -0.85f, 10.70f, 1.12f, 0.55f, true),
            new P("Tree_2", 1.02f, 10.73f, 1.01f, 0.55f, false),
            new P("Tree_3", 3.12f, 10.54f, 1.08f, 0.55f, false),
            new P("Tree_2", 4.35f, 10.71f, 1.10f, 0.55f, true),
            new P("Tree_3", 5.96f, 10.60f, 1.17f, 0.55f, true),
            new P("Tree_3", 7.75f, 10.47f, 1.09f, 0.55f, false),
            new P("Tree_2", 14.08f, 10.48f, 1.04f, 0.55f, true),
            new P("Tree_2", 15.65f, 10.49f, 1.06f, 0.55f, true),
            new P("Tree_3", 17.31f, 10.88f, 1.14f, 0.55f, false),
            new P("Tree_3", 19.28f, 10.63f, 1.17f, 0.55f, false),
            new P("Tree_3", -20.06f, 8.67f, 0.92f, 0.80f, false),
            new P("Tree_1", -17.77f, 8.69f, 0.92f, 0.80f, false),
            new P("Tree_3", -16.29f, 8.51f, 0.97f, 0.80f, true),
            new P("Tree_3", -13.65f, 8.80f, 1.09f, 0.80f, false),
            new P("Tree_1", -11.13f, 9.00f, 0.99f, 0.80f, true),
            new P("Tree_1", -9.23f, 8.86f, 1.00f, 0.80f, false),
            new P("Tree_1", -6.85f, 8.74f, 0.93f, 0.80f, false),
            new P("Tree_3", -5.32f, 8.80f, 0.92f, 0.80f, false),
            new P("Tree_1", -3.02f, 8.57f, 1.01f, 0.80f, false),
            new P("Tree_3", -0.41f, 8.99f, 1.07f, 0.80f, false),
            new P("Tree_3", 2.06f, 8.56f, 1.00f, 0.80f, false),
            new P("Tree_2", 4.46f, 8.56f, 1.02f, 0.95f, true),
            new P("Tree_3", 7.23f, 8.98f, 0.97f, 0.95f, true),
            new P("Tree_3", 13.37f, 8.79f, 0.90f, 0.95f, false),
            new P("Tree_3", 14.97f, 8.81f, 1.08f, 0.95f, false),
            new P("Tree_2", 17.88f, 8.69f, 1.03f, 0.95f, true),
            new P("Tree_3", 20.14f, 8.67f, 1.09f, 0.95f, false),
            new P("Bush_2", -19.95f, -10.22f, 0.86f, 0.75f, false),
            new P("Tree_1", -18.15f, -10.27f, 0.95f, 0.75f, false),
            new P("Bush_1", -16.64f, -10.40f, 1.05f, 0.75f, false),
            new P("Bush_2", -15.42f, -10.32f, 1.06f, 0.75f, true),
            new P("Tree_1", -13.70f, -10.03f, 0.85f, 0.75f, true),
            new P("Bush_1", -11.73f, -10.19f, 1.02f, 0.75f, false),
            new P("Bush_2", -9.94f, -10.44f, 0.98f, 0.75f, true),
            new P("Bush_1", -8.62f, -10.38f, 1.04f, 0.75f, false),
            new P("Bush_1", -6.96f, -10.17f, 0.93f, 0.75f, false),
            new P("Tree_1", -5.66f, -10.43f, 0.81f, 0.75f, true),
            new P("Bush_1", -3.63f, -10.23f, 1.04f, 0.75f, false),
            new P("Tree_1", -2.25f, -10.20f, 0.83f, 0.75f, false),
            new P("Bush_2", -0.24f, -9.98f, 1.02f, 0.75f, true),
            new P("Bush_2", 1.12f, -10.39f, 1.06f, 0.75f, true),
            new P("Bush_1", 7.37f, -10.09f, 1.07f, 0.75f, false),
            new P("Bush_2", 8.88f, -9.97f, 1.09f, 0.75f, true),
            new P("Tree_3", 10.55f, -10.23f, 0.92f, 0.75f, false),
            new P("Bush_2", 12.12f, -10.27f, 0.94f, 0.75f, true),
            new P("Tree_3", 13.91f, -10.14f, 0.80f, 0.75f, false),
            new P("Bush_2", 15.24f, -10.32f, 1.09f, 0.75f, true),
            new P("Bush_3", 17.46f, -10.03f, 1.04f, 0.75f, false),
            new P("Bush_1", 19.48f, -10.16f, 0.89f, 0.75f, false),
            new P("Bush_1", 20.56f, -10.00f, 1.05f, 0.75f, true),
            new P("Tree_1", -18.82f, 3.40f, 0.86f, 0.75f, false),
            new P("Tree_1", -19.14f, -2.60f, 0.87f, 0.75f, true),
            new P("Tree_3", -19.05f, -5.20f, 0.99f, 0.75f, false),
            new P("Tree_1", -18.98f, -7.40f, 0.89f, 0.75f, true),
            new P("Tree_1", 18.43f, 6.00f, 0.88f, 0.95f, false),
            new P("Tree_3", 18.45f, 2.80f, 0.96f, 0.95f, true),
            new P("Tree_3", 18.64f, -1.50f, 0.89f, 0.78f, false),
            new P("Tree_2", 18.32f, -4.50f, 0.85f, 0.78f, false),
            new P("Tree_1", 18.56f, -7.20f, 0.89f, 0.78f, true),
            new P("Rock_1", -15.58f, 0.45f, 0.49f, 0.82f, true),
            new P("Rock_2", -12.08f, -5.24f, 0.56f, 0.82f, true),
            new P("Bush_2", -5.07f, -6.80f, 0.73f, 0.82f, true),
            new P("Rock_2", -4.74f, -4.24f, 0.45f, 0.82f, true),
            new P("Rock_1", 0.51f, -7.59f, 0.57f, 0.82f, true),
            new P("Stump_1", -0.44f, 5.90f, 0.57f, 0.82f, false),
            new P("Bush_1", -5.04f, 3.72f, 0.68f, 0.82f, false),
            new P("Tree_2", -11.93f, 8.19f, 0.87f, 0.82f, false),
            new P("Stump_1", -16.64f, 4.98f, 0.45f, 0.82f, false),
            new P("Bush_3", 1.50f, -3.60f, 0.65f, 0.82f, false),
            new P("Bush_2", -10.67f, -6.12f, 0.67f, 0.82f, true),
            new P("Tree_1", -16.97f, -7.63f, 0.80f, 0.82f, false),
            new P("Rock_1", 9.75f, -7.01f, 0.49f, 0.82f, true),
            new P("Rock_2", 12.87f, -2.57f, 0.56f, 0.82f, false),
            new P("Bush_2", 15.15f, -6.56f, 0.71f, 0.82f, false),
            new P("Rock_2", 10.61f, -8.19f, 0.41f, 0.82f, true),
            new P("Rock_1", -3.96f, 7.08f, 0.62f, 0.82f, false),
            new P("Stump_1", 1.38f, 6.93f, 0.55f, 0.82f, true),
            new P("Bush_1", 5.52f, 7.52f, 0.60f, 1.00f, true),
            new P("Bush_2", 16.34f, 7.83f, 0.60f, 1.00f, false),
            new P("Rock_1", 13.61f, 6.99f, 0.45f, 1.00f, false),
            new P("Bush_3", 16.18f, 2.60f, 0.60f, 1.00f, true),
            new P("Rock_2", 4.63f, 3.70f, 0.40f, 1.00f, true),
            new P("Stump_1", 16.14f, 0.70f, 0.45f, 1.00f, true)
        };

        [MenuItem("Tools/NecroDefense/Updates/Decorate Map2")]
        public static void Apply()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Decorate Map2", "ไม่เจอซีน Map2 - กด Updates > Create Map2 Scene ก่อน", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();

            // ----- รูป -----
            Sprite ground = ImportGround();
            Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
            foreach (string f in Directory.GetFiles(PropDir, "Prop_*.png"))
            {
                string path = f.Replace('\\', '/');
                Sprite s = ImportProp(path);
                if (s != null) sprites[Path.GetFileNameWithoutExtension(path).Substring(5)] = s;
            }
            Sprite shadow = AssetDatabase.LoadAssetAtPath<Sprite>(ShadowFile);
            if (ground == null || sprites.Count == 0)
            {
                EditorUtility.DisplayDialog("Decorate Map2", "ไม่เจอรูปใน " + ArtDir, "OK");
                return;
            }

            string start = SceneManager.GetActiveScene().path;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MapBounds bounds = Object.FindAnyObjectByType<MapBounds>(FindObjectsInactive.Include);
            Transform map = bounds != null ? bounds.transform : null;
            if (map == null) { EditorUtility.DisplayDialog("Decorate Map2", "ไม่เจอ Map ในซีน Map2", "OK"); return; }

            // ของเดิม: ปิดไว้
            Transform oldDeco = map.Find("Decorations_Map2");
            if (oldDeco != null) oldDeco.gameObject.SetActive(false);
            Transform groundMap2 = map.Find("Ground_Map2");
            Transform gray = groundMap2 != null ? groundMap2.Find("Ground_Gray") : null;
            if (gray != null) gray.gameObject.SetActive(false);

            Transform oldRoot = map.Find(RootName);
            if (oldRoot != null) Object.DestroyImmediate(oldRoot.gameObject);
            Transform root = new GameObject(RootName).transform;
            root.SetParent(map, false);

            // ----- พื้นหลังมืด (ปูต่อกัน) -----
            GameObject g = new GameObject("Ground_Dark");
            g.transform.SetParent(root, false);
            SpriteRenderer gsr = g.AddComponent<SpriteRenderer>();
            gsr.sprite = ground;
            gsr.drawMode = SpriteDrawMode.Tiled;
            gsr.tileMode = SpriteTileMode.Continuous;
            gsr.size = new Vector2(48f, 32f);
            gsr.sortingOrder = -31;

            // ----- ขอบทางมอน / ขอบพื้นที่ฝั่งเรา -----
            int borders = 0;
            if (groundMap2 != null)
            {
                Transform borderRoot = new GameObject("LaneBorders").transform;
                borderRoot.SetParent(root, false);
                foreach (SpriteRenderer sr in groundMap2.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (sr.transform == gray || !sr.gameObject.activeInHierarchy) continue;
                    GameObject b = new GameObject(sr.name + "_Border");
                    b.transform.SetParent(borderRoot, false);
                    b.transform.position = sr.transform.position;
                    b.transform.rotation = sr.transform.rotation;
                    Vector3 sc = sr.transform.lossyScale;
                    b.transform.localScale = new Vector3(sc.x + BorderWidth * 2f, sc.y + BorderWidth * 2f, 1f);
                    SpriteRenderer bsr = b.AddComponent<SpriteRenderer>();
                    bsr.sprite = sr.sprite;
                    bsr.color = BorderColor;
                    bsr.sortingOrder = sr.sortingOrder - 1 >= -30 ? -30 : sr.sortingOrder - 1;
                    borders++;
                }
            }

            // ----- ของตกแต่ง -----
            Transform propsRoot = new GameObject("Props").transform;
            propsRoot.SetParent(root, false);
            int n = 0;
            foreach (P p in Props)
            {
                if (!sprites.TryGetValue(p.kind, out Sprite s)) continue;
                GameObject go = new GameObject(p.kind + "_" + (++n));
                go.transform.SetParent(propsRoot, false);
                go.transform.position = new Vector3(p.x, p.y, 0f);
                go.transform.localScale = new Vector3(p.scale, p.scale, 1f);

                if (shadow != null)
                {
                    GameObject sh = new GameObject("Shadow");
                    sh.transform.SetParent(go.transform, false);
                    float w = s.rect.width / PropPPU;
                    sh.transform.localScale = new Vector3(w * 0.8f, w * 0.22f, 1f);
                    SpriteRenderer ssr = sh.AddComponent<SpriteRenderer>();
                    ssr.sprite = shadow;
                    ssr.color = new Color(0f, 0f, 0f, 0.28f);
                    ssr.sortingOrder = -3;
                }

                GameObject art = new GameObject("Art");
                art.transform.SetParent(go.transform, false);
                SpriteRenderer sr = art.AddComponent<SpriteRenderer>();
                sr.sprite = s;
                sr.flipX = p.flip;
                sr.color = new Color(p.tint, p.tint, p.tint, 1f);
                sr.sortingOrder = 1; // เรียงหน้า-หลังกับยูนิตตามแกน Y
                if (p.kind.StartsWith("Tree")) art.AddComponent<PropSway>();
                NecroUpdate_PropLayers.Setup(go.transform); // เรียงหน้า-หลังกับผู้เล่น + วงรีชน
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(start) && start != ScenePath) EditorSceneManager.OpenScene(start, OpenSceneMode.Single);

            string log = "พื้นหลังมืด + ขอบทาง " + borders + " ชิ้น + ของตกแต่ง " + n + " ชิ้น\n" +
                         "ของเดิม (Ground_Gray / Decorations_Map2) ปิดไว้ ไม่ลบ\n\n" +
                         "ย้าย / ลบ / เพิ่ม ได้อิสระที่ Map > " + RootName + " > Props\n" +
                         "(ก็อปชิ้นที่มีอยู่ แล้วลากไปวาง = เพิ่มชิ้นใหม่)";
            Debug.Log("[NecroDefense] Decorate Map2\n" + log);
            EditorUtility.DisplayDialog("Decorate Map2", log, "OK");
        }

        // ------------------------------------------------------------------

        private static Sprite ImportProp(string file)
        {
            TextureImporter ti = AssetImporter.GetAtPath(file) as TextureImporter;
            if (ti == null) return null;
            TextureImporterSettings st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single
                         || !Mathf.Approximately(ti.spritePixelsPerUnit, PropPPU) || st.spriteAlignment != (int)SpriteAlignment.BottomCenter
                         || ti.mipmapEnabled || !ti.alphaIsTransparency;
            if (dirty)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = PropPPU;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.ReadTextureSettings(st);
                st.spriteAlignment = (int)SpriteAlignment.BottomCenter; // จุดหมุน = โคนต้น (เรียงหน้า-หลังถูก)
                ti.SetTextureSettings(st);
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(file);
        }

        private static Sprite ImportGround()
        {
            TextureImporter ti = AssetImporter.GetAtPath(GroundFile) as TextureImporter;
            if (ti == null) return null;
            TextureImporterSettings st = new TextureImporterSettings();
            ti.ReadTextureSettings(st);
            bool dirty = ti.textureType != TextureImporterType.Sprite || ti.wrapMode != TextureWrapMode.Repeat
                         || st.spriteMeshType != SpriteMeshType.FullRect || !Mathf.Approximately(ti.spritePixelsPerUnit, 120f);
            if (dirty)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = 120f;     // รูป 1920px = 16 หน่วย
                ti.wrapMode = TextureWrapMode.Repeat;
                ti.mipmapEnabled = false;
                ti.ReadTextureSettings(st);
                st.spriteMeshType = SpriteMeshType.FullRect; // จำเป็นสำหรับโหมดปูต่อกัน (Tiled)
                ti.SetTextureSettings(st);
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(GroundFile);
        }
    }
}
