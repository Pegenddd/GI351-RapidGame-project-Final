using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NecroDefense
{
    /// <summary>มินเนียน 1 ชนิดในกระเป๋า</summary>
    [Serializable]
    public class MinionBagEntry
    {
        public MinionUnit minionPrefab;
        [Tooltip("จำนวนที่มีในกระเป๋าตอนเริ่มเกม")]
        public int count = 0;
        [Tooltip("ปุ่มลัดอัญเชิญ")]
        public Key summonKey = Key.None;
        [Tooltip("มีมินเนียนชนิดนี้ได้สูงสุดกี่ตัว (ในกระเป๋า + ในสนาม) ก่อนบวกการ์ด Legion")]
        public int maxOwned = 5;
    }

    /// <summary>
    /// ระบบกองทัพมินเนียน
    /// - Minion Bag: เก็บมินเนียนที่ชุบได้ (ลำดับในลิสต์ = ลำดับช่องใน UI)
    /// - Summon: คลิกช่อง / กดปุ่มลัด -> เลือกจุดวางในสนามด้วยคลิกซ้าย (คลิกขวา/Esc = ยกเลิก)
    ///           หรือลากช่องไปปล่อยในสนามได้เลย (ใช้ Mana ตอนวางจริง)
    /// - Sacrifice: ลากช่องไปปล่อยที่คริสตัล
    /// - Army Cap: แต่ละชนิดมีได้จำกัด (Max Owned) ชุบเกินไม่ได้ / การ์ด Legion +1 ทุกชนิด
    /// - มินเนียนตาย -> วิญญาณกลับเข้ากระเป๋า (เรียกใหม่ได้ เสีย Mana)
    /// - Command: คลิกที่มินเนียนทีละตัว -> Hold Area / Move / Follow Player
    /// </summary>
    public class MinionManager : MonoBehaviour
    {
        [Header("Minion Bag")]
        [SerializeField] private List<MinionBagEntry> bag = new List<MinionBagEntry>();

        [Header("Summon")]
        [Tooltip("อัญเชิญรอบตัวผู้เล่นในรัศมีนี้ (ใช้เมื่อไม่ได้ระบุจุดวาง)")]
        [SerializeField] private float summonRadius = 1.2f;
        [Tooltip("ภาพเงาที่ตามเมาส์ตอนเลือกจุดวาง (Map/SummonPlacementGhost)")]
        [SerializeField] private SpriteRenderer placementGhost;
        [SerializeField] private Color ghostValidColor = new Color(1f, 1f, 1f, 0.55f);
        [SerializeField] private Color ghostInvalidColor = new Color(1f, 0.3f, 0.3f, 0.45f);

        [Header("Move Order (คำสั่ง Move)")]
        [Tooltip("ธงที่ตามเมาส์ตอนเลือกจุดหมาย (Map/MoveOrderMarker)")]
        [SerializeField] private SpriteRenderer moveMarker;
        [Tooltip("จำนวนมินเนียนในสนามสูงสุด")]
        [SerializeField] private int maxActiveMinions = 30;
        [SerializeField] private RingEffect summonEffect;

        [Header("Command")]
        [Tooltip("คำสั่งเริ่มต้นของมินเนียนที่เพิ่งอัญเชิญ")]
        [SerializeField] private MinionCommand defaultCommand = MinionCommand.SelfDefense;

        [Header("Upgrade Multipliers (การ์ดเลเวลอัปจะเพิ่มค่าเหล่านี้)")]
        [SerializeField] private float healthMultiplier = 1f;
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float speedMultiplier = 1f;

        [Header("Army Capacity")]
        [Tooltip("โบนัสจำนวนสูงสุดทุกชนิด (การ์ด Legion เพิ่มค่านี้)")]
        [SerializeField] private int armyCapacityBonus = 0;

        private readonly Dictionary<MinionUnit, int> _incoming = new Dictionary<MinionUnit, int>();

        public IReadOnlyList<MinionBagEntry> Bag => bag;

        /// <summary>จำนวนสูงสุดของมินเนียนชนิดนี้</summary>
        public int GetCap(MinionUnit prefab)
        {
            MinionBagEntry e = bag.Find(x => x.minionPrefab == prefab);
            return (e != null ? e.maxOwned : 5) + armyCapacityBonus;
        }

        /// <summary>มีอยู่ทั้งหมดกี่ตัว (กระเป๋า + สนาม + วิญญาณที่กำลังบินเข้ากระเป๋า)</summary>
        public int GetOwned(MinionUnit prefab)
        {
            MinionBagEntry e = bag.Find(x => x.minionPrefab == prefab);
            int n = e != null ? e.count : 0;
            for (int i = 0; i < Registry.Minions.Count; i++) if (Registry.Minions[i].SourcePrefab == prefab) n++;
            if (_incoming.TryGetValue(prefab, out int inc)) n += inc;
            return n;
        }

        /// <summary>ซีนเทส: เติมมินเนียนชนิดนี้เข้ากระเป๋าจนเต็มเพดาน</summary>
        public void DebugFillToCap(MinionUnit prefab)
        {
            int room = GetCap(prefab) - GetOwned(prefab);
            if (room > 0) AddToBag(prefab, room, "Test");
        }

        public bool HasRoom(MinionUnit prefab) => prefab != null && GetOwned(prefab) < GetCap(prefab);

        /// <summary>การ์ด Legion</summary>
        public void AddArmyCapacity(int amount) => armyCapacityBonus += amount;
        public MinionUnit SelectedMinion { get; private set; }
        public int ActiveCount => Registry.Minions.Count;
        public int MaxActive => maxActiveMinions;

        /// <summary>กำลังเลือกจุดวางมินเนียนอยู่</summary>
        public bool IsPlacing => _placingIndex >= 0 || _moveOrder != null;
        public bool IsOrderingMove => _moveOrder != null;
        public int PlacingIndex => _placingIndex;
        /// <summary>เฟรมที่เพิ่งวาง/ยกเลิก (กันคลิกเดียวกันไปโดนระบบอื่น)</summary>
        public int LastPlacementFrame { get; private set; } = -1;

        public event Action BagChanged;

        private int _placingIndex = -1;
        private MinionUnit _moveOrder;

        private void Start()
        {
            if (placementGhost != null) placementGhost.gameObject.SetActive(false);
            if (moveMarker != null) moveMarker.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (!GameManager.IsGameplayRunning) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            for (int i = 0; i < bag.Count; i++)
                if (bag[i].summonKey != Key.None && kb[bag[i].summonKey].wasPressedThisFrame) BeginPlacement(i);

            if (_moveOrder != null) UpdateMoveOrder();
            else if (_placingIndex >= 0) UpdatePlacement();

            // ตัวที่เลือกตายแล้ว -> ปิดเมนู
            if (SelectedMinion == null && GameManager.Instance.UI.CommandPanel != null && GameManager.Instance.UI.CommandPanel.IsOpen)
                Deselect();
        }

        // ---------------- Bag ----------------

        /// <summary>ชุบสำเร็จ: วิญญาณ (รูปมินเนียน) บินจากศพเข้ากระเป๋า แล้วค่อยนับเข้า Bag</summary>
        public void RaiseIntoBag(MinionUnit prefab, Vector2 fromWorld)
        {
            if (prefab == null) return;
            SendSoulToBag(prefab, fromWorld);
        }

        /// <summary>วิญญาณบินจากจุดนี้เข้ากระเป๋า (ใช้ตอนชุบสำเร็จ และตอนมินเนียนตาย)</summary>
        private void SendSoulToBag(MinionUnit prefab, Vector2 fromWorld)
        {
            SoulFlyUI fly = GameManager.Instance != null ? GameManager.Instance.UI.SoulFly : null;
            int index = bag.FindIndex(e => e.minionPrefab == prefab);
            if (fly == null) { AddToBag(prefab, 1, null, false); return; }

            _incoming.TryGetValue(prefab, out int inc);
            _incoming[prefab] = inc + 1;
            fly.Fly(prefab.Icon, fromWorld, index, () =>
            {
                _incoming[prefab] = Mathf.Max(0, _incoming[prefab] - 1);
                AddToBag(prefab, 1, null, false);
            });
        }

        /// <summary>มินเนียนตาย -> กลับเข้ากระเป๋า</summary>
        public void OnMinionDied(MinionUnit minion)
        {
            if (minion == null || minion.SourcePrefab == null) return;
            if (SelectedMinion == minion) Deselect();
            SendSoulToBag(minion.SourcePrefab, minion.Position);
        }

        public void AddToBag(MinionUnit prefab, int amount = 1, string reason = null, bool showMessage = true)
        {
            if (prefab == null || amount <= 0) return;
            MinionBagEntry entry = bag.Find(e => e.minionPrefab == prefab);
            if (entry == null)
            {
                entry = new MinionBagEntry { minionPrefab = prefab, count = 0 };
                bag.Add(entry);
            }
            entry.count += amount;
            BagChanged?.Invoke();
            if (showMessage && GameManager.Instance != null)
                GameManager.Instance.UI.ShowMessage((reason != null ? reason + ": " : "") + "+" + amount + " " + prefab.DisplayName);
        }

        // ---------------- Summon ----------------

        /// <summary>อัญเชิญมินเนียนช่องที่ index (position = null -> รอบตัวผู้เล่น)</summary>
        public bool TrySummon(int index, Vector2? position = null)
        {
            if (index < 0 || index >= bag.Count) return false;
            MinionBagEntry entry = bag[index];
            UIManager ui = GameManager.Instance.UI;

            if (entry.minionPrefab == null) return false;
            if (entry.count <= 0) { ui.ShowMessage("No " + entry.minionPrefab.DisplayName + " in bag"); return false; }
            if (ActiveCount >= maxActiveMinions) { ui.ShowMessage("Too many minions (" + maxActiveMinions + ")"); return false; }
            if (!GameManager.Instance.Economy.TrySpendMana(entry.minionPrefab.ManaCost)) return false;

            Vector2 pos;
            if (position.HasValue) pos = position.Value;
            else
            {
                PlayerNecromancer p = Registry.Player;
                pos = (p != null ? p.Position : Vector2.zero) + UnityEngine.Random.insideUnitCircle.normalized * summonRadius;
            }
            MapBounds bounds = GameManager.Instance.Bounds;
            if (bounds != null) pos = bounds.Clamp(pos);

            MinionUnit m = Instantiate(entry.minionPrefab, pos, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Minions));
            m.name = entry.minionPrefab.name;
            m.SourcePrefab = entry.minionPrefab;
            m.ApplyMultipliers(healthMultiplier, damageMultiplier, speedMultiplier);
            m.SetCommand(defaultCommand);

            if (summonEffect != null)
            {
                RingEffect fx = Instantiate(summonEffect, pos, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Effects));
                fx.Play(0.8f);
            }

            entry.count--;
            BagChanged?.Invoke();
            return true;
        }

        // ---------------- Placement (เลือกจุดวาง) ----------------

        /// <summary>เริ่มโหมดเลือกจุดวาง (กดช่องเดิมซ้ำ = ยกเลิก)</summary>
        public void BeginPlacement(int index)
        {
            if (index < 0 || index >= bag.Count || bag[index].minionPrefab == null) return;
            if (_placingIndex == index) { CancelPlacement(); return; }

            MinionBagEntry entry = bag[index];
            UIManager ui = GameManager.Instance.UI;
            if (entry.count <= 0) { ui.ShowMessage("No " + entry.minionPrefab.DisplayName + " in bag"); return; }
            if (ActiveCount >= maxActiveMinions) { ui.ShowMessage("Too many minions (" + maxActiveMinions + ")"); return; }
            if (GameManager.Instance.Economy.Mana < entry.minionPrefab.ManaCost) { ui.ShowMessage("Not enough Mana"); return; }

            Deselect();
            GameManager.Instance.Towers.CloseSlotPanel();
            _placingIndex = index;
            if (placementGhost != null)
            {
                placementGhost.sprite = entry.minionPrefab.Icon;
                placementGhost.transform.localScale = entry.minionPrefab.transform.Find("Body") != null
                    ? entry.minionPrefab.transform.Find("Body").localScale : Vector3.one;
                placementGhost.gameObject.SetActive(true);
            }
            UpdatePlacement();
        }

        /// <summary>คำสั่ง Move: เลือกจุดหมายด้วยคลิกซ้าย (คลิกขวา/Esc ยกเลิก)</summary>
        public void BeginMoveOrder(MinionUnit minion)
        {
            if (minion == null) return;
            CancelPlacement();
            Deselect();
            _moveOrder = minion;
            minion.SetSelected(true);
            if (moveMarker != null) moveMarker.gameObject.SetActive(true);
            GameManager.Instance.UI.ShowMessage("Click where to move");
        }

        private void UpdateMoveOrder()
        {
            if (_moveOrder == null || !_moveOrder.IsAlive) { CancelPlacement(); return; }
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 world = PlayerSkills.GetMouseWorld();
            MapBounds bounds = GameManager.Instance.Bounds;
            bool inside = bounds == null || bounds.Clamp(world) == world;
            bool overUI = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            if (moveMarker != null)
            {
                moveMarker.transform.position = world;
                moveMarker.color = inside && !overUI ? ghostValidColor : ghostInvalidColor;
            }
            if (mouse.rightButton.wasPressedThisFrame) { CancelPlacement(); return; }
            if (mouse.leftButton.wasPressedThisFrame && !overUI && inside)
            {
                MinionUnit m = _moveOrder;
                CancelPlacement();
                m.MoveTo(world);
            }
        }

        public void CancelPlacement()
        {
            if (_moveOrder != null) { _moveOrder.SetSelected(false); _moveOrder = null; }
            if (moveMarker != null) moveMarker.gameObject.SetActive(false);
            _placingIndex = -1;
            LastPlacementFrame = Time.frameCount;
            if (placementGhost != null) placementGhost.gameObject.SetActive(false);
        }

        private void UpdatePlacement()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 world = PlayerSkills.GetMouseWorld();
            MapBounds bounds = GameManager.Instance.Bounds;
            bool inside = bounds == null || bounds.Clamp(world) == world;
            bool overUI = UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();

            if (placementGhost != null)
            {
                placementGhost.transform.position = world;
                placementGhost.color = inside && !overUI ? ghostValidColor : ghostInvalidColor;
            }

            if (mouse.rightButton.wasPressedThisFrame) { CancelPlacement(); return; }

            if (mouse.leftButton.wasPressedThisFrame && !overUI && inside)
            {
                int index = _placingIndex;
                CancelPlacement();
                TrySummon(index, world);
            }
        }

        // ---------------- Sacrifice ----------------

        public bool TrySacrifice(int index)
        {
            if (index < 0 || index >= bag.Count) return false;
            MinionBagEntry entry = bag[index];
            Crystal crystal = Registry.Crystal;
            if (entry.minionPrefab == null || entry.count <= 0 || crystal == null || !crystal.IsAlive) return false;

            entry.count--;
            crystal.Sacrifice(entry.minionPrefab);
            BagChanged?.Invoke();
            GameManager.Instance.UI.ShowMessage("Sacrificed " + entry.minionPrefab.DisplayName + ": Crystal +" + entry.minionPrefab.SacrificeHealPercent + "% HP");
            return true;
        }

        // ---------------- Command ----------------

        /// <summary>คลิกที่มินเนียน -> เลือกตัวนั้นและเปิดเมนูคำสั่งรอบตัว</summary>
        public void Select(MinionUnit minion)
        {
            if (SelectedMinion != null) SelectedMinion.SetSelected(false);
            SelectedMinion = minion;
            if (minion == null) { Deselect(); return; }
            minion.SetSelected(true);
            GameManager.Instance.UI.CommandPanel.Show(minion);
        }

        public void Deselect()
        {
            if (SelectedMinion != null) SelectedMinion.SetSelected(false);
            SelectedMinion = null;
            if (GameManager.Instance != null && GameManager.Instance.UI.CommandPanel != null)
                GameManager.Instance.UI.CommandPanel.Hide();
        }

        /// <summary>สั่งมินเนียนตัวที่เลือกอยู่</summary>
        public void CommandSelected(MinionCommand command)
        {
            if (SelectedMinion == null) return;
            SelectedMinion.SetCommand(command);
        }

        // ---------------- Upgrades ----------------

        public void AddHealthPercent(float p) { healthMultiplier += p; RefreshActiveMinions(); }
        public void AddDamagePercent(float p) { damageMultiplier += p; RefreshActiveMinions(); }
        public void AddSpeedPercent(float p) { speedMultiplier += p; RefreshActiveMinions(); }

        private void RefreshActiveMinions()
        {
            for (int i = 0; i < Registry.Minions.Count; i++)
                Registry.Minions[i].ApplyMultipliers(healthMultiplier, damageMultiplier, speedMultiplier);
        }
    }
}
