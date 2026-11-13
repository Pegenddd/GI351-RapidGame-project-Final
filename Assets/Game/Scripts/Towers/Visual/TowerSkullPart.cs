using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// กะโหลก 1 หัวบนป้อม Magic Orb: ลอยตอนว่าง, หันหาเป้า, อ้าปาก (กรามเลื่อนลง) แล้วยิงกระสุนออกจากปาก
    /// โครง: SkullX (ตัวนี้) > Cranium (หัว) / Jaw (กราม) / Mouth (จุดปล่อยกระสุน) > MouthGlow (แสงในปาก)
    /// </summary>
    public class TowerSkullPart : MonoBehaviour
    {
        [Header("Parts")]
        [SerializeField] private Transform jaw;
        [Tooltip("จุดปล่อยกระสุน (อยู่ระหว่างฟันบน-ล่าง)")]
        [SerializeField] private Transform mouth;
        [Tooltip("แสงในปาก (ว่างได้)")]
        [SerializeField] private SpriteRenderer mouthGlow;

        [Header("Idle (ตอนว่าง)")]
        [Tooltip("ลอยขึ้นลงกี่หน่วย")]
        [SerializeField] private float bobHeight = 0.035f;
        [Tooltip("รอบต่อวินาที")]
        [SerializeField] private float bobSpeed = 0.9f;
        [Tooltip("เอียงไปมากี่องศา")]
        [SerializeField] private float idleTilt = 3f;
        [Tooltip("เลื่อนจังหวะ 0..1 (กะโหลกคู่ใส่ต่างกันจะลอยสลับกัน)")]
        [Range(0f, 1f)]
        [SerializeField] private float phase = 0f;
        [Tooltip("งับปากเบาๆ เองตอนว่าง ทุกๆ กี่วินาที (0 = ปิด)")]
        [SerializeField] private float idleChatterEvery = 3.5f;

        [Header("Look (หันหาเป้า)")]
        [Tooltip("เอียงหัวไปทางเป้าได้สูงสุดกี่องศา")]
        [SerializeField] private float lookTilt = 10f;
        [Tooltip("โน้มตัวไปทางเป้ากี่หน่วย")]
        [SerializeField] private float lookLean = 0.04f;
        [Tooltip("ไม่มีเป้านานเท่านี้ (วินาที) จะหันกลับตรง")]
        [SerializeField] private float lookHoldTime = 1.2f;

        [Header("Attack (อ้าปากยิง)")]
        [Tooltip("เวลาอ้าปากตั้งท่าก่อนยิง (วินาที) = ดีเลย์ก่อนกระสุนออก")]
        [SerializeField] private float openTime = 0.14f;
        [Tooltip("อ้าค้างหลังยิงกี่วินาที")]
        [SerializeField] private float holdOpenTime = 0.06f;
        [Tooltip("เวลาหุบปาก (วินาที)")]
        [SerializeField] private float closeTime = 0.22f;
        [Tooltip("กรามเลื่อนลงกี่หน่วยตอนอ้าสุด")]
        [SerializeField] private float jawOpenDistance = 0.09f;
        [Tooltip("หัวพองขึ้นตอนตั้งท่า (1.1 = +10%)")]
        [SerializeField] private float chargeScale = 1.08f;
        [Tooltip("หัวยกขึ้นตอนตั้งท่า (หน่วย)")]
        [SerializeField] private float chargeRise = 0.04f;
        [Tooltip("ถีบถอยหลังตอนยิง (หน่วย)")]
        [SerializeField] private float recoilDistance = 0.08f;
        [Tooltip("ยุบแบน-ขยายข้างตอนยิง (squash)")]
        [SerializeField] private float fireSquash = 0.1f;

        [Header("Mouth Glow")]
        [Tooltip("สีแสงในปาก (โทนเกม: ขาว/เทา = ฝั่งเรา, ดำ = ศัตรู, แดง = เน้น)")]
        [SerializeField] private Color glowColor = Color.white;
        [Tooltip("ขนาดแสงในปาก (หน่วย)")]
        [SerializeField] private float glowSize = 0.5f;
        [Tooltip("ความสว่างแสงตอนตั้งท่า 0..1")]
        [SerializeField] private float glowChargeAlpha = 0.75f;

        private Vector3 _pos, _scale, _jawPos, _jawScale, _mouthPos;
        private float _rot;
        private bool _captured;

        private enum Phase { Idle, Charging, Fired }
        private Phase _phase = Phase.Idle;
        private float _t;              // เวลาในเฟสปัจจุบัน
        private float _punch;          // แรงตอนยิง (ค่อยๆ ลดลง)
        private Vector2 _lookDir = Vector2.down;
        private float _lookWeight, _lookTimer, _chatterTimer;

        public float OpenTime => openTime;
        public Vector2 MouthPosition => mouth != null ? (Vector2)mouth.position : (Vector2)transform.position;

        private void Awake()
        {
            Capture();
            _chatterTimer = idleChatterEvery * (0.5f + Random.value);
        }

        private void Capture()
        {
            if (_captured) return;
            _pos = transform.localPosition;
            _scale = transform.localScale;
            _rot = transform.localEulerAngles.z;
            if (jaw != null) { _jawPos = jaw.localPosition; _jawScale = jaw.localScale; }
            if (mouth != null) _mouthPos = mouth.localPosition;
            _captured = true;
        }

        private void OnDisable()
        {
            if (!_captured) return;
            _phase = Phase.Idle;
            _punch = 0f;
            transform.localPosition = _pos;
            transform.localScale = _scale;
            transform.localRotation = Quaternion.Euler(0f, 0f, _rot);
            if (jaw != null) { jaw.localPosition = _jawPos; jaw.localScale = _jawScale; }
            if (mouth != null) mouth.localPosition = _mouthPos;
            SetGlow(0f, 0f);
        }

        /// <summary>เริ่มอ้าปากเล็งเป้า</summary>
        public void BeginAttack(Vector2 targetPos)
        {
            Vector2 d = targetPos - (Vector2)transform.position;
            if (d.sqrMagnitude > 0.0001f) _lookDir = d.normalized;
            _lookTimer = lookHoldTime;
            _phase = Phase.Charging;
            _t = 0f;
        }

        /// <summary>กระสุนออกจากปากแล้ว</summary>
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

            // หันหาเป้า แล้วค่อยๆ กลับตรงเมื่อว่าง
            _lookTimer -= dt;
            _lookWeight = Mathf.MoveTowards(_lookWeight, _lookTimer > 0f ? 1f : 0f, dt * (_lookTimer > 0f ? 8f : 1.5f));

            // ----- ปากอ้าแค่ไหน (0..1) -----
            float open = 0f, charge = 0f;
            switch (_phase)
            {
                case Phase.Charging:
                    open = EaseOutBack(Mathf.Clamp01(_t / Mathf.Max(0.01f, openTime)));
                    charge = Mathf.Clamp01(_t / Mathf.Max(0.01f, openTime));
                    if (_t > openTime + 0.4f) { _phase = Phase.Fired; _t = holdOpenTime; } // ไม่ได้ยิง (เป้าหาย) -> หุบปาก
                    break;
                case Phase.Fired:
                    if (_t < holdOpenTime) open = 1f + 0.15f * _punch;
                    else
                    {
                        float c = Mathf.Clamp01((_t - holdOpenTime) / Mathf.Max(0.01f, closeTime));
                        open = 1f - c * c * (3f - 2f * c);
                        if (c >= 1f) _phase = Phase.Idle;
                    }
                    charge = open * 0.6f;
                    break;
                default:
                    // งับปากเบาๆ ตอนว่าง ให้ดูมีชีวิต
                    if (idleChatterEvery > 0f)
                    {
                        _chatterTimer -= dt;
                        if (_chatterTimer < 0.3f && _chatterTimer > 0f) open = Mathf.Sin((0.3f - _chatterTimer) / 0.3f * Mathf.PI) * 0.35f;
                        if (_chatterTimer <= 0f) _chatterTimer = idleChatterEvery * (0.7f + Random.value * 0.6f);
                    }
                    break;
            }

            float tau = Mathf.PI * 2f;
            float tt = Time.time;
            float p = phase * tau;

            // ----- หัว -----
            Vector2 lean = _lookDir * lookLean * _lookWeight;
            Vector2 recoil = -_lookDir * recoilDistance * _punch * _punch;
            Vector3 pos = _pos;
            pos.x += lean.x + recoil.x;
            pos.y += Mathf.Sin(tt * bobSpeed * tau + p) * bobHeight + chargeRise * charge + lean.y * 0.5f + recoil.y;
            transform.localPosition = pos;

            float rot = _rot + Mathf.Sin(tt * bobSpeed * 0.7f * tau + p * 1.7f) * idleTilt
                        - _lookDir.x * lookTilt * _lookWeight;
            transform.localRotation = Quaternion.Euler(0f, 0f, rot);

            float grow = 1f + (chargeScale - 1f) * charge;
            float sq = fireSquash * _punch;
            transform.localScale = new Vector3(_scale.x * grow * (1f + sq), _scale.y * grow * (1f - sq), _scale.z);

            // ----- กราม -----
            if (jaw != null)
            {
                jaw.localPosition = _jawPos + Vector3.down * (jawOpenDistance * open);
                jaw.localScale = new Vector3(_jawScale.x * (1f + 0.06f * open), _jawScale.y, _jawScale.z);
            }
            if (mouth != null) mouth.localPosition = _mouthPos + Vector3.down * (jawOpenDistance * open * 0.5f);

            // ----- แสงในปาก -----
            float glowA = Mathf.Max(glowChargeAlpha * (_phase == Phase.Charging ? open : 0f), _punch);
            SetGlow(glowA, glowSize * (0.55f + 0.45f * Mathf.Clamp01(open) + 0.5f * _punch));
        }

        private void SetGlow(float alpha, float size)
        {
            if (mouthGlow == null) return;
            Color c = glowColor;
            c.a *= Mathf.Clamp01(alpha);
            mouthGlow.color = c;
            mouthGlow.enabled = c.a > 0.01f;
            mouthGlow.transform.localScale = Vector3.one * Mathf.Max(0.001f, size);
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float a = x - 1f;
            return 1f + c3 * a * a * a + c1 * a * a;
        }
    }
}
