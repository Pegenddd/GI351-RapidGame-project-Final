using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// สร้างซีน Map2 (ทางมอน 2 ทาง: ซ้ายแบบซิกแซก + ล่างแบบตรง มาบรรจบกันหน้าฐาน)
    /// เมนู: Tools > NecroDefense > Updates > Create Map2 Scene
    /// - ก็อปซีน Gameplay เป็น Assets/Scenes/Map2.unity (UI / ผู้เล่น / Manager / ค่าที่ปรับไว้ มาครบ)
    ///   ซีน Gameplay เดิมไม่ถูกแตะ
    /// - ใน Map2: ย้ายทางมอน / จุดสร้างป้อม / ฐาน / ผู้เล่น + พื้นและป่าใหม่ + เวฟใหม่ (ผู้เล่นใหม่ตั้งตัวทัน)
    ///   พื้น/ป่าเดิมแค่ปิดไว้ (ไม่ลบ) / ทางขวา (Path_East) ปิดไว้
    /// - กดซ้ำได้: เลือกจัดแมพใหม่ในซีน Map2 เดิม (ไม่แตะเวฟที่แก้ไว้) หรือก็อปจาก Gameplay ใหม่ทั้งซีน
    /// ปรับตำแหน่งได้อิสระใน Scene หลังสร้าง (ทุกอย่างเป็น GameObject ธรรมดา)
    /// </summary>
    public static class NecroUpdate_Map2
    {
        private const string SourceScene = "Assets/Scenes/Gameplay.unity";
        private const string TargetScene = "Assets/Scenes/Map2.unity";
        private const string PrefabDir = "Assets/Game/Prefabs/Enemies/";
        private const string GroundName = "Ground_Map2";
        private const string DecoName = "Decorations_Map2";

        // ---------------- Layout (หน่วยโลก, กลางแมพ = 0,0, แมพ 38 x 22) ----------------
        private static readonly Color GroundGray = new Color(0.6f, 0.6f, 0.6f);
        private static readonly Color LaneWhite = new Color(0.89f, 0.89f, 0.89f);
        private static readonly Color Forest = new Color(0.02f, 0.02f, 0.02f);
        private const float LaneWidth = 2.6f;

        private static readonly Vector3 BasePos = new Vector3(10.5f, 7.2f, 0f);
        private static readonly Vector3 PlayerPos = new Vector3(12f, 2.4f, 0f);
        // พื้นที่ขาว (ฝั่งเรา) = FreeZone: ศัตรูเข้ามาแล้วถึงจะเลิกสนใจแค่มินเนียน
        private static readonly Vector3 FreeZoneCenter = new Vector3(11f, 5.2f, 0f);
        private static readonly Vector2 FreeZoneSize = new Vector2(16f, 10f);

        // จุดร่วมของทั้ง 2 ทาง: ทางแยก -> เดินเข้าฐาน
        private static readonly Vector3 Junction = new Vector3(4.06f, 1.31f, 0f);
        private static readonly Vector3 ToBaseA = new Vector3(9.5f, 1.6f, 0f);
        private static readonly Vector3 ToBaseB = new Vector3(10.5f, 4.6f, 0f);

        // ทางซ้าย: ขึ้นเฉียง -> ขวา -> ลง -> ขึ้นเฉียง -> ขวาถึงทางแยก
        private static readonly Vector3 WestSpawn = new Vector3(-19.2f, 0.65f, 0f);
        private static readonly Vector3 WestWarning = new Vector3(-17f, 2.2f, 0f);
        private static readonly Vector3[] WestPoints =
        {
            new Vector3(-11.4f, 6.06f), new Vector3(-8.25f, 6.06f), new Vector3(-8.25f, -2.75f),
            new Vector3(-3.1f, 1.31f), Junction, ToBaseA, ToBaseB,
        };

        // ทางล่าง: ตรงขึ้นมาที่ทางแยก
        private static readonly Vector3 SouthSpawn = new Vector3(4.06f, -10.8f, 0f);
        private static readonly Vector3 SouthWarning = new Vector3(4.06f, -8.4f, 0f);
        private static readonly Vector3[] SouthPoints = { Junction, ToBaseA, ToBaseB };

        // จุดสร้างป้อม 7 จุด (เกินจากนี้ปิดไว้)
        private static readonly Vector3[] SlotPositions =
        {
            new Vector3(-14.4f, -3.4f),  // 1 ต้นทางซ้าย (ใต้ทางเฉียงแรก)
            new Vector3(-11.1f, 3.0f),   // 2 มุมในโค้งบนซ้าย (ยิงได้ 3 ช่วง)
            new Vector3(-2.2f, 4.75f),   // 3 ช่องว่างที่ทางล้อม (ยิงทางลง + ทางขวา)
            new Vector3(-1.25f, -1.5f),  // 4 ใต้ทางเฉียงที่ 2
            new Vector3(7.2f, -4.0f),    // 5 ข้างทางล่าง
            new Vector3(7.0f, 4.6f),     // 6 ซ้ายฐาน
            new Vector3(14.2f, 4.75f),   // 7 ขวาฐาน
        };

        private static Sprite _square, _circle, _tree;

        [MenuItem("Tools/NecroDefense/Updates/Create Map2 Scene")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene) != null;
            bool fresh = true;
            if (exists)
            {
                int c = EditorUtility.DisplayDialogComplex("Map2",
                    "มีซีน Map2 อยู่แล้ว จะทำอะไร?\n\n" +
                    "จัดแมพใหม่: ย้ายทาง/ป้อม/ฐาน + พื้นใหม่ ในซีน Map2 เดิม (เวฟ + ของอื่นที่แก้ไว้ไม่แตะ)\n" +
                    "ก็อปใหม่ทั้งซีน: ทับ Map2 ด้วยซีน Gameplay ล่าสุด แล้วจัดแมพ + เวฟใหม่ (ของที่แก้ใน Map2 หาย)",
                    "จัดแมพใหม่", "ยกเลิก", "ก็อปใหม่ทั้งซีน");
                if (c == 1) return;
                fresh = c == 2;
            }

            if (fresh)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene) == null)
                {
                    EditorUtility.DisplayDialog("Map2", "ไม่เจอ " + SourceScene, "OK");
                    return;
                }
                if (exists) AssetDatabase.DeleteAsset(TargetScene);
                if (!AssetDatabase.CopyAsset(SourceScene, TargetScene))
                {
                    EditorUtility.DisplayDialog("Map2", "ก็อปซีนไม่สำเร็จ", "OK");
                    return;
                }
            }

            _square = AssetDatabase.LoadAssetAtPath<Sprite>(BU.ArtDir + "/Square.png");
            _circle = AssetDatabase.LoadAssetAtPath<Sprite>(BU.ArtDir + "/Circle.png");
            _tree = AssetDatabase.LoadAssetAtPath<Sprite>(BU.ArtDir + "/Deco_Tree.png");

            Scene s = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
            string log = "";
            try
            {
                log += Layout(s, fresh);
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                log += "\nเกิดข้อผิดพลาด: " + e.Message;
            }
            EditorSceneManager.MarkSceneDirty(s);
            EditorSceneManager.SaveScene(s);
            AddToBuildSettings();

            Debug.Log("[NecroDefense] Create Map2 Scene\n" + log);
            EditorUtility.DisplayDialog("Map2", log + "\nเปิดซีนไว้แล้ว กด Play ได้เลย", "OK");
        }

        // ------------------------------------------------------------------

        private static string Layout(Scene s, bool setWaves)
        {
            string log = "";
            MapBounds bounds = Find<MapBounds>();
            Transform map = bounds != null ? bounds.transform : FindRoot(s, "Map");
            if (map == null) return "ไม่เจอ Map ในซีน";
            if (bounds != null) new SO(bounds).Set("size", new Vector2(38f, 22f)).Apply();

            // ----- พื้น / ป่า: ของเดิมปิดไว้ สร้างชุดใหม่ -----
            foreach (string old in new[] { "Ground", "Decorations" })
            {
                Transform t = map.Find(old);
                if (t != null) t.gameObject.SetActive(false);
            }
            DestroyChild(map, GroundName);
            DestroyChild(map, DecoName);
            BuildGround(map);
            BuildDecorations(map);
            log += "พื้น + ป่าใหม่ (ของเดิม Ground / Decorations ปิดไว้)\n";

            // ----- ฐาน / ผู้เล่น / FreeZone -----
            Crystal crystal = Find<Crystal>();
            if (crystal != null) crystal.transform.position = BasePos;
            PlayerNecromancer player = Find<PlayerNecromancer>();
            if (player != null) player.transform.position = PlayerPos;
            Camera cam = Camera.main != null ? Camera.main : Find<Camera>();
            if (cam != null) cam.transform.position = new Vector3(PlayerPos.x, PlayerPos.y, cam.transform.position.z);
            FreeZone fz = Find<FreeZone>();
            if (fz != null)
            {
                fz.transform.position = FreeZoneCenter;
                new SO(fz).Set("size", FreeZoneSize).Apply();
            }

            // ----- จุดสร้างป้อม -----
            List<TowerSlot> slots = new List<TowerSlot>(Object.FindObjectsByType<TowerSlot>(FindObjectsInactive.Include));
            slots.Sort((a, b) => a.SlotNumber.CompareTo(b.SlotNumber));
            if (slots.Count == 0) log += "ไม่เจอ TowerSlot\n";
            while (slots.Count > 0 && slots.Count < SlotPositions.Length)
            {
                TowerSlot copy = Object.Instantiate(slots[slots.Count - 1], slots[0].transform.parent);
                slots.Add(copy);
            }
            for (int i = 0; i < slots.Count; i++)
            {
                bool use = i < SlotPositions.Length;
                slots[i].gameObject.SetActive(use);
                if (!use) continue;
                slots[i].transform.position = SlotPositions[i];
                slots[i].name = "TowerSlot_" + (i + 1);
                new SO(slots[i]).Set("slotNumber", i + 1).Apply();
            }
            log += "จุดสร้างป้อม " + Mathf.Min(slots.Count, SlotPositions.Length) + " จุด" +
                   (slots.Count > SlotPositions.Length ? " (เกินปิดไว้ " + (slots.Count - SlotPositions.Length) + ")" : "") + "\n";

            // ----- ทางมอน -----
            WaveManager wm = Find<WaveManager>();
            if (wm == null) return log + "ไม่เจอ WaveManager";
            SerializedObject wso = new SerializedObject(wm);
            EnemyPath west = wso.FindProperty("pathWest").objectReferenceValue as EnemyPath;
            EnemyPath south = wso.FindProperty("pathSouth").objectReferenceValue as EnemyPath;
            EnemyPath east = wso.FindProperty("pathEast").objectReferenceValue as EnemyPath;
            EnemyPath north = wso.FindProperty("pathNorth").objectReferenceValue as EnemyPath;
            if (west == null || south == null) return log + "WaveManager ไม่มี Path West / South";

            SetPath(west, WestSpawn, WestWarning, WestPoints);
            SetPath(south, SouthSpawn, SouthWarning, SouthPoints);
            foreach (EnemyPath p in new[] { east, north })
                if (p != null && p != west && p != south) p.gameObject.SetActive(false);
            wso.FindProperty("pathEast").objectReferenceValue = null;
            wso.FindProperty("pathNorth").objectReferenceValue = null;
            wso.FindProperty("spawnScatter").floatValue = 0.6f;
            wso.ApplyModifiedPropertiesWithoutUndo();
            log += "ทางมอน 2 ทาง: ซ้าย (ซิกแซก) + ล่าง / ทางขวาปิดไว้\n";

            if (setWaves) log += FillWaves(wm);
            else log += "เวฟ: ไม่แตะ (ใช้ของที่แก้ไว้ใน Map2)\n";
            return log;
        }

        private static void SetPath(EnemyPath path, Vector3 spawn, Vector3 warning, Vector3[] points)
        {
            path.gameObject.SetActive(true);
            path.transform.position = spawn;

            List<Transform> remove = new List<Transform>();
            foreach (Transform c in path.transform) if (c.name.StartsWith("WP_")) remove.Add(c);
            foreach (Transform c in remove) Object.DestroyImmediate(c.gameObject);

            Object[] wps = new Object[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                GameObject wp = BU.Go("WP_" + (i + 1), path.transform);
                wp.transform.position = points[i];
                wps[i] = wp.transform;
            }
            SO so = new SO(path).ObjList("waypoints", wps);
            Transform warn = path.transform.Find("WaveWarning");
            if (warn != null) { warn.position = warning; so.Set("warningPoint", warn); }
            so.Apply();
        }

        // ------------------------------------------------------------------
        // พื้น: เทา (ฝั่งศัตรู) + ขาว (ฝั่งเรา / ทางมอน)
        // ------------------------------------------------------------------

        private static void BuildGround(Transform map)
        {
            Transform g = BU.Go(GroundName, map).transform;
            BU.Sprite("Ground_Gray", g, _square, GroundGray, new Vector2(44f, 28f), -30);
            // ฝั่งเรา (ขาว) มุมขวาบน
            BU.Sprite("OurZone_White", g, _square, LaneWhite, new Vector2(18f, 13f), -29, new Vector3(12.05f, 6.6f, 0f));

            Transform lanes = BU.Go("EnemyLanes", g).transform;
            // ทางซ้าย: เริ่มนอกจอ แล้วตามจุด
            List<Vector3> west = new List<Vector3> { WestSpawn + (WestSpawn - WestPoints[0]).normalized * 3f, WestSpawn };
            for (int i = 0; i < 5; i++) west.Add(WestPoints[i]);
            Lane(lanes, "Lane_West", west);
            Lane(lanes, "Lane_South", new List<Vector3> { SouthSpawn + Vector3.down * 3f, SouthSpawn, Junction });
        }

        private static void Lane(Transform parent, string name, List<Vector3> pts)
        {
            Transform t = BU.Go(name, parent).transform;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                Vector3 a = pts[i], b = pts[i + 1];
                Vector3 d = b - a;
                if (d.sqrMagnitude < 0.0001f) continue;
                SpriteRenderer seg = BU.Sprite("Segment_" + (i + 1), t, _square, LaneWhite, new Vector2(d.magnitude, LaneWidth), -28, (a + b) * 0.5f);
                seg.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                if (i > 0) BU.Sprite("Corner_" + i, t, _circle, LaneWhite, new Vector2(LaneWidth, LaneWidth), -28, a);
            }
        }

        // ------------------------------------------------------------------
        // ป่าดำ: บนสุด (ทั้งแถบ ยกเว้นหลังฐาน) + ล่าง (เว้นช่องทางมอน)
        // ------------------------------------------------------------------

        private static void BuildDecorations(Transform map)
        {
            Transform deco = BU.Go(DecoName, map).transform;

            Transform top = BU.Go("Forest_Top", deco).transform;
            BU.Sprite("Band", top, _square, Forest, new Vector2(44f, 3f), -27, new Vector3(0f, 10.6f, 0f));
            for (float x = -20f; x <= 20f; x += 1.7f)
            {
                if (Mathf.Abs(x - BasePos.x) < 2.4f) continue; // เว้นหลังฐาน
                float h = 2.4f + Mathf.PerlinNoise(x * 0.37f, 0.5f) * 1.2f;
                BU.Sprite("Tree", top, _tree, Forest, new Vector2(1.9f, h), -26, new Vector3(x, 8.5f + Mathf.PerlinNoise(x, 1f) * 0.5f, 0f));
            }

            Transform bottom = BU.Go("Forest_Bottom", deco).transform;
            BU.Sprite("Band", bottom, _square, Forest, new Vector2(44f, 3f), -24, new Vector3(0f, -11f, 0f));
            for (float x = -20f; x <= 20f; x += 1.3f)
            {
                if (Mathf.Abs(x - SouthSpawn.x) < 2.3f) continue; // ช่องทางมอนด้านล่าง
                BU.Sprite("Bush", bottom, _circle, Forest, new Vector2(2.0f, 1.7f), -24, new Vector3(x, -9.2f + Mathf.PerlinNoise(x, 2f) * 0.6f, 0f));
            }
        }

        // ------------------------------------------------------------------
        // เวฟ: 1-3 มาทางซ้ายทางเดียว / 4 เปิดทางล่าง / 8+ ทั้ง 2 ทาง
        // ------------------------------------------------------------------

        private struct G
        {
            public EnemyUnit e; public int n; public float every, delay; public SpawnSide side;
            public G(EnemyUnit e, int n, float every, float delay, SpawnSide side)
            { this.e = e; this.n = n; this.every = every; this.delay = delay; this.side = side; }
        }

        private static EnemyUnit Enemy(string file)
        {
            GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + file + ".prefab");
            if (go == null) Debug.LogWarning("[NecroDefense] Map2: ไม่เจอ " + PrefabDir + file + ".prefab");
            return go != null ? go.GetComponent<EnemyUnit>() : null;
        }

        private static string FillWaves(WaveManager wm)
        {
            EnemyUnit slime = Enemy("Enemy_Slime"), goblin = Enemy("Enemy_Goblin"), archer = Enemy("Enemy_SkeletonArcher"),
                      vulture = Enemy("Enemy_Vulture"), giant = Enemy("Enemy_Giant");
            SpawnSide W = SpawnSide.West, S = SpawnSide.South, A = SpawnSide.AllSides;

            var waves = new List<(string, G[])>
            {
                // ช่วงฝึก: ทางซ้ายทางเดียว
                ("1 - First Blood", new[] { new G(slime, 5, 2.0f, 0f, W) }),
                ("2 - Ooze", new[] { new G(slime, 7, 1.8f, 0f, W), new G(archer, 2, 2.5f, 6f, W) }),
                ("3 - Bones", new[] { new G(slime, 8, 1.6f, 0f, W), new G(archer, 4, 2.2f, 4f, W) }),
                // เปิดทางล่าง
                ("4 - Second Front", new[] { new G(slime, 6, 1.8f, 0f, W), new G(slime, 4, 2.0f, 6f, S) }),
                ("5 - Goblin Rush", new[] { new G(goblin, 6, 1.4f, 0f, W), new G(slime, 6, 1.6f, 4f, S), new G(archer, 3, 2.2f, 8f, W) }),
                ("6 - Wings", new[] { new G(goblin, 6, 1.3f, 0f, W), new G(vulture, 4, 2.2f, 5f, S), new G(archer, 4, 2.0f, 8f, W) }),
                ("7 - The Giant", new[] { new G(slime, 10, 1.3f, 0f, W), new G(goblin, 6, 1.3f, 4f, S), new G(giant, 1, 1f, 12f, W) }),
                // ทั้ง 2 ทาง
                ("8 - Siege", new[] { new G(goblin, 10, 1.1f, 0f, A), new G(archer, 6, 1.6f, 4f, W), new G(vulture, 4, 2.0f, 8f, S), new G(giant, 1, 1f, 14f, S) }),
                ("9 - Onslaught", new[] { new G(slime, 14, 0.9f, 0f, A), new G(goblin, 10, 0.9f, 3f, A), new G(archer, 6, 1.4f, 6f, W), new G(vulture, 6, 1.6f, 8f, A), new G(giant, 2, 6f, 10f, A) }),
                ("10 - Dark Tide", new[] { new G(slime, 16, 0.8f, 0f, A), new G(goblin, 12, 0.8f, 3f, A), new G(archer, 8, 1.2f, 5f, A), new G(vulture, 6, 1.4f, 7f, A), new G(giant, 2, 6f, 9f, A) }),
                ("11 - Endless Night", new[] { new G(slime, 18, 0.7f, 0f, A), new G(goblin, 14, 0.7f, 3f, A), new G(archer, 10, 1.1f, 5f, A), new G(vulture, 8, 1.2f, 7f, A), new G(giant, 3, 5f, 9f, A) }),
                ("12 - FINAL WAVE", new[] { new G(slime, 22, 0.6f, 0f, A), new G(goblin, 18, 0.6f, 2f, A), new G(archer, 12, 1.0f, 4f, A), new G(vulture, 10, 1.0f, 6f, A), new G(giant, 4, 5f, 8f, A) }),
            };

            new SO(wm)
                .Set("firstWaveDelay", 25f).Set("timeBetweenWaves", 20f)
                .Set("healthGrowthPerWave", 0.08f).Set("damageGrowthPerWave", 0.08f)
                .List("waves", waves.Count, (w, i) =>
                {
                    SO.Rel(w, "waveName", waves[i].Item1);
                    G[] groups = waves[i].Item2;
                    SerializedProperty gp = w.FindPropertyRelative("groups");
                    gp.arraySize = groups.Length;
                    for (int k = 0; k < groups.Length; k++)
                    {
                        SerializedProperty g = gp.GetArrayElementAtIndex(k);
                        SO.Rel(g, "enemyPrefab", groups[k].e);
                        SO.Rel(g, "count", groups[k].n);
                        SO.Rel(g, "spawnInterval", groups[k].every);
                        SO.Rel(g, "startDelay", groups[k].delay);
                        SO.Rel(g, "side", groups[k].side);
                    }
                }).Apply();
            return "เวฟ 12 เวฟ: 1-3 ทางซ้าย / 4-7 เปิดทางล่าง / 8-12 ทั้ง 2 ทาง\n" +
                   "เวลา: ก่อนเวฟแรก 25 วิ / พักระหว่างเวฟ 20 วิ / ศัตรูแข็งขึ้น +8% ต่อเวฟ\n";
        }

        // ------------------------------------------------------------------

        private static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

        private static Transform FindRoot(Scene s, string name)
        {
            foreach (GameObject go in s.GetRootGameObjects()) if (go.name == name) return go.transform;
            return null;
        }

        private static void DestroyChild(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            if (t != null) Object.DestroyImmediate(t.gameObject);
        }

        private static void AddToBuildSettings()
        {
            List<EditorBuildSettingsScene> list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (EditorBuildSettingsScene e in list) if (e.path == TargetScene) return;
            list.Add(new EditorBuildSettingsScene(TargetScene, true));
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
