using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// เปลี่ยนสีทุกอย่างในเกมให้อยู่ในโทน ขาว / เทา / ดำ + แดง
    /// เมนู: Tools > NecroDefense > Updates > Apply Palette (White-Gray-Black + Red)
    /// - แตะ "เฉพาะค่าสี" ใน Prefab ทุกตัว (Assets/Game/Prefabs), ซีน Gameplay + TestArena, ไฟล์ Data (การ์ด ฯลฯ)
    /// - สีโทนแดง/ส้ม -> แดง (คงความสว่างเดิม) / สีอื่นทั้งหมด (เขียว ฟ้า ม่วง เหลือง ...) -> เทา ตามความสว่างเดิม / ความใสคงเดิม
    /// - กดซ้ำได้ (สีที่อยู่ในโทนแล้วจะไม่เปลี่ยน)
    /// </summary>
    public static class NecroUpdate_Palette
    {
        private static readonly string[] Scenes = { "Assets/Scenes/Gameplay.unity", "Assets/Scenes/TestArena.unity" };
        private const string PrefabDir = "Assets/Game/Prefabs";
        private const string DataDir = "Assets/Game/Data";

        // โทนแดงของเกม (ใช้แทนสีแดง/ส้มทุกเฉด)
        private const float RedSaturation = 0.85f;

        private static int _changed;

        [MenuItem("Tools/NecroDefense/Updates/Apply Palette (White-Gray-Black + Red)")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string startScene = SceneManager.GetActiveScene().path;
            _changed = 0;
            string log = "";

            // 1) Prefab ทุกตัว
            int prefabs = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int before = _changed;
                    ProcessGameObject(root);
                    if (_changed != before) { PrefabUtility.SaveAsPrefabAsset(root, path); prefabs++; }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            log += "Prefab ที่เปลี่ยน: " + prefabs + "\n";

            // 2) ไฟล์ Data (การ์ดอัปเกรด ฯลฯ)
            int assets = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { DataDir }))
            {
                Object a = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                if (a == null) continue;
                int before = _changed;
                ProcessObject(a);
                if (_changed != before) { EditorUtility.SetDirty(a); assets++; }
            }
            log += "ไฟล์ Data ที่เปลี่ยน: " + assets + "\n";

            // 3) ซีน
            foreach (string scenePath in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) continue;
                Scene s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                int before = _changed;
                foreach (GameObject go in s.GetRootGameObjects()) ProcessGameObject(go);
                if (_changed != before) { EditorSceneManager.MarkSceneDirty(s); EditorSceneManager.SaveScene(s); }
                log += System.IO.Path.GetFileNameWithoutExtension(scenePath) + ": เปลี่ยน " + (_changed - before) + " สี\n";
            }

            AssetDatabase.SaveAssets();
            if (!string.IsNullOrEmpty(startScene)) EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);

            log += "\nรวมเปลี่ยนทั้งหมด " + _changed + " สี";
            Debug.Log("[NecroDefense] Apply Palette\n" + log);
            EditorUtility.DisplayDialog("Apply Palette", log, "OK");
        }

        // ------------------------------------------------------------------

        private static void ProcessGameObject(GameObject root)
        {
            foreach (Component c in root.GetComponentsInChildren<Component>(true))
            {
                if (c == null || c is Transform) continue;
                ProcessObject(c);
                // TextMeshPro / UI: ตั้งผ่าน API ด้วย (TMP เก็บสีไว้หลายช่อง)
                if (c is Graphic g)
                {
                    Color m = ToPalette(g.color);
                    if (!Same(m, g.color)) { g.color = m; EditorUtility.SetDirty(g); _changed++; }
                }
            }
        }

        /// <summary>ไล่ทุกช่องสี (Color / Gradient) ใน component หรือ asset แล้วแปลงเป็นโทนเกม</summary>
        private static void ProcessObject(Object o)
        {
            SerializedObject so = new SerializedObject(o);
            SerializedProperty it = so.GetIterator();
            bool changed = false;
            while (it.Next(true))
            {
                if (it.propertyType == SerializedPropertyType.Color)
                {
                    Color c = it.colorValue, m = ToPalette(c);
                    if (!Same(c, m)) { it.colorValue = m; changed = true; _changed++; }
                }
                else if (it.propertyType == SerializedPropertyType.Gradient)
                {
                    Gradient gr = it.gradientValue;
                    if (gr == null) continue;
                    GradientColorKey[] keys = gr.colorKeys;
                    bool any = false;
                    for (int i = 0; i < keys.Length; i++)
                    {
                        Color m = ToPalette(keys[i].color);
                        if (!Same(keys[i].color, m)) { keys[i].color = m; any = true; }
                    }
                    if (any) { gr.SetKeys(keys, gr.alphaKeys); it.gradientValue = gr; changed = true; _changed++; }
                }
            }
            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>แปลงสี 1 สี: แดง/ส้ม -> แดง, สีอื่น -> เทาตามความสว่าง, ความใสคงเดิม</summary>
        public static Color ToPalette(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            float lum = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
            Color result;
            if (s < 0.15f || v < 0.05f) result = new Color(lum, lum, lum);              // เกือบเทาอยู่แล้ว -> เทาล้วน
            else if (h < 0.1f || h > 0.92f) result = Color.HSVToRGB(0f, Mathf.Max(s, RedSaturation), v); // แดง/ส้ม -> แดง
            else result = new Color(lum, lum, lum);                                        // สีอื่น -> เทา
            result.a = c.a;
            return result;
        }

        private static bool Same(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f && Mathf.Abs(a.b - b.b) < 0.004f && Mathf.Abs(a.a - b.a) < 0.004f;
    }
}
