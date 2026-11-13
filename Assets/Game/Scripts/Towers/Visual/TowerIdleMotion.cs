using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ขยับชิ้นส่วนป้อมตอนว่างๆ: ลอยขึ้นลง / แกว่ง / หายใจ (ยืดหด) / ไฟสั่น
    /// ใส่ได้กับทุกชิ้น (หิน, เปลวไฟ, ฐาน) ปรับค่าใน Inspector ได้เลย
    /// </summary>
    public class TowerIdleMotion : MonoBehaviour
    {
        [Header("Float (ลอยขึ้นลง)")]
        [Tooltip("ลอยสูงต่ำกี่หน่วย (0 = ไม่ลอย)")]
        [SerializeField] private float bobHeight = 0f;
        [Tooltip("รอบต่อวินาที")]
        [SerializeField] private float bobSpeed = 0.8f;

        [Header("Sway (แกว่งซ้ายขวา)")]
        [Tooltip("องศาที่แกว่ง (0 = ไม่แกว่ง)")]
        [SerializeField] private float swayAngle = 0f;
        [SerializeField] private float swaySpeed = 0.6f;

        [Header("Breathe (ยืดหดเบาๆ)")]
        [Tooltip("ยืดตามแนวตั้งกี่ % (0.02 = 2%) แนวนอนจะหดสวนกันให้ดูมีน้ำหนัก")]
        [SerializeField] private float breathe = 0f;
        [SerializeField] private float breatheSpeed = 0.5f;

        [Header("Flicker (สั่นแบบเปลวไฟ)")]
        [Tooltip("สั่นขนาดแบบสุ่มกี่ % (0 = ปิด)")]
        [SerializeField] private float flicker = 0f;
        [SerializeField] private float flickerSpeed = 8f;

        [Header("Attack Pulse (พองขึ้นตอนป้อมโจมตี)")]
        [Tooltip("พองขึ้นกี่เท่าตอนป้อมยิง (0.6 = +60%, 0 = ไม่พอง)")]
        [SerializeField] private float attackPulse = 0f;
        [Tooltip("พองค้างนานแค่ไหนหลังกระสุนออก (วินาที)")]
        [SerializeField] private float attackPulseRelease = 0.25f;

        [Tooltip("เลื่อนจังหวะ 0..1 (ชิ้นที่อยู่คู่กันใส่ต่างกันจะไม่ขยับพร้อมกันเป๊ะ)")]
        [Range(0f, 1f)]
        [SerializeField] private float phase = 0f;

        private Vector3 _pos, _scale;
        private float _rot, _seed;
        private bool _captured;
        private float _pulseT = -1f, _pulseRise, _pulseTotal;

        public bool HasAttackPulse => attackPulse > 0f;

        /// <summary>ป้อมเริ่มโจมตี: พองขึ้นจนสุดตอนกระสุนออก (peakDelay วินาที) แล้วค่อยๆ ยุบกลับ</summary>
        public void PulseForAttack(float peakDelay)
        {
            if (attackPulse <= 0f) return;
            _pulseRise = Mathf.Max(0.05f, peakDelay);
            _pulseTotal = _pulseRise + Mathf.Max(0.05f, attackPulseRelease);
            _pulseT = 0f;
        }

        private float PulseAmount()
        {
            if (_pulseT < 0f) return 0f;
            _pulseT += Time.deltaTime;
            if (_pulseT >= _pulseTotal) { _pulseT = -1f; return 0f; }
            if (_pulseT < _pulseRise)
            {
                float x = _pulseT / _pulseRise;
                return attackPulse * x * x;                      // ค่อยๆ พอง (เร่งตอนท้าย)
            }
            float r = (_pulseT - _pulseRise) / (_pulseTotal - _pulseRise);
            return attackPulse * (1f - r) * (1f - r);            // ยุบกลับ
        }

        private void Awake() => Capture();

        private void Capture()
        {
            if (_captured) return;
            _pos = transform.localPosition;
            _scale = transform.localScale;
            _rot = transform.localEulerAngles.z;
            _seed = Random.value * 100f;
            _captured = true;
        }

        private void OnDisable()
        {
            if (!_captured) return;
            transform.localPosition = _pos;
            transform.localScale = _scale;
            transform.localRotation = Quaternion.Euler(0f, 0f, _rot);
            _pulseT = -1f;
        }

        private void LateUpdate()
        {
            float t = Time.time;
            float tau = Mathf.PI * 2f;
            float p = phase * tau;

            Vector3 pos = _pos;
            if (bobHeight != 0f) pos.y += Mathf.Sin(t * bobSpeed * tau + p) * bobHeight;
            transform.localPosition = pos;

            float rot = _rot;
            if (swayAngle != 0f) rot += Mathf.Sin(t * swaySpeed * tau + p * 1.3f) * swayAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, rot);

            float sy = 1f, sx = 1f;
            if (breathe != 0f)
            {
                float b = Mathf.Sin(t * breatheSpeed * tau + p) * breathe;
                sy += b;
                sx -= b * 0.5f;
            }
            if (flicker != 0f)
            {
                float n = (Mathf.PerlinNoise(_seed, t * flickerSpeed) - 0.5f) * 2f * flicker;
                sy += n;
                sx -= n * 0.4f;
            }
            float pulse = 1f + PulseAmount();
            transform.localScale = new Vector3(_scale.x * sx * pulse, _scale.y * sy * pulse, _scale.z);
        }
    }
}
