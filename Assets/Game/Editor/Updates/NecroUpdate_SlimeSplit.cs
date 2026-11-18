using UnityEditor;
using UnityEngine;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// มินเนียน Slime แตกตัวตอนตายเหมือน Slime ศัตรู (ตัวเล็กตายหมด -> วิญญาณ Slime ใหญ่กลับเข้ากระเป๋า 1 ตัว)
    /// เมนู: Tools > NecroDefense > Updates > Setup Slime Minion Split
    /// - สร้าง Minion_SlimeSmall.prefab (ก็อปจาก Minion_Slime: ตัวเล็กลง / เลือดน้อยลง / ตีเบาลง / ไม่แตกต่อ)
    ///   ถ้ามีอยู่แล้วไม่สร้างทับ (ค่าที่ปรับไว้ไม่หาย)
    /// - ผูก Split Prefab ใน Minion_Slime (ค่าอื่นไม่แตะ)
    /// ปรับตัวเล็กได้ที่ Prefabs/Minions/Minion_SlimeSmall (ไม่ผูกไฟล์ Data - แก้ค่าใน Prefab ตรงๆ)
    /// </summary>
    public static class NecroUpdate_SlimeSplit
    {
        private const string BigPath = "Assets/Game/Prefabs/Minions/Minion_Slime.prefab";
        private const string SmallPath = "Assets/Game/Prefabs/Minions/Minion_SlimeSmall.prefab";
        private const float Scale = 0.6f, HealthMult = 0.35f, DamageMult = 0.5f;

        [MenuItem("Tools/NecroDefense/Updates/Setup Slime Minion Split")]
        public static void Apply()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(BigPath) == null)
            {
                EditorUtility.DisplayDialog("Slime Split", "ไม่เจอ " + BigPath, "OK");
                return;
            }

            string log = "";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SmallPath) == null)
            {
                AssetDatabase.CopyAsset(BigPath, SmallPath);
                AssetDatabase.ImportAsset(SmallPath);
                GameObject small = PrefabUtility.LoadPrefabContents(SmallPath);
                try
                {
                    MinionUnit m = small.GetComponent<MinionUnit>();
                    SerializedObject so = new SerializedObject(m);
                    MinionStats st = so.FindProperty("stats").objectReferenceValue as MinionStats;
                    float hp = st != null ? st.maxHealth : so.FindProperty("maxHealth").floatValue;
                    float dmg = st != null ? st.attackDamage : so.FindProperty("attackDamage").floatValue;
                    if (st != null) NecroDataTools.CopyMatching(new SerializedObject(st), m); // เอาค่าปัจจุบันจาก Data มาใส่ก่อน
                    so.Update();
                    so.FindProperty("stats").objectReferenceValue = null;           // ตัวเล็กไม่ใช้ Data ของตัวใหญ่
                    so.FindProperty("displayName").stringValue = "Mini Slime";
                    so.FindProperty("maxHealth").floatValue = Mathf.Max(1f, Mathf.Round(hp * HealthMult));
                    so.FindProperty("attackDamage").floatValue = Mathf.Max(1f, Mathf.Round(dmg * DamageMult));
                    so.FindProperty("bodyRadius").floatValue *= Scale;
                    so.FindProperty("splitPrefab").objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    small.transform.localScale = small.transform.localScale * Scale;
                    PrefabUtility.SaveAsPrefabAsset(small, SmallPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(small); }
                log += "สร้าง Minion_SlimeSmall (ตัวเล็ก x" + Scale + " / เลือด x" + HealthMult + " / ดาเมจ x" + DamageMult + ")\n";
            }
            else log += "มี Minion_SlimeSmall อยู่แล้ว (ไม่แตะ)\n";

            MinionUnit smallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SmallPath).GetComponent<MinionUnit>();
            GameObject big = PrefabUtility.LoadPrefabContents(BigPath);
            try
            {
                SerializedObject so = new SerializedObject(big.GetComponent<MinionUnit>());
                so.FindProperty("splitPrefab").objectReferenceValue = smallPrefab;
                if (so.FindProperty("splitCount").intValue <= 0) so.FindProperty("splitCount").intValue = 2;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(big, BigPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(big); }
            log += "Minion_Slime: ตายแล้วแตกเป็น Mini Slime 2 ตัว\n";

            AssetDatabase.SaveAssets();
            Debug.Log("[NecroDefense] Setup Slime Minion Split\n" + log);
            EditorUtility.DisplayDialog("Slime Split", log +
                "\nตัวเล็กตายครบ -> วิญญาณ Slime ใหญ่กลับเข้ากระเป๋า 1 ตัว\nปรับตัวเล็กได้ที่ " + SmallPath +
                "\nปรับจำนวนแตกที่ Minion_Slime > Split Count", "OK");
        }
    }
}
