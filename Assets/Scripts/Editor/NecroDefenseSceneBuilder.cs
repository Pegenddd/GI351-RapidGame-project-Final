using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using NecroDefense.Base;
using NecroDefense.Characters.Base;
using NecroDefense.Characters.Player;
using NecroDefense.Characters.Enemies;
using NecroDefense.Characters.Minions;
using NecroDefense.Combat;
using NecroDefense.Factories;
using NecroDefense.Managers;
using NecroDefense.UI;
using NecroDefense.Utils;

namespace NecroDefense.Editor
{
    /// <summary>
    /// Editor tool that creates all Prefabs and generates a complete, clean
    /// Game Hierarchy directly in the Unity Editor Scene.
    /// Allows the user to select, inspect, and modify ANY property in the Hierarchy.
    /// </summary>
    public static class NecroDefenseSceneBuilder
    {
        private const string PrefabBasePath = "Assets/Prefabs";
        private const string CorePrefabPath = "Assets/Prefabs/Core";
        private const string EnemyPrefabPath = "Assets/Prefabs/Enemies";
        private const string MinionPrefabPath = "Assets/Prefabs/Minions";
        private const string ProjPrefabPath = "Assets/Prefabs/Projectiles";

        [MenuItem("NecroDefense/⚡ Setup Complete Hierarchy (สร้างทุกอย่างใน Hierarchy)", false, 1)]
        public static void SetupCompleteHierarchy()
        {
            EnsureDirectories();
            CreateAllPrefabs();

            Undo.SetCurrentGroupName("Setup NecroDefense Hierarchy");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Managers Parent
            GameObject managersParent = GetOrCreateRoot("=== MANAGERS ===");
            
            var gmObj = GetOrCreateChild(managersParent, "GameManager");
            var gm = GetOrAddComponent<GameManager>(gmObj);

            var spawnerObj = GetOrCreateChild(managersParent, "WaveSpawner");
            var spawner = GetOrAddComponent<WaveSpawner>(spawnerObj);
            spawner.PopulateDefaultWaves();

            var minionFactObj = GetOrCreateChild(managersParent, "MinionFactory");
            var minionFactory = GetOrAddComponent<MinionFactory>(minionFactObj);

            var enemyFactObj = GetOrCreateChild(managersParent, "EnemyFactory");
            var enemyFactory = GetOrAddComponent<EnemyFactory>(enemyFactObj);

            // Assign prefabs into factories
            AssignFactoryPrefabs(enemyFactory, minionFactory);

            // 2. Citadel Base Core
            GameObject baseParent = GetOrCreateRoot("=== ENVIRONMENT & BASE ===");
            var citadelObj = GetOrCreateChild(baseParent, "Citadel_BaseCore");
            citadelObj.transform.position = Vector3.zero;
            SetupCitadelComponent(citadelObj);

            // 3. Player Necromancer
            GameObject playerParent = GetOrCreateRoot("=== PLAYER ===");
            var playerObj = GetOrCreateChild(playerParent, "Player_Necromancer");
            playerObj.transform.position = new Vector3(0, -2.5f, 0);
            SetupPlayerComponent(playerObj);

            // 4. UI HUD
            GameObject uiParent = GetOrCreateRoot("=== UI ===");
            var hudObj = GetOrCreateChild(uiParent, "GameHUD");
            GetOrAddComponent<GameHUD>(hudObj);

            // 5. Camera Setup
            SetupCamera(playerObj.transform);

            // Link references into GameManager Master Panel
            var gmSo = new SerializedObject(gm);
            gmSo.FindProperty("baseCrystal").objectReferenceValue = citadelObj.GetComponent<BaseCrystal>();
            gmSo.FindProperty("player").objectReferenceValue = playerObj.GetComponent<PlayerNecromancer>();
            gmSo.FindProperty("waveSpawner").objectReferenceValue = spawner;
            gmSo.ApplyModifiedPropertiesWithoutUndo();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog(
                "NecroDefense Hierarchy Ready!",
                "สร้าง GameObjects ทั้งหมดใน Hierarchy สำเร็จเรียบร้อย!\n\n" +
                "ตอนนี้คุณสามารถคลิกที่แต่ละ GameObject ใน Hierarchy แล้วแก้ไขค่าทุกอย่างใน Inspector ได้ทันที:\n" +
                "- ปรับความเร็ว/เลือด/วิญญาณของ Necromancer\n" +
                "- ปรับเลือด/รัศมีออร่าของ Citadel Base\n" +
                "- ปรับแต่ง Wave ศัตรูใน WaveSpawner\n" +
                "- เปลี่ยนรูป Sprite หรือสีใน SpriteRenderer\n" +
                "- ปรับแต่งสถิติมอนสเตอร์ใน Prefabs",
                "ตกลง"
            );
        }

