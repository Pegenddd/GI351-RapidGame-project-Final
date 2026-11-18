using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// สร้างไฟล์ Data จากค่าที่ตั้งไว้ใน Prefab แล้วผูกให้อัตโนมัติ (ค่าเดิมไม่หาย)
    /// เมนู: Tools > NecroDefense > Data > Create & Link Stats From Prefabs
    /// - ตัวไหนผูกไฟล์ไว้แล้ว จะข้าม (ไม่ทับค่าในไฟล์)
    /// - ผู้เล่นอยู่ในซีน: เปิดซีน Gameplay (และ TestArena) แล้วกดเมนูนี้ในแต่ละซีน (ใช้ไฟล์ Player เดียวกัน)
    /// </summary>
    public static class NecroDataTools
    {
        public const string DataRoot = "Assets/Game/Data";
        public const string EnemyDataDir = DataRoot + "/Units/Enemies";
        public const string MinionDataDir = DataRoot + "/Units/Minions";
        public const string TowerDataDir = DataRoot + "/Towers";
        public const string PlayerDataPath = DataRoot + "/Units/Player.asset";

        [MenuItem("Tools/NecroDefense/Data/Create & Link Stats From Prefabs", false, 0)]
        public static void CreateAndLink()
        {
            string log = "";
            log += Link<EnemyUnit, EnemyStats>("Assets/Game/Prefabs/Enemies", EnemyDataDir, "ศัตรู");
            log += Link<MinionUnit, MinionStats>("Assets/Game/Prefabs/Minions", MinionDataDir, "มินเนียน");
            log += Link<Tower, TowerStats>("Assets/Game/Prefabs/Towers", TowerDataDir, "ป้อม");
            log += LinkPlayer();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[NecroDefense] Create & Link Stats\n" + log);
            EditorUtility.DisplayDialog("Create & Link Stats", log +
                "\nปรับค่าได้ที่โฟลเดอร์ " + DataRoot + " หรือ Tools > NecroDefense > Data > Balance Table", "OK");
        }

        /// <summary>ผูก Player.asset ให้ผู้เล่นในทุกซีนใน Assets/Scenes (ตัวที่ผูกไว้แล้วไม่แตะ)</summary>
        [MenuItem("Tools/NecroDefense/Data/Link Player Stats In All Scenes", false, 2)]
        public static void LinkPlayerAllScenes()
        {
            PlayerStats asset = AssetDatabase.LoadAssetAtPath<PlayerStats>(PlayerDataPath);
            if (asset == null)
            {
                EditorUtility.DisplayDialog("Link Player Stats", "ไม่เจอ " + PlayerDataPath + "\nกด Create & Link Stats From Prefabs ก่อน", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string start = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;
            string log = "";
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int linked = 0, already = 0;
                foreach (PlayerNecromancer p in Object.FindObjectsByType<PlayerNecromancer>(FindObjectsInactive.Include))
                {
                    SerializedObject so = new SerializedObject(p);
                    SerializedProperty sp = so.FindProperty("stats");
                    if (sp.objectReferenceValue != null) { already++; continue; }
                    sp.objectReferenceValue = asset;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    linked++;
                }
                if (linked > 0) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
                if (linked + already > 0)
                    log += Path.GetFileNameWithoutExtension(path) + ": ผูกใหม่ " + linked + (already > 0 ? " / ผูกไว้แล้ว " + already : "") + "\n";
            }
            if (!string.IsNullOrEmpty(start)) EditorSceneManager.OpenScene(start, OpenSceneMode.Single);
            Debug.Log("[NecroDefense] Link Player Stats\n" + log);
            EditorUtility.DisplayDialog("Link Player Stats", log + "\nตอนนี้ปรับค่าผู้เล่นที่ " + PlayerDataPath + " ได้แล้ว\n(ค่าในไฟล์ใช้ทุกซีน)", "OK");
        }

        /// <summary>Prefab ทุกตัวในโฟลเดอร์: ถ้ายังไม่มีไฟล์ Data -> สร้างจากค่าใน Prefab แล้วผูกช่อง Stats</summary>
        private static string Link<TComp, TStats>(string prefabDir, string dataDir, string label)
            where TComp : Component where TStats : ScriptableObject
        {
            EnsureFolder(dataDir);
            int created = 0, linked = 0, skipped = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { prefabDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    TComp comp = root.GetComponent<TComp>();
                    if (comp == null) continue;
                    SerializedObject cso = new SerializedObject(comp);
                    SerializedProperty statsProp = cso.FindProperty("stats");
                    if (statsProp == null) continue;
                    if (statsProp.objectReferenceValue != null) { skipped++; continue; }

                    string assetPath = dataDir + "/" + Path.GetFileNameWithoutExtension(path) + ".asset";
                    TStats asset = AssetDatabase.LoadAssetAtPath<TStats>(assetPath);
                    if (asset == null)
                    {
                        asset = ScriptableObject.CreateInstance<TStats>();
                        CopyMatching(cso, asset);
                        AssetDatabase.CreateAsset(asset, assetPath);
                        created++;
                    }
                    statsProp.objectReferenceValue = asset;
                    cso.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    linked++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return label + ": สร้างใหม่ " + created + " / ผูกแล้ว " + linked + (skipped > 0 ? " / ผูกไว้ก่อนแล้ว " + skipped : "") + "\n";
        }

        /// <summary>ผู้เล่นในซีนที่เปิดอยู่: สร้าง Player.asset (ครั้งแรก) แล้วผูกทุกตัว</summary>
        private static string LinkPlayer()
        {
            PlayerNecromancer[] players = Object.FindObjectsByType<PlayerNecromancer>(FindObjectsInactive.Include);
            if (players.Length == 0) return "ผู้เล่น: ไม่เจอในซีนที่เปิดอยู่ (เปิดซีน Gameplay แล้วกดเมนูนี้อีกครั้ง)\n";

            EnsureFolder(Path.GetDirectoryName(PlayerDataPath).Replace('\\', '/'));
            PlayerStats asset = AssetDatabase.LoadAssetAtPath<PlayerStats>(PlayerDataPath);
            bool created = false;
            int linked = 0;
            foreach (PlayerNecromancer p in players)
            {
                SerializedObject pso = new SerializedObject(p);
                SerializedProperty statsProp = pso.FindProperty("stats");
                if (statsProp == null || statsProp.objectReferenceValue != null) continue;
                if (asset == null)
                {
                    asset = ScriptableObject.CreateInstance<PlayerStats>();
                    CopyMatching(pso, asset);
                    PlayerAutoAttack auto = p.GetComponent<PlayerAutoAttack>();
                    if (auto != null) CopyMatching(new SerializedObject(auto), asset);
                    PlayerSkills skills = p.GetComponent<PlayerSkills>();
                    if (skills != null) CopyMatching(new SerializedObject(skills), asset);
                    AssetDatabase.CreateAsset(asset, PlayerDataPath);
                    created = true;
                }
                statsProp.objectReferenceValue = asset;
                pso.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(p.gameObject.scene);
                linked++;
            }
            return "ผู้เล่น: " + (created ? "สร้าง Player.asset / " : "") + "ผูกแล้ว " + linked + (linked > 0 ? " (อย่าลืมกด Ctrl+S เซฟซีน)" : "") + "\n";
        }

        /// <summary>คัดลอกค่าทุกช่องที่ชื่อตรงกัน จาก component ไปไฟล์ Data</summary>
        public static void CopyMatching(SerializedObject src, Object dst)
        {
            SerializedObject dso = new SerializedObject(dst);
            SerializedProperty it = dso.GetIterator();
            bool enter = true;
            while (it.NextVisible(enter))
            {
                enter = false;
                if (it.name == "m_Script") continue;
                SerializedProperty sp = src.FindProperty(it.name);
                if (sp != null && sp.propertyType == it.propertyType) dso.CopyFromSerializedProperty(sp);
            }
            dso.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
