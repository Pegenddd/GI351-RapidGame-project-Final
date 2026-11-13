using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// กะโหลกเขาวัว (ป้อม Soul Beam): ลอยตอนว่าง -> ตอนโจมตีทำหน้าโกรธ (ตาแดง + คิ้วขมวด) รวมพลังระหว่างเขา แล้วยิงลำแสง
    /// - กะโหลกหลัก: มี Beam Point (จุดยิงลำแสงกลางระหว่างเขา) + แสงรวมพลัง
    /// - กะโหลกเล็กข้างฐาน (Level 3): ไม่มี Beam Point แค่โกรธ + สั่นไปพร้อมกัน
    /// </summary>
    public class TowerAngrySkullPart : MonoBehaviour
    {
        [Header("Parts")]
        [Tooltip("หน้าโกรธ (วางทับหน้าเดิม)")]
        [SerializeField] private SpriteRenderer angryFace;
        [Tooltip("จุดยิงลำแสง (กลางระหว่างเขา) - ว่าง = กะโหลกนี้ไม่ยิง")]
        [SerializeField] private Transform beamPoint;
        [Tooltip("แสงรวมพลังที่จุดยิง (ว่างได้)")]
        [SerializeField] private SpriteRenderer chargeGlow;

        [Header("Idle (ตอนว่าง)")]
        [SerializeField] private float bobHeight = 0.03f;
        [SerializeField] private float bobSpeed = 0.6f;
        [Tooltip("เอียงไปมากี่องศา")]
        [SerializeField] private float idleTilt = 2f;
        [Range(0f, 1f)]
        [SerializeField] private float phase = 0f;

        [Header("Look (หันหาเป้า)")]
        [SerializeField] private float lookTilt = 7f;
        [SerializeField] private float lookLean = 0.04f;
        [SerializeField] private float lookHoldTime = 1.2f;

        [Header("Attack (โกรธ + ยิงลำแสง)")]
        [Tooltip("เวลารวมพลังก่อนยิง (วินาที) = ดีเลย์ก่อนลำแสงออก")]
        [SerializeField] private float chargeTime = 0.3f;
        [Tooltip("หน้าโกรธค้างหลังยิงกี่วินาที")]
        [SerializeField] private float angryHoldTime = 0.45f;
        [Tooltip("เวลาหน้าโกรธค่อยๆ หาย (วินาที)")]
        [SerializeField] private float angryFadeTime = 0.2f;
        [Tooltip("หัวพองขึ้นตอนรวมพลัง (1.06 = +6%)")]
        [SerializeField] private float chargeScale = 1.06f;
        [Tooltip("หัวเชิดขึ้นตอนรวมพลัง (หน่วย)")]
        [SerializeField] private float chargeRise = 0.04f;
        [Tooltip("สั่นตอนโกรธ (หน่วย)")]
        [SerializeField] private float angryShake = 0.008f;
        [Tooltip("ถีบถอยหลังตอนยิง (หน่วย)")]
        [SerializeField] private float recoilDistance = 0.06f;
        [Tooltip("ยุบ-ขยายตอนยิง")]
        [SerializeField] private float fireSquash = 0.08f;

        [Header("Charge Glow")]
        [Tooltip("สีแสงรวมพลัง (โทนเกม: ขาว = ฝั่งเรา)")]
        [SerializeField] private Color glowColor = Color.white;
        [Tooltip("ขนาดแสงตอนรวมพลังเต็ม (หน่วย)")]
        [SerializeField] private float glowSize = 0.38f;

        private Vector3 _pos, _scale;
        private float _rot;
        private bool _captured;

        private enum Phase { Idle, Charging, Fired }
        private Phase _phase = Phase.Idle;
        private float _t, _punch, _angry;
        private Vector2 _lookDir = Vector2.down;
        private float _lookWeight, _lookTimer;

        public bool CanFire => beamPoint != null;
        public float ChargeTime => chargeTime;
        public Vector2 BeamPosition => beamPoint != null ? (Vector2)beamPoint.position : (Vector2)transform.position;

        private void Awake() => Capture();

        private void Capture()
        {
            if (_captured) return;
            _pos = transform.localPosition;
            _scale = transform.localScale;
            _rot = transform.localEulerAngles.z;
            _captured = true;
        }

        private void OnDisable()
        {
            if (!_captured) return;
            _phase = Phase.Idle;
            _punch = 0f;
            _angry = 0f;
            transform.localPosition = _pos;
            transform.localScale = _scale;
            transform.localRotation = Quaternion.Euler(0f, 0f, _rot);
            SetAngry(0f);
            SetGlow(0f, 0f);
        }

        public void BeginAttack(Vector2 targetPos)
        {
            Vector2 d = targetPos - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.0001f) _lookDir = d.normalized;
            _lookTimer = lookHoldTime;
            _phase = Phase.Charging;
            _t = 0f;
        }

        public void Fire()
        {
            _phase = Phase.Fired;
            _t = 0f;
            _punch = 1f;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            _punch = Mathf.MoveTowards(_punch, 0f, dt * 5f);
            _lookTimer -= dt;
            _lookWeight = Mathf.MoveTowards(_lookWeight, _lookTimer > 0f ? 1f : 0f, dt * (_lookTimer > 0f ? 8f : 1.5f));

            float charge = 0f, glow = 0f;
            switch (_phase)
            {
                case Phase.Charging:
                    charge = Mathf.Clamp01(_t / Mathf.Max(0.01f, chargeTime));
                    glow = charge * charge;
                    _angry = Mathf.MoveTowards(_angry, 1f, dt * 12f);
                    if (_t > chargeTime + 0.4f) { _phase = Phase.Fired; _t = 0f; } // ไม่ได้ยิง (เป้าหาย)
                    break;
                case Phase.Fired:
                    charge = Mathf.Clamp01(1f - _t / 0.25f) * 0.6f;
                    _angry = _t < angryHoldTime ? 1f : Mathf.Clamp01(1f - (_t - angryHoldTime) / Mathf.Max(0.01f, angryFadeTime));
                    if (_t > angryHoldTime + angryFadeTime) _phase = Phase.Idle;
                    break;
                default:
                    _angry = Mathf.MoveTowards(_angry, 0f, dt * 5f);
                    break;
            }

            float tau = Mathf.PI * 2f, tt = Time.time, p = phase * tau;
            Vector2 lean = _lookDir * lookLean * _lookWeight;
            Vector2 recoil = -_lookDir * recoilDistance * _punch * _punch;
            float shake = _angry > 0f ? Mathf.Sin(tt * 75f + p) * angryShake * _angry : 0f;

            Vector3 pos = _pos;
            pos.x += lean.x + recoil.x + shake;
            pos.y += Mathf.Sin(tt * bobSpeed * tau + p) * bobHeight + chargeRise * charge + lean.y * 0.5f + recoil.y;
            transform.localPosition = pos;

            float rot = _rot + Mathf.Sin(tt * bobSpeed * 0.7f * tau + p * 1.7f) * idleTilt - _lookDir.x * lookTilt * _lookWeight;
            transform.localRotation = Quaternion.Euler(0f, 0f, rot);

            float grow = 1f + (chargeScale - 1f) * charge;
            float sq = fireSquash * _punch;
            transform.localScale = new Vector3(_scale.x * grow * (1f + sq), _scale.y * grow * (1f - sq), _scale.z);

            SetAngry(_angry);
            SetGlow(Mathf.Max(glow * 0.85f, _punch), glowSize * (0.3f + 0.7f * glow + 0.6f * _punch));
        }

        private void SetAngry(float a)
        {
            if (angryFace == null) return;
            angryFace.enabled = a > 0.01f;
            Color c = angryFace.color;
            c.a = Mathf.Clamp01(a);
            angryFace.color = c;
        }

        private void SetGlow(float alpha, float size)
        {
            if (chargeGlow == null) return;
            Color c = glowColor;
            c.a *= Mathf.Clamp01(alpha);
            chargeGlow.color = c;
            chargeGlow.enabled = c.a > 0.01f;
            chargeGlow.transform.localScale = Vector3.one * Mathf.Max(0.001f, size);
        }
    }
}
