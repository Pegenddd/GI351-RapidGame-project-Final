using UnityEngine;
using UnityEngine.InputSystem;

namespace NecroDefense
{
    /// <summary>
    /// ตัวผู้เล่น: เลือด + การเดิน (WASD / ลูกศร)
    /// การโจมตีอยู่ใน PlayerAutoAttack / สกิลอยู่ใน PlayerSkills (อยู่บน GameObject เดียวกัน)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerNecromancer : Unit
    {
        [Header("Data (ค่าบาลานซ์)")]
        [Tooltip("ไฟล์ค่าผู้เล่น (Data/Units/Player) - เลือด / เดิน / ยิงอัตโนมัติ / สกิล ใช้ค่าจากไฟล์นี้แทนค่าใน Inspector (ว่าง = ใช้ค่าใน Inspector)")]
        [SerializeField] private PlayerStats stats;

        [Header("Movement")]
        [Tooltip("ความเร็วเดินพื้นฐาน (GDD = 4.5)")]
        [SerializeField] private float moveSpeed = 4.5f;
        [Tooltip("ตำแหน่งเท้าเทียบกับกลางตัว (ใช้ชนฐานป้อม)")]
        [SerializeField] private float footOffset = -0.55f;
        [Tooltip("ขนาดเท้า (ยิ่งมากยิ่งชนฐานป้อมเร็ว)")]
        [SerializeField] private float footRadius = 0.18f;

        private Rigidbody2D _rb;
        private float _moveSpeedBonusPercent;
        private Vector2 _moveInput;

        public override Team Team => Team.Player;
        public PlayerStats Stats => stats;

#if UNITY_EDITOR
        private const string DefaultStatsPath = "Assets/Game/Data/Units/Player.asset";

        /// <summary>ใน Editor: ช่อง Stats ว่าง -> ผูก Player.asset ให้อัตโนมัติ (กด Ctrl+S เซฟซีนเพื่อให้ติดไปตอน Build)</summary>
        private void OnValidate() => AutoLinkStats();
        private void Reset() => AutoLinkStats();

        private void AutoLinkStats()
        {
            if (stats != null) return;
            stats = UnityEditor.AssetDatabase.LoadAssetAtPath<PlayerStats>(DefaultStatsPath);
            if (stats != null && !Application.isPlaying) UnityEditor.EditorUtility.SetDirty(this);
        }
#endif

        protected override void ApplyStats()
        {
#if UNITY_EDITOR
            AutoLinkStats();
#endif
            if (stats == null)
            {
                Debug.LogWarning("[NecroDefense] ผู้เล่นในซีนนี้ยังไม่ได้ผูกไฟล์ Data (ช่อง Stats ว่าง) -> ใช้ค่าในสคริปต์แทน\n" +
                                 "แก้: Tools > NecroDefense > Data > Link Player Stats In All Scenes", this);
                return;
            }
            maxHealth = stats.maxHealth;
            armor = stats.armor;
            moveSpeed = stats.moveSpeed;
        }
        public float MoveSpeed => moveSpeed * (1f + _moveSpeedBonusPercent);
        public Vector2 MoveInput => _moveInput;
        /// <summary>true = ยืนนิ่ง (กำลังร่ายเวท)</summary>
        public bool MovementLocked { get; private set; }

        public void SetMovementLocked(bool locked) => MovementLocked = locked;

        protected override void Awake()
        {
            base.Awake();
            _rb = GetComponent<Rigidbody2D>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Registry.Player = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Registry.Player == this) Registry.Player = null;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || !GameManager.IsGameplayRunning)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            _moveInput = MovementLocked ? Vector2.zero : ReadMoveInput();
            _rb.linearVelocity = _moveInput * MoveSpeed;
            if (bodyRenderer != null && Mathf.Abs(_moveInput.x) > 0.01f) bodyRenderer.flipX = _moveInput.x < 0f;
        }

        private void LateUpdate()
        {
            MapBounds bounds = GameManager.Instance != null ? GameManager.Instance.Bounds : null;
            if (bounds != null) transform.position = bounds.Clamp(transform.position);
            PushOutOfTowers();
        }

        /// <summary>เดินทะลุฐานป้อมไม่ได้: ถ้าเท้าเข้าไปในวงรีฐาน ดันออกไปที่ขอบ (เดินอ้อมได้ลื่นๆ)</summary>
        private void PushOutOfTowers()
        {
            Vector2 feet = (Vector2)transform.position + new Vector2(0f, footOffset);
            for (int i = 0; i < Registry.TowerSlots.Count; i++)
            {
                TowerSlot slot = Registry.TowerSlots[i];
                Tower t = slot != null ? slot.CurrentTower : null;
                if (t == null || !t.BlocksPlayer) continue;

                Vector2 c = t.FootprintCenter;
                float rx = t.FootprintSize.x * 0.5f + footRadius;
                float ry = t.FootprintSize.y * 0.5f + footRadius * 0.5f;
                if (rx <= 0f || ry <= 0f) continue;
                Vector2 d = new Vector2((feet.x - c.x) / rx, (feet.y - c.y) / ry);
                float len = d.magnitude;
                if (len >= 1f) continue;
                if (len < 0.0001f) d = Vector2.down; else d /= len;
                Vector2 edge = c + new Vector2(d.x * rx, d.y * ry);
                Vector2 delta = edge - feet;
                feet += delta;
                transform.position += (Vector3)delta;
                _rb.position = transform.position;
            }
        }

        private static Vector2 ReadMoveInput()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null) return Vector2.zero;
            Vector2 v = Vector2.zero;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1f;
            return v.sqrMagnitude > 1f ? v.normalized : v;
        }

        /// <summary>วาร์ปไปตำแหน่ง (โดนจำกัดในขอบแมพ)</summary>
        public void TeleportTo(Vector2 position)
        {
            MapBounds bounds = GameManager.Instance != null ? GameManager.Instance.Bounds : null;
            if (bounds != null) position = bounds.Clamp(position);
            _rb.position = position;
            transform.position = position;
        }

        /// <summary>การ์ด Swift Boots: +% ความเร็ว (0.1 = +10%)</summary>
        public void AddMoveSpeedPercent(float percent) => _moveSpeedBonusPercent += percent;

        protected override void OnDeath()
        {
            _rb.linearVelocity = Vector2.zero;
            if (bodyRenderer != null) bodyRenderer.color = Color.gray;
            if (GameManager.Instance != null) GameManager.Instance.Defeat("The Necromancer has fallen");
        }
    }
}
