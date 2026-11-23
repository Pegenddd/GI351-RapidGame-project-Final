using System.IO;
using UnityEditor;
using UnityEngine;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// ใส่รูปศัตรูชุดใหม่ (โทนดำ) + ท่าเดิน / ท่าตี ให้ศัตรูทุกตัว
    /// เมนู: Tools > NecroDefense > Updates > Apply Enemy Art (Black)
    /// - Goblin / Slime + Mini Slime / Vulture (ค้างคาว) / Skeleton Archer (ขว้างมีด) / Giant
    /// - สร้าง Visual/Body_Animated ใหม่ (กดซ้ำได้) / รูปเดิม (Body, Outline, 1_0 + Animator) ไม่ลบ แค่ปิดไว้
    /// - Skeleton: กระสุนเปลี่ยนเป็นมีดหมุน (Proj_SkeletonKnife ก็อปจากลูกธนูเดิม) ขว้างหลังท่า 4
    /// - ค่าอื่นของศัตรู (HP, ดาเมจ, ความเร็ว) ไม่แตะ
    /// ปรับความเร็วท่าได้ที่ SpriteFrameAnimator (Walk Fps / Attack Fps) / ลำดับเฟรมก็แก้ได้ในนั้น
    /// </summary>
    public static class NecroUpdate_EnemyArt
    {
        private const string ArtRoot = "Assets/Game/Art/Units/Enemies";
        private const string PrefabDir = "Assets/Game/Prefabs/Enemies/";
        private const string KnifePrefabPath = "Assets/Game/Prefabs/Projectiles/Proj_SkeletonKnife.prefab";
        private const string BodyName = "Body_Animated";

        private class Cfg
        {
            public string prefab, art; public int frames;
            public float height;       // ความสูงรูปในโลก (หน่วยของ Prefab ก่อนคูณสเกลตัว)
            public float width;        // > 0 = ใช้ความกว้างแทน (ค้างคาว)
            public float feetY;        // ตำแหน่งเท้า (ล่างสุดของรูป)
            public bool centered;      // ตัวบิน: กลางรูปอยู่ที่จุดศูนย์กลาง
            public int[] walk, attack; public float walkFps, attackFps;
        }

        private static readonly Cfg[] Enemies =
        {
            new Cfg { prefab = "Enemy_Goblin", art = "Goblin", frames = 4, height = 1.05f, feetY = -0.48f,
                      walk = new[] { 1, 2, 3, 4 }, attack = new[] { 4, 1 }, walkFps = 8f, attackFps = 8f },
            new Cfg { prefab = "Enemy_Slime", art = "Slime", frames = 4, height = 0.78f, feetY = -0.42f,
                      walk = new[] { 1, 2, 3, 4 }, attack = new[] { 2, 4, 1 }, walkFps = 7f, attackFps = 10f },
            new Cfg { prefab = "Enemy_SlimeSmall", art = "SlimeSmall", frames = 4, height = 0.44f, feetY = -0.235f,
                      walk = new[] { 1, 2, 3, 4 }, attack = new[] { 2, 4, 1 }, walkFps = 9f, attackFps = 12f },
            new Cfg { prefab = "Enemy_Vulture", art = "Bat", frames = 3, width = 1.55f, centered = true,
                      walk = new[] { 1, 2, 3, 2 }, attack = new[] { 3, 1, 2 }, walkFps = 10f, attackFps = 12f },
            new Cfg { prefab = "Enemy_SkeletonArcher", art = "Skeleton", frames = 4, height = 1.25f, feetY = -0.53f,
                      walk = new[] { 1, 2, 3, 2 }, attack = new[] { 4, 4, 1 }, walkFps = 7f, attackFps = 8f },
            new Cfg { prefab = "Enemy_Giant", art = "Giant", frames = 4, height = 1.9f, feetY = -0.87f,
                      walk = new[] { 1, 2, 3, 4 }, attack = new[] { 4, 4, 2, 1 }, walkFps = 5f, attackFps = 8f },
        };

        [MenuItem("Tools/NecroDefense/Updates/Apply Enemy Art (Black)")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            string log = "";
            foreach (Cfg c in Enemies)
            {
                try { log += ApplyOne(c) + "\n"; }
                catch (System.Exception e) { Debug.LogException(e); log += c.prefab + ": ผิดพลาด " + e.Message + "\n"; }
            }
            log += SetupSkeletonKnife() + "\n";
            AssetDatabase.SaveAssets();
            Debug.Log("[NecroDefense] Apply Enemy Art\n" + log);
            EditorUtility.DisplayDialog("Enemy Art", log +
                "\nปรับความเร็วท่า / ลำดับเฟรมได้ที่ Prefab ศัตรู > SpriteFrameAnimator\nตำแหน่ง/ขนาดรูป = Visual > " + BodyName, "OK");
        }

        // ------------------------------------------------------------------

        private static string ApplyOne(Cfg c)
        {
            string path = PrefabDir + c.prefab + ".prefab";
            if (!File.Exists(path)) return c.prefab + ": ไม่เจอ Prefab (ข้าม)";
            string dir = ArtRoot + "/" + c.art;

            Sprite[] frames = new Sprite[c.frames];
            for (int i = 0; i < c.frames; i++)
            {
                string file = dir + "/" + c.art + "_" + (i + 1) + ".png";
                frames[i] = ImportSprite(file, c);
                if (frames[i] == null) return c.prefab + ": ไม่เจอรูป " + file;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                EnemyUnit unit = root.GetComponent<EnemyUnit>();
                Transform visual = root.transform.Find("Visual");
                if (visual == null) { visual = new GameObject("Visual").transform; visual.SetParent(root.transform, false); }

                // ของเดิม: ปิดไว้ ไม่ลบ
                foreach (Transform child in visual)
                    if (child.name != BodyName) child.gameObject.SetActive(false);
                foreach (Transform child in root.transform)
                    if (child != visual && child.GetComponent<SpriteRenderer>() != null && child.GetComponent<Animator>() != null)
                        child.gameObject.SetActive(false);

                Transform old = visual.Find(BodyName);
                if (old != null) Object.DestroyImmediate(old.gameObject);
                GameObject body = new GameObject(BodyName);
                body.transform.SetParent(visual, false);
                Rect r = frames[0].rect;
                float h = c.width > 0f ? c.width * r.height / r.width : c.height;
                body.transform.localPosition = new Vector3(0f, c.centered ? 0f : c.feetY + h * 0.5f, 0f);
                SpriteRenderer sr = body.AddComponent<SpriteRenderer>();
                sr.sprite = frames[0];
                sr.sortingOrder = 1;

                SpriteFrameAnimator anim = root.GetComponent<SpriteFrameAnimator>();
                if (anim == null) anim = root.AddComponent<SpriteFrameAnimator>();
                SerializedObject a = new SerializedObject(anim);
                a.FindProperty("target").objectReferenceValue = sr;
                SetFrames(a.FindProperty("walkFrames"), frames, c.walk);
                SetFrames(a.FindProperty("attackFrames"), frames, c.attack);
                a.FindProperty("walkFps").floatValue = c.walkFps;
                a.FindProperty("attackFps").floatValue = c.attackFps;
                a.ApplyModifiedPropertiesWithoutUndo();

                if (unit != null)
                {
                    SerializedObject u = new SerializedObject(unit);
                    u.FindProperty("bodyRenderer").objectReferenceValue = sr;
                    u.FindProperty("visualRoot").objectReferenceValue = visual;
                    u.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return c.prefab + ": เดิน " + Seq(c.walk) + " / ตี " + Seq(c.attack);
        }

        /// <summary>Skeleton: กระสุน = มีดหมุน ขว้างหลังท่า 4 จากมือที่ชูเหนือหัว</summary>
        private static string SetupSkeletonKnife()
        {
            string path = PrefabDir + "Enemy_SkeletonArcher.prefab";
            string knifeFile = ArtRoot + "/Skeleton/Skeleton_Knife.png";
            if (!File.Exists(path)) return "Skeleton: ไม่เจอ Prefab";
            Sprite knife = ImportSprite(knifeFile, null, 420f);
            if (knife == null) return "Skeleton: ไม่เจอรูปมีด " + knifeFile;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                EnemyUnit unit = root.GetComponent<EnemyUnit>();
                SerializedObject u = new SerializedObject(unit);
                Projectile current = u.FindProperty("projectilePrefab").objectReferenceValue as Projectile;

                // สร้าง Proj_SkeletonKnife (ก็อปจากกระสุนเดิม) ครั้งแรกครั้งเดียว
                if (AssetDatabase.LoadAssetAtPath<GameObject>(KnifePrefabPath) == null)
                {
                    string src = current != null ? AssetDatabase.GetAssetPath(current) : null;
                    if (string.IsNullOrEmpty(src)) return "Skeleton: ไม่มีกระสุนเดิมให้ก็อป";
                    AssetDatabase.CopyAsset(src, KnifePrefabPath);
                    AssetDatabase.ImportAsset(KnifePrefabPath);
                }
                GameObject kp = PrefabUtility.LoadPrefabContents(KnifePrefabPath);
                try
                {
                    SpriteRenderer ksr = kp.GetComponent<SpriteRenderer>();
                    if (ksr == null) ksr = kp.GetComponentInChildren<SpriteRenderer>(true);
                    if (ksr != null) { ksr.sprite = knife; ksr.color = Color.white; ksr.sortingOrder = Mathf.Max(ksr.sortingOrder, 30); ksr.transform.localScale = Vector3.one; }
                    kp.transform.localScale = Vector3.one;
                    Projectile proj = kp.GetComponent<Projectile>();
                    if (proj != null)
                    {
                        SerializedObject ps = new SerializedObject(proj);
                        ps.FindProperty("spinSpeed").floatValue = -900f;
                        ps.FindProperty("rotateToDirection").boolValue = false;
                        ps.ApplyModifiedPropertiesWithoutUndo();
                    }
                    PrefabUtility.SaveAsPrefabAsset(kp, KnifePrefabPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(kp); }

                u.FindProperty("projectilePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Projectile>(KnifePrefabPath);
                u.FindProperty("projectileDelay").floatValue = 0.22f;
                u.FindProperty("projectileSpawnOffset").vector2Value = new Vector2(-0.18f, 0.55f);
                u.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return "Skeleton: ขว้างมีดหมุน หลังท่า 4 (0.22 วิ) / กระสุน = Proj_SkeletonKnife";
        }

        // ------------------------------------------------------------------

        private static Sprite ImportSprite(string file, Cfg c, float fixedPpu = 0f)
        {
            if (!File.Exists(file)) return null;
            TextureImporter ti = AssetImporter.GetAtPath(file) as TextureImporter;
            if (ti == null) { AssetDatabase.ImportAsset(file); ti = AssetImporter.GetAtPath(file) as TextureImporter; }
            if (ti == null) return null;

            // PPU จากขนาดรูป: สูง (หรือกว้าง) เท่าที่ตั้งไว้ในโลก
            float ppu = fixedPpu;
            if (ppu <= 0f)
            {
                ti.GetSourceTextureWidthAndHeight(out int w, out int h);
                ppu = c.width > 0f ? w / c.width : h / c.height;
            }
            bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single
                         || !Mathf.Approximately(ti.spritePixelsPerUnit, ppu) || ti.mipmapEnabled || !ti.alphaIsTransparency;
            if (dirty)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = ppu;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(file);
        }

        private static void SetFrames(SerializedProperty p, Sprite[] frames, int[] seq)
        {
            p.arraySize = seq.Length;
            for (int i = 0; i < seq.Length; i++)
                p.GetArrayElementAtIndex(i).objectReferenceValue = frames[Mathf.Clamp(seq[i] - 1, 0, frames.Length - 1)];
        }

        private static string Seq(int[] s) => string.Join("-", System.Array.ConvertAll(s, x => x.ToString()));
    }
}
