using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// เมนู Tools > NecroDefense > Update - Unit Art (Player + White Minions)
    /// 1) มินเนียนทุกตัวใช้ภาพจริงสีขาว (ภาพศัตรูกลับสี) + ท่าเดิน/ตี ชุดเดียวกับศัตรู
    ///    ขนาด/ตำแหน่งตัวเท่ากับศัตรูชนิดเดียวกัน, ปิดเส้นขอบเก่า, ไอคอนในกระเป๋าเป็นภาพใหม่
    /// 2) Skeleton มินเนียนขว้างมีดสีขาวแทนลูกธนู
    /// 3) Necromancer (ซีน Map2) ใช้ภาพจริง: ท่าเดิน 2 เฟรม, ท่าร่าย (ชูไม้เท้า → ชี้) ตอนใช้สกิล
    ///    ไอคอน UI ที่ใช้รูป Necromancer เดิมเปลี่ยนเป็นรูปใหม่ด้วย
    /// กดซ้ำได้
    /// </summary>
    public static class NecroUpdate_UnitArt
    {
        private const string EnemyArtDir = "Assets/Game/Art/Units/Enemies/";
        private const string MinionArtDir = "Assets/Game/Art/Units/Minions/";
        private const string PlayerArtDir = "Assets/Game/Art/Units/Player/";
        private const string PrefabDir = "Assets/Game/Prefabs/";
        private const string ScenePath = "Assets/Scenes/Map2.unity";
        private const string OldPlayerSprite = "Assets/picture/Player_Necromancer.pt.png";

        // จุดเท้าของภาพ Player (สัดส่วนจากซ้าย-ล่างของภาพ)
        private static readonly Vector2 PlayerPivot = new Vector2(0.68f, 0.01f);
        // ภาพใหม่สูงกว่ารูปเดิมกี่เท่า (ผ้าคลุม + ไม้เท้า)
        private const float PlayerHeightScale = 1.35f;

        private static readonly string[,] Pairs =
        {
            { "Minions/Minion_Giant", "Enemies/Enemy_Giant" },
            { "Minions/Minion_Goblin", "Enemies/Enemy_Goblin" },
            { "Minions/Minion_Skeleton", "Enemies/Enemy_SkeletonArcher" },
            { "Minions/Minion_Slime", "Enemies/Enemy_Slime" },
            { "Minions/Minion_SlimeSmall", "Enemies/Enemy_SlimeSmall" },
            { "Minions/Minion_Vulture", "Enemies/Enemy_Vulture" },
        };

        [MenuItem("Tools/NecroDefense/Update - Unit Art (Player + White Minions)")]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog("NecroDefense - Unit Art",
                    "• มินเนียน 6 ชนิด → ภาพจริงสีขาว + ท่าเดิน/ตี (แก้ Prefab ใน Prefabs/Minions)\n" +
                    "• Skeleton มินเนียนขว้างมีดสีขาว (แก้ Proj_MinionArrow)\n" +
                    "• Necromancer ในซีน Map2 → ภาพจริง + ท่าเดิน/ท่าร่าย\n\n" +
                    "ศัตรูไม่ถูกแก้\nแนะนำให้ commit git ก่อนกด", "Apply", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            List<string> log = new List<string>();
            try
            {
                EditorUtility.DisplayProgressBar("NecroDefense", "Importing sprites...", 0.1f);
                AssetDatabase.Refresh();
                ImportWhiteSprites(log);
                ImportPlayerSprites(log);

                EditorUtility.DisplayProgressBar("NecroDefense", "Minion prefabs...", 0.4f);
                for (int i = 0; i < Pairs.GetLength(0); i++) PatchMinion(Pairs[i, 0], Pairs[i, 1], log);
                PatchMinionProjectile(log);

                EditorUtility.DisplayProgressBar("NecroDefense", "Player (Map2)...", 0.8f);
                PatchPlayer(log);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            string summary = string.Join("\n", log);
            Debug.Log("<b>[NecroDefense] Unit Art</b>\n" + summary);
            EditorUtility.DisplayDialog("Unit Art done", summary.Length > 1500 ? summary.Substring(0, 1500) + "\n..." : summary, "OK");
        }

        // ================================================================== import

        private static void ImportWhiteSprites(List<string> log)
        {
            if (!Directory.Exists(MinionArtDir)) { log.Add("!! ไม่พบ " + MinionArtDir); return; }
            int n = 0;
            foreach (string full in Directory.GetFiles(MinionArtDir, "*.png", SearchOption.AllDirectories))
            {
                string path = full.Replace('\\', '/');
                string black = EnemyArtDir + path.Substring(MinionArtDir.Length);
                TextureImporter w = AssetImporter.GetAtPath(path) as TextureImporter;
                TextureImporter b = AssetImporter.GetAtPath(black) as TextureImporter;
                if (w == null || b == null) continue;
                TextureImporterSettings s = new TextureImporterSettings();
                b.ReadTextureSettings(s);
                w.SetTextureSettings(s);
                w.textureType = TextureImporterType.Sprite;
                w.spriteImportMode = SpriteImportMode.Single;
                w.spritePixelsPerUnit = b.spritePixelsPerUnit;
                w.spritePivot = b.spritePivot;
                w.maxTextureSize = b.maxTextureSize;
                w.textureCompression = b.textureCompression;
                w.filterMode = b.filterMode;
                w.mipmapEnabled = b.mipmapEnabled;
                w.SaveAndReimport();
                n++;
            }
            log.Add("Imported " + n + " white minion sprites (same settings as enemy sprites)");
        }

        private static void ImportPlayerSprites(List<string> log)
        {
            int n = 0;
            foreach (string name in new[] { "Player_Walk_1", "Player_Walk_2", "Player_Attack_1", "Player_Attack_2" })
            {
                TextureImporter imp = AssetImporter.GetAtPath(PlayerArtDir + name + ".png") as TextureImporter;
                if (imp == null) { log.Add("!! ไม่พบ " + PlayerArtDir + name + ".png"); continue; }
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                TextureImporterSettings s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = PlayerPivot;
                s.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(s);
                imp.spritePixelsPerUnit = 400f;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.filterMode = FilterMode.Bilinear;
                imp.maxTextureSize = 1024;
                imp.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.SaveAndReimport();
                n++;
            }
            log.Add("Imported " + n + " player sprites");
        }

        private static Sprite White(Sprite black, List<string> log)
        {
            if (black == null) return null;
            string p = AssetDatabase.GetAssetPath(black);
            if (!p.StartsWith(EnemyArtDir)) return black;
            Sprite w = AssetDatabase.LoadAssetAtPath<Sprite>(MinionArtDir + p.Substring(EnemyArtDir.Length));
            if (w == null) { log.Add("!! ไม่มีภาพขาวของ " + p); return black; }
            return w;
        }

        private static Sprite PlayerSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(PlayerArtDir + name + ".png");

        // ================================================================== minions

        private static void PatchMinion(string minionRel, string enemyRel, List<string> log)
        {
            string mPath = PrefabDir + minionRel + ".prefab", ePath = PrefabDir + enemyRel + ".prefab";
            if (!File.Exists(mPath) || !File.Exists(ePath)) { log.Add("!! ข้าม " + minionRel + " (ไม่พบ prefab)"); return; }

            GameObject e = PrefabUtility.LoadPrefabContents(ePath);
            GameObject m = PrefabUtility.LoadPrefabContents(mPath);
            try
            {
                EnemyUnit eu = e.GetComponent<EnemyUnit>();
                MinionUnit mu = m.GetComponent<MinionUnit>();
                SpriteRenderer eb = eu != null ? new SerializedObject(eu).FindProperty("bodyRenderer").objectReferenceValue as SpriteRenderer : null;
                SerializedObject mso = mu != null ? new SerializedObject(mu) : null;
                SpriteRenderer mb = mso != null ? mso.FindProperty("bodyRenderer").objectReferenceValue as SpriteRenderer : null;
                if (eb == null || mb == null) { log.Add("!! " + minionRel + ": ไม่พบ bodyRenderer"); return; }

                // ภาพ + ขนาด/ตำแหน่งเหมือนศัตรู
                mb.sprite = White(eb.sprite, log);
                mb.flipX = eb.flipX;
                mb.color = Color.white;
                Vector3 rel = e.transform.InverseTransformPoint(eb.transform.position);
                mb.transform.position = m.transform.TransformPoint(rel);
                Vector3 want = eb.transform.lossyScale, parent = mb.transform.parent != null ? mb.transform.parent.lossyScale : Vector3.one;
                mb.transform.localScale = new Vector3(Mathf.Abs(want.x / parent.x), Mathf.Abs(want.y / parent.y), 1f);

                // เส้นขอบเก่า (รูปการ์ตูน) ปิดไว้เหมือนฝั่งศัตรู
                foreach (Transform t in m.GetComponentsInChildren<Transform>(true))
                    if (t.name == "Outline") t.gameObject.SetActive(false);

                // ท่าเดิน / ตี
                SpriteFrameAnimator ea = e.GetComponentInChildren<SpriteFrameAnimator>(true);
                SpriteFrameAnimator ma = m.GetComponent<SpriteFrameAnimator>();
                if (ma == null) ma = m.AddComponent<SpriteFrameAnimator>();
                SerializedObject ms = new SerializedObject(ma);
                ms.FindProperty("target").objectReferenceValue = mb;
                Sprite first = mb.sprite;
                if (ea != null)
                {
                    SerializedObject es = new SerializedObject(ea);
                    CopyFrames(es, ms, "walkFrames", log);
                    CopyFrames(es, ms, "attackFrames", log);
                    ms.FindProperty("walkFps").floatValue = es.FindProperty("walkFps").floatValue;
                    ms.FindProperty("attackFps").floatValue = es.FindProperty("attackFps").floatValue;
                    ms.FindProperty("moveThreshold").floatValue = es.FindProperty("moveThreshold").floatValue;
                    ms.FindProperty("idleFrame").objectReferenceValue = White(es.FindProperty("idleFrame").objectReferenceValue as Sprite, log);
                    SerializedProperty wf = ms.FindProperty("walkFrames");
                    if (wf.arraySize > 0 && wf.GetArrayElementAtIndex(0).objectReferenceValue is Sprite s0) first = s0;
                }
                ms.ApplyModifiedPropertiesWithoutUndo();

                mso.FindProperty("icon").objectReferenceValue = first;
                mso.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(m, mPath);
                log.Add(Path.GetFileName(mPath) + ": white art + walk/attack frames");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(e);
                PrefabUtility.UnloadPrefabContents(m);
            }
        }

        private static void CopyFrames(SerializedObject from, SerializedObject to, string field, List<string> log)
        {
            SerializedProperty a = from.FindProperty(field), b = to.FindProperty(field);
            b.arraySize = a.arraySize;
            for (int i = 0; i < a.arraySize; i++)
                b.GetArrayElementAtIndex(i).objectReferenceValue = White(a.GetArrayElementAtIndex(i).objectReferenceValue as Sprite, log);
        }

        /// <summary>Skeleton มินเนียน: ลูกธนู → มีดขาวหมุน (หน้าตาเหมือนมีดของศัตรู)</summary>
        private static void PatchMinionProjectile(List<string> log)
        {
            string mPath = PrefabDir + "Projectiles/Proj_MinionArrow.prefab", kPath = PrefabDir + "Projectiles/Proj_SkeletonKnife.prefab";
            if (!File.Exists(mPath) || !File.Exists(kPath)) return;
            GameObject k = PrefabUtility.LoadPrefabContents(kPath);
            GameObject m = PrefabUtility.LoadPrefabContents(mPath);
            try
            {
                SpriteRenderer ks = k.GetComponentInChildren<SpriteRenderer>(true), msr = m.GetComponentInChildren<SpriteRenderer>(true);
                Projectile kp = k.GetComponent<Projectile>(), mp = m.GetComponent<Projectile>();
                if (ks == null || msr == null || kp == null || mp == null) return;
                msr.sprite = White(ks.sprite, log);
                msr.color = Color.white;
                msr.sortingOrder = ks.sortingOrder;
                m.transform.localScale = k.transform.localScale;
                SerializedObject ko = new SerializedObject(kp), mo = new SerializedObject(mp);
                mo.FindProperty("rotateToDirection").boolValue = ko.FindProperty("rotateToDirection").boolValue;
                mo.FindProperty("spinSpeed").floatValue = ko.FindProperty("spinSpeed").floatValue;
                mo.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(m, mPath);
                log.Add("Proj_MinionArrow: white spinning knife");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(k);
                PrefabUtility.UnloadPrefabContents(m);
            }
        }

        // ================================================================== player

        private static void PatchPlayer(List<string> log)
        {
            if (!File.Exists(ScenePath)) { log.Add("!! ไม่พบ " + ScenePath); return; }
            Sprite walk1 = PlayerSprite("Player_Walk_1"), walk2 = PlayerSprite("Player_Walk_2");
            Sprite atk1 = PlayerSprite("Player_Attack_1"), atk2 = PlayerSprite("Player_Attack_2");
            if (walk1 == null) { log.Add("!! ไม่พบภาพ Player"); return; }
            Sprite old = AssetDatabase.LoadAssetAtPath<Sprite>(OldPlayerSprite);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            PlayerNecromancer player = null;
            foreach (GameObject r in scene.GetRootGameObjects())
            {
                player = r.GetComponentInChildren<PlayerNecromancer>(true);
                if (player != null) break;
            }
            if (player == null) { log.Add("!! Map2: ไม่พบ PlayerNecromancer"); return; }

            SerializedObject pso = new SerializedObject(player);
            SpriteRenderer body = pso.FindProperty("bodyRenderer").objectReferenceValue as SpriteRenderer;
            if (body == null) { log.Add("!! Map2: Player ไม่มี bodyRenderer"); return; }

            // ขนาด: สูงกว่ารูปเดิม PlayerHeightScale เท่า, เท้าอยู่ที่ขอบล่างเดิม (ทำครั้งแรกเท่านั้น)
            if (body.sprite == old || body.sprite == null || !AssetDatabase.GetAssetPath(body.sprite).StartsWith(PlayerArtDir))
            {
                Bounds ob = body.bounds;
                float oldH = ob.size.y > 0.01f ? ob.size.y : 1f;
                body.sprite = walk1;
                body.flipX = false;
                float newH = walk1.bounds.size.y;
                Vector3 parentScale = body.transform.parent != null ? body.transform.parent.lossyScale : Vector3.one;
                float k = oldH * PlayerHeightScale / newH;
                body.transform.localScale = new Vector3(k / Mathf.Abs(parentScale.x), k / Mathf.Abs(parentScale.y), 1f);
                body.transform.position = new Vector3(ob.center.x, ob.min.y, body.transform.position.z);
                log.Add("Map2: Necromancer sprite replaced (height x" + PlayerHeightScale + ")");
            }
            body.color = Color.white;
            pso.FindProperty("artFacesRight").boolValue = false; // รูปหันซ้าย
            pso.ApplyModifiedPropertiesWithoutUndo();

            // เส้นขอบเก่า (รูปการ์ตูน) ปิด
            foreach (SpriteRenderer sr in player.GetComponentsInChildren<SpriteRenderer>(true))
                if (sr != body && old != null && sr.sprite == old) { sr.gameObject.SetActive(false); }

            // ท่าเดิน / ท่าร่าย
            SpriteFrameAnimator anim = player.GetComponent<SpriteFrameAnimator>();
            if (anim == null) anim = player.gameObject.AddComponent<SpriteFrameAnimator>();
            SerializedObject a = new SerializedObject(anim);
            a.FindProperty("target").objectReferenceValue = body;
            SetFrames(a.FindProperty("walkFrames"), walk1, walk2);
            SetFrames(a.FindProperty("attackFrames"), atk1, atk2);
            a.FindProperty("idleFrame").objectReferenceValue = walk1;
            a.FindProperty("walkFps").floatValue = 5f;
            a.FindProperty("attackFps").floatValue = 7f;
            a.ApplyModifiedPropertiesWithoutUndo();
            log.Add("Map2: Necromancer walk (2 frames) + cast animation (raise staff → point)");

            // ไอคอน UI ที่ใช้รูปเดิม
            int icons = 0;
            if (old != null)
                foreach (GameObject r in scene.GetRootGameObjects())
                    foreach (Image img in r.GetComponentsInChildren<Image>(true))
                        if (img.sprite == old) { img.sprite = walk1; img.preserveAspect = true; EditorUtility.SetDirty(img); icons++; }
            if (icons > 0) log.Add("Map2: " + icons + " UI icon(s) → new Necromancer art");

            EditorUtility.SetDirty(player);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void SetFrames(SerializedProperty p, params Sprite[] frames)
        {
            List<Sprite> list = new List<Sprite>();
            foreach (Sprite s in frames) if (s != null) list.Add(s);
            p.arraySize = list.Count;
            for (int i = 0; i < list.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }
    }
}