        [MenuItem("NecroDefense/📦 Create All Prefabs (สร้าง Prefab ทั้งหมด)", false, 2)]
        public static void CreateAllPrefabs()
        {
            EnsureDirectories();

            // Projectiles
            var darkBolt = CreateDarkBoltPrefab();
            var boneArrow = CreateBoneArrowPrefab();
            var spectralArrow = CreateSpectralArrowPrefab();

            // Corpse
            var corpse = CreateCorpsePrefab();

            // Enemies
            CreateEnemyPrefab(Core.EnemyType.Zombie, "Enemy_Zombie", corpse);
            CreateEnemyPrefab(Core.EnemyType.SkeletonArcher, "Enemy_SkeletonArcher", corpse, boneArrow);
            CreateEnemyPrefab(Core.EnemyType.Golem, "Enemy_Golem", corpse);
            CreateEnemyPrefab(Core.EnemyType.Ghost, "Enemy_Ghost", corpse);

            // Minions
            CreateMinionPrefab(Core.EnemyType.Zombie, "Minion_Zombie");
            CreateMinionPrefab(Core.EnemyType.SkeletonArcher, "Minion_SkeletonArcher", spectralArrow);
            CreateMinionPrefab(Core.EnemyType.Golem, "Minion_Golem");
            CreateMinionPrefab(Core.EnemyType.Ghost, "Minion_Ghost");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(CorePrefabPath)) Directory.CreateDirectory(CorePrefabPath);
            if (!Directory.Exists(EnemyPrefabPath)) Directory.CreateDirectory(EnemyPrefabPath);
            if (!Directory.Exists(MinionPrefabPath)) Directory.CreateDirectory(MinionPrefabPath);
            if (!Directory.Exists(ProjPrefabPath)) Directory.CreateDirectory(ProjPrefabPath);
        }

