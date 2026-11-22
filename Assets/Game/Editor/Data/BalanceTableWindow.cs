using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// ตารางบาลานซ์: ดู/แก้ค่าของศัตรู / มินเนียน / ป้อม / ผู้เล่น ทุกตัวในหน้าเดียว (แก้ในตาราง = แก้ไฟล์ Data จริง, Ctrl+Z ได้)
    /// เมนู: Tools > NecroDefense > Data > Balance Table
    /// ชี้เมาส์ที่หัวตาราง/ช่อง จะเห็นคำอธิบาย
    /// </summary>
    public class BalanceTableWindow : EditorWindow
    {
        private static readonly string[] Tabs = { "Enemies", "Minions", "Towers", "Player" };

        private static readonly string[] EnemyCols =
        {
            "displayName", "maxHealth", "armor", "moveSpeed", "attackDamage", "attackInterval", "attackRange", "areaRadius",
            "aggroRange", "priorityChaseRange", "soulReward", "xpReward", "soulOrbValue", "raiseChance", "corpseHarvestSouls", "corpseExpireSouls", "splitCount",
            "isFlying", "canAttackFlying", "attackPlayer", "attackMinions", "attackMainBase", "priority", "onlyMinionsOnLane", "shootWhileWalking", "attackStyle"
        };
        private static readonly string[] MinionCols =
        {
            "displayName", "soulCost", "maxHealth", "armor", "moveSpeed", "attackDamage", "attackInterval", "attackRange", "areaRadius",
            "aggroRange", "chaseLeash", "guardRadius", "followDistance", "isFlying", "canAttackFlying", "attackStyle"
        };
        private static readonly string[] TowerLevelCols =
        {
            "label", "cost", "damage", "attackInterval", "range", "targetsPerShot", "areaRadius",
            "slowPercent", "slowDuration", "chainTargets", "soulCaptureChance", "poisonPoolChance"
        };

        private static readonly Dictionary<string, string> Short = new Dictionary<string, string>
        {
            { "displayName", "Name" }, { "maxHealth", "HP" }, { "armor", "Armor" }, { "moveSpeed", "Speed" },
            { "attackDamage", "DMG" }, { "attackInterval", "Sec/Hit" }, { "attackRange", "Range" }, { "areaRadius", "AoE" },
            { "aggroRange", "Sight" }, { "priorityChaseRange", "Chase" }, { "soulReward", "Souls" }, { "xpReward", "XP" }, { "soulOrbValue", "Soul ◆" },
            { "raiseChance", "Raise %" }, { "corpseHarvestSouls", "Corpse ◆" }, { "corpseExpireSouls", "Expire ◆" }, { "splitCount", "Split" }, { "isFlying", "Fly" }, { "canAttackFlying", "Hit Fly" },
            { "attackPlayer", "→Player" }, { "attackMinions", "→Minion" }, { "attackMainBase", "→Base" }, { "priority", "Priority" },
            { "onlyMinionsOnLane", "Lane:Min" }, { "shootWhileWalking", "Walk+Shoot" }, { "attackStyle", "Style" },
            { "soulCost", "Soul" }, { "chaseLeash", "Leash" }, { "guardRadius", "Guard" }, { "followDistance", "Follow" },
            { "label", "Tier" }, { "cost", "Cost" }, { "damage", "DMG" },
            { "range", "Range" }, { "targetsPerShot", "Shots" }, { "slowPercent", "Slow" }, { "slowDuration", "Slow s" },
            { "chainTargets", "Chain" }, { "soulCaptureChance", "Capture" }, { "poisonPoolChance", "Poison" },
        };

        private int _tab;
        private Vector2 _scroll;
        private Editor _playerEditor;

        [MenuItem("Tools/NecroDefense/Data/Balance Table", false, 1)]
        public static void Open()
        {
            BalanceTableWindow w = GetWindow<BalanceTableWindow>("Balance Table");
            w.minSize = new Vector2(700, 300);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            _tab = GUILayout.Toolbar(_tab, Tabs, EditorStyles.toolbarButton, GUILayout.Width(360));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Create & Link From Prefabs", EditorStyles.toolbarButton)) NecroDataTools.CreateAndLink();
            if (GUILayout.Button("Save", EditorStyles.toolbarButton)) AssetDatabase.SaveAssets();
            EditorGUILayout.EndHorizontal();

            if (EditorApplication.isPlaying)
                EditorGUILayout.HelpBox("กำลังเล่นอยู่: ค่าที่แก้จะถูกบันทึกลงไฟล์จริง แต่มีผลกับตัวที่เกิดใหม่ (ป้อมมีผลทันที)", MessageType.Info);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case 0: DrawTable<EnemyStats>(EnemyCols); break;
                case 1: DrawTable<MinionStats>(MinionCols); break;
                case 2: DrawTowers(); break;
                case 3: DrawPlayer(); break;
            }
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------

        private static List<T> FindAll<T>() where T : Object
        {
            List<T> list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { NecroDataTools.DataRoot }))
            {
                T a = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (a != null) list.Add(a);
            }
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return list;
        }

        private static float Width(string col, SerializedProperty p)
        {
            if (col == "displayName" || col == "label") return 110f;
            if (p != null && p.propertyType == SerializedPropertyType.Boolean) return 64f;
            if (p != null && p.propertyType == SerializedPropertyType.Enum) return 90f;
            return 62f;
        }

        private static GUIContent Header(string col, SerializedProperty p)
        {
            string text = Short.TryGetValue(col, out string s) ? s : col;
            return new GUIContent(text, (p != null ? p.tooltip + "\n" : "") + "(" + col + ")");
        }

        private void DrawTable<T>(string[] cols) where T : ScriptableObject
        {
            List<T> assets = FindAll<T>();
            if (assets.Count == 0)
            {
                EditorGUILayout.HelpBox("ยังไม่มีไฟล์ Data - กดปุ่ม Create & Link From Prefabs ด้านบน", MessageType.Warning);
                return;
            }

            SerializedObject first = new SerializedObject(assets[0]);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("File", EditorStyles.boldLabel, GUILayout.Width(140));
            foreach (string c in cols)
            {
                SerializedProperty p = first.FindProperty(c);
                GUILayout.Label(Header(c, p), EditorStyles.miniBoldLabel, GUILayout.Width(Width(c, p)));
            }
            EditorGUILayout.EndHorizontal();

            foreach (T asset in assets)
            {
                SerializedObject so = new SerializedObject(asset);
                so.Update();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent(asset.name, "คลิกเพื่อหาไฟล์ใน Project"), EditorStyles.label, GUILayout.Width(140)))
                {
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                }
                foreach (string c in cols)
                {
                    SerializedProperty p = so.FindProperty(c);
                    if (p == null) { GUILayout.Space(Width(c, null)); continue; }
                    EditorGUILayout.PropertyField(p, new GUIContent("", p.tooltip), GUILayout.Width(Width(c, p)));
                }
                EditorGUILayout.EndHorizontal();
                so.ApplyModifiedProperties();
            }
        }

        private void DrawTowers()
        {
            List<TowerStats> towers = FindAll<TowerStats>();
            if (towers.Count == 0)
            {
                EditorGUILayout.HelpBox("ยังไม่มีไฟล์ Data - กดปุ่ม Create & Link From Prefabs ด้านบน", MessageType.Warning);
                return;
            }

            foreach (TowerStats t in towers)
            {
                SerializedObject so = new SerializedObject(t);
                so.Update();
                EditorGUILayout.Space(6);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(t.name, EditorStyles.boldLabel, GUILayout.Width(140))) { EditorGUIUtility.PingObject(t); Selection.activeObject = t; }
                EditorGUILayout.PropertyField(so.FindProperty("towerName"), new GUIContent("Name"), GUILayout.Width(220));
                EditorGUIUtility.labelWidth = 80;
                EditorGUILayout.PropertyField(so.FindProperty("constructionTime"), new GUIContent("Build sec"), GUILayout.Width(140));
                EditorGUILayout.PropertyField(so.FindProperty("canHitFlying"), new GUIContent("Hit Fly"), GUILayout.Width(110));
                EditorGUILayout.PropertyField(so.FindProperty("chainRange"), new GUIContent("Chain Rng"), GUILayout.Width(140));
                EditorGUIUtility.labelWidth = 0;
                EditorGUILayout.EndHorizontal();

                SerializedProperty levels = so.FindProperty("levels");
                if (levels.arraySize > 0)
                {
                    SerializedProperty e0 = levels.GetArrayElementAtIndex(0);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(144);
                    foreach (string c in TowerLevelCols)
                    {
                        SerializedProperty p = e0.FindPropertyRelative(c);
                        GUILayout.Label(Header(c, p), EditorStyles.miniBoldLabel, GUILayout.Width(Width(c, p)));
                    }
                    EditorGUILayout.EndHorizontal();
                }
                for (int i = 0; i < levels.arraySize; i++)
                {
                    SerializedProperty e = levels.GetArrayElementAtIndex(i);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label(i == 0 ? "  Build" : "  Upgrade " + i, GUILayout.Width(140));
                    foreach (string c in TowerLevelCols)
                    {
                        SerializedProperty p = e.FindPropertyRelative(c);
                        if (p == null) continue;
                        EditorGUILayout.PropertyField(p, new GUIContent("", p.tooltip), GUILayout.Width(Width(c, p)));
                    }
                    EditorGUILayout.EndHorizontal();
                }
                so.ApplyModifiedProperties();
            }
        }

        private void DrawPlayer()
        {
            PlayerStats p = AssetDatabase.LoadAssetAtPath<PlayerStats>(NecroDataTools.PlayerDataPath);
            if (p == null)
            {
                List<PlayerStats> all = FindAll<PlayerStats>();
                if (all.Count > 0) p = all[0];
            }
            if (p == null)
            {
                EditorGUILayout.HelpBox("ยังไม่มีไฟล์ Player - เปิดซีน Gameplay แล้วกดปุ่ม Create & Link From Prefabs ด้านบน", MessageType.Warning);
                return;
            }
            if (GUILayout.Button(p.name, EditorStyles.boldLabel)) { EditorGUIUtility.PingObject(p); Selection.activeObject = p; }
            Editor.CreateCachedEditor(p, null, ref _playerEditor);
            EditorGUIUtility.labelWidth = 180;
            _playerEditor.OnInspectorGUI();
            EditorGUIUtility.labelWidth = 0;
        }

        private void OnInspectorUpdate() => Repaint();
    }
}
