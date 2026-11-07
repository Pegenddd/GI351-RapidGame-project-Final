using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// เมนู Tools > NecroDefense > Build Gameplay Scene
    /// สร้างซีน Assets/Scenes/Gameplay.unity (Hierarchy + UI + เชื่อม Reference ครบ)
    /// หลังจากนั้นทุกอย่างปรับใน Hierarchy / Inspector / Prefab ได้เลย ไม่ต้องแก้โค้ด
    /// </summary>
    public static class GameplaySceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/Gameplay.unity";

        [MenuItem("Tools/NecroDefense/Build Gameplay Scene")]
        public static void BuildMenu()
        {
            int choice = EditorUtility.DisplayDialogComplex("Build Gameplay Scene",
                "สร้างซีน Assets/Scenes/Gameplay.unity ใหม่\n\n" +
                "• Rebuild All = สร้าง Prefab ใหม่ทั้งหมดด้วย (ค่าที่เคยแก้ใน Prefab จะกลับเป็นค่าเริ่มต้น, ดึงรูปจาก Assets/picture ใหม่)\n" +
                "• Scene Only = สร้างแค่ซีน ใช้ Prefab เดิม",
                "Rebuild All", "Cancel", "Scene Only");
            if (choice == 1) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            try
            {
                GameplayPrefabBuilder.Overwrite = choice == 0;
                EditorUtility.DisplayProgressBar("NecroDefense", "Creating sprites...", 0.1f);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                BU.CreateSprites();

                EditorUtility.DisplayProgressBar("NecroDefense", "Creating prefabs...", 0.4f);
                GameplayPrefabBuilder p = new GameplayPrefabBuilder();
                p.BuildAll();

                EditorUtility.DisplayProgressBar("NecroDefense", "Building scene...", 0.7f);
                BuildScene(p);

                BU.EnsureFolder("Assets/Scenes");
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePath);
                AddToBuildSettings();
                AssetDatabase.SaveAssets();
                Debug.Log("<b>[NecroDefense]</b> Gameplay scene built: " + ScenePath);
            }
            finally
            {
                GameplayPrefabBuilder.Overwrite = false;
                EditorUtility.ClearProgressBar();
            }
        }

        private static void AddToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes
                .Where(s => s.path != ScenePath && System.IO.File.Exists(s.path)).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ==================================================================
        // Colors (Pale Wasteland: ขาว-เทา-ดำ)
        // ==================================================================
        private static readonly Color GroundColor = new Color(0.33f, 0.33f, 0.35f);
        private static readonly Color FreeZoneColor = new Color(0.88f, 0.88f, 0.87f);
        private static readonly Color ForestColor = new Color(0.07f, 0.07f, 0.08f);
        private static readonly Color RockColor = new Color(0.45f, 0.45f, 0.47f);

        // ==================================================================
        // Scene
        // ==================================================================

        /// <summary>ของที่ส่วนแมพสร้าง แล้วส่งต่อให้ BuildCore (ใช้ร่วมกับซีนเทส)</summary>
        internal class MapRefs
        {
            public CameraFollow follow;
            public MapBounds bounds;
            public Crystal crystal;
            public RangeIndicator range;
            public EnemyPath north = null, east, south, west;
            public SpriteRenderer ghost, moveMarker;
            public Vector3 playerStart;
        }

        /// <summary>กล้อง + แสง + EventSystem</summary>
        internal static CameraFollow BuildCameraAndSystems()
        {
            GameObject camGo = BU.Go("Main Camera", null, new Vector3(0f, 0f, -10f));
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 7f; // ซูม: ยิ่งน้อยยิ่งใกล้ (เลื่อนลูกกลิ้งเมาส์ตอนเล่นได้)
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.03f, 0.03f, 0.04f);
            camGo.AddComponent<AudioListener>();
            CameraFollow follow = camGo.AddComponent<CameraFollow>();

            CreateGlobalLight();

            GameObject es = BU.Go("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>(); // ใช้ Default UI Actions อัตโนมัติ
            return follow;
        }

        /// <summary>ป้อมหลัก (MainBase) ใช้ร่วมกับซีนเทส</summary>
        internal static Crystal BuildMainBase(GameplayPrefabBuilder p, Transform parent, Vector3 pos)
        {
            GameObject baseGo = BU.Go("MainBase", parent, pos);
            SpriteRenderer baseBody = BU.Sprite("Body", baseGo.transform, BU.Castle, new Color(0.78f, 0.78f, 0.8f), new Vector2(3.4f, 3.4f), 2);
            WorldHealthBar baseHb = BU.HealthBar(baseGo.transform, 2f, 2.6f, false);
            Crystal crystal = baseGo.AddComponent<Crystal>();
            new SO(crystal).Set("maxHealth", 500f).Set("bodyRadius", 1.4f).Set("bodyRenderer", baseBody).Set("healthBar", baseHb)
                .Set("hitFlashColor", new Color(1f, 0.45f, 0.45f))
                .Set("healInterval", 30f).Set("healPercent", 0.1f).Set("healEffect", p.FxHeal).Set("healEffectRadius", 6f)
                .Set("sacrificeDropRadius", 2.2f).Set("sacrificeEffect", p.FxSacrifice).Apply();
            return crystal;
        }

        /// <summary>แท่นป้อม 1 จุด</summary>
        internal static TowerSlot BuildTowerSlot(Transform parent, int number, Vector3 pos)
        {
            GameObject s = BU.Go("TowerSlot_" + number, parent, pos);
            SpriteRenderer sr = BU.Sprite("SlotBase", s.transform, BU.Circle, new Color(0.5f, 0.5f, 0.52f), new Vector2(1.8f, 1.1f), -6, new Vector3(0f, -0.45f, 0f));
            BU.Sprite("SlotRim", s.transform, BU.Ring, new Color(0.2f, 0.2f, 0.22f), new Vector2(1.8f, 1.1f), -5, new Vector3(0f, -0.45f, 0f));
            TowerSlot slot = s.AddComponent<TowerSlot>();
            new SO(slot).Set("slotNumber", number).Set("slotRenderer", sr)
                .Set("normalColor", new Color(0.5f, 0.5f, 0.52f)).Set("selectedColor", new Color(1f, 0.85f, 0.3f)).Apply();
            return slot;
        }

        /// <summary>วงระยะป้อม + เงาวางมินเนียน + ธง Move</summary>
        internal static void BuildMapHelpers(Transform map, MapRefs m)
        {
            GameObject rangeGo = BU.Go("RangeIndicator", map);
            SpriteRenderer rFill = BU.Sprite("Fill", rangeGo.transform, BU.Circle, new Color(0.45f, 1f, 0.45f, 0.14f), Vector2.one, -4);
            SpriteRenderer rLine = BU.Sprite("Outline", rangeGo.transform, BU.Ring, new Color(0.45f, 1f, 0.45f, 0.6f), Vector2.one, -3);
            m.range = rangeGo.AddComponent<RangeIndicator>();
            new SO(m.range).Set("fill", rFill).Set("outline", rLine).Apply();

            m.ghost = BU.Sprite("SummonPlacementGhost", map, BU.Circle, new Color(1f, 1f, 1f, 0.55f), Vector2.one, 40);
            m.ghost.gameObject.SetActive(false);
            m.moveMarker = BU.Sprite("MoveOrderMarker", map, BU.Flag, new Color(0.5f, 1f, 0.5f, 0.9f), new Vector2(0.9f, 0.9f), 40);
            m.moveMarker.gameObject.SetActive(false);
        }

        private static void BuildScene(GameplayPrefabBuilder p)
        {
            MapRefs m = new MapRefs();
            m.follow = BuildCameraAndSystems();

            // ---------- Map (ตามภาพ Level Design) ----------
            GameObject map = BU.Go("Map");
            MapBounds bounds = map.AddComponent<MapBounds>();
            new SO(bounds).Set("size", new Vector2(38f, 22f)).Apply();

            GameObject ground = BU.Go("Ground", map.transform);
            BU.Sprite("Ground_Wasteland", ground.transform, BU.Square, GroundColor, new Vector2(42f, 26f), -30);
            // Free Zone (ลานกลาง สีขาว) + ทางมอน 3 ทาง
            BU.Sprite("FreeZone_Center", ground.transform, BU.Square, FreeZoneColor, new Vector2(15f, 6.8f), -28, new Vector3(0f, 1.3f, 0f));
            BU.Sprite("EnemyLane_West", ground.transform, BU.Square, FreeZoneColor, new Vector2(12f, 2.4f), -28, new Vector3(-13.5f, 1.4f, 0f));
            BU.Sprite("EnemyLane_East", ground.transform, BU.Square, FreeZoneColor, new Vector2(12f, 2.4f), -28, new Vector3(13.5f, 1.4f, 0f));
            BU.Sprite("EnemyLane_South", ground.transform, BU.Square, FreeZoneColor, new Vector2(2.4f, 9f), -28, new Vector3(0f, -6.5f, 0f));
            GameObject freeZone = BU.Go("FreeZone (Area)", map.transform, new Vector3(0f, 1.3f, 0f));
            new SO(freeZone.AddComponent<FreeZone>()).Set("size", new Vector2(15f, 6.8f)).Apply();
            BU.Sprite("Path_ToMainBase", ground.transform, BU.Square, FreeZoneColor, new Vector2(3.5f, 2.5f), -28, new Vector3(0f, 5.3f, 0f));

            // Main Base (ป้อมหลัก) - ด้านบนกลาง
            Crystal crystal = BuildMainBase(p, map.transform, new Vector3(0f, 7.2f, 0f));

            // Tower slots (ป้อมรอง 6 จุด ตามภาพ)
            GameObject slotsRoot = BU.Go("TowerSlots", map.transform);
            Vector3[] slotPos =
            {
                new Vector3(-13.5f, 4.8f), new Vector3(13.5f, 4.8f),   // บนซ้าย / บนขวา
                new Vector3(-4.5f, 1.4f), new Vector3(4.5f, 1.4f),     // ในลานกลาง
                new Vector3(-13.5f, -2.8f), new Vector3(13.5f, -2.8f), // ล่างซ้าย / ล่างขวา
            };
            for (int i = 0; i < slotPos.Length; i++) BuildTowerSlot(slotsRoot.transform, i + 1, slotPos[i]);

            // Enemy paths (ทางมอน 3 ทาง: ซ้าย / ขวา / ล่าง)
            GameObject pathsRoot = BU.Go("EnemyPaths", map.transform);
            EnemyPath west = MakePath("Path_West", pathsRoot.transform, new Vector3(-19f, 1.4f), new Vector3(-16.8f, 1.4f), new Vector3(-7f, 1.4f), new Vector3(-1.2f, 4.4f));
            EnemyPath east = MakePath("Path_East", pathsRoot.transform, new Vector3(19f, 1.4f), new Vector3(16.8f, 1.4f), new Vector3(7f, 1.4f), new Vector3(1.2f, 4.4f));
            EnemyPath south = MakePath("Path_South", pathsRoot.transform, new Vector3(0f, -10.8f), new Vector3(0f, -9f), new Vector3(0f, -1.5f), new Vector3(0f, 4.4f));

            m.bounds = bounds; m.crystal = crystal; m.west = west; m.east = east; m.south = south;
            m.playerStart = new Vector3(0f, 2.2f, 0f);
            BuildMapHelpers(map.transform, m);

            BuildDecorations(map.transform);

            GameObject gmGo = BuildCore(p, m, true);
            Selection.activeGameObject = gmGo;
        }

        /// <summary>
        /// ผู้เล่น + Runtime folders + GameManager และ Manager ย่อย + Canvas UI แล้วเชื่อม Reference ทั้งหมด
        /// (ใช้ร่วมกันระหว่างซีน Gameplay และซีนเทส)
        /// </summary>
        internal static GameObject BuildCore(GameplayPrefabBuilder p, MapRefs m, bool fillWaves)
        {
            CameraFollow follow = m.follow;
            MapBounds bounds = m.bounds;
            Crystal crystal = m.crystal;
            RangeIndicator range = m.range;
            SpriteRenderer ghost = m.ghost, moveMarker = m.moveMarker;

            // ---------- Player ----------
            Sprite necroArt = BU.Art("Player_Necromancer");
            GameObject playerGo = BU.Go("Player_Necromancer", null, m.playerStart);
            BU.Body2D(playerGo, 0.4f, false, 2f);
            BU.Sprite("Shadow", playerGo.transform, BU.Circle, new Color(0f, 0f, 0f, 0.3f), new Vector2(0.9f, 0.28f), -2, new Vector3(0f, -0.6f, 0f));
            SpriteRenderer pBody = necroArt != null
                ? BU.Sprite("Body", playerGo.transform, necroArt, Color.white, Vector2.one * BU.FitScale(necroArt, 1.4f), 5)
                : BU.Sprite("Body", playerGo.transform, BU.Triangle, new Color(0.75f, 0.45f, 1f), new Vector2(0.8f, 0.9f), 5);
            WorldHealthBar pHb = BU.HealthBar(playerGo.transform, 0.9f, 0.9f, false);
            Transform orbHolder = BU.Go("OrbitOrbs", playerGo.transform).transform;

            PlayerNecromancer player = playerGo.AddComponent<PlayerNecromancer>();
            new SO(player).Set("maxHealth", 100f).Set("bodyRadius", 0.4f).Set("bodyRenderer", pBody).Set("healthBar", pHb)
                .Set("hitFlashColor", new Color(1f, 0.45f, 0.45f, 0.6f)).Set("moveSpeed", 4.5f).Apply();

            PlayerAutoAttack attack = playerGo.AddComponent<PlayerAutoAttack>();
            new SO(attack).Set("boltPrefab", p.PlayerBolt).Set("orbPrefab", p.OrbitOrb).Set("orbHolder", orbHolder).Apply();

            PlayerSkills skills = playerGo.AddComponent<PlayerSkills>();
            WorldHealthBar castBar = BU.HealthBar(playerGo.transform, -0.95f, 1f, true);
            castBar.name = "CastBar";
            castBar.transform.Find("Fill").GetComponent<SpriteRenderer>().color = new Color(0.8f, 0.55f, 1f);
            new SO(skills).Set("teleportEffect", p.FxTeleport).Set("raiseEffect", p.FxRaise)
                .Set("massRaiseEffect", p.FxMassRaise).Set("raiseFailEffect", p.FxRaiseFail)
                .Set("castBar", castBar).Set("castStartEffect", p.FxCast)
                .Set("novaEffect", p.FxShockwave).Apply();

            new SO(follow).Set("target", playerGo.transform).Set("clampToBounds", bounds).Apply();

            // ---------- Runtime folders ----------
            GameObject runtime = BU.Go("Runtime (ของที่เกิดตอนเล่น)");
            Transform fEnemies = BU.Go("Enemies", runtime.transform).transform;
            Transform fMinions = BU.Go("Minions", runtime.transform).transform;
            Transform fProj = BU.Go("Projectiles", runtime.transform).transform;
            Transform fFx = BU.Go("Effects", runtime.transform).transform;
            Transform fPickups = BU.Go("Pickups", runtime.transform).transform;

            // ---------- Managers ----------
            GameObject gmGo = BU.Go("GameManager");
            GameManager gm = gmGo.AddComponent<GameManager>();
            EconomyManager economy = BU.Go("EconomyManager", gmGo.transform).AddComponent<EconomyManager>();
            WaveManager waves = BU.Go("WaveManager", gmGo.transform).AddComponent<WaveManager>();
            MinionManager minions = BU.Go("MinionManager", gmGo.transform).AddComponent<MinionManager>();
            TowerManager towers = BU.Go("TowerManager", gmGo.transform).AddComponent<TowerManager>();
            LevelUpManager levelUp = BU.Go("LevelUpManager", gmGo.transform).AddComponent<LevelUpManager>();
            UIManager ui = BU.Go("UIManager", gmGo.transform).AddComponent<UIManager>();

            new SO(waves).Set("pathNorth", m.north).Set("pathEast", m.east).Set("pathSouth", m.south).Set("pathWest", m.west).Set("spawnScatter", 0.8f).Apply();
            if (fillWaves) FillWaves(waves, p);

            MinionUnit[] bagOrder = { p.MinionSlime, p.MinionGoblin, p.MinionSkeleton, p.MinionVulture, p.MinionGiant };
            Key[] bagKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
            int[] bagCaps = { 10, 6, 3, 3, 1 }; // จำนวนสูงสุดต่อชนิด: Slime / Goblin / Skeleton / Vulture / Giant
            new SO(minions).Set("summonEffect", p.FxSummon).Set("placementGhost", ghost).Set("moveMarker", moveMarker)
                .List("bag", bagOrder.Length, (e, i) =>
                {
                    SO.Rel(e, "minionPrefab", bagOrder[i]);
                    SO.Rel(e, "count", 0);
                    SO.Rel(e, "summonKey", bagKeys[i]);
                    SO.Rel(e, "maxOwned", bagCaps[i]);
                }).Apply();

            new SO(towers).ObjList("towerPrefabs", p.TowerMagicOrb, p.TowerExplosive, p.TowerSoulBeam).Set("rangeIndicator", range).Apply();
            new SO(levelUp).ObjList("cardPool", p.Cards.Cast<Object>().ToArray()).Apply();

            // ---------- UI ----------
            UIRefs uiRefs = BuildCanvas(bagOrder, necroArt);
            new SO(ui)
                .Set("playerStatus", uiRefs.playerStatus).Set("resources", uiRefs.resources).Set("wave", uiRefs.wave)
                .Set("skillBar", uiRefs.skillBar).Set("minionBag", uiRefs.minionBag).Set("commandPanel", uiRefs.commandPanel)
                .Set("message", uiRefs.message).Set("soulFly", uiRefs.soulFly).Set("towerPanel", uiRefs.towerPanel).Set("levelUpPanel", uiRefs.levelUpPanel)
                .Set("pausePanel", uiRefs.pausePanel).Set("gameOverPanel", uiRefs.gameOverPanel).Apply();

            new SO(gm)
                .Set("economy", economy).Set("waves", waves).Set("minions", minions).Set("towers", towers)
                .Set("levelUp", levelUp).Set("ui", ui)
                .Set("player", player).Set("crystal", crystal).Set("bounds", bounds)
                .Set("enemiesFolder", fEnemies).Set("minionsFolder", fMinions).Set("projectilesFolder", fProj)
                .Set("effectsFolder", fFx).Set("pickupsFolder", fPickups).Apply();

            gmGo.transform.SetSiblingIndex(0);
            return gmGo;
        }

        internal static EnemyPath MakePath(string name, Transform parent, Vector3 spawn, Vector3 warningPos, params Vector3[] waypoints)
        {
            GameObject go = BU.Go(name, parent, spawn);
            BU.Sprite("SpawnMarker", go.transform, BU.Ring, new Color(1f, 0.25f, 0.25f, 0.55f), new Vector2(1.8f, 1.8f), -15);

            // ป้ายเตือนเวฟ (กะโหลกแดงกระพริบที่ปากทาง)
            GameObject warn = BU.Go("WaveWarning", go.transform);
            warn.transform.position = warningPos;
            BU.Sprite("Glow", warn.transform, BU.Circle, new Color(1f, 0.15f, 0.15f, 0.35f), new Vector2(2.2f, 2.2f), 34);
            BU.Sprite("Badge", warn.transform, BU.Circle, new Color(0.35f, 0.02f, 0.02f, 0.95f), new Vector2(1.3f, 1.3f), 35);
            BU.Sprite("Skull", warn.transform, BU.Skull, new Color(1f, 0.9f, 0.85f), new Vector2(0.9f, 0.9f), 36);
            warn.SetActive(false);
            Transform[] wps = new Transform[waypoints.Length];
            for (int i = 0; i < waypoints.Length; i++)
            {
                GameObject wp = BU.Go("WP_" + (i + 1), go.transform);
                wp.transform.position = waypoints[i];
                wps[i] = wp.transform;
            }
            EnemyPath path = go.AddComponent<EnemyPath>();
            new SO(path).ObjList("waypoints", wps.Cast<Object>().ToArray())
                .Set("warningMarker", warn).Set("warningPoint", warn.transform).Apply();
            return path;
        }

        /// <summary>ป่าดำรอบขอบแมพ + หินตกแต่ง (เป็น Sprite ล้วน ลบ/ย้าย/เปลี่ยนรูปได้อิสระ)</summary>
        private static void BuildDecorations(Transform map)
        {
            GameObject deco = BU.Go("Decorations", map);
            Transform forestTop = BU.Go("Forest_Top", deco.transform).transform;
            BU.Sprite("Band", forestTop, BU.Square, ForestColor, new Vector2(42f, 3f), -27, new Vector3(0f, 10.5f, 0f));
            for (float x = -20f; x <= 20f; x += 1.7f)
            {
                if (Mathf.Abs(x) < 3f) continue; // เว้นหลังป้อมหลัก
                float h = 2.6f + Mathf.PerlinNoise(x * 0.37f, 0.5f) * 1.6f;
                BU.Sprite("Tree", forestTop, BU.Tree, ForestColor, new Vector2(1.9f, h), -26, new Vector3(x, 8.4f + Mathf.PerlinNoise(x, 1f) * 0.6f, 0f));
            }

            Transform forestBottom = BU.Go("Forest_Bottom", deco.transform).transform;
            BU.Sprite("Band", forestBottom, BU.Square, ForestColor, new Vector2(42f, 2.5f), -24, new Vector3(0f, -10.8f, 0f));
            for (float x = -20f; x <= 20f; x += 1.3f)
            {
                if (Mathf.Abs(x) < 1.6f) continue; // ทางมอนด้านล่าง
                BU.Sprite("Bush", forestBottom, BU.Circle, ForestColor, new Vector2(1.8f, 1.5f), -24, new Vector3(x, -9.3f + Mathf.PerlinNoise(x, 2f) * 0.5f, 0f));
            }

            Transform sideTrees = BU.Go("Forest_Sides", deco.transform).transform;
            foreach (Vector3 v in new[] { new Vector3(-18.3f, 4f), new Vector3(18.3f, 4f), new Vector3(-18.3f, -5.5f), new Vector3(18.3f, -5.5f), new Vector3(-17.5f, -8f), new Vector3(17.5f, -8f) })
                BU.Sprite("Tree", sideTrees, BU.Tree, ForestColor, new Vector2(2.2f, 3.6f), -25, v);

            Transform rocks = BU.Go("Rocks", deco.transform).transform;
            Vector3[] rockPos =
            {
                new Vector3(-9f, 6f), new Vector3(-6.5f, 6.8f), new Vector3(8.5f, 6.2f), new Vector3(6f, 7f),
                new Vector3(-9.5f, -3.5f), new Vector3(-6f, -6f), new Vector3(9f, -4f), new Vector3(5.5f, -7f),
                new Vector3(-16f, -6f), new Vector3(16f, -7f), new Vector3(-10f, 3.6f), new Vector3(10f, 3.6f),
            };
            for (int i = 0; i < rockPos.Length; i++)
                BU.Sprite("Rock_" + (i + 1), rocks, BU.Circle, RockColor, new Vector2(1.3f + (i % 3) * 0.3f, 0.8f), -27, rockPos[i]);
        }

        private static void CreateGlobalLight()
        {
            Type lightType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("UnityEngine.Rendering.Universal.Light2D"))
                .FirstOrDefault(t => t != null);
            if (lightType == null) return;
            GameObject go = BU.Go("Global Light 2D");
            Component light = go.AddComponent(lightType);
            SerializedObject so = new SerializedObject(light);
            SerializedProperty type = so.FindProperty("m_LightType");
            if (type != null) type.intValue = 4; // Global
            SerializedProperty intensity = so.FindProperty("m_Intensity");
            if (intensity != null) intensity.floatValue = 1f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ==================================================================
        // Waves (GDD: Phase 1 = 1-3, Phase 2 = 4-8, Phase 3 = 9-12)
        // ==================================================================

        private struct G
        {
            public EnemyUnit e; public int n; public float every, delay; public SpawnSide side;
            public G(EnemyUnit e, int n, float every, float delay = 0f, SpawnSide side = SpawnSide.Random)
            { this.e = e; this.n = n; this.every = every; this.delay = delay; this.side = side; }
        }

        private static void FillWaves(WaveManager wm, GameplayPrefabBuilder p)
        {
            EnemyUnit slime = p.EnemySlime, goblin = p.EnemyGoblin, vulture = p.EnemyVulture, giant = p.EnemyGiant, archer = p.EnemyArcher;
            SpawnSide all = SpawnSide.AllSides, W = SpawnSide.West, E = SpawnSide.East, S = SpawnSide.South;

            var waves = new List<(string, G[])>
            {
                // Phase 1: Early Game - Slime, Skeleton Archer (ทีละทาง)
                ("Phase 1 - First Blood", new[] { new G(slime, 6, 1.5f, 0f, W) }),
                ("Phase 1 - Bones", new[] { new G(slime, 8, 1.3f, 0f, E), new G(archer, 3, 2f, 4f, E) }),
                ("Phase 1 - Ooze Tide", new[] { new G(slime, 10, 1.1f, 0f, S), new G(archer, 5, 1.8f, 3f, W) }),
                // Phase 2: Mid Game - + Goblin, Vulture, Giant (หลายทาง)
                ("Phase 2 - Goblin Rush", new[] { new G(slime, 8, 1.2f, 0f, W), new G(goblin, 6, 1f, 2f, E), new G(archer, 4, 2f, 5f, S) }),
                ("Phase 2 - Wings", new[] { new G(goblin, 10, 0.8f, 0f, all), new G(vulture, 4, 2f, 3f), new G(archer, 4, 2f, 6f) }),
                ("Phase 2 - The Giant", new[] { new G(slime, 10, 1f, 0f, all), new G(vulture, 6, 1.6f, 4f), new G(giant, 1, 1f, 8f, S) }),
                ("Phase 2 - Siege", new[] { new G(goblin, 12, 0.7f, 0f, all), new G(archer, 6, 1.5f, 3f), new G(giant, 1, 1f, 10f, W) }),
                ("Phase 2 - Pressure", new[] { new G(slime, 12, 0.9f, 0f, all), new G(goblin, 10, 0.8f, 3f, all), new G(vulture, 6, 1.4f, 6f), new G(giant, 2, 6f, 10f, all) }),
                // Phase 3: Late Game & Climax - ทุกประเภท ทุกทาง
                ("Phase 3 - Onslaught", new[] { new G(slime, 16, 0.7f, 0f, all), new G(goblin, 12, 0.7f, 2f, all), new G(archer, 8, 1.2f, 4f, all), new G(vulture, 6, 1.4f, 6f, all), new G(giant, 2, 6f, 8f, all) }),
                ("Phase 3 - Dark Tide", new[] { new G(slime, 18, 0.6f, 0f, all), new G(goblin, 14, 0.6f, 2f, all), new G(archer, 10, 1f, 4f, all), new G(vulture, 8, 1.2f, 6f, all), new G(giant, 3, 5f, 8f, all) }),
                ("Phase 3 - Endless Night", new[] { new G(slime, 20, 0.55f, 0f, all), new G(goblin, 16, 0.55f, 2f, all), new G(archer, 12, 0.9f, 4f, all), new G(vulture, 10, 1f, 6f, all), new G(giant, 4, 5f, 8f, all) }),
                ("Phase 3 - FINAL WAVE", new[] { new G(slime, 24, 0.5f, 0f, all), new G(goblin, 20, 0.5f, 2f, all), new G(archer, 14, 0.8f, 4f, all), new G(vulture, 12, 0.9f, 6f, all), new G(giant, 6, 4f, 6f, all) }),
            };

            new SO(wm).List("waves", waves.Count, (w, i) =>
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
        }

        // ==================================================================
        // Canvas / UI  (เน้นไอคอน ข้อความน้อย - รายละเอียดดูจาก Tooltip ตอนชี้เมาส์)
        // ==================================================================

        private class UIRefs
        {
            public PlayerStatusUI playerStatus;
            public ResourceUI resources;
            public WaveUI wave;
            public SkillBarUI skillBar;
            public MinionBagUI minionBag;
            public CommandPanelUI commandPanel;
            public MessageUI message;
            public SoulFlyUI soulFly;
            public TowerPanelUI towerPanel;
            public LevelUpPanelUI levelUpPanel;
            public PausePanelUI pausePanel;
            public GameOverPanelUI gameOverPanel;
        }

        private static readonly Vector2 TL = new Vector2(0f, 1f), TC = new Vector2(0.5f, 1f), TR = new Vector2(1f, 1f);
        private static readonly Vector2 BL = new Vector2(0f, 0f), BC = new Vector2(0.5f, 0f), BR = new Vector2(1f, 0f);
        private static readonly Vector2 MC = new Vector2(0.5f, 0.5f), ML = new Vector2(0f, 0.5f);

        private static readonly Color Gold = new Color(1f, 0.85f, 0.35f);
        private static readonly Color ManaBlue = new Color(0.35f, 0.6f, 1f);
        private static readonly Color SoulPurple = new Color(0.8f, 0.6f, 1f);

        private static UIRefs BuildCanvas(MinionUnit[] bagOrder, Sprite necroArt)
        {
            UIRefs r = new UIRefs();

            GameObject canvasGo = new GameObject("Canvas_Gameplay", typeof(RectTransform));
            canvasGo.layer = 5;
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform root = canvasGo.transform;

            // ===== World menus (อยู่ล่างสุดของ UI เพื่อไม่ทับ HUD) =====
            r.towerPanel = BuildTowerRingMenu(root, out TowerInfoBarUI infoBar);
            r.commandPanel = BuildCommandRingMenu(root, necroArt);

            // ================= HUD =================
            RectTransform hud = BU.Stretch(BU.UI("HUD", root));

            // --- Top Left: Player status (ไอคอน + หลอด) ---
            RectTransform ps = BU.Panel("TopLeft_PlayerStatus", hud, TL, TL, new Vector2(20f, -20f), new Vector2(400f, 132f));
            BU.Icon("HP_Icon", ps, BU.Heart, new Color(0.95f, 0.3f, 0.35f), TL, TL, new Vector2(12f, -12f), new Vector2(32f, 32f));
            BarUI hp = BU.Bar("HP_Bar", ps, "HP", new Color(0.85f, 0.2f, 0.25f), TL, TL, new Vector2(52f, -12f), new Vector2(334f, 32f));
            BU.Icon("Mana_Icon", ps, BU.Diamond, ManaBlue, TL, TL, new Vector2(12f, -50f), new Vector2(32f, 32f));
            BarUI mana = BU.Bar("Mana_Bar", ps, "Mana", new Color(0.25f, 0.45f, 1f), TL, TL, new Vector2(52f, -50f), new Vector2(334f, 32f));
            BU.Icon("XP_Icon", ps, BU.Star, Gold, TL, TL, new Vector2(12f, -90f), new Vector2(32f, 32f));
            BarUI xp = BU.Bar("XP_Bar", ps, "XP", new Color(0.95f, 0.8f, 0.25f), TL, TL, new Vector2(52f, -90f), new Vector2(250f, 30f));
            TextMeshProUGUI lvText = BU.Text("Level_Text", ps, "Lv 1", 24f, TL, TL, new Vector2(306f, -88f), new Vector2(84f, 34f));
            lvText.fontStyle = FontStyles.Bold;
            r.playerStatus = ps.gameObject.AddComponent<PlayerStatusUI>();
            new SO(r.playerStatus).Set("healthBar", hp).Set("manaBar", mana).Set("xpBar", xp).Set("levelText", lvText).Apply();

            // Help (?) - ชี้เพื่อดูปุ่มควบคุม
            RectTransform help = BU.Panel("Help_Button", hud, TL, TL, new Vector2(20f, -160f), new Vector2(44f, 44f), new Color(0.1f, 0.1f, 0.14f, 0.9f));
            BU.StretchText("Label", help, "?", 28f, TextAlignmentOptions.Center, Gold);
            help.gameObject.AddComponent<TooltipTrigger>().Set("Controls",
                "WASD: Move   Mouse wheel: Zoom\nLeft-click corpse: Raise Dead\nLeft-click slot / tower: Build menu\nLeft-click minion: Command menu (Move = click a spot)\n" +
                "Q: Mass Raise (all corpses around you)\nRight-click: Teleport   E: Arcane Nova\nTab: Minion Bag   1-5: pick minion, then click to place\nEsc: Pause / cancel");

            // --- Top Center: Wave ---
            RectTransform wv = BU.Panel("TopCenter_Wave", hud, TC, TC, new Vector2(0f, -20f), new Vector2(380f, 120f));
            BU.Icon("Wave_Icon", wv, BU.Skull, new Color(1f, 0.45f, 0.45f), TL, TL, new Vector2(14f, -10f), new Vector2(40f, 40f));
            TextMeshProUGUI waveText = BU.Text("Wave_Text", wv, "Wave 0 / 12", 32f, TC, TC, new Vector2(20f, -8f), new Vector2(300f, 42f));
            waveText.fontStyle = FontStyles.Bold;
            TextMeshProUGUI waveStatus = BU.Text("Status_Text", wv, "", 20f, TC, TC, new Vector2(0f, -50f), new Vector2(360f, 28f), TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.8f));
            Button startWave = BU.Button("StartWave_Button", wv, "Call Wave", TC, TC, new Vector2(0f, -80f), new Vector2(200f, 32f), out _, 18f,
                new Color(0.5f, 0.18f, 0.18f, 1f));
            startWave.gameObject.AddComponent<TooltipTrigger>().Set("Call next wave", "Start the next wave now instead of waiting.");
            r.wave = wv.gameObject.AddComponent<WaveUI>();
            new SO(r.wave).Set("waveText", waveText).Set("statusText", waveStatus).Set("startWaveButton", startWave).Apply();

            // --- Top Right: Souls + Main Base ---
            RectTransform rs = BU.Panel("TopRight_Resources", hud, TR, TR, new Vector2(-20f, -20f), new Vector2(330f, 120f));
            BU.Icon("Souls_Icon", rs, BU.Diamond, SoulPurple, TL, TL, new Vector2(12f, -10f), new Vector2(40f, 40f));
            TextMeshProUGUI souls = BU.Text("Souls_Text", rs, "100", 34f, TL, TL, new Vector2(60f, -8f), new Vector2(160f, 44f), TextAlignmentOptions.Left, SoulPurple);
            souls.fontStyle = FontStyles.Bold;
            rs.Find("Souls_Icon").gameObject.AddComponent<TooltipTrigger>().Set("Souls", "Build and upgrade towers. Gained by killing enemies.");
            BU.Icon("Heal_Icon", rs, BU.Clock, new Color(0.6f, 1f, 0.7f), TR, TR, new Vector2(-70f, -16f), new Vector2(26f, 26f));
            TextMeshProUGUI healTxt = BU.Text("BaseHeal_Text", rs, "30s", 18f, TR, TR, new Vector2(-12f, -14f), new Vector2(56f, 30f), TextAlignmentOptions.Left, new Color(0.6f, 1f, 0.7f));
            rs.Find("Heal_Icon").gameObject.AddComponent<TooltipTrigger>().Set("Main Base heal", "Every 30s the Main Base heals you and all minions by 10%.");
            BU.Icon("Base_Icon", rs, BU.Castle, new Color(0.85f, 0.85f, 0.9f), TL, TL, new Vector2(12f, -66f), new Vector2(40f, 40f));
            BarUI baseBar = BU.Bar("Base_HP_Bar", rs, "Base", new Color(0.3f, 0.85f, 1f), TL, TL, new Vector2(60f, -70f), new Vector2(256f, 32f));
            r.resources = rs.gameObject.AddComponent<ResourceUI>();
            new SO(r.resources).Set("soulsText", souls).Set("crystalBar", baseBar).Set("crystalHealText", healTxt).Apply();

            // --- Bottom Left: Minion Bag (Tab / คลิกปุ่มเพื่อเปิด-ปิด) ---
            RectTransform bagRoot = BU.Place(BU.UI("BottomLeft_MinionBag", hud), BL, BL, new Vector2(20f, 20f), new Vector2(720f, 130f));
            RectTransform bagBtnRt = BU.Panel("Bag_Button", bagRoot, BL, BL, Vector2.zero, new Vector2(110f, 110f), new Color(0.12f, 0.1f, 0.16f, 0.95f));
            Button bagBtn = BU.MakeButton(bagBtnRt.gameObject, bagBtnRt.GetComponent<Image>());
            BU.Icon("Bag_Icon", bagBtnRt, BU.Bag, new Color(0.75f, 0.6f, 0.4f), MC, MC, new Vector2(0f, 4f), new Vector2(72f, 72f));
            BU.Badge("Key_Badge", bagBtnRt, "Tab", TL, TL, new Vector2(4f, -4f), new Vector2(44f, 22f), null, Gold, 14f);
            TextMeshProUGUI totalTxt = BU.Badge("Total_Badge", bagBtnRt, "0", BR, BR, new Vector2(-4f, 4f), new Vector2(40f, 26f), new Color(0.35f, 0.15f, 0.5f, 0.95f), Color.white, 18f);
            bagBtnRt.gameObject.AddComponent<TooltipTrigger>().Set("Minion Bag (Tab)", "Raised minions are stored here.");

            RectTransform bagPanel = BU.Panel("Bag_Panel", bagRoot, BL, BL, new Vector2(124f, 0f), new Vector2(580f, 120f), new Color(0.05f, 0.05f, 0.08f, 0.88f));
            List<MinionBagSlotUI> bagSlots = new List<MinionBagSlotUI>();
            for (int i = 0; i < bagOrder.Length; i++)
            {
                string mName = bagOrder[i] != null ? bagOrder[i].DisplayName : "Empty";
                bagSlots.Add(BagSlot("Slot_" + (i + 1) + "_" + mName, bagPanel, i, new Vector2(10f + i * 104f, 0f)));
            }
            TextMeshProUGUI activeTxt = BU.Badge("OnField_Badge", bagPanel, "0/30", TR, TR, new Vector2(-6f, -6f), new Vector2(46f, 100f), new Color(0f, 0f, 0f, 0f), new Color(1f, 1f, 1f, 0.7f), 15f);
            activeTxt.transform.parent.gameObject.AddComponent<TooltipTrigger>().Set("On field", "Minions alive / max.");
            activeTxt.transform.parent.GetComponent<Image>().raycastTarget = true;
            r.minionBag = bagRoot.gameObject.AddComponent<MinionBagUI>();

            // --- Bottom Right: Skills (ย้ายมาตรงนี้) ---
            RectTransform sb = BU.Panel("BottomRight_Skills", hud, BR, BR, new Vector2(-20f, 20f), new Vector2(340f, 120f));
            SkillSlotUI raise = SkillSlot("Skill_Q_MassRaise", sb, 0, BU.Skull, new Color(0.6f, 1f, 0.6f));
            SkillSlotUI tele = SkillSlot("Skill_Teleport", sb, 1, BU.Ring, new Color(0.8f, 0.55f, 1f));
            SkillSlotUI nova = SkillSlot("Skill_E_ArcaneNova", sb, 2, BU.Star, new Color(1f, 0.65f, 0.3f));
            r.skillBar = sb.gameObject.AddComponent<SkillBarUI>();
            new SO(r.skillBar).Set("raiseDeadSlot", raise).Set("teleportSlot", tele).Set("novaSlot", nova).Apply();

            // --- Tower info bar (ล่างกลาง โผล่ตอนเลือกป้อม) ---
            infoBar.transform.SetParent(hud, false);

            // ================= Popups =================
            r.levelUpPanel = BuildLevelUpPanel(root);
            r.pausePanel = BuildPausePanel(root);
            r.gameOverPanel = BuildGameOverPanel(root);

            // ================= Wave warnings (ไอคอนเตือนทางที่ศัตรูจะมา) =================
            BuildWaveWarnings(root);

            // ================= Soul fly (วิญญาณบินเข้ากระเป๋าตอนชุบสำเร็จ) =================
            RectTransform soulRoot = BU.Stretch(BU.UI("SoulFly", root));
            Image soulTpl = BU.Icon("Soul_Template", soulRoot, BU.Circle, new Color(0.75f, 1f, 0.85f, 0.9f), BL, MC, Vector2.zero, new Vector2(70f, 70f));
            r.soulFly = soulRoot.gameObject.AddComponent<SoulFlyUI>();
            new SO(r.soulFly).Set("soulTemplate", soulTpl.rectTransform).Set("bag", r.minionBag).Apply();

            // ================= Message / Drag ghost / Tooltip (บนสุด) =================
            TextMeshProUGUI msg = BU.Text("Message_Text", root, "", 28f, MC, MC, new Vector2(0f, 300f), new Vector2(1100f, 50f),
                TextAlignmentOptions.Center, new Color(1f, 0.95f, 0.7f));
            msg.fontStyle = FontStyles.Bold;
            r.message = msg.gameObject.AddComponent<MessageUI>();
            new SO(r.message).Set("text", msg).Apply();

            Image ghostImg = BU.Icon("DragGhost", root, BU.Circle, new Color(1f, 1f, 1f, 0.85f), MC, MC, Vector2.zero, new Vector2(90f, 90f));
            ghostImg.gameObject.SetActive(false);

            new SO(r.minionBag).Set("toggleButton", bagBtn).Set("panel", bagPanel.gameObject).Set("totalCountText", totalTxt)
                .ObjList("slots", bagSlots.Cast<Object>().ToArray()).Set("activeCountText", activeTxt)
                .Set("dragGhost", ghostImg).Apply();

            BuildTooltip(root);
            return r;
        }

        // ---------- Pieces ----------

        private static SkillSlotUI SkillSlot(string name, Transform parent, int index, Sprite iconSprite, Color iconColor)
        {
            RectTransform rt = BU.Place(BU.UI(name, parent), ML, ML, new Vector2(12f + index * 108f, 0f), new Vector2(100f, 100f));
            Image bg = BU.Img(rt, new Color(0.15f, 0.15f, 0.2f, 0.95f));
            Image icon = BU.Icon("Icon", rt, iconSprite, iconColor, MC, MC, Vector2.zero, new Vector2(64f, 64f));
            RectTransform cdRt = BU.Stretch(BU.UI("Cooldown_Overlay", rt));
            Image cd = BU.Img(cdRt, new Color(0f, 0f, 0f, 0.7f), false, BU.Square);
            cd.type = Image.Type.Filled;
            cd.fillMethod = Image.FillMethod.Radial360;
            cd.fillOrigin = (int)Image.Origin360.Top;
            cd.fillClockwise = false;
            cd.fillAmount = 0f;
            TextMeshProUGUI cdText = BU.StretchText("Cooldown_Text", rt, "", 34f);
            cdText.fontStyle = FontStyles.Bold;
            TextMeshProUGUI key = BU.Badge("Key_Badge", rt, "?", TL, TL, new Vector2(3f, -3f), new Vector2(42f, 22f), null, Gold, 14f);
            TextMeshProUGUI cost = BU.Badge("Mana_Badge", rt, "", BR, BR, new Vector2(-3f, 3f), new Vector2(54f, 24f), new Color(0.05f, 0.1f, 0.3f, 0.95f), Color.white, 15f, BU.Diamond, ManaBlue);
            Image lockImg = BU.Icon("Locked_Overlay", rt, BU.Lock, new Color(1f, 0.85f, 0.4f), MC, MC, Vector2.zero, new Vector2(46f, 46f));
            lockImg.gameObject.SetActive(false);
            TooltipTrigger tip = rt.gameObject.AddComponent<TooltipTrigger>();
            SkillSlotUI s = rt.gameObject.AddComponent<SkillSlotUI>();
            new SO(s).Set("lockedOverlay", lockImg.gameObject).Set("icon", icon).Set("keyText", key).Set("costText", cost).Set("cooldownOverlay", cd)
                .Set("cooldownText", cdText).Set("background", bg).Set("tooltip", tip).Apply();
            return s;
        }

        private static MinionBagSlotUI BagSlot(string name, Transform parent, int index, Vector2 pos)
        {
            RectTransform rt = BU.Place(BU.UI(name, parent), ML, ML, pos, new Vector2(96f, 104f));
            Image bg = BU.Img(rt, new Color(0.16f, 0.16f, 0.21f, 0.98f));
            BU.MakeButton(rt.gameObject, bg);
            CanvasGroup cg = rt.gameObject.AddComponent<CanvasGroup>();

            Image icon = BU.Icon("Icon", rt, BU.Circle, Color.white, MC, MC, new Vector2(0f, 4f), new Vector2(76f, 76f));
            TextMeshProUGUI key = BU.Badge("Key_Badge", rt, "", TL, TL, new Vector2(3f, -3f), new Vector2(24f, 22f), null, Gold, 15f);
            TextMeshProUGUI count = BU.Badge("Count_Badge", rt, "x0", TR, TR, new Vector2(-3f, -3f), new Vector2(40f, 24f), new Color(0.35f, 0.15f, 0.5f, 0.95f), Color.white, 16f);
            TextMeshProUGUI cost = BU.Badge("Mana_Badge", rt, "", BC, BC, new Vector2(0f, 3f), new Vector2(56f, 22f), new Color(0.05f, 0.1f, 0.3f, 0.95f), Color.white, 14f, BU.Diamond, ManaBlue);
            TooltipTrigger tip = rt.gameObject.AddComponent<TooltipTrigger>();

            MinionBagSlotUI s = rt.gameObject.AddComponent<MinionBagSlotUI>();
            new SO(s).Set("bagIndex", index).Set("icon", icon).Set("countText", count)
                .Set("costText", cost).Set("keyText", key).Set("canvasGroup", cg).Set("tooltip", tip).Set("background", bg).Apply();
            return s;
        }

        /// <summary>ปุ่มวงกลม 1 ปุ่มของเมนูวงกลม</summary>
        private static RingOptionUI RingOption(string name, Transform parent, Vector2 pos, Sprite iconSprite, Color iconColor, float size = 84f, bool withCost = true)
        {
            RectTransform rt = BU.Place(BU.UI(name, parent), MC, MC, pos, new Vector2(size, size));
            Image bg = BU.Img(rt, new Color(0.15f, 0.15f, 0.18f, 1f), true, BU.Circle);
            BU.Icon("Rim", rt, BU.Ring, new Color(0.85f, 0.75f, 0.5f), MC, MC, Vector2.zero, new Vector2(size, size));
            Button b = BU.MakeButton(rt.gameObject, bg);
            Image icon = BU.Icon("Icon", rt, iconSprite, iconColor, MC, MC, new Vector2(0f, 4f), new Vector2(size * 0.62f, size * 0.62f));

            GameObject badge = null;
            TextMeshProUGUI costTxt = null;
            if (withCost)
            {
                costTxt = BU.Badge("Cost_Badge", rt, "0", BC, MC, new Vector2(0f, -2f), new Vector2(70f, 24f), new Color(0.1f, 0.06f, 0.15f, 0.95f), Gold, 15f, BU.Diamond, SoulPurple);
                badge = costTxt.transform.parent.gameObject;
            }

            Image check = BU.Icon("Confirm_Check", rt, BU.Check, new Color(0.4f, 1f, 0.4f), TR, MC, new Vector2(-8f, -8f), new Vector2(38f, 38f));
            check.gameObject.SetActive(false);

            RingOptionUI o = rt.gameObject.AddComponent<RingOptionUI>();
            new SO(o).Set("button", b).Set("background", bg).Set("icon", icon).Set("costBadge", badge).Set("costText", costTxt)
                .Set("confirmMark", check.gameObject).Apply();
            return o;
        }

        private static TowerPanelUI BuildTowerRingMenu(Transform root, out TowerInfoBarUI infoBar)
        {
            RectTransform menu = BU.Place(BU.UI("TowerPanel (Ring Menu)", root), MC, MC, Vector2.zero, new Vector2(10f, 10f));
            BU.Icon("Ring", menu, BU.Ring, new Color(0.9f, 0.8f, 0.55f, 0.85f), MC, MC, Vector2.zero, new Vector2(230f, 230f));

            RectTransform build = BU.Stretch(BU.UI("BuildGroup", menu));
            RingOptionUI b1 = RingOption("Build_MagicOrb", build, new Vector2(-85f, 80f), BU.TowerOrb, Color.white);
            RingOptionUI b2 = RingOption("Build_Explosive", build, new Vector2(85f, 80f), BU.TowerBomb, Color.white);
            RingOptionUI b3 = RingOption("Build_SoulBeam", build, new Vector2(0f, -115f), BU.TowerBeam, Color.white);

            RectTransform manage = BU.Stretch(BU.UI("ManageGroup", menu));
            RingOptionUI up = RingOption("Upgrade", manage, new Vector2(0f, 118f), BU.ArrowUp, Gold);
            RingOptionUI sell = RingOption("Sell", manage, new Vector2(0f, -118f), BU.Coin, Gold, 70f);

            RectTransform info = BU.Panel("InfoBox", menu, MC, ML, new Vector2(150f, 0f), new Vector2(330f, 140f), new Color(0.06f, 0.05f, 0.09f, 0.95f));
            TextMeshProUGUI infoTitle = BU.Text("Title_Text", info, "", 22f, TL, TL, new Vector2(12f, -8f), new Vector2(306f, 30f), TextAlignmentOptions.Left, Gold);
            infoTitle.fontStyle = FontStyles.Bold;
            TextMeshProUGUI infoBody = BU.Text("Body_Text", info, "", 16f, TL, TL, new Vector2(12f, -40f), new Vector2(306f, 92f), TextAlignmentOptions.TopLeft);
            info.gameObject.SetActive(false);

            // แถบข้อมูลด้านล่างจอ
            RectTransform bar = BU.Panel("TowerInfoBar", root, BC, BC, new Vector2(0f, 20f), new Vector2(600f, 64f), new Color(0.05f, 0.05f, 0.08f, 0.92f));
            Image barIcon = BU.Icon("Icon", bar, BU.TowerOrb, Color.white, ML, ML, new Vector2(10f, 0f), new Vector2(50f, 50f));
            TextMeshProUGUI barName = BU.Text("Name_Text", bar, "Tower", 22f, ML, ML, new Vector2(66f, 0f), new Vector2(200f, 40f), TextAlignmentOptions.Left);
            barName.fontStyle = FontStyles.Bold;
            BU.Icon("Damage_Icon", bar, BU.Sword, new Color(1f, 0.5f, 0.45f), ML, ML, new Vector2(275f, 0f), new Vector2(30f, 30f));
            TextMeshProUGUI dmg = BU.Text("Damage_Text", bar, "0", 20f, ML, ML, new Vector2(308f, 0f), new Vector2(80f, 34f), TextAlignmentOptions.Left);
            BU.Icon("Speed_Icon", bar, BU.Clock, new Color(1f, 0.9f, 0.5f), ML, ML, new Vector2(390f, 0f), new Vector2(30f, 30f));
            TextMeshProUGUI spd = BU.Text("Speed_Text", bar, "0", 20f, ML, ML, new Vector2(423f, 0f), new Vector2(70f, 34f), TextAlignmentOptions.Left);
            BU.Icon("Range_Icon", bar, BU.Ring, new Color(0.5f, 1f, 0.5f), ML, ML, new Vector2(495f, 0f), new Vector2(30f, 30f));
            TextMeshProUGUI rng = BU.Text("Range_Text", bar, "0", 20f, ML, ML, new Vector2(528f, 0f), new Vector2(60f, 34f), TextAlignmentOptions.Left);
            infoBar = bar.gameObject.AddComponent<TowerInfoBarUI>();
            new SO(infoBar).Set("icon", barIcon).Set("nameText", barName).Set("damageText", dmg).Set("speedText", spd).Set("rangeText", rng).Apply();
            bar.gameObject.SetActive(false);

            TowerPanelUI ui = menu.gameObject.AddComponent<TowerPanelUI>();
            new SO(ui).Set("buildGroup", build.gameObject).ObjList("buildOptions", b1, b2, b3)
                .Set("manageGroup", manage.gameObject).Set("upgradeOption", up).Set("sellOption", sell).Set("maxLevelIcon", BU.Star)
                .Set("infoBox", info.gameObject).Set("infoTitle", infoTitle).Set("infoBody", infoBody)
                .Set("infoBar", infoBar).Apply();
            menu.gameObject.SetActive(false);
            return ui;
        }

        private static CommandPanelUI BuildCommandRingMenu(Transform root, Sprite necroArt)
        {
            RectTransform menu = BU.Place(BU.UI("MinionCommandPanel (Ring Menu)", root), MC, MC, Vector2.zero, new Vector2(10f, 10f));
            RingOptionUI hold = RingOption("Cmd_HoldArea", menu, new Vector2(-95f, 45f), BU.Shield, new Color(0.6f, 0.8f, 1f), 68f, false);
            RingOptionUI moveO = RingOption("Cmd_Move", menu, new Vector2(0f, 100f), BU.Flag, new Color(0.5f, 1f, 0.5f), 68f, false);
            RingOptionUI followO = RingOption("Cmd_FollowPlayer", menu, new Vector2(95f, 45f), necroArt != null ? necroArt : BU.Triangle,
                necroArt != null ? Color.white : new Color(0.75f, 0.45f, 1f), 68f, false);
            hold.gameObject.AddComponent<TooltipTrigger>().Set("Hold Area (default)", "Guard this area: attack enemies that enter its sight range, then come back.");
            moveO.gameObject.AddComponent<TooltipTrigger>().Set("Move", "Click a spot on the field. The minion walks there (ignoring enemies), then guards that area.");
            followO.gameObject.AddComponent<TooltipTrigger>().Set("Follow Player", "Follow the Necromancer and protect him.");

            CommandPanelUI ui = menu.gameObject.AddComponent<CommandPanelUI>();
            new SO(ui).Set("holdAreaOption", hold).Set("moveOption", moveO).Set("followPlayerOption", followO).Apply();
            menu.gameObject.SetActive(false);
            return ui;
        }

        private static void BuildWaveWarnings(Transform root)
        {
            RectTransform holder = BU.Stretch(BU.UI("WaveWarnings", root));
            List<WaveWarningIndicator> list = new List<WaveWarningIndicator>();
            for (int i = 0; i < 4; i++)
            {
                RectTransform rt = BU.Place(BU.UI("Warning_" + (i + 1), holder), MC, MC, Vector2.zero, new Vector2(84f, 84f));

                // ลูกศรชี้ไปทางที่ศัตรูจะมา (หมุนรอบตัวไอคอน)
                RectTransform pivot = BU.Place(BU.UI("ArrowPivot", rt), MC, MC, Vector2.zero, new Vector2(10f, 10f));
                Image arrow = BU.Icon("Arrow", pivot, BU.Triangle, new Color(1f, 0.25f, 0.2f), MC, MC, new Vector2(62f, 0f), new Vector2(34f, 30f));
                arrow.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -90f);

                Image bg = BU.Img(rt, new Color(0.35f, 0.03f, 0.03f, 0.95f), true, BU.Circle);
                BU.Icon("Rim", rt, BU.Ring, new Color(1f, 0.4f, 0.3f), MC, MC, Vector2.zero, new Vector2(84f, 84f));
                BU.Icon("Skull", rt, BU.Skull, new Color(1f, 0.92f, 0.88f), MC, MC, new Vector2(0f, 4f), new Vector2(50f, 50f));
                Button b = BU.MakeButton(rt.gameObject, bg);
                TextMeshProUGUI count = BU.Badge("Count_Badge", rt, "x0", TR, MC, new Vector2(-6f, -6f), new Vector2(46f, 24f), new Color(0f, 0f, 0f, 0.9f), Color.white, 16f);

                // แถบรูปศัตรูที่จะมา (พื้นสว่าง เพราะรูปศัตรูเป็นสีดำ)
                RectTransform strip = BU.Panel("Enemy_Strip", rt, BC, TC, new Vector2(0f, -4f), new Vector2(120f, 42f), new Color(0.92f, 0.9f, 0.88f, 0.95f));
                strip.GetComponent<Image>().raycastTarget = false;
                List<Image> icons = new List<Image>();
                for (int k = 0; k < 3; k++)
                    icons.Add(BU.Icon("Enemy_" + (k + 1), strip, BU.Circle, Color.white, ML, ML, new Vector2(4f + k * 38f, 0f), new Vector2(36f, 36f)));

                TooltipTrigger tip = rt.gameObject.AddComponent<TooltipTrigger>();
                WaveWarningIndicator ind = rt.gameObject.AddComponent<WaveWarningIndicator>();
                new SO(ind).Set("button", b).Set("arrow", pivot).Set("countText", count)
                    .ObjList("enemyIcons", icons.Cast<Object>().ToArray()).Set("tooltip", tip).Apply();
                rt.gameObject.SetActive(false);
                list.Add(ind);
            }
            WaveWarningUI ui = holder.gameObject.AddComponent<WaveWarningUI>();
            new SO(ui).ObjList("indicators", list.Cast<Object>().ToArray()).Apply();
        }

        private static void BuildTooltip(Transform root)
        {
            RectTransform tipRoot = BU.Stretch(BU.UI("Tooltip", root));
            RectTransform box = BU.Panel("Box", tipRoot, BL, BL, Vector2.zero, new Vector2(360f, 210f), new Color(0.04f, 0.04f, 0.07f, 0.96f));
            box.GetComponent<Image>().raycastTarget = false;
            TextMeshProUGUI title = BU.Text("Title_Text", box, "", 20f, TL, TL, new Vector2(10f, -6f), new Vector2(320f, 28f), TextAlignmentOptions.Left, Gold);
            title.fontStyle = FontStyles.Bold;
            TextMeshProUGUI body = BU.Text("Body_Text", box, "", 16f, TL, TL, new Vector2(10f, -36f), new Vector2(340f, 168f), TextAlignmentOptions.TopLeft);
            TooltipUI t = tipRoot.gameObject.AddComponent<TooltipUI>();
            new SO(t).Set("box", box).Set("titleText", title).Set("bodyText", body).Apply();
            box.gameObject.SetActive(false);
        }

        private static LevelUpPanelUI BuildLevelUpPanel(Transform root)
        {
            RectTransform lp = BU.Stretch(BU.UI("LevelUpPanel", root));
            BU.Img(lp, new Color(0f, 0f, 0f, 0.78f));
            TextMeshProUGUI title = BU.Text("Title_Text", lp, "LEVEL UP!", 46f, TC, TC, new Vector2(0f, -130f), new Vector2(1400f, 64f), TextAlignmentOptions.Center, Gold);
            title.fontStyle = FontStyles.Bold;

            List<UpgradeCardUI> cards = new List<UpgradeCardUI>();
            for (int i = 0; i < 3; i++)
            {
                RectTransform c = BU.Place(BU.UI("Card_" + (i + 1), lp), MC, MC, new Vector2((i - 1) * 420f, -30f), new Vector2(370f, 470f));
                Image bg = BU.Img(c, new Color(0.12f, 0.12f, 0.17f, 1f));
                Button b = BU.MakeButton(c.gameObject, bg);
                RectTransform strip = BU.Place(BU.UI("Category_Strip", c), TC, TC, Vector2.zero, new Vector2(370f, 44f));
                Image stripImg = BU.Img(strip, Color.white, false);
                TextMeshProUGUI cat = BU.StretchText("Category_Text", strip, "PLAYER", 20f, TextAlignmentOptions.Center, Color.black);
                cat.fontStyle = FontStyles.Bold;
                TextMeshProUGUI t = BU.Text("Title_Text", c, "Card", 32f, TC, TC, new Vector2(0f, -64f), new Vector2(340f, 80f));
                t.fontStyle = FontStyles.Bold;
                TextMeshProUGUI d = BU.Text("Description_Text", c, "", 22f, TC, TC, new Vector2(0f, -160f), new Vector2(330f, 220f), TextAlignmentOptions.Top);
                TextMeshProUGUI st = BU.Text("Stacks_Text", c, "", 18f, BC, BC, new Vector2(0f, 16f), new Vector2(330f, 30f), TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.6f));
                UpgradeCardUI ui = c.gameObject.AddComponent<UpgradeCardUI>();
                new SO(ui).Set("button", b).Set("categoryStrip", stripImg).Set("categoryText", cat).Set("titleText", t)
                    .Set("descriptionText", d).Set("stacksText", st).Apply();
                cards.Add(ui);
            }

            LevelUpPanelUI panel = lp.gameObject.AddComponent<LevelUpPanelUI>();
            new SO(panel).Set("titleText", title).ObjList("cards", cards.Cast<Object>().ToArray()).Apply();
            lp.gameObject.SetActive(false);
            return panel;
        }

        private static PausePanelUI BuildPausePanel(Transform root)
        {
            RectTransform pp = BU.Stretch(BU.UI("PausePanel", root));
            BU.Img(pp, new Color(0f, 0f, 0f, 0.7f));
            RectTransform box = BU.Panel("Box", pp, MC, MC, Vector2.zero, new Vector2(440f, 300f), new Color(0.08f, 0.08f, 0.12f, 0.97f));
            BU.Text("Title_Text", box, "PAUSED", 48f, TC, TC, new Vector2(0f, -20f), new Vector2(400f, 60f)).fontStyle = FontStyles.Bold;
            Button resume = BU.Button("Resume_Button", box, "Resume", TC, TC, new Vector2(0f, -110f), new Vector2(320f, 60f), out _, 26f);
            Button restart = BU.Button("Restart_Button", box, "Restart", TC, TC, new Vector2(0f, -185f), new Vector2(320f, 60f), out _, 26f);
            PausePanelUI ui = pp.gameObject.AddComponent<PausePanelUI>();
            new SO(ui).Set("resumeButton", resume).Set("restartButton", restart).Apply();
            pp.gameObject.SetActive(false);
            return ui;
        }

        private static GameOverPanelUI BuildGameOverPanel(Transform root)
        {
            RectTransform gp = BU.Stretch(BU.UI("GameOverPanel", root));
            BU.Img(gp, new Color(0f, 0f, 0f, 0.82f));
            TextMeshProUGUI title = BU.Text("Title_Text", gp, "DEFEAT", 90f, MC, MC, new Vector2(0f, 120f), new Vector2(1200f, 110f), TextAlignmentOptions.Center, Gold);
            title.fontStyle = FontStyles.Bold;
            TextMeshProUGUI detail = BU.Text("Detail_Text", gp, "", 28f, MC, MC, new Vector2(0f, 10f), new Vector2(1200f, 100f));
            Button restart = BU.Button("Restart_Button", gp, "Play Again", MC, MC, new Vector2(0f, -110f), new Vector2(340f, 70f), out _, 30f);
            GameOverPanelUI ui = gp.gameObject.AddComponent<GameOverPanelUI>();
            new SO(ui).Set("titleText", title).Set("detailText", detail).Set("restartButton", restart).Apply();
            gp.gameObject.SetActive(false);
            return ui;
        }
    }
}
