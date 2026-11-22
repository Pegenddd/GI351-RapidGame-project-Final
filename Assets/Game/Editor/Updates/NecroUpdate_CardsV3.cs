using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// เปลี่ยนชุดการ์ดอัปเกรดเป็นชุดใหม่ 9 ใบ
    /// เมนู: Tools > NecroDefense > Updates > Apply Upgrade Cards v3
    /// - แก้ / สร้างไฟล์การ์ดใน Data/UpgradeCards (ชื่อ คำอธิบาย ผล ค่า จำนวนใบ)
    /// - การ์ดที่ไม่ใช้แล้ว ย้ายไป Data/UpgradeCards/_Unused (ไม่ลบ ย้ายกลับได้)
    /// - ใส่การ์ดชุดใหม่ใน LevelUpManager > Card Pool ของซีน Gameplay / Map2 / TestArena
    /// - ป้อมหลัก (Crystal) ในซีนเหล่านั้น: ฮีล 10% ทุก 45 วิ (ผู้เล่น + มินเนียน + ตัวเอง)
    /// กดซ้ำได้ (ค่าการ์ดจะถูกตั้งกลับเป็นค่าในตารางด้านล่าง)
    /// </summary>
    public static class NecroUpdate_CardsV3
    {
        private const string CardDir = "Assets/Game/Data/UpgradeCards";
        private const string UnusedDir = CardDir + "/_Unused";
        private static readonly string[] Scenes = { "Assets/Scenes/Gameplay.unity", "Assets/Scenes/Map2.unity", "Assets/Scenes/TestArena.unity" };

        private static readonly string[] Unused =
        {
            "Card_NecromancyMastery", "Card_RaiseRadius", "Card_MinionPower", "Card_MinionHaste", "Card_MinionVitality", "Card_SwiftBoots",
        };

        private static readonly Color PlayerColor = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color MinionColor = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color SupportColor = new Color(0.85f, 0.2f, 0.2f);

        private struct Def
        {
            public string file, title, desc; public CardCategory cat; public CardEffect fx; public float v1, v2; public int max; public Color color;
            public Def(string file, string title, CardCategory cat, CardEffect fx, float v1, float v2, int max, string desc, Color color)
            { this.file = file; this.title = title; this.cat = cat; this.fx = fx; this.v1 = v1; this.v2 = v2; this.max = max; this.desc = desc; this.color = color; }
        }

        private static readonly Def[] Cards =
        {
            new Def("Card_MinionUpgrade", "Minion Upgrade", CardCategory.Minion, CardEffect.MinionUpgrade, 0.1f, 0.1f, 0,
                "All minions +10% ATK, +10% Max HP. (No limit)", MinionColor),
            new Def("Card_TeleportMastery", "Teleport Mastery", CardCategory.Player, CardEffect.TeleportMastery, 0.1f, 0f, 5,
                "Teleport cooldown -10%.", PlayerColor),
            new Def("Card_NovaBoost", "Nova Boost", CardCategory.Player, CardEffect.NovaBoost, 0.1f, 0.1f, 5,
                "Arcane Nova (E): +10% radius, +10% damage.", PlayerColor),
            new Def("Card_Legion", "Legion", CardCategory.Minion, CardEffect.ArmySize, 1f, 0f, 5,
                "+1 max army size for EVERY minion type.", MinionColor),
            new Def("Card_SoulHarvester", "Soul Harvester", CardCategory.Support, CardEffect.SoulHarvester, 0.1f, 0f, 3,
                "Tower build & upgrade cost -10%.", SupportColor),
            new Def("Card_ManaFont", "Soul Font", CardCategory.Support, CardEffect.SoulFontDrainPercent, 20f, 0.2f, 5,
                "+20 Max Soul, Soul drains 20% slower.", SupportColor),
            new Def("Card_BaseArchitect", "Base Architect", CardCategory.Support, CardEffect.BaseHealFaster, 5f, 0f, 3,
                "Main Base heals you, your minions and itself 5 sec sooner.\n(10% HP every 45 sec)", SupportColor),
            new Def("Card_RaiseCooldown", "Arise: Faster", CardCategory.Player, CardEffect.RaiseCooldown, 0.15f, 0f, 5,
                "Mass Raise (Q) cooldown -15%.", PlayerColor),
            new Def("Card_OrbitalMagic", "Orbital Magic", CardCategory.Player, CardEffect.OrbitalMagic, 1f, 0.2f, 3,
                "+1 orbiting orb, +20% orb damage.", PlayerColor),
        };

        [MenuItem("Tools/NecroDefense/Updates/Apply Upgrade Cards v3")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            NecroDataTools.EnsureFolder(CardDir);
            string log = "";

            // ----- การ์ดที่ไม่ใช้: ย้ายไป _Unused -----
            int moved = 0;
            foreach (string name in Unused)
            {
                string from = CardDir + "/" + name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<UpgradeCard>(from) == null) continue;
                NecroDataTools.EnsureFolder(UnusedDir);
                string to = UnusedDir + "/" + name + ".asset";
                if (AssetDatabase.LoadAssetAtPath<Object>(to) != null) to = AssetDatabase.GenerateUniqueAssetPath(to);
                string err = AssetDatabase.MoveAsset(from, to);
                if (string.IsNullOrEmpty(err)) moved++; else Debug.LogWarning("[NecroDefense] ย้าย " + from + " ไม่ได้: " + err);
            }
            log += "การ์ดที่เลิกใช้ ย้ายไป _Unused: " + moved + " ใบ\n";

            // ----- การ์ดชุดใหม่ -----
            List<UpgradeCard> pool = new List<UpgradeCard>();
            int created = 0;
            foreach (Def d in Cards)
            {
                string path = CardDir + "/" + d.file + ".asset";
                UpgradeCard c = AssetDatabase.LoadAssetAtPath<UpgradeCard>(path);
                if (c == null)
                {
                    c = ScriptableObject.CreateInstance<UpgradeCard>();
                    c.cardColor = d.color; // การ์ดเดิมใช้สีเดิม
                    AssetDatabase.CreateAsset(c, path);
                    created++;
                }
                c.title = d.title;
                c.category = d.cat;
                c.description = d.desc;
                c.effect = d.fx;
                c.value1 = d.v1;
                c.value2 = d.v2;
                c.maxStacks = d.max;
                c.minLevel = 1;
                c.requires = null;
                EditorUtility.SetDirty(c);
                pool.Add(c);
            }
            AssetDatabase.SaveAssets();
            log += "การ์ดชุดใหม่ " + pool.Count + " ใบ (สร้างใหม่ " + created + ")\n";

            // ----- ใส่ในซีน -----
            string start = SceneManager.GetActiveScene().path;
            foreach (string scenePath in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null) continue;
                Scene s = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                string label = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                LevelUpManager lm = Object.FindAnyObjectByType<LevelUpManager>(FindObjectsInactive.Include);
                if (lm != null)
                {
                    SerializedObject so = new SerializedObject(lm);
                    SerializedProperty list = so.FindProperty("cardPool");
                    list.arraySize = pool.Count;
                    for (int i = 0; i < pool.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = pool[i];
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                Crystal crystal = Object.FindAnyObjectByType<Crystal>(FindObjectsInactive.Include);
                if (crystal != null)
                {
                    SerializedObject cso = new SerializedObject(crystal);
                    cso.FindProperty("healInterval").floatValue = 45f;
                    cso.FindProperty("healPercent").floatValue = 0.1f;
                    cso.FindProperty("healSelf").boolValue = true;
                    cso.ApplyModifiedPropertiesWithoutUndo();
                }

                EditorSceneManager.MarkSceneDirty(s);
                EditorSceneManager.SaveScene(s);
                log += label + ": Card Pool " + (lm != null ? "OK" : "(ไม่เจอ LevelUpManager)") +
                       " / ป้อมหลักฮีล 10% ทุก 45 วิ " + (crystal != null ? "OK" : "(ไม่เจอ)") + "\n";
            }
            if (!string.IsNullOrEmpty(start)) EditorSceneManager.OpenScene(start, OpenSceneMode.Single);

            Debug.Log("[NecroDefense] Apply Upgrade Cards v3\n" + log);
            EditorUtility.DisplayDialog("Upgrade Cards v3", log + "\nปรับค่าการ์ดได้ที่ " + CardDir, "OK");
        }
    }
}
