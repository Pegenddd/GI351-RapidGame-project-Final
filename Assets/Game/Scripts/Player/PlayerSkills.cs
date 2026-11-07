using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace NecroDefense
{
    /// <summary>
    /// สกิลและการคลิกของผู้เล่น
    /// - คลิกขวา       : Teleport (คูลดาวน์ 10 วิ ฟรี)
    /// - คลิกซ้ายที่ซาก : Raise Dead ทีละศพ (สุ่มโอกาส -> วิญญาณบินเข้า Minion Bag)
    /// - Q             : Mass Raise "จงตื่น" ชุบทุกศพรอบตัว (คูลดาวน์ 10 วิ, แต่ละศพสุ่มโอกาสแยกกัน)
    /// - คลิกซ้ายที่แท่น: เปิดหน้าต่างสร้าง/อัปเกรดป้อม
    /// - E             : Arcane Nova ยืนร่ายแล้วระเบิดวงเวทรอบตัว (AoE)
    /// สกิล Q / E มีเวลาร่าย (Cast Time): ผู้เล่นหยุดเดินจนร่ายเสร็จ
    /// </summary>
    public class PlayerSkills : MonoBehaviour
    {
        [Header("Teleport (Right Click)")]
        [SerializeField] private float teleportCooldown = 10f;
        [Tooltip("ระยะวาร์ปสูงสุด (0 = ไม่จำกัด)")]
        [SerializeField] private float teleportMaxDistance = 0f;
        [SerializeField] private RingEffect teleportEffect;

        [Header("Raise Dead (Left Click on Corpse)")]
        [Tooltip("ระยะคลิกโดนซาก")]
        [SerializeField] private float corpseClickRadius = 0.8f;
        [Tooltip("โบนัสโอกาสชุบสำเร็จ (บวกเพิ่มจากค่าของศัตรู) - การ์ด Necromancy Mastery เพิ่มค่านี้")]
        [SerializeField] private float raiseChanceBonus = 0f;
        [SerializeField] private RingEffect raiseEffect;

        [Header("Skill Q - Mass Raise (จงตื่น)")]
        [SerializeField] private Key massRaiseKey = Key.Q;
        [Tooltip("คูลดาวน์ (วินาที)")]
        [SerializeField] private float massRaiseCooldown = 10f;
        [Tooltip("รัศมีรอบตัวผู้เล่นที่จะชุบศพทั้งหมด")]
        [SerializeField] private float massRaiseRadius = 5f;
        [Tooltip("เวลาร่าย (วินาที) ผู้เล่นหยุดเดินระหว่างนี้")]
        [SerializeField] private float massRaiseCastTime = 0.6f;
        [SerializeField] private RingEffect massRaiseEffect;
        [Tooltip("เอฟเฟกต์ตอนชุบไม่สำเร็จ (ซากสลาย)")]
        [SerializeField] private RingEffect raiseFailEffect;

        [Header("Casting (ร่ายเวท)")]
        [Tooltip("หลอดร่ายเวทใต้ตัวผู้เล่น")]
        [SerializeField] private WorldHealthBar castBar;
        [Tooltip("เอฟเฟกต์ตอนเริ่มร่าย")]
        [SerializeField] private RingEffect castStartEffect;

        [Header("Tower Slot / Minion (Left Click)")]
        [SerializeField] private float slotClickRadius = 1.1f;
        [Tooltip("ระยะคลิกโดนมินเนียน (บวกกับขนาดตัว)")]
        [SerializeField] private float minionClickPadding = 0.35f;

        [Header("Skill E - Arcane Nova (ระเบิดวงเวทรอบตัว)")]
        [FormerlySerializedAs("enigmaKey")]
        [SerializeField] private Key novaKey = Key.E;
        [Tooltip("คูลดาวน์ (วินาที)")]
        [SerializeField] private float novaCooldown = 8f;
        [Tooltip("เวลาร่าย (วินาที) ผู้เล่นยืนนิ่งระหว่างนี้")]
        [SerializeField] private float novaCastTime = 0.8f;
        [Tooltip("รัศมีระเบิดรอบตัวผู้เล่น")]
        [SerializeField] private float novaRadius = 3.5f;
        [SerializeField] private float novaDamage = 45f;
        [Tooltip("โดนตัวบินด้วยไหม")]
        [SerializeField] private bool novaHitsFlying = true;
        [Tooltip("เอฟเฟกต์วงระเบิด")]
        [FormerlySerializedAs("shockwaveEffect")]
        [SerializeField] private RingEffect novaEffect;

        private PlayerNecromancer _player;
        private float _teleportTimer, _novaTimer, _massRaiseTimer;
        private float _raiseRadiusMult = 1f, _raiseCooldownMult = 1f;
        private Action _castAction;
        private float _castTimer, _castDuration;
        private float _novaRadiusMult = 1f, _novaDamageMult = 1f;
        private float _teleportCooldownMult = 1f;
        private static readonly List<Unit> _buffer = new List<Unit>();

        // ---- ข้อมูลให้ UI อ่าน ----
        public float TeleportCooldown => teleportCooldown * _teleportCooldownMult;
        public float TeleportRemaining => Mathf.Max(0f, _teleportTimer);
        public Key NovaKey => novaKey;
        public float NovaCooldown => novaCooldown;
        public float NovaRemaining => Mathf.Max(0f, _novaTimer);
        public float NovaRadius => novaRadius * _novaRadiusMult;
        public float NovaDamage => novaDamage * _novaDamageMult;
        public float RaiseChanceBonus => raiseChanceBonus;
        public Key MassRaiseKey => massRaiseKey;
        public float MassRaiseCooldown => massRaiseCooldown * _raiseCooldownMult;
        public float MassRaiseRemaining => Mathf.Max(0f, _massRaiseTimer);
        public float MassRaiseRadius => massRaiseRadius * _raiseRadiusMult;
        public bool IsCasting => _castAction != null;

        private void Awake()
        {
            _player = GetComponent<PlayerNecromancer>();
            if (castBar != null) castBar.SetValue(1f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _teleportTimer -= dt;
            _novaTimer -= dt;
            _massRaiseTimer -= dt;

            if (_player == null || !_player.IsAlive || !GameManager.IsGameplayRunning) return;

            // ----- กำลังร่ายเวท: ยืนนิ่ง ทำอย่างอื่นไม่ได้ -----
            if (IsCasting)
            {
                _castTimer -= dt;
                if (castBar != null) castBar.SetValue(1f - Mathf.Clamp01(_castTimer / _castDuration));
                if (_castTimer <= 0f) FinishCast();
                return;
            }

            // กำลังเลือกตำแหน่งวางมินเนียนอยู่ -> MinionManager จัดการคลิกเอง
            MinionManager mm = GameManager.Instance.Minions;
            bool placing = mm.IsPlacing || mm.LastPlacementFrame == Time.frameCount;

            Mouse mouse = Mouse.current;
            Keyboard kb = Keyboard.current;
            Vector2 mouseWorld = GetMouseWorld();
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

            if (mouse != null && !overUI && !placing)
            {
                if (mouse.leftButton.wasPressedThisFrame) HandleLeftClick(mouseWorld);
                if (mouse.rightButton.wasPressedThisFrame) TryTeleport(mouseWorld);
            }

            if (kb != null)
            {
                if (kb[massRaiseKey].wasPressedThisFrame) TryMassRaise();
                if (kb[novaKey].wasPressedThisFrame) TryNova();
            }
        }

        public static Vector2 GetMouseWorld()
        {
            Camera cam = Camera.main;
            Mouse mouse = Mouse.current;
            if (cam == null || mouse == null) return Vector2.zero;
            Vector3 screen = mouse.position.ReadValue();
            screen.z = -cam.transform.position.z;
            return cam.ScreenToWorldPoint(screen);
        }

        // ---------------- Left click ----------------

        /// <summary>ลำดับคลิกซ้าย: ซากศพ -> มินเนียน -> แท่นป้อม -> พื้นว่าง (ปิดเมนู)</summary>
        private void HandleLeftClick(Vector2 world)
        {
            TowerManager towers = GameManager.Instance.Towers;
            MinionManager minions = GameManager.Instance.Minions;

            if (TryRaiseDead(world)) return;

            MinionUnit m = FindMinionAt(world);
            if (m != null)
            {
                towers.CloseSlotPanel();
                minions.Select(m);
                return;
            }

            minions.Deselect();
            TowerSlot slot = FindSlotAt(world);
            if (slot != null && slot != towers.SelectedSlot) towers.SelectSlot(slot);
            else towers.CloseSlotPanel();
        }

        private MinionUnit FindMinionAt(Vector2 world)
        {
            MinionUnit best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < Registry.Minions.Count; i++)
            {
                MinionUnit mu = Registry.Minions[i];
                float d = Vector2.Distance(world, mu.Position);
                if (d <= mu.BodyRadius + minionClickPadding && d < bestDist) { bestDist = d; best = mu; }
            }
            return best;
        }

        private bool TryRaiseDead(Vector2 world)
        {
            Corpse best = null;
            float bestDist = corpseClickRadius;
            for (int i = 0; i < Registry.Corpses.Count; i++)
            {
                float d = Vector2.Distance(world, Registry.Corpses[i].Position);
                if (d <= bestDist) { bestDist = d; best = Registry.Corpses[i]; }
            }
            if (best == null) return false;

            int r = RollRaise(best);
            if (r == RaiseFailed)
                GameManager.Instance.UI.ShowMessage("Raise Dead failed (" + Mathf.RoundToInt(Mathf.Clamp01(best.RaiseChance + raiseChanceBonus) * 100f) + "%)");
            else if (r == RaiseArmyFull)
                GameManager.Instance.UI.ShowMessage(best.MinionPrefab.DisplayName + " army is full (" + GameManager.Instance.Minions.GetCap(best.MinionPrefab) + ")");
            return true;
        }

        private const int RaiseOk = 0, RaiseFailed = 1, RaiseArmyFull = 2;

        /// <summary>
        /// ลองชุบ 1 ศพ (สุ่มโอกาสของศพนั้น) สำเร็จ = วิญญาณบินเข้ากระเป๋า / ไม่สำเร็จ = ซากสลาย
        /// กองทัพชนิดนั้นเต็ม = ไม่ชุบ ศพยังอยู่
        /// </summary>
        private int RollRaise(Corpse c)
        {
            if (!GameManager.Instance.Minions.HasRoom(c.MinionPrefab)) return RaiseArmyFull;
            float chance = Mathf.Clamp01(c.RaiseChance + raiseChanceBonus);
            bool ok = UnityEngine.Random.value < chance;
            if (ok)
            {
                GameManager.Instance.Minions.RaiseIntoBag(c.MinionPrefab, c.Position);
                if (raiseEffect != null) SpawnRing(raiseEffect, c.Position, 0.8f);
            }
            else if (raiseFailEffect != null) SpawnRing(raiseFailEffect, c.Position, 0.5f);
            c.Consume();
            return ok ? RaiseOk : RaiseFailed;
        }

        private TowerSlot FindSlotAt(Vector2 world)
        {
            TowerSlot best = null;
            float bestDist = slotClickRadius;
            for (int i = 0; i < Registry.TowerSlots.Count; i++)
            {
                float d = Vector2.Distance(world, Registry.TowerSlots[i].transform.position);
                if (d <= bestDist) { bestDist = d; best = Registry.TowerSlots[i]; }
            }
            return best;
        }

        // ---------------- Skills ----------------

        private void TryTeleport(Vector2 world)
        {
            if (_teleportTimer > 0f)
            {
                GameManager.Instance.UI.ShowMessage("Teleport on cooldown");
                return;
            }
            Vector2 from = _player.Position;
            Vector2 to = world;
            if (teleportMaxDistance > 0f && Vector2.Distance(from, to) > teleportMaxDistance)
                to = from + (to - from).normalized * teleportMaxDistance;

            if (teleportEffect != null) SpawnRing(teleportEffect, from, 0.8f);
            _player.TeleportTo(to);
            if (teleportEffect != null) SpawnRing(teleportEffect, _player.Position, 0.8f);
            _teleportTimer = TeleportCooldown;
        }

        // ---------------- Casting ----------------

        /// <summary>เริ่มร่าย: ผู้เล่นหยุดเดิน แล้วค่อยปล่อยสกิลเมื่อครบเวลา</summary>
        private void BeginCast(float castTime, Action action)
        {
            if (castTime <= 0f) { action(); return; }
            _castAction = action;
            _castDuration = castTime;
            _castTimer = castTime;
            _player.SetMovementLocked(true);
            if (castBar != null) castBar.SetValue(0f);
            if (castStartEffect != null) SpawnRing(castStartEffect, _player.Position, 1f);
        }

        private void FinishCast()
        {
            Action a = _castAction;
            _castAction = null;
            _player.SetMovementLocked(false);
            if (castBar != null) castBar.SetValue(1f);
            a?.Invoke();
        }

        // ---------------- Q: Mass Raise ----------------

        private void TryMassRaise()
        {
            if (_massRaiseTimer > 0f) { GameManager.Instance.UI.ShowMessage("Mass Raise on cooldown"); return; }
            _massRaiseTimer = MassRaiseCooldown;
            BeginCast(massRaiseCastTime, DoMassRaise);
        }

        private readonly List<Corpse> _corpseBuffer = new List<Corpse>();

        private void DoMassRaise()
        {
            float radius = MassRaiseRadius;
            if (massRaiseEffect != null) SpawnRing(massRaiseEffect, _player.Position, radius);

            _corpseBuffer.Clear();
            for (int i = 0; i < Registry.Corpses.Count; i++)
                if (Vector2.Distance(_player.Position, Registry.Corpses[i].Position) <= radius) _corpseBuffer.Add(Registry.Corpses[i]);

            if (_corpseBuffer.Count == 0) { GameManager.Instance.UI.ShowMessage("No corpses nearby"); return; }

            int ok = 0, full = 0;
            foreach (Corpse c in _corpseBuffer)
            {
                if (c == null) continue;
                int r = RollRaise(c);
                if (r == RaiseOk) ok++;
                else if (r == RaiseArmyFull) full++;
            }
            GameManager.Instance.UI.ShowMessage("Arise! " + ok + " / " + (_corpseBuffer.Count - full) + " raised" + (full > 0 ? "   (" + full + " skipped: army full)" : ""));
        }

        // ---------------- E: Arcane Nova ----------------

        private void TryNova()
        {
            if (_novaTimer > 0f) { GameManager.Instance.UI.ShowMessage("Arcane Nova on cooldown"); return; }
            _novaTimer = novaCooldown;
            BeginCast(novaCastTime, DoNova);
        }

        private void DoNova()
        {
            float radius = NovaRadius;
            Registry.GetHostilesInRadius(_player.Position, radius, Team.Player, novaHitsFlying, _buffer);
            for (int i = 0; i < _buffer.Count; i++) _buffer[i].TakeDamage(NovaDamage);
            if (novaEffect != null) SpawnRing(novaEffect, _player.Position, radius);
        }

        private static void SpawnRing(RingEffect prefab, Vector2 pos, float radius)
        {
            RingEffect fx = Instantiate(prefab, pos, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Effects));
            fx.Play(radius);
        }

        // ---------------- Upgrade hooks (การ์ดเลเวลอัป) ----------------

        /// <summary>ซีนเทส: รีเซ็ตคูลดาวน์ทุกสกิล</summary>
        public void DebugResetCooldowns() { _teleportTimer = 0f; _novaTimer = 0f; _massRaiseTimer = 0f; }

        public void AddRaiseChance(float amount) => raiseChanceBonus += amount;
        public void AddMassRaiseRadius(float percent) => _raiseRadiusMult += percent;
        public void ReduceMassRaiseCooldown(float percent) => _raiseCooldownMult = Mathf.Max(0.1f, _raiseCooldownMult * (1f - percent));
        public void ReduceTeleportCooldown(float percent) => _teleportCooldownMult = Mathf.Max(0.1f, _teleportCooldownMult * (1f - percent));
        public void BoostNova(float radiusPercent, float damagePercent) { _novaRadiusMult += radiusPercent; _novaDamageMult += damagePercent; }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.2f, 1f);
            Gizmos.DrawWireSphere(transform.position, novaRadius);
            Gizmos.color = new Color(0.4f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, massRaiseRadius);
        }
    }
}
