using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// เล่นท่าเดิน / ท่าตี จากรูปทีละเฟรม (ไม่ต้องใช้ Animator)
    /// - เดินอยู่: วนท่าเดิน / ยืนนิ่ง: ค้างเฟรมแรกของท่าเดิน (หรือ Idle Frame ถ้าใส่)
    /// - ตี: เล่นท่าตีครั้งเดียว แล้วกลับไปท่าเดิน
    /// ใส่ไว้ที่ตัวยูนิต (EnemyUnit / MinionUnit) - ลำดับเฟรมจัดได้ใน Inspector
    /// </summary>
    public class SpriteFrameAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;

        [Header("Walk (เดิน)")]
        [Tooltip("เฟรมท่าเดิน เรียงตามลำดับ (วนซ้ำ)")]
        [SerializeField] private Sprite[] walkFrames = new Sprite[0];
        [Tooltip("เฟรมต่อวินาทีตอนเดิน")]
        [SerializeField] private float walkFps = 8f;
        [Tooltip("เร็วกว่านี้ถือว่ากำลังเดิน (หน่วย/วินาที)")]
        [SerializeField] private float moveThreshold = 0.05f;
        [Tooltip("รูปตอนยืนนิ่ง (ว่าง = เฟรมแรกของท่าเดิน)")]
        [SerializeField] private Sprite idleFrame;

        [Header("Attack (ตี)")]
        [Tooltip("เฟรมท่าตี เรียงตามลำดับ (เล่นครั้งเดียวต่อการตี)")]
        [SerializeField] private Sprite[] attackFrames = new Sprite[0];
        [Tooltip("เฟรมต่อวินาทีตอนตี")]
        [SerializeField] private float attackFps = 10f;

        private CombatUnit _unit;
        private Rigidbody2D _rb;
        private Vector3 _lastPos;
        private float _walkTime, _attackTime = -1f;

        private void Awake()
        {
            _unit = GetComponentInParent<CombatUnit>();
            _rb = GetComponentInParent<Rigidbody2D>();
            _lastPos = transform.position;
        }

        private void OnEnable()
        {
            if (_unit == null) _unit = GetComponentInParent<CombatUnit>();
            if (_unit != null) _unit.Attacked += OnAttacked;
        }

        private void OnDisable()
        {
            if (_unit != null) _unit.Attacked -= OnAttacked;
        }

        private void OnAttacked()
        {
            if (attackFrames != null && attackFrames.Length > 0) _attackTime = 0f;
        }

        private void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.deltaTime;

            // ความเร็วจริง (ใช้ได้ทั้งตอนมี/ไม่มี Rigidbody)
            float speed = _rb != null ? _rb.linearVelocity.magnitude
                                      : (dt > 0f ? Vector3.Distance(transform.position, _lastPos) / dt : 0f);
            _lastPos = transform.position;

            if (_attackTime >= 0f)
            {
                _attackTime += dt;
                int i = Mathf.FloorToInt(_attackTime * Mathf.Max(0.1f, attackFps));
                if (i < attackFrames.Length) { Show(attackFrames[i]); return; }
                _attackTime = -1f;
            }

            if (walkFrames == null || walkFrames.Length == 0) return;
            if (speed > moveThreshold)
            {
                _walkTime += dt;
                int i = Mathf.FloorToInt(_walkTime * Mathf.Max(0.1f, walkFps)) % walkFrames.Length;
                Show(walkFrames[i]);
            }
            else
            {
                _walkTime = 0f;
                Show(idleFrame != null ? idleFrame : walkFrames[0]);
            }
        }

        private void Show(Sprite s)
        {
            if (s != null && target.sprite != s) target.sprite = s;
        }
    }
}
