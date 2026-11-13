using System.IO;
using UnityEditor;
using UnityEngine;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// จัดตำแหน่งรูปป้อมให้พอดีจุดสร้างป้อม + ย้ายดาวเลเวล/หลอดก่อสร้างขึ้นไปเหนือหัวป้อม (ทั้ง 3 ป้อม)
    /// เมนู: Tools > NecroDefense > Updates > Fit Tower Art To Slot
    /// ตั้งค่าแบบ "ค่าตายตัว" (กดซ้ำได้ ไม่เลื่อนสะสม) - อยากได้ค่าอื่นแก้ตัวเลขด้านล่าง หรือปรับใน Prefab เองได้เลย
    /// </summary>
    public static class NecroUpdate_TowerLayout
    {
        // ความสูงของรูปป้อม (Visual_*) เทียบกับจุดสร้างป้อม: เดิม -0.45 / ยิ่งมากยิ่งสูง
        private const float ArtY = -0.30f;

        // (Prefab, ชื่อ Visual, ความสูงหัวป้อมที่สูงสุดทุกเลเวล เมื่อ Visual อยู่ที่ y = 0)
        private static readonly (string path, string visual, float topHeight)[] Towers =
        {
            ("Assets/Game/Prefabs/Towers/Tower_MagicOrb.prefab", "Visual_MagicOrb", 1.95f),
            ("Assets/Game/Prefabs/Towers/Tower_Explosive.prefab", "Visual_Explosive", 1.46f),
            ("Assets/Game/Prefabs/Towers/Tower_SoulBeam.prefab", "Visual_SoulBeam", 1.83f),
        };
        private const float PipsGap = 0.25f;       // ดาวเลเวลลอยเหนือหัวป้อมเท่านี้
        private const float BarGap = 0.3f;         // หลอดก่อสร้างอยู่เหนือดาวเท่านี้

        [MenuItem("Tools/NecroDefense/Updates/Fit Tower Art To Slot")]
        public static void Apply()
        {
            string log = "";
            foreach (var t in Towers)
            {
                if (!File.Exists(t.path)) { log += "ไม่เจอ " + t.path + "\n"; continue; }
                GameObject root = PrefabUtility.LoadPrefabContents(t.path);
                try
                {
                    Transform vis = root.transform.Find(t.visual);
                    if (vis == null) { log += Path.GetFileNameWithoutExtension(t.path) + ": ยังไม่มี " + t.visual + " (กด Apply ... Tower Art ก่อน)\n"; continue; }

                    SetY(vis, ArtY);
                    float top = ArtY + t.topHeight * vis.localScale.y;
                    float pipsY = top + PipsGap;
                    SetY(root.transform.Find("LevelPips"), pipsY);
                    SetY(root.transform.Find("ConstructionBar"), pipsY + BarGap);

                    PrefabUtility.SaveAsPrefabAsset(root, t.path);
                    log += Path.GetFileNameWithoutExtension(t.path) + ": รูป y = " + ArtY.ToString("0.##") + ", ดาว y = " + pipsY.ToString("0.##") + "\n";
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Debug.Log("[NecroDefense] Fit Tower Art To Slot\n" + log);
            EditorUtility.DisplayDialog("Fit Tower Art To Slot", log + "\nปรับเองต่อได้ที่ Prefab ป้อม > Visual_* > Position Y", "OK");
        }

        private static void SetY(Transform t, float y)
        {
            if (t == null) return;
            Vector3 p = t.localPosition;
            p.y = y;
            t.localPosition = p;
        }
    }
}