        private static GameObject CreateDarkBoltPrefab()
        {
            string path = $"{ProjPrefabPath}/Proj_DarkBolt.prefab";
            GameObject obj = new GameObject("Proj_DarkBolt");
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralVisuals.GetProjectileSprite("DarkBolt");
            sr.color = Color.white;
            sr.sortingOrder = 12;

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            var homing = obj.AddComponent<HomingProjectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateBoneArrowPrefab()
        {
            string path = $"{ProjPrefabPath}/Proj_BoneArrow.prefab";
            GameObject obj = new GameObject("Proj_BoneArrow");
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralVisuals.GetProjectileSprite("BoneArrow");
            sr.color = Color.white;
            sr.sortingOrder = 10;

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            var proj = obj.AddComponent<Projectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateSpectralArrowPrefab()
        {
            string path = $"{ProjPrefabPath}/Proj_SpectralArrow.prefab";
            GameObject obj = new GameObject("Proj_SpectralArrow");
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralVisuals.GetProjectileSprite("SpectralArrow");
            sr.color = Color.white;
            sr.sortingOrder = 10;

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            var proj = obj.AddComponent<Projectile>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateCorpsePrefab()
        {
            string path = $"{CorePrefabPath}/Corpse.prefab";
            GameObject obj = new GameObject("Corpse");
            var sr = obj.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralVisuals.GetCorpseSprite();
            sr.color = Color.white;
            sr.sortingOrder = 2;

            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            var corpse = obj.AddComponent<Corpse>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateEnemyPrefab(Core.EnemyType type, string name, GameObject corpsePrefab, GameObject arrowPrefab = null)
        {
            string path = $"{EnemyPrefabPath}/{name}.prefab";
            GameObject obj = new GameObject(name);
            var sr = obj.AddComponent<SpriteRenderer>();
            var col = obj.AddComponent<CircleCollider2D>();
            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            sr.sprite = ProceduralVisuals.GetEnemySprite(type);
            sr.color = Color.white;

            switch (type)
            {
                case Core.EnemyType.Zombie:
                    col.radius = 0.5f;
                    var zombie = obj.AddComponent<ZombieEnemy>();
                    var zSo = new SerializedObject(zombie);
                    zSo.FindProperty("attackRange").floatValue = 1.4f;
                    zSo.FindProperty("attackPower").floatValue = 8f;
                    zSo.FindProperty("attackInterval").floatValue = 1.0f;
                    zSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                case Core.EnemyType.SkeletonArcher:
                    col.radius = 0.45f;
                    var archer = obj.AddComponent<SkeletonArcherEnemy>();
                    var aSo = new SerializedObject(archer);
                    aSo.FindProperty("attackRange").floatValue = 6.5f;
                    aSo.FindProperty("attackPower").floatValue = 7f;
                    aSo.FindProperty("attackInterval").floatValue = 1.5f;
                    if (arrowPrefab != null)
                    {
                        aSo.FindProperty("arrowPrefab").objectReferenceValue = arrowPrefab;
                    }
                    aSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                case Core.EnemyType.Golem:
                    col.radius = 0.8f;
                    obj.transform.localScale = Vector3.one * 1.3f;
                    var golem = obj.AddComponent<GolemEnemy>();
                    var gSo = new SerializedObject(golem);
                    gSo.FindProperty("attackRange").floatValue = 1.8f;
                    gSo.FindProperty("attackPower").floatValue = 20f;
                    gSo.FindProperty("attackInterval").floatValue = 2.0f;
                    gSo.FindProperty("maxHealth").floatValue = 250f;
                    gSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                case Core.EnemyType.Ghost:
                    col.radius = 0.45f;
                    var ghost = obj.AddComponent<GhostEnemy>();
                    var ghSo = new SerializedObject(ghost);
                    ghSo.FindProperty("attackRange").floatValue = 1.4f;
                    ghSo.FindProperty("attackPower").floatValue = 6f;
                    ghSo.FindProperty("attackInterval").floatValue = 1.2f;
                    ghSo.FindProperty("moveSpeed").floatValue = 3.5f;
                    ghSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
            }

            var enemyUnit = obj.GetComponent<EnemyUnit>();
            if (enemyUnit != null && corpsePrefab != null)
            {
                var so = new SerializedObject(enemyUnit);
                so.FindProperty("corpsePrefab").objectReferenceValue = corpsePrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            sr.sortingOrder = 5;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static GameObject CreateMinionPrefab(Core.EnemyType type, string name, GameObject arrowPrefab = null)
        {
            string path = $"{MinionPrefabPath}/{name}.prefab";
            GameObject obj = new GameObject(name);
            var sr = obj.AddComponent<SpriteRenderer>();
            var col = obj.AddComponent<CircleCollider2D>();
            var rb = obj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            sr.sprite = ProceduralVisuals.GetMinionSprite(type);
            sr.color = Color.white;

            switch (type)
            {
                case Core.EnemyType.Zombie:
                    col.radius = 0.5f;
                    var zm = obj.AddComponent<ZombieMinion>();
                    var zmSo = new SerializedObject(zm);
                    zmSo.FindProperty("attackRange").floatValue = 1.4f;
                    zmSo.FindProperty("attackPower").floatValue = 12f;
                    zmSo.FindProperty("attackInterval").floatValue = 0.9f;
                    zmSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                case Core.EnemyType.SkeletonArcher:
                    col.radius = 0.45f;
                    var archer = obj.AddComponent<SkeletonArcherMinion>();
                    var aSo = new SerializedObject(archer);
                    aSo.FindProperty("attackRange").floatValue = 6.5f;
                    aSo.FindProperty("attackPower").floatValue = 10f;
                    aSo.FindProperty("attackInterval").floatValue = 1.2f;
                    if (arrowPrefab != null)
                    {
                        aSo.FindProperty("spectralArrowPrefab").objectReferenceValue = arrowPrefab;
                    }
                    aSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                case Core.EnemyType.Golem:
                    col.radius = 0.8f;
                    obj.transform.localScale = Vector3.one * 1.3f;
                    var gm = obj.AddComponent<GolemMinion>();
                    var gmSo = new SerializedObject(gm);
                    gmSo.FindProperty("attackRange").floatValue = 1.8f;
                    gmSo.FindProperty("attackPower").floatValue = 25f;
                    gmSo.FindProperty("attackInterval").floatValue = 1.8f;
                    gmSo.FindProperty("maxHealth").floatValue = 300f;
                    gmSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
                case Core.EnemyType.Ghost:
                    col.radius = 0.45f;
                    var ghm = obj.AddComponent<GhostMinion>();
                    var ghmSo = new SerializedObject(ghm);
                    ghmSo.FindProperty("attackRange").floatValue = 1.4f;
                    ghmSo.FindProperty("attackPower").floatValue = 8f;
                    ghmSo.FindProperty("attackInterval").floatValue = 1.0f;
                    ghmSo.ApplyModifiedPropertiesWithoutUndo();
                    break;
            }

            sr.sortingOrder = 6;
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Object.DestroyImmediate(obj);
            return prefab;
        }

        private static void AssignFactoryPrefabs(EnemyFactory enemyFact, MinionFactory minionFact)
        {
            if (enemyFact != null)
            {
                var so = new SerializedObject(enemyFact);
                so.FindProperty("zombiePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabPath}/Enemy_Zombie.prefab");
                so.FindProperty("archerPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabPath}/Enemy_SkeletonArcher.prefab");
                so.FindProperty("golemPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabPath}/Enemy_Golem.prefab");
                so.FindProperty("ghostPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{EnemyPrefabPath}/Enemy_Ghost.prefab");
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (minionFact != null)
            {
                var so = new SerializedObject(minionFact);
                so.FindProperty("zombieMinionPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{MinionPrefabPath}/Minion_Zombie.prefab");
                so.FindProperty("archerMinionPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{MinionPrefabPath}/Minion_SkeletonArcher.prefab");
                so.FindProperty("golemMinionPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{MinionPrefabPath}/Minion_Golem.prefab");
                so.FindProperty("ghostMinionPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>($"{MinionPrefabPath}/Minion_Ghost.prefab");
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetupCitadelComponent(GameObject obj)
        {
            var sr = GetOrAddComponent<SpriteRenderer>(obj);
            sr.sprite = ProceduralVisuals.GetCitadelSprite();
            sr.color = Color.white;
            sr.sortingOrder = 3;

            var col = GetOrAddComponent<CircleCollider2D>(obj);
            col.radius = 1.2f;

            GetOrAddComponent<BaseCrystal>(obj);
        }

        private static void SetupPlayerComponent(GameObject obj)
        {
            var sr = GetOrAddComponent<SpriteRenderer>(obj);
            sr.sprite = ProceduralVisuals.GetNecromancerSprite();
            sr.color = Color.white;
            sr.sortingOrder = 7;

            var col = GetOrAddComponent<CircleCollider2D>(obj);
            col.radius = 0.5f;

            var rb = GetOrAddComponent<Rigidbody2D>(obj);
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            var player = GetOrAddComponent<PlayerNecromancer>(obj);

            // Link dark bolt prefab
            var darkBolt = AssetDatabase.LoadAssetAtPath<GameObject>($"{ProjPrefabPath}/Proj_DarkBolt.prefab");
            if (darkBolt != null)
            {
                var so = new SerializedObject(player);
                var boltProp = so.FindProperty("darkBoltPrefab");
                if (boltProp != null)
                {
                    boltProp.objectReferenceValue = darkBolt;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void SetupCamera(Transform playerTransform)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.backgroundColor = Color.black; // 2-Color Pure Black Background
            cam.clearFlags = CameraClearFlags.SolidColor;

            var follow = GetOrAddComponent<CameraFollow>(cam.gameObject);
            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = playerTransform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject GetOrCreateRoot(string name)
        {
            var existing = GameObject.Find(name);
            if (existing != null) return existing;

            var obj = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            return obj;
        }

        private static GameObject GetOrCreateChild(GameObject parent, string name)
        {
            var child = parent.transform.Find(name);
            if (child != null) return child.gameObject;

            var obj = new GameObject(name);
            obj.transform.SetParent(parent.transform, false);
            Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
            return obj;
        }

        private static T GetOrAddComponent<T>(GameObject obj) where T : Component
        {
            var comp = obj.GetComponent<T>();
            if (comp == null)
            {
                comp = Undo.AddComponent<T>(obj);
            }
            return comp;
        }
    }
}

