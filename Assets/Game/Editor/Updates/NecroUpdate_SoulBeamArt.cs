using System.IO;
using UnityEditor;
using UnityEngine;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// ใส่รูปวาดป้อม Soul Beam (กะโหลกเขาวัว) + ภาพเคลื่อนไหว ลงใน Prefab ป้อมเดิม (ไม่ลบค่าอื่น / ไม่แตะซีน / ไม่แตะสีลำแสง)
    /// เมนู: Tools > NecroDefense > Updates > Apply Soul Beam Tower Art
    /// กดซ้ำได้: สร้างเฉพาะ Visual_SoulBeam ใหม่ (ตำแหน่ง/ขนาดของ Visual_SoulBeam ที่ปรับไว้จะถูกเก็บไว้)
    /// </summary>
    public static class NecroUpdate_SoulBeamArt
    {
        private const string ArtDir = "Assets/Game/Art/Towers/SoulBeam";
        private const string PrefabPath = "Assets/Game/Prefabs/Towers/Tower_SoulBeam.prefab";
        private const float PPU = 420f;           // พิกเซลต่อหน่วย (เท่าป้อมอื่น)
        private const float OriginX = 512f;        // จุดในรูปวาด (1024px) ที่ตรงกับพื้นใต้ป้อม
        private const float OriginY = 940f;
        private const string VisualName = "Visual_SoulBeam";

        // ลำดับการซ้อน (หลัง -> หน้า)
        private const int OrderPole = 2, OrderSpine = 3, OrderShard = 4, OrderSkull = 5, OrderAngry = 6,
                          OrderBase = 7, OrderSide = 8, OrderSideAngry = 9, OrderGlow = 10;

        private struct SkullDef { public string sprite; public float cx, cy, beamX, beamY; }
        private static readonly SkullDef[] MainSkull =
        {
            new SkullDef { sprite = "SoulBeam_Skull0", cx = 512f, cy = 449.5f, beamX = 512f, beamY = 386.4f },
            new SkullDef { sprite = "SoulBeam_Skull1", cx = 512f, cy = 326.5f, beamX = 512f, beamY = 263.4f },
            new SkullDef { sprite = "SoulBeam_Skull2", cx = 510.5f, cy = 326.5f, beamX = 510f, beamY = 252f },
            new SkullDef { sprite = "SoulBeam_Skull2", cx = 510.5f, cy = 326.5f, beamX = 510f, beamY = 252f },
        };
        // เศษหินลอย (Level 2 / 3): ชื่อรูป, x, y
        private static readonly float[,] Shards2 = { { 413.5f, 381f }, { 626.5f, 395.5f }, { 403.5f, 574f }, { 621f, 574f }, { 671f, 694f }, { 408f, 745f } };
        private static readonly float[,] Shards3 = { { 413.5f, 381f }, { 648.5f, 414f }, { 699.5f, 526f }, { 308f, 537f }, { 607f, 735f }, { 408f, 745f } };

        [MenuItem("Tools/NecroDefense/Updates/Apply Soul Beam Tower Art")]
        public static void Apply()
        {
            if (!File.Exists(PrefabPath)) { EditorUtility.DisplayDialog("Soul Beam Art", "ไม่เจอ " + PrefabPath, "OK"); return; }
            ImportSprites();

            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Tower tower = root.GetComponent<Tower>();
                if (tower == null) { Debug.LogError("Tower_SoulBeam ไม่มี Tower component"); return; }

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

                    if (i == 0)
                    {
                        Part(lv.transform, "Pole", "SoulBeam_Pole0", 512.5f, 756.5f, OrderPole);
                        Part(lv.transform, "Spine", "SoulBeam_Spine0", 512.5f, 789f, OrderSpine, breathe: 0.01f);
                    }
                    else
                    {
                        Part(lv.transform, "Pole", "SoulBeam_Pole", 512.5f, 697f, OrderPole);
                        Part(lv.transform, "Spine", "SoulBeam_Spine", 512.5f, 686.5f, OrderSpine, breathe: 0.01f);
                    }

                    float[,] shards = i == 2 ? Shards2 : i == 3 ? Shards3 : null;
                    if (shards != null)
                        for (int k = 0; k < shards.GetLength(0); k++)
                            Part(lv.transform, "Shard_" + (k + 1), "SoulBeam_Shard" + i + "_" + (k + 1), shards[k, 0], shards[k, 1], OrderShard,
                                bob: 0.03f + 0.01f * (k % 3), sway: 10f, phase: k / 6f, attackPulse: 0.3f);

                    MainSkullRig(lv.transform, MainSkull[i]);

                    if (i == 3)
                    {
                        Part(lv.transform, "Base", "SoulBeam_Base3", 512.5f, 836f, OrderBase);
                        SideSkull(lv.transform, "SideSkull_L", "SoulBeam_SideSkullL", 298.5f, 655.5f, 0.2f);
                        SideSkull(lv.transform, "SideSkull_R", "SoulBeam_SideSkullR", 716f, 637.5f, 0.7f);
                    }
                    else Part(lv.transform, "Base", "SoulBeam_Base", 508.5f, 875f, OrderBase);

                    if (i > 0) lv.SetActive(false);
                }

                SoulBeamTowerVisual v = vis.AddComponent<SoulBeamTowerVisual>();
                SerializedObject so = new SerializedObject(v);
                SetArray(so.FindProperty("levelRoots"), levels);
                SetArray(so.FindProperty("levelIcons"), new Object[] { Load("SoulBeam_Full0"), Load("SoulBeam_Full1"), Load("SoulBeam_Full2"), Load("SoulBeam_Full3") });
                so.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject ts = new SerializedObject(tower);
                ts.FindProperty("visual").objectReferenceValue = v;
                ts.FindProperty("scalePerLevel").floatValue = 0f;
                ts.ApplyModifiedPropertiesWithoutUndo();

                Transform body = root.transform.Find("Body");
                if (body != null && body.TryGetComponent(out SpriteRenderer bodySr)) bodySr.enabled = false;

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Debug.Log("[NecroDefense] Soul Beam art applied -> " + PrefabPath);
                EditorUtility.DisplayDialog("Soul Beam Art",
                    "ใส่รูป + ภาพเคลื่อนไหวป้อม Soul Beam เรียบร้อย\n\n" +
                    "ปรับท่าทางได้ที่ Prefab Tower_SoulBeam > Visual_SoulBeam\n" +
                    "(กะโหลก = TowerAngrySkullPart, เศษหิน/กระดูก = TowerIdleMotion)\n" +
                    "สีลำแสง = Prefabs/Effects/FX_SoulBeam (ไม่ได้แตะ)", "OK");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ------------------------------------------------------------------

        private static Vector3 Px(float cx, float cy) => new Vector3((cx - OriginX) / PPU, (OriginY - cy) / PPU, 0f);

        private static void Part(Transform parent, string name, string sprite, float cx, float cy, int order,
            float bob = 0f, float sway = 0f, float breathe = 0f, float phase = 0f, float attackPulse = 0f)
        {
            SpriteRenderer sr = Sprite(parent, name, Load(sprite), Px(cx, cy), order);
            if (bob == 0f && sway == 0f && breathe == 0f && attackPulse == 0f) return;
            TowerIdleMotion m = sr.gameObject.AddComponent<TowerIdleMotion>();
            SerializedObject so = new SerializedObject(m);
            so.FindProperty("bobHeight").floatValue = bob;
            so.FindProperty("swayAngle").floatValue = sway;
            so.FindProperty("breathe").floatValue = breathe;
            so.FindProperty("phase").floatValue = phase;
            so.FindProperty("attackPulse").floatValue = attackPulse;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>กะโหลกหลัก: หัว + หน้าโกรธ + จุดยิงลำแสงกลางระหว่างเขา + แสงรวมพลัง</summary>
        private static void MainSkullRig(Transform parent, SkullDef d)
        {
            GameObject rig = new GameObject("Skull");
            rig.transform.SetParent(parent, false);
            rig.transform.localPosition = Px(d.cx, d.cy);

            Sprite(rig.transform, "SkullFace", Load(d.sprite), Vector3.zero, OrderSkull);
            SpriteRenderer angry = Sprite(rig.transform, "AngryFace", Load(d.sprite.Replace("Skull", "SkullAngry")), Vector3.zero, OrderAngry);
            angry.enabled = false;

            GameObject beam = new GameObject("BeamPoint");
            beam.transform.SetParent(rig.transform, false);
            beam.transform.localPosition = new Vector3((d.beamX - d.cx) / PPU, (d.cy - d.beamY) / PPU, 0f);
            SpriteRenderer glow = Sprite(beam.transform, "ChargeGlow", Load("SoulBeam_Glow"), Vector3.zero, OrderGlow);
            glow.color = new Color(1f, 1f, 1f, 0f);
            glow.enabled = false;

            TowerAngrySkullPart part = rig.AddComponent<TowerAngrySkullPart>();
            SerializedObject so = new SerializedObject(part);
            so.FindProperty("angryFace").objectReferenceValue = angry;
            so.FindProperty("beamPoint").objectReferenceValue = beam.transform;
            so.FindProperty("chargeGlow").objectReferenceValue = glow;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>กะโหลกเล็กข้างฐาน (Level 3): ลอยเบาๆ + โกรธตามกะโหลกหลัก (ไม่ยิง)</summary>
        private static void SideSkull(Transform parent, string name, string sprite, float cx, float cy, float phase)
        {
            GameObject rig = new GameObject(name);
            rig.transform.SetParent(parent, false);
            rig.transform.localPosition = Px(cx, cy);
            Sprite(rig.transform, "SkullFace", Load(sprite), Vector3.zero, OrderSide);
            SpriteRenderer angry = Sprite(rig.transform, "AngryFace", Load(sprite.Replace("Skull", "SkullAngry")), Vector3.zero, OrderSideAngry);
            angry.enabled = false;

            TowerAngrySkullPart part = rig.AddComponent<TowerAngrySkullPart>();
            SerializedObject so = new SerializedObject(part);
            so.FindProperty("angryFace").objectReferenceValue = angry;
            so.FindProperty("phase").floatValue = phase;
            so.FindProperty("bobHeight").floatValue = 0.015f;
            so.FindProperty("lookTilt").floatValue = 4f;
            so.FindProperty("chargeScale").floatValue = 1.04f;
            so.FindProperty("chargeRise").floatValue = 0.02f;
            so.FindProperty("recoilDistance").floatValue = 0.03f;
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

        private static void ImportSprites()
        {
            AssetDatabase.Refresh();
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
                if (ti == null) continue;
                float ppu = path.Contains("Glow") ? 128f : PPU;   // แสง: 128px = 1 หน่วย
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
