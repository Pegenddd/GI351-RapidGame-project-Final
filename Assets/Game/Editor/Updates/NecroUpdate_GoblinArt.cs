using System.IO;
using UnityEditor;
using UnityEngine;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// ใส่รูปก็อบลิน (ศัตรู) 4 เฟรม + ท่าเดิน (1-2-3-4) / ท่าตี (4-1) ลงใน Enemy_Goblin
    /// เมนู: Tools > NecroDefense > Updates > Apply Goblin Art
    /// - สร้าง Visual/Body_Animated ใหม่ (กดซ้ำได้)
    /// - ของเดิมที่ใส่ไว้ (1_0 + Animator) ไม่ลบ แค่ปิดไว้ (เปิดกลับได้ใน Hierarchy ของ Prefab)
    /// - ค่าอื่นของก็อบลิน (HP, ดาเมจ, ความเร็ว ฯลฯ) ไม่แตะ
    /// </summary>
    public static class NecroUpdate_GoblinArt
    {
        private const string ArtDir = "Assets/Game/Art/Units/Goblin";
        private const string PrefabPath = "Assets/Game/Prefabs/Enemies/Enemy_Goblin.prefab";
        private const float PPU = 400f;           // รูป 420px สูง = ตัวสูงประมาณ 1.05 หน่วย
        private const string BodyName = "Body_Animated";

        [MenuItem("Tools/NecroDefense/Updates/Apply Goblin Art")]
        public static void Apply()
        {
            if (!File.Exists(PrefabPath)) { EditorUtility.DisplayDialog("Goblin Art", "ไม่เจอ " + PrefabPath, "OK"); return; }
            ImportSprites();
            Sprite f1 = Load("Goblin_1"), f2 = Load("Goblin_2"), f3 = Load("Goblin_3"), f4 = Load("Goblin_4");
            if (f1 == null) return;

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                EnemyUnit unit = root.GetComponent<EnemyUnit>();
                if (unit == null) { Debug.LogError("Enemy_Goblin ไม่มี EnemyUnit"); return; }

                // กลุ่มภาพที่กลับซ้าย-ขวาตามทิศเดิน
                Transform visual = root.transform.Find("Visual");
                if (visual == null)
                {
                    visual = new GameObject("Visual").transform;
                    visual.SetParent(root.transform, false);
                }

                // ตัวเดิม (ถ้ามี) ปิดไว้ ไม่ลบ
                Vector3 keepPos = new Vector3(0f, 0.04f, 0f);
                Transform old = visual.Find(BodyName);
                if (old != null) { keepPos = old.localPosition; Object.DestroyImmediate(old.gameObject); }
                foreach (Transform child in root.transform)
                {
                    if (child == visual) continue;
                    if (child.GetComponent<SpriteRenderer>() != null && child.GetComponent<Animator>() != null)
                        child.gameObject.SetActive(false);
                }

                GameObject body = new GameObject(BodyName);
                body.transform.SetParent(visual, false);
                body.transform.localPosition = keepPos;
                SpriteRenderer sr = body.AddComponent<SpriteRenderer>();
                sr.sprite = f1;
                sr.sortingOrder = 1;

                SpriteFrameAnimator anim = root.GetComponent<SpriteFrameAnimator>();
                if (anim == null) anim = root.AddComponent<SpriteFrameAnimator>();
                SerializedObject a = new SerializedObject(anim);
                a.FindProperty("target").objectReferenceValue = sr;
                SetArray(a.FindProperty("walkFrames"), new Object[] { f1, f2, f3, f4 });
                SetArray(a.FindProperty("attackFrames"), new Object[] { f4, f1 });
                a.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject u = new SerializedObject(unit);
                u.FindProperty("bodyRenderer").objectReferenceValue = sr;
                u.FindProperty("visualRoot").objectReferenceValue = visual;
                u.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[NecroDefense] Goblin art applied -> " + PrefabPath);
                EditorUtility.DisplayDialog("Goblin Art",
                    "ใส่รูปก็อบลิน + ท่าเดิน (1-2-3-4) / ท่าตี (4-1) เรียบร้อย\n\n" +
                    "ปรับความเร็วท่าได้ที่ Enemy_Goblin > SpriteFrameAnimator (Walk Fps / Attack Fps)\n" +
                    "ตำแหน่ง/ขนาดตัว = Visual > " + BodyName, "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
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

        private static void ImportSprites()
        {
            AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                bool dirty = ti.textureType != TextureImporterType.Sprite || ti.spriteImportMode != SpriteImportMode.Single
                             || !Mathf.Approximately(ti.spritePixelsPerUnit, PPU) || ti.mipmapEnabled || !ti.alphaIsTransparency;
                if (!dirty) continue;
                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.spritePixelsPerUnit = PPU;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.filterMode = FilterMode.Bilinear;
                ti.SaveAndReimport();
            }
        }
    }
}
