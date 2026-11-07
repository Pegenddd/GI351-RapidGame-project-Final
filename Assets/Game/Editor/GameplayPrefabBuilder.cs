using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// สร้าง Prefab ทั้งหมด (ศัตรู / มินเนียน / ป้อม / กระสุน / เอฟเฟกต์) และการ์ดเลเวลอัป
    /// Overwrite = false : ถ้ามีไฟล์อยู่แล้วจะไม่ทับ (แก้ค่าใน Prefab ไปแล้วจะไม่หาย)
    /// Overwrite = true  : สร้าง Prefab ใหม่ทั้งหมด (ค่ากลับเป็นค่าเริ่มต้น)
    /// รูปตัวละครจะดึงจาก Assets/picture/&lt;ชื่อ&gt;.pt.png อัตโนมัติ (ไม่มีรูป = ใช้รูปทรง placeholder)
    /// </summary>
    internal class GameplayPrefabBuilder
    {
        public const string PrefabDir = "Assets/Game/Prefabs";
        public const string CardDir = "Assets/Game/Data/UpgradeCards";

        // ---- ผลลัพธ์ให้ Scene builder ใช้ต่อ ----
        public Projectile PlayerBolt, EnemyArrow, MinionArrow, MagicOrbShot, ExplosiveShot;
        public RingEffect FxTeleport, FxEnigma, FxShockwave, FxExplosion, FxSummon, FxHeal, FxRaise, FxSlam, FxSacrifice, FxMassRaise, FxRaiseFail, FxCast;
        public BeamEffect FxSoulBeam;
        public DamageZone FxPoisonPool;
        public Corpse CorpsePrefab;
        public XpOrb XpOrbPrefab;
        public GameObject OrbitOrb;
        public MinionUnit MinionSlime, MinionGoblin, MinionSkeleton, MinionVulture, MinionGiant;
        public EnemyUnit EnemySlimeSmall, EnemySlime, EnemyGoblin, EnemyVulture, EnemyGiant, EnemyArcher;
        public Tower TowerMagicOrb, TowerExplosive, TowerSoulBeam;
        public UpgradeCard[] Cards;

        private static readonly Color MinionTint = new Color(0.45f, 1f, 0.55f);

        /// <summary>true = ทับ Prefab ที่มีอยู่</summary>
        public static bool Overwrite;

        public void BuildAll()
        {
            foreach (string d in new[] { "Effects", "Projectiles", "Pickups", "Minions", "Enemies", "Towers", "Player" })
                BU.EnsureFolder(PrefabDir + "/" + d);
            BU.EnsureFolder(CardDir);

            BuildEffects();
            BuildProjectiles();
            BuildPickups();
            BuildMinions();
            BuildEnemies();
            BuildTowers();
            BuildCards();
            AssetDatabase.SaveAssets();
        }

        // ==================================================================
        // Generic prefab helper
        // ==================================================================

        private static T Prefab<T>(string relativePath, Func<GameObject> build) where T : Component
        {
            string path = PrefabDir + "/" + relativePath + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null && !Overwrite) return existing.GetComponent<T>();

            GameObject go = build();
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset.GetComponent<T>();
        }

        // ==================================================================
        // Effects
        // ==================================================================

        private void BuildEffects()
        {
            FxTeleport = Ring("Effects/FX_Teleport", new Color(0.7f, 0.4f, 1f, 0.9f), 0.3f);
            FxEnigma = Ring("Effects/FX_EnigmaWave", new Color(0.55f, 0.3f, 1f, 0.85f), 0.45f);
            FxShockwave = Ring("Effects/FX_Shockwave", new Color(1f, 0.95f, 0.8f, 0.95f), 0.35f);
            FxExplosion = Ring("Effects/FX_Explosion", new Color(1f, 0.55f, 0.15f, 0.9f), 0.25f);
            FxSummon = Ring("Effects/FX_Summon", new Color(0.4f, 1f, 0.5f, 0.9f), 0.3f);
            FxHeal = Ring("Effects/FX_CrystalHeal", new Color(0.5f, 1f, 0.7f, 0.7f), 0.8f);
            FxRaise = Ring("Effects/FX_RaiseDead", new Color(0.3f, 1f, 0.4f, 1f), 0.35f);
            FxSlam = Ring("Effects/FX_GroundSlam", new Color(0.8f, 0.6f, 0.4f, 0.9f), 0.3f);
            FxSacrifice = Ring("Effects/FX_Sacrifice", new Color(1f, 0.5f, 0.8f, 0.9f), 0.5f);
            FxMassRaise = Ring("Effects/FX_MassRaise", new Color(0.4f, 1f, 0.55f, 0.9f), 0.6f);
            FxRaiseFail = Ring("Effects/FX_RaiseFail", new Color(0.55f, 0.55f, 0.55f, 0.8f), 0.3f);
            FxCast = Ring("Effects/FX_CastStart", new Color(0.75f, 0.5f, 1f, 0.8f), 0.4f);

            FxSoulBeam = Prefab<BeamEffect>("Effects/FX_SoulBeam", () =>
            {
                GameObject go = BU.Go("FX_SoulBeam");
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = BU.Square;
                sr.color = new Color(0.4f, 1f, 1f, 0.9f);
                sr.sortingOrder = 12;
                BeamEffect b = go.AddComponent<BeamEffect>();
                new SO(b).Set("spriteRenderer", sr).Set("width", 0.15f).Set("duration", 0.15f).Apply();
                return go;
            });

            FxPoisonPool = Prefab<DamageZone>("Effects/FX_PoisonPool", () =>
            {
                GameObject go = BU.Go("FX_PoisonPool");
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = BU.Circle;
                sr.color = new Color(0.4f, 1f, 0.2f, 0.35f);
                sr.sortingOrder = -4;
                DamageZone z = go.AddComponent<DamageZone>();
                new SO(z).Set("team", Team.Player).Set("radius", 1.5f).Set("damagePerSecond", 8f)
                    .Set("duration", 3f).Set("canHitFlying", false).Apply();
                return go;
            });
        }

        private static RingEffect Ring(string path, Color color, float duration)
        {
            return Prefab<RingEffect>(path, () =>
            {
                GameObject go = BU.Go(System.IO.Path.GetFileName(path));
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = BU.Ring;
                sr.color = color;
                sr.sortingOrder = 12;
                RingEffect r = go.AddComponent<RingEffect>();
                new SO(r).Set("spriteRenderer", sr).Set("duration", duration).Apply();
                return go;
            });
        }

        // ==================================================================
        // Projectiles
        // ==================================================================

        private void BuildProjectiles()
        {
            PlayerBolt = Proj("Projectiles/Proj_PlayerBolt", BU.Circle, new Color(0.75f, 0.45f, 1f), 0.35f, 12f, true, 0f, null);
            EnemyArrow = Proj("Projectiles/Proj_EnemyArrow", BU.Diamond, new Color(1f, 0.4f, 0.4f), 0.28f, 9f, true, 0f, null);
            MinionArrow = Proj("Projectiles/Proj_MinionArrow", BU.Diamond, MinionTint, 0.28f, 10f, true, 0f, null);
            MagicOrbShot = Proj("Projectiles/Proj_MagicOrb", BU.Circle, new Color(0.4f, 0.7f, 1f), 0.3f, 11f, true, 0f, null);
            ExplosiveShot = Proj("Projectiles/Proj_Explosive", BU.Circle, new Color(1f, 0.55f, 0.15f), 0.4f, 7f, true, 1.2f, FxExplosion);
        }

        private static Projectile Proj(string path, Sprite sprite, Color color, float size, float speed, bool homing, float area, RingEffect impact)
        {
            return Prefab<Projectile>(path, () =>
            {
                GameObject go = BU.Go(System.IO.Path.GetFileName(path));
                go.transform.localScale = Vector3.one * size;
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = color;
                sr.sortingOrder = 10;
                Projectile p = go.AddComponent<Projectile>();
                new SO(p).Set("speed", speed).Set("homing", homing).Set("hitRadius", 0.2f).Set("lifetime", 3f)
                    .Set("areaRadius", area).Set("impactEffect", impact).Apply();
                return go;
            });
        }

        // ==================================================================
        // Pickups
        // ==================================================================

        private void BuildPickups()
        {
            CorpsePrefab = Prefab<Corpse>("Pickups/Corpse", () =>
            {
                GameObject go = BU.Go("Corpse");
                BU.Sprite("Shadow", go.transform, BU.Circle, new Color(0f, 0f, 0f, 0.35f), new Vector2(0.8f, 0.3f), -4, new Vector3(0f, -0.2f, 0f));
                SpriteRenderer sr = BU.Sprite("Body", go.transform, BU.Skull, new Color(0.75f, 0.72f, 0.68f), new Vector2(0.55f, 0.55f), -3);
                Corpse c = go.AddComponent<Corpse>();
                new SO(c).Set("lifetime", 15f).Set("blinkTime", 3f).Set("spriteRenderer", sr).Apply();
                return go;
            });

            XpOrbPrefab = Prefab<XpOrb>("Pickups/XpOrb", () =>
            {
                GameObject go = BU.Go("XpOrb");
                go.transform.localScale = Vector3.one * 0.28f;
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = BU.Diamond;
                sr.color = new Color(0.4f, 1f, 1f);
                sr.sortingOrder = -2;
                go.AddComponent<XpOrb>();
                return go;
            });

            OrbitOrb = Prefab<Transform>("Player/Player_OrbitOrb", () =>
            {
                GameObject go = BU.Go("Player_OrbitOrb");
                go.transform.localScale = Vector3.one * 0.32f;
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = BU.Circle;
                sr.color = new Color(0.8f, 0.55f, 1f);
                sr.sortingOrder = 6;
                return go;
            }).gameObject;
        }

        // ==================================================================
        // Units
        // ==================================================================

        private class UnitSpec
        {
            public string name, label;
            public Sprite sprite;
            public Sprite art;   // รูปจริงจาก Assets/picture (ถ้ามี)
            public Color color;
            public float size = 0.6f, radius = 0.3f, mass = 1f;
            public bool flying;
            public float hp, atk, range = 0.3f, interval = 1f, speed = 2f, area = 1.5f;
            public AttackStyle style = AttackStyle.Melee;
            public Projectile projectile;
            public RingEffect areaFx;
            public float sight = 5f;                              // ระยะมองหาศัตรู (มินเนียน)
            public Color outline = new Color(0f, 0f, 0f, 0f);   // สีเส้นขอบ (ทำให้ภาพชัด)
            public Color footRing = new Color(0f, 0f, 0f, 0f);  // วงใต้เท้า
        }

        /// <summary>สร้าง GameObject ยูนิต: Rigidbody2D + Collider + Body + Label + HealthBar แล้วใส่ค่า CombatUnit</summary>
        private static GameObject BuildUnitObject<T>(UnitSpec s, out T unit) where T : CombatUnit
        {
            GameObject go = BU.Go(s.name);
            BU.Body2D(go, s.radius, s.flying, s.mass);
            BU.Sprite("Shadow", go.transform, BU.Circle, new Color(0f, 0f, 0f, 0.3f), new Vector2(s.size * 0.7f, s.size * 0.22f), -2,
                new Vector3(0f, s.flying ? -s.size * 0.75f : -s.size * 0.45f, 0f));
            if (s.footRing.a > 0f)
                BU.Sprite("FootRing", go.transform, BU.Ring, s.footRing, new Vector2(s.size * 0.95f, s.size * 0.38f), -1,
                    new Vector3(0f, s.flying ? -s.size * 0.75f : -s.size * 0.45f, 0f));

            // Visual = Body + เส้นขอบ 8 ทิศ (กลับซ้าย-ขวาทั้งกลุ่ม)
            Transform visual = BU.Go("Visual", go.transform).transform;
            Sprite spr = s.art != null ? s.art : s.sprite;
            float scale = s.art != null ? BU.FitScale(s.art, s.size) : s.size;
            int order = s.flying ? 3 : 1;
            if (s.outline.a > 0f)
            {
                Transform ol = BU.Go("Outline", visual).transform;
                const float w = 0.05f;
                for (int k = 0; k < 8; k++)
                {
                    float a = k * Mathf.PI / 4f;
                    BU.Sprite("Outline_" + k, ol, spr, s.outline, Vector2.one * scale, order - 1, new Vector3(Mathf.Cos(a) * w, Mathf.Sin(a) * w, 0f));
                }
            }
            SpriteRenderer body = BU.Sprite("Body", visual, spr, s.art != null ? Color.white : s.color, Vector2.one * scale, order);
            WorldHealthBar hb = BU.HealthBar(go.transform, s.size * 0.55f + 0.1f, Mathf.Max(0.6f, s.size * 0.8f), true);
            unit = go.AddComponent<T>();
            new SO(unit)
                .Set("hitFlashColor", new Color(1f, 0.45f, 0.45f, 0.45f))
                .Set("maxHealth", s.hp).Set("bodyRadius", s.radius).Set("isFlying", s.flying)
                .Set("bodyRenderer", body).Set("visualRoot", visual).Set("healthBar", hb)
                .Set("moveSpeed", s.speed).Set("attackStyle", s.style).Set("attackDamage", s.atk)
                .Set("attackRange", s.range).Set("attackInterval", s.interval).Set("areaRadius", s.area)
                .Set("projectilePrefab", s.projectile).Set("areaAttackEffect", s.areaFx)
                .Apply();
            return go;
        }

        private MinionUnit Minion(string file, UnitSpec s, string display, int mana, float sacrificePct)
        {
            s.outline = new Color(0.05f, 0.05f, 0.05f, 0.9f);
            return Prefab<MinionUnit>("Minions/" + file, () =>
            {
                GameObject go = BuildUnitObject(s, out MinionUnit m);
                SpriteRenderer ring = BU.Sprite("SelectionRing", go.transform, BU.Ring, new Color(1f, 0.85f, 0.2f, 0.95f),
                    new Vector2(s.size * 1.1f, s.size * 0.45f), 0, new Vector3(0f, -s.size * 0.45f, 0f));
                ring.gameObject.SetActive(false);
                // วงระยะมองหาศัตรู (โชว์ตอนเลือกมินเนียน, ขนาดปรับอัตโนมัติตาม Aggro Range)
                SpriteRenderer sight = BU.Sprite("DetectionRing", go.transform, BU.Ring, new Color(0.35f, 0.9f, 1f, 0.55f), Vector2.one, -3);
                sight.gameObject.SetActive(false);
                new SO(m).Set("detectionRing", sight.transform).Set("aggroRange", s.sight).Set("displayName", display).Set("icon", s.art).Set("manaCost", mana)
                    .Set("sacrificeHealPercent", sacrificePct).Set("selectionRing", ring.gameObject).Apply();
                return go;
            });
        }

        private void BuildMinions()
        {
            // ค่าตามตาราง GDD (Mana / HP / ATK)
            MinionSlime = Minion("Minion_Slime", new UnitSpec
            {
                name = "Minion_Slime", label = "Slime (ally)", sprite = BU.Circle, color = new Color(0.4f, 1f, 0.5f), art = BU.Art("Minion_slime"),
                size = 0.85f, radius = 0.3f, hp = 30, atk = 5, interval = 1f, speed = 2.2f
            }, "Slime", 10, 5f);

            MinionGoblin = Minion("Minion_Goblin", new UnitSpec
            {
                name = "Minion_Goblin", label = "Goblin (ally)", sprite = BU.Triangle, color = new Color(0.55f, 0.95f, 0.35f), art = BU.Art("Minion_Goblin"),
                size = 1f, radius = 0.28f, hp = 50, atk = 12, interval = 0.8f, speed = 3.2f
            }, "Goblin", 20, 5f);

            MinionSkeleton = Minion("Minion_Skeleton", new UnitSpec
            {
                name = "Minion_Skeleton", label = "Skeleton (ally)", sprite = BU.Diamond, color = new Color(0.8f, 1f, 0.8f), art = BU.Art("Minion_SkeletonArcher"),
                size = 1.15f, radius = 0.3f, hp = 40, atk = 15, interval = 1.2f, speed = 2.2f,
                style = AttackStyle.Ranged, range = 4.5f, projectile = MinionArrow, sight = 6.5f
            }, "Skeleton", 25, 10f);

            MinionVulture = Minion("Minion_Vulture", new UnitSpec
            {
                name = "Minion_Vulture", label = "Vulture (ally)", sprite = BU.Triangle, color = new Color(0.55f, 1f, 0.9f), art = BU.Art("Minion_Vulture"),
                size = 1.3f, radius = 0.3f, flying = true, hp = 60, atk = 10, interval = 0.9f, speed = 3.5f
            }, "Vulture", 35, 10f);

            MinionGiant = Minion("Minion_Giant", new UnitSpec
            {
                name = "Minion_Giant", label = "Giant (ally)", sprite = BU.Square, color = new Color(0.3f, 0.8f, 0.4f), art = BU.Art("Minion_Giant"),
                size = 1.8f, radius = 0.55f, mass = 8f, hp = 200, atk = 25, interval = 1.6f, speed = 1.4f,
                style = AttackStyle.MeleeArea, area = 1.6f, areaFx = FxSlam
            }, "Giant", 60, 20f);
        }

        /// <summary>ติ๊กเป้าหมายของศัตรู</summary>
        private class Aim
        {
            public bool player = true, minions = true, mainBase = true, laneOnlyMinions = true, shootWhileWalking;
            public TargetPriority priority = TargetPriority.Nearest;
        }

        private EnemyUnit Enemy(string file, UnitSpec s, string display, Aim aim, float aggro,
            int souls, int xp, MinionUnit raiseInto, float raiseChance, EnemyUnit split = null, int splitCount = 0)
        {
            // ทำให้ศัตรูชัด: เส้นขอบขาว + วงแดงใต้เท้า
            s.outline = new Color(1f, 1f, 1f, 0.95f);
            s.footRing = new Color(1f, 0.2f, 0.2f, 0.75f);
            return Prefab<EnemyUnit>("Enemies/" + file, () =>
            {
                GameObject go = BuildUnitObject(s, out EnemyUnit e);
                go.tag = "Untagged";
                new SO(e).Set("displayName", display).Set("aggroRange", aggro)
                    .Set("attackPlayer", aim.player).Set("attackMinions", aim.minions).Set("attackMainBase", aim.mainBase)
                    .Set("priority", aim.priority).Set("onlyMinionsOnLane", aim.laneOnlyMinions).Set("shootWhileWalking", aim.shootWhileWalking)
                    .Set("soulReward", souls).Set("xpReward", xp).Set("xpOrbPrefab", XpOrbPrefab)
                    .Set("corpsePrefab", CorpsePrefab).Set("minionPrefab", raiseInto).Set("raiseChance", raiseChance)
                    .Set("splitPrefab", split).Set("splitCount", splitCount).Apply();
                return go;
            });
        }

        private void BuildEnemies()
        {
            EnemySlimeSmall = Enemy("Enemy_SlimeSmall", new UnitSpec
            {
                name = "Enemy_SlimeSmall", label = "Mini Slime", sprite = BU.Circle, color = new Color(1f, 0.55f, 0.55f), art = BU.Art("Enemy_slime"),
                size = 0.5f, radius = 0.2f, hp = 12, atk = 3, interval = 1f, speed = 1.6f
            }, "Mini Slime", new Aim(), 4f, 1, 1, MinionSlime, 0.3f);

            EnemySlime = Enemy("Enemy_Slime", new UnitSpec
            {
                name = "Enemy_Slime", label = "Slime", sprite = BU.Circle, color = new Color(1f, 0.35f, 0.35f), art = BU.Art("Enemy_slime"),
                size = 0.9f, radius = 0.35f, hp = 35, atk = 5, interval = 1f, speed = 1.2f
            }, "Slime", new Aim(), 4f, 2, 1, MinionSlime, 0.3f, EnemySlimeSmall, 2);

            EnemyGoblin = Enemy("Enemy_Goblin", new UnitSpec
            {
                name = "Enemy_Goblin", label = "Goblin", sprite = BU.Triangle, color = new Color(1f, 0.6f, 0.2f), art = BU.Art("Enemy_Goblin"),
                size = 1f, radius = 0.28f, hp = 28, atk = 6, interval = 0.8f, speed = 3f
            }, "Goblin", new Aim { player = false, priority = TargetPriority.MainBase }, 3f, 3, 2, MinionGoblin, 0.5f);

            EnemyVulture = Enemy("Enemy_Vulture", new UnitSpec
            {
                name = "Enemy_Vulture", label = "Vulture", sprite = BU.Triangle, color = new Color(0.95f, 0.3f, 0.65f), art = BU.Art("Enemy_Vulture"),
                size = 1.3f, radius = 0.3f, flying = true, hp = 30, atk = 6, interval = 0.9f, speed = 2.6f
            }, "Vulture", new Aim { mainBase = false, laneOnlyMinions = false, priority = TargetPriority.Player }, 6f, 3, 2, MinionVulture, 0.3f);

            EnemyGiant = Enemy("Enemy_Giant", new UnitSpec
            {
                name = "Enemy_Giant", label = "GIANT", sprite = BU.Square, color = new Color(0.75f, 0.2f, 0.2f), art = BU.Art("Enemy_giant"),
                size = 1.9f, radius = 0.6f, mass = 10f, hp = 260, atk = 25, interval = 1.8f, speed = 0.8f,
                style = AttackStyle.MeleeArea, area = 1.6f, areaFx = FxSlam
            }, "Giant", new Aim(), 4f, 12, 8, MinionGiant, 0.2f);

            EnemyArcher = Enemy("Enemy_SkeletonArcher", new UnitSpec
            {
                name = "Enemy_SkeletonArcher", label = "Skeleton Archer", sprite = BU.Diamond, color = new Color(1f, 0.85f, 0.85f), art = BU.Art("Enemy_SkeletonArcher"),
                size = 1.15f, radius = 0.3f, hp = 22, atk = 6, interval = 1.4f, speed = 1.6f,
                style = AttackStyle.Ranged, range = 5f, projectile = EnemyArrow
            }, "Skeleton Archer", new Aim { shootWhileWalking = true }, 6f, 3, 2, MinionSkeleton, 0.3f);
        }

        // ==================================================================
        // Towers
        // ==================================================================

        private struct Lv
        {
            public string label, desc;
            public int cost, targets, chain;
            public float dmg, interval, range, area, slow, slowDur, capture, poison;
        }

        private Tower TowerPrefab(string file, string display, string desc, Sprite sprite, Color color,
            TowerAttackType type, Projectile proj, bool hitFlying, Lv[] levels)
        {
            return Prefab<Tower>("Towers/" + file, () =>
            {
                GameObject go = BU.Go(file);
                SpriteRenderer body = BU.Sprite("Body", go.transform, sprite, color, Vector2.one * 1.7f, 2, new Vector3(0f, -0.45f, 0f));
                Transform fire = BU.Go("FirePoint", go.transform, new Vector3(0f, 0.9f, 0f)).transform;
                GameObject pips = BU.Go("LevelPips", go.transform, new Vector3(0f, 1.45f, 0f));
                GameObject[] pipObjs = new GameObject[3];
                for (int p = 0; p < 3; p++)
                {
                    pipObjs[p] = BU.Sprite("Pip_" + (p + 1), pips.transform, BU.Star, new Color(1f, 0.85f, 0.3f), Vector2.one * 0.32f, 25,
                        new Vector3((p - 1) * 0.34f, 0f, 0f)).gameObject;
                    pipObjs[p].SetActive(false);
                }
                WorldHealthBar buildBar = BU.HealthBar(go.transform, 1.2f, 1.2f, true);
                buildBar.name = "ConstructionBar";
                buildBar.transform.Find("Fill").GetComponent<SpriteRenderer>().color = new Color(1f, 0.85f, 0.3f);

                Tower t = go.AddComponent<Tower>();
                new SO(t).Set("towerName", display).Set("description", desc).Set("attackType", type)
                    .Set("projectilePrefab", proj).Set("beamPrefab", FxSoulBeam).Set("canHitFlying", hitFlying)
                    .Set("firePoint", fire).Set("chainRange", 3f).Set("poisonPoolPrefab", FxPoisonPool)
                    .Set("bodyRenderer", body).Set("constructionTime", 1f).Set("constructionBar", buildBar)
                    .List("levelPips", 3, (e, i) => e.objectReferenceValue = pipObjs[i])
                    .List("levels", levels.Length, (e, i) =>
                    {
                        Lv l = levels[i];
                        SO.Rel(e, "label", l.label); SO.Rel(e, "description", l.desc ?? "");
                        SO.Rel(e, "cost", l.cost); SO.Rel(e, "damage", l.dmg); SO.Rel(e, "attackInterval", l.interval);
                        SO.Rel(e, "range", l.range); SO.Rel(e, "targetsPerShot", Mathf.Max(1, l.targets));
                        SO.Rel(e, "areaRadius", l.area); SO.Rel(e, "slowPercent", l.slow); SO.Rel(e, "slowDuration", l.slowDur);
                        SO.Rel(e, "chainTargets", l.chain); SO.Rel(e, "soulCaptureChance", l.capture); SO.Rel(e, "poisonPoolChance", l.poison);
                    })
                    .Apply();
                return go;
            });
        }

        private void BuildTowers()
        {
            TowerMagicOrb = TowerPrefab("Tower_MagicOrb", "Magic Orb", "Single target. Can hit flying.", BU.TowerOrb, new Color(0.55f, 0.75f, 1f),
                TowerAttackType.Projectile, MagicOrbShot, true, new[]
                {
                    new Lv { label = "Base",   cost = 30, dmg = 10, interval = 0.8f, range = 5f,   targets = 1, desc = "Single target magic orb." },
                    new Lv { label = "Tier 1", cost = 25, dmg = 15, interval = 0.8f, range = 5.2f, targets = 1, desc = "+Damage" },
                    new Lv { label = "Tier 2", cost = 40, dmg = 17, interval = 0.75f, range = 5.4f, targets = 2, desc = "Fires 2 orbs at once" },
                    new Lv { label = "Tier 3", cost = 60, dmg = 22, interval = 0.7f, range = 5.6f, targets = 2, capture = 0.3f,
                             desc = "Soul Capture: 30% of kills go straight into the Minion Bag" },
                });

            TowerExplosive = TowerPrefab("Tower_Explosive", "Explosive", "Ground AoE. Cannot hit flying.", BU.TowerBomb, new Color(1f, 0.6f, 0.25f),
                TowerAttackType.Projectile, ExplosiveShot, false, new[]
                {
                    new Lv { label = "Base",   cost = 40, dmg = 14, interval = 1.6f, range = 4.5f, area = 1.2f, desc = "Area damage on the ground." },
                    new Lv { label = "Tier 1", cost = 30, dmg = 20, interval = 1.6f, range = 4.7f, area = 1.4f, desc = "+Damage, +Radius" },
                    new Lv { label = "Tier 2", cost = 45, dmg = 26, interval = 1.5f, range = 4.9f, area = 1.7f, desc = "+Damage, +Radius" },
                    new Lv { label = "Tier 3", cost = 65, dmg = 30, interval = 1.5f, range = 5f, area = 1.8f, poison = 0.7f,
                             desc = "Poison Pool: 70% chance to leave poison for 3s" },
                });

            TowerSoulBeam = TowerPrefab("Tower_SoulBeam", "Soul Beam", "Light damage beam that slows. Can hit flying.", BU.TowerBeam, new Color(0.45f, 1f, 0.95f),
                TowerAttackType.Beam, null, true, new[]
                {
                    new Lv { label = "Base",   cost = 35, dmg = 4, interval = 1f,   range = 5f,   slow = 0.4f,  slowDur = 2f, desc = "Slows target for 2s." },
                    new Lv { label = "Tier 1", cost = 25, dmg = 5, interval = 1f,   range = 5.2f, slow = 0.4f,  slowDur = 3f, desc = "Slow lasts 3s" },
                    new Lv { label = "Tier 2", cost = 40, dmg = 6, interval = 0.9f, range = 5.4f, slow = 0.45f, slowDur = 3f, chain = 1, desc = "Beam bounces to 2 targets" },
                    new Lv { label = "Tier 3", cost = 60, dmg = 8, interval = 0.9f, range = 5.6f, slow = 0.5f,  slowDur = 3f, chain = 2,
                             desc = "Chain Beam: hits up to 3 targets, no damage falloff" },
                });
        }

        // ==================================================================
        // Upgrade cards (ScriptableObjects)
        // ==================================================================

        private void BuildCards()
        {
            Color player = new Color(0.65f, 0.45f, 1f), minion = new Color(0.4f, 1f, 0.5f), support = new Color(1f, 0.8f, 0.3f);
            Cards = new[]
            {
                Card("Card_NecromancyMastery", "Necromancy Mastery", CardCategory.Player, CardEffect.NecromancyMastery, 0.1f, 0f, 5, player, "+10% Raise Dead success chance."),
                Card("Card_RaiseRadius", "Arise: Wider", CardCategory.Player, CardEffect.RaiseRadius, 0.2f, 0f, 5, player, "Mass Raise (Q): +20% radius."),
                Card("Card_RaiseCooldown", "Arise: Faster", CardCategory.Player, CardEffect.RaiseCooldown, 0.15f, 0f, 5, player, "Mass Raise (Q): -15% cooldown."),
                Card("Card_TeleportMastery", "Teleport Mastery", CardCategory.Player, CardEffect.TeleportMastery, 0.15f, 0f, 5, player, "-15% Teleport cooldown."),
                Card("Card_NovaBoost", "Nova Boost", CardCategory.Player, CardEffect.NovaBoost, 0.2f, 0.2f, 5, player, "Arcane Nova (E): +20% radius, +20% damage."),
                Card("Card_Legion", "Legion", CardCategory.Minion, CardEffect.ArmySize, 1f, 0f, 5, minion, "+1 max army size for EVERY minion type."),
                Card("Card_OrbitalMagic", "Orbital Magic", CardCategory.Player, CardEffect.OrbitalMagic, 1f, 0.2f, 0, player, "+1 orbiting orb, +20% orb damage. (No level cap)"),
                Card("Card_MinionPower", "Minion Power", CardCategory.Minion, CardEffect.MinionPower, 0.15f, 0f, 5, minion, "All minions +15% ATK."),
                Card("Card_MinionHaste", "Minion Haste", CardCategory.Minion, CardEffect.MinionHaste, 0.2f, 0f, 5, minion, "All minions +20% move & attack speed."),
                Card("Card_MinionVitality", "Minion Vitality", CardCategory.Minion, CardEffect.MinionVitality, 0.25f, 0f, 5, minion, "All minions +25% Max HP."),
                Card("Card_ManaFont", "Mana Font", CardCategory.Support, CardEffect.ManaFont, 20f, 1f, 5, support, "+20 Max Mana, +1 Mana/sec."),
                Card("Card_BaseArchitect", "Base Architect", CardCategory.Support, CardEffect.BaseArchitect, 100f, 1f, 5, support, "Crystal +100 Max HP, +1 armor."),
                Card("Card_SwiftBoots", "Swift Boots", CardCategory.Support, CardEffect.SwiftBoots, 0.1f, 0f, 5, support, "+10% Necromancer move speed."),
                Card("Card_SoulHarvester", "Soul Harvester", CardCategory.Support, CardEffect.SoulHarvester, 0.15f, 0f, 3, support, "All tower build & upgrade costs -15%."),
            };
        }

        private static UpgradeCard Card(string file, string title, CardCategory cat, CardEffect effect, float v1, float v2, int maxStacks, Color color, string desc,
            int minLevel = 1, UpgradeCard requires = null)
        {
            string path = CardDir + "/" + file + ".asset";
            UpgradeCard existing = AssetDatabase.LoadAssetAtPath<UpgradeCard>(path);
            if (existing != null && !Overwrite) return existing;

            UpgradeCard c = existing != null ? existing : ScriptableObject.CreateInstance<UpgradeCard>();
            c.title = title;
            c.category = cat;
            c.effect = effect;
            c.value1 = v1;
            c.value2 = v2;
            c.maxStacks = maxStacks;
            c.cardColor = color;
            c.description = desc;
            c.minLevel = minLevel;
            c.requires = requires;
            if (existing == null) AssetDatabase.CreateAsset(c, path);
            else EditorUtility.SetDirty(c);
            return c;
        }
    }
}
