using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// จัดเลเยอร์ของตกแต่ง Map2 ให้เหมือนป้อม: เรียงหน้า-หลังกับผู้เล่นตามแกน Y + ผู้เล่นเดินทะลุโคนไม่ได้
    /// เมนู: Tools > NecroDefense > Updates > Fix Map2 Prop Layers
    /// - ทำกับของที่อยู่ใต้ Map > Decor_Map2 > Props (ของที่ย้าย / เพิ่มเองก็ได้ด้วย ตำแหน่งไม่เปลี่ยน)
    /// - กดซ้ำได้ (ขนาดวงรีชน คำนวณใหม่ตามขนาดรูป)
    /// ปรับวงรีชนรายชิ้นได้ที่ WorldProp (Footprint Size / Offset / Block Player)
    /// </summary>
    public static class NecroUpdate_PropLayers
    {
        private const string ScenePath = "Assets/Scenes/Map2.unity";
        // จุดเรียงหน้า-หลัง = สูงกว่าโคนเท่ากับผู้เล่น (จุดกลางผู้เล่นสูงจากเท้า ~0.55) จะได้เทียบกันตรง
        public const float SortLift = 0.55f;
        public const int SortOrder = 5; // เท่ากับผู้เล่น / ป้อม

        [MenuItem("Tools/NecroDefense/Updates/Fix Map2 Prop Layers")]
        public static void Apply()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null) { EditorUtility.DisplayDialog("Prop Layers", "ไม่เจอซีน Map2", "OK"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string start = SceneManager.GetActiveScene().path;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            MapBounds bounds = Object.FindAnyObjectByType<MapBounds>(FindObjectsInactive.Include);
            Transform props = bounds != null ? bounds.transform.Find("Decor_Map2/Props") : null;
            if (props == null) { EditorUtility.DisplayDialog("Prop Layers", "ไม่เจอ Map > Decor_Map2 > Props (กด Decorate Map2 ก่อน)", "OK"); return; }

            int n = 0;
            foreach (Transform p in props) { if (Setup(p)) n++; }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(start) && start != ScenePath) EditorSceneManager.OpenScene(start, OpenSceneMode.Single);
            EditorUtility.DisplayDialog("Prop Layers", "จัดเลเยอร์ + วงรีชน " + n + " ชิ้น\n\n" +
                "ผู้เล่นเดินไปหลังต้นไม้ / พุ่มไม้ได้ แต่ทะลุโคนไม่ได้\nปรับวงรีชนรายชิ้น: เลือกชิ้นนั้น > WorldProp (วงรีสีเขียวใน Scene)", "OK");
        }

        /// <summary>จัด 1 ชิ้น: ห่อ Art ด้วย "Sorted" (SortingGroup) + ใส่ WorldProp (วงรีชน)</summary>
        public static bool Setup(Transform root)
        {
            Transform sorted = root.Find("Sorted");
            Transform art = sorted != null ? sorted.Find("Art") : root.Find("Art");
            if (art == null) return false;
            SpriteRenderer sr = art.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return false;

            float sy = Mathf.Max(0.01f, Mathf.Abs(root.lossyScale.y));
            if (sorted == null)
            {
                sorted = new GameObject("Sorted").transform;
                sorted.SetParent(root, false);
                art.SetParent(sorted, true);
            }
            Vector3 artWorld = art.position;
            sorted.localPosition = new Vector3(0f, SortLift / sy, 0f);
            art.position = artWorld;
            SortingGroup g = sorted.GetComponent<SortingGroup>();
            if (g == null) g = sorted.gameObject.AddComponent<SortingGroup>();
            g.sortingOrder = SortOrder;

            // วงรีชน ตามขนาดรูป
            float w = sr.sprite.rect.width / sr.sprite.pixelsPerUnit * Mathf.Abs(root.lossyScale.x);
            string n = root.name;
            Vector2 size;
            if (n.StartsWith("Tree")) size = new Vector2(Mathf.Clamp(w * 0.22f, 0.35f, 0.7f), 0.24f);
            else if (n.StartsWith("Bush")) size = new Vector2(w * 0.72f, Mathf.Max(0.25f, w * 0.2f));
            else if (n.StartsWith("Rock")) size = new Vector2(w * 0.75f, Mathf.Max(0.2f, w * 0.3f));
            else size = new Vector2(w * 0.65f, Mathf.Max(0.2f, w * 0.25f));

            WorldProp wp = root.GetComponent<WorldProp>();
            if (wp == null) wp = root.gameObject.AddComponent<WorldProp>();
            SerializedObject so = new SerializedObject(wp);
            so.FindProperty("footprintSize").vector2Value = size;
            so.FindProperty("footprintOffset").vector2Value = new Vector2(0f, size.y * 0.25f);
            so.FindProperty("blockPlayer").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();
            return true;
        }
    }
}
