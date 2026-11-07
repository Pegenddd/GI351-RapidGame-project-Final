using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// อัปเดต v2 แบบ "ไม่ทับของเดิม" (ใช้แทน Build Gameplay Scene)
    /// แก้เฉพาะจุดที่ระบบใหม่ต้องการ ของอื่นที่ผู้ใช้ปรับไว้ไม่แตะ
    /// กดซ้ำได้ ไม่เพิ่มซ้ำ
    /// </summary>
    public static class NecroUpdate_v2
    {
        private const string ScenePath = "Assets/Scenes/Gameplay.unity";
        private const string CardDir = "Assets/Game/Data/UpgradeCards";
        private const string IconDir = "Assets/Game/Art/Placeholder";

        [MenuItem("Tools/NecroDefense/Update v2 - Nova Skill, 3 Commands, Army Caps")]
        public static void Run()
        {
            if (!EditorUtility.DisplayDialog("NecroDefense Update v2",
                    "จะแก้เฉพาะจุดเหล่านี้ (ของอื่นที่คุณปรับไว้ไม่แตะ):\n\n" +
                    "• ลบการ์ด Unlock Enigma / Unlock Shockwave / Shockwave Boost\n" +
                    "• เปลี่ยน Enigma Boost -> Nova Boost, เพิ่มการ์ด Legion\n" +
                    "• ศพหายใน 10 วิ (Corpse prefab)\n" +
                    "• ซีน Gameplay: ลบช่องสกิล R, ลบปุ่มคำสั่งเก่า 2 ปุ่ม, ตั้งจำนวนกองทัพสูงสุดต่อชนิด\n\n" +
                    "แนะนำให้ commit git ก่อนกด", "Apply", "Cancel"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            List<string> log = new List<string>();
            UpgradeCard legion = UpdateCards(log);
            UpdateCorpsePrefab(log);
            UpdateScene(legion, log);
            AssetDatabase.SaveAssets();

            string summary = string.Join("\n", log);
            Debug.Log("<b>[NecroDefense] Update v2</b>\n" + summary);
            EditorUtility.DisplayDialog("Update v2 done", summary, "OK");
        }

        // ---------------------------------------------------------------- Cards

        private static UpgradeCard UpdateCards(List<string> log)
        {
            foreach (string old in new[] { "Card_UnlockEnigma", "Card_UnlockShockwave", "Card_ShockwaveBoost" })
            {
                string path = CardDir + "/" + old + ".asset";
                if (AssetDatabase.LoadAssetAtPath<UpgradeCard>(path) != null && AssetDatabase.DeleteAsset(path))
                    log.Add("Deleted card " + old);
            }

            string enigmaPath = CardDir + "/Card_EnigmaBoost.asset";
            UpgradeCard nova = AssetDatabase.LoadAssetAtPath<UpgradeCard>(enigmaPath);
            if (nova != null)
            {
                nova.title = "Nova Boost";
                nova.effect = CardEffect.NovaBoost;
                nova.value1 = 0.2f;
                nova.value2 = 0.2f;
                nova.minLevel = 1;
                nova.requires = null;
                nova.description = "Arcane Nova (E): +20% radius, +20% damage.";
                EditorUtility.SetDirty(nova);
                AssetDatabase.RenameAsset(enigmaPath, "Card_NovaBoost");
                log.Add("Enigma Boost -> Nova Boost");
            }

            string legionPath = CardDir + "/Card_Legion.asset";
            UpgradeCard legion = AssetDatabase.LoadAssetAtPath<UpgradeCard>(legionPath);
            if (legion == null)
            {
                legion = ScriptableObject.CreateInstance<UpgradeCard>();
                legion.title = "Legion";
                legion.category = CardCategory.Minion;
                legion.effect = CardEffect.ArmySize;
                legion.value1 = 1f;
                legion.maxStacks = 5;
                legion.minLevel = 1;
                legion.cardColor = new Color(0.4f, 1f, 0.5f);
                legion.description = "+1 max army size for EVERY minion type.";
                AssetDatabase.CreateAsset(legion, legionPath);
                log.Add("Created card Legion");
            }
            return legion;
        }

        // ---------------------------------------------------------------- Corpse

        private static void UpdateCorpsePrefab(List<string> log)
        {
            const string path = "Assets/Game/Prefabs/Pickups/Corpse.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            Corpse c = root.GetComponent<Corpse>();
            if (c != null)
            {
                new SO(c).Set("lifetime", 10f).Apply();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                log.Add("Corpse lifetime = 10s");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        // ---------------------------------------------------------------- Scene

        private static T Find<T>(Scene scene) where T : Component
        {
            foreach (GameObject g in scene.GetRootGameObjects())
            {
                T t = g.GetComponentInChildren<T>(true);
                if (t != null) return t;
            }
            return null;
        }

        private static void UpdateScene(UpgradeCard legion, List<string> log)
        {
            if (!System.IO.File.Exists(ScenePath)) { log.Add("Scene not found: " + ScenePath); return; }
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // ----- Minion Manager: คำสั่งเริ่มต้น + จำนวนสูงสุดต่อชนิด -----
            MinionManager mm = Find<MinionManager>(scene);
            if (mm != null)
            {
                SerializedObject so = new SerializedObject(mm);
                so.FindProperty("defaultCommand").intValue = (int)MinionCommand.SelfDefense;
                SerializedProperty bag = so.FindProperty("bag");
                for (int i = 0; i < bag.arraySize; i++)
                {
                    SerializedProperty e = bag.GetArrayElementAtIndex(i);
                    Object prefab = e.FindPropertyRelative("minionPrefab").objectReferenceValue;
                    string n = prefab != null ? prefab.name : "";
                    int cap = n.Contains("Slime") ? 10 : n.Contains("Goblin") ? 6 : n.Contains("Skeleton") ? 3 : n.Contains("Vulture") ? 3 : n.Contains("Giant") ? 1 : 5;
                    e.FindPropertyRelative("maxOwned").intValue = cap;
                    log.Add("Army cap " + n + " = " + cap);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // ----- Card pool: ลบช่องว่าง + เพิ่ม Legion -----
            LevelUpManager lvm = Find<LevelUpManager>(scene);
            if (lvm != null)
            {
                SerializedObject so = new SerializedObject(lvm);
                SerializedProperty pool = so.FindProperty("cardPool");
                List<Object> cards = new List<Object>();
                for (int i = 0; i < pool.arraySize; i++)
                {
                    Object c = pool.GetArrayElementAtIndex(i).objectReferenceValue;
                    if (c != null && !cards.Contains(c)) cards.Add(c);
                }
                if (legion != null && !cards.Contains(legion)) { cards.Add(legion); log.Add("Card pool + Legion"); }
                pool.arraySize = cards.Count;
                for (int i = 0; i < cards.Count; i++) pool.GetArrayElementAtIndex(i).objectReferenceValue = cards[i];
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // ----- Player skills: ให้แน่ใจว่ามีเอฟเฟกต์ Nova -----
            PlayerSkills ps = Find<PlayerSkills>(scene);
            if (ps != null)
            {
                SerializedObject so = new SerializedObject(ps);
                SerializedProperty fx = so.FindProperty("novaEffect");
                if (fx != null && fx.objectReferenceValue == null)
                {
                    RingEffect shock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Effects/FX_Shockwave.prefab")?.GetComponent<RingEffect>();
                    fx.objectReferenceValue = shock;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // ----- Skill bar: ลบช่องที่ไม่ใช้แล้ว (R) + ไอคอน E ใหม่ -----
            SkillBarUI bar = Find<SkillBarUI>(scene);
            if (bar != null)
            {
                SerializedObject so = new SerializedObject(bar);
                HashSet<Object> keep = new HashSet<Object>
                {
                    so.FindProperty("raiseDeadSlot").objectReferenceValue,
                    so.FindProperty("teleportSlot").objectReferenceValue,
                    so.FindProperty("novaSlot").objectReferenceValue,
                };
                foreach (SkillSlotUI slot in bar.GetComponentsInChildren<SkillSlotUI>(true).ToArray())
                {
                    if (keep.Contains(slot)) continue;
                    log.Add("Removed skill slot " + slot.name);
                    Object.DestroyImmediate(slot.gameObject);
                }

                SkillSlotUI nova = so.FindProperty("novaSlot").objectReferenceValue as SkillSlotUI;
                if (nova != null && nova.name != "Skill_E_ArcaneNova")
                {
                    nova.name = "Skill_E_ArcaneNova";
                    Image icon = nova.Icon;
                    Sprite star = AssetDatabase.LoadAssetAtPath<Sprite>(IconDir + "/Icon_Star.png");
                    if (icon != null && star != null) { icon.sprite = star; icon.color = new Color(1f, 0.65f, 0.3f); }
                    log.Add("E slot -> Arcane Nova");
                }

                // แผงเดิมกว้าง 4 ช่อง -> หดเหลือ 3 ช่อง (เฉพาะถ้ายังเป็นขนาดเดิม)
                RectTransform rt = bar.transform as RectTransform;
                if (rt != null && Mathf.Abs(rt.sizeDelta.x - 448f) < 1f)
                    rt.sizeDelta = new Vector2(340f, rt.sizeDelta.y);
            }

            // ----- Command ring: เหลือ Hold Area / Move / Follow -----
            CommandPanelUI cmd = Find<CommandPanelUI>(scene);
            if (cmd != null)
            {
                SerializedObject so = new SerializedObject(cmd);
                RingOptionUI hold = so.FindProperty("holdAreaOption").objectReferenceValue as RingOptionUI;
                RingOptionUI move = so.FindProperty("moveOption").objectReferenceValue as RingOptionUI;
                RingOptionUI follow = so.FindProperty("followPlayerOption").objectReferenceValue as RingOptionUI;
                HashSet<Object> keep = new HashSet<Object> { hold, move, follow };
                foreach (RingOptionUI o in cmd.GetComponentsInChildren<RingOptionUI>(true).ToArray())
                {
                    if (keep.Contains(o)) continue;
                    log.Add("Removed command button " + o.name);
                    Object.DestroyImmediate(o.gameObject);
                }

                // ย้ายตำแหน่งเฉพาะปุ่มที่ยังอยู่ตำแหน่งเดิมที่ผมวางไว้ (ถ้าคุณย้ายเองแล้วจะไม่แตะ)
                MoveIfDefault(hold, new Vector2(72f, 95f), new Vector2(-95f, 45f));
                MoveIfDefault(move, new Vector2(-72f, 95f), new Vector2(0f, 100f));
                MoveIfDefault(follow, new Vector2(125f, 25f), new Vector2(95f, 45f));

                if (hold != null)
                {
                    hold.name = "Cmd_HoldArea";
                    SetTooltip(hold, "Hold Area (default)", "Guard this area: attack enemies that enter its sight range, then come back.");
                }
                if (move != null) SetTooltip(move, "Move", "Click a spot on the field. The minion walks there (ignoring enemies), then guards that area.");
            }

            // ----- Help tooltip -----
            foreach (TooltipTrigger t in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TooltipTrigger>(true)))
            {
                SerializedObject so = new SerializedObject(t);
                if (so.FindProperty("title").stringValue != "Controls") continue;
                SerializedProperty body = so.FindProperty("body");
                body.stringValue = body.stringValue.Replace("E / R: Skills (unlock Lv5+)", "E: Arcane Nova").Replace("E / R: Skills", "E: Arcane Nova");
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log.Add("Scene saved");
        }

        private static void MoveIfDefault(RingOptionUI o, Vector2 oldPos, Vector2 newPos)
        {
            if (o == null) return;
            RectTransform rt = (RectTransform)o.transform;
            if (Vector2.Distance(rt.anchoredPosition, oldPos) < 1f) rt.anchoredPosition = newPos;
        }

        private static void SetTooltip(Component c, string title, string body)
        {
            TooltipTrigger t = c.GetComponent<TooltipTrigger>();
            if (t == null) return;
            SerializedObject so = new SerializedObject(t);
            so.FindProperty("title").stringValue = title;
            so.FindProperty("body").stringValue = body;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
