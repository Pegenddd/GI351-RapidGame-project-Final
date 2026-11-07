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
        [Header("Movement")]
        [Tooltip("ความเร็วเดินพื้นฐาน (GDD = 4.5)")]
        [SerializeField] private float moveSpeed = 4.5f;

        private Rigidbody2D _rb;
        private float _moveSpeedBonusPercent;
        private Vector2 _moveInput;

        public override Team Team => Team.Player;
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
