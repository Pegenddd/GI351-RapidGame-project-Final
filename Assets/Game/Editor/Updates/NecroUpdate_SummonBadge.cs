using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// เพิ่มป้าย "R x1" (จำนวนที่เสกต่อครั้ง) ที่มุมขวาบนของปุ่มกระเป๋า แล้วผูกกับ MinionBagUI
    /// เมนู: Tools > NecroDefense > Updates > Add Summon Batch Badge
    /// - ทำในซีน Gameplay + TestArena / กดซ้ำได้ (ถ้ามีป้ายอยู่แล้วแค่ผูกใหม่ ไม่สร้างซ้ำ)
    /// - ไม่แตะอย่างอื่นในซีน
    /// </summary>
    public static class NecroUpdate_SummonBadge
    {
        private static readonly string[] Scenes = { "Assets/Scenes/Gameplay.unity", "Assets/Scenes/TestArena.unity" };
        private const string BadgeName = "Summon_Badge";

        [MenuItem("Tools/NecroDefense/Updates/Add Summon Batch Badge")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string startScene = SceneManager.GetActiveScene().path;
            string log = "";

            foreach (string scenePath in Scenes)
            {
                string label = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) continue;
                Scene s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                MinionBagUI bagUI = null;
                foreach (GameObject go in s.GetRootGameObjects())
                {
                    bagUI = go.GetComponentInChildren<MinionBagUI>(true);
                    if (bagUI != null) break;
                }
                if (bagUI == null) { log += label + ": ไม่เจอ MinionBagUI (ข้าม)\n"; continue; }

                Transform btn = FindDeep(bagUI.transform, "Bag_Button");
                if (btn == null)
                    foreach (GameObject go in s.GetRootGameObjects()) { btn = FindDeep(go.transform, "Bag_Button"); if (btn != null) break; }
                if (btn == null) { log += label + ": ไม่เจอ Bag_Button (ข้าม)\n"; continue; }

                TMP_Text text;
                Transform existing = btn.Find(BadgeName);
                if (existing != null)
                {
                    text = existing.GetComponentInChildren<TMP_Text>(true);
                    log += label + ": มีป้ายอยู่แล้ว -> ผูกใหม่\n";
                }
                else
                {
                    Vector2 TR = new Vector2(1f, 1f);
                    text = BU.Badge(BadgeName, btn, "R x1", TR, TR, new Vector2(-4f, -4f), new Vector2(54f, 22f),
                        new Color(0f, 0f, 0f, 0.85f), new Color(0.9f, 0.15f, 0.15f), 14f);
                    log += label + ": เพิ่มป้าย R x1\n";
                }

                SerializedObject so = new SerializedObject(bagUI);
                so.FindProperty("summonBatchText").objectReferenceValue = text;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorSceneManager.MarkSceneDirty(s);
                EditorSceneManager.SaveScene(s);
            }

            if (!string.IsNullOrEmpty(startScene)) EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);
            Debug.Log("[NecroDefense] Add Summon Batch Badge\n" + log);
            EditorUtility.DisplayDialog("Summon Batch Badge", log +
                "\nเล่นเกม: กด R เปลี่ยนจำนวนเสก 1 -> 2 -> 3 -> 1 / กด 1-5 หรือคลิกช่อง = เสกรอบตัว", "OK");
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                Transform r = FindDeep(c, name);
                if (r != null) return r;
            }
            return null;
        }
    }
}
