using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// สไลม์ที่แช่อยู่ในหม้อต้ม (ป้อม Explosive)
    /// - ว่าง: ลอยขึ้นลงในลาวา + หายใจยืดหด + บางทีเด้งตัวเล็กน้อย
    /// - โจมตี: ย่อตัว (ตั้งท่า) -> กระโดดออกไป (ตัวในหม้อหายไป กระสุนรูปสไลม์บินออกแทน) -> ผุดขึ้นจากลาวาตัวใหม่
    /// จุดหมุนของตัวนี้ = ระดับผิวลาวา (ผุดขึ้น = ยืดจากผิวลาวาขึ้นมา)
    /// </summary>
    public class TowerSlimePart : MonoBehaviour
    {
        [Header("Parts")]
        [Tooltip("รูปสไลม์ในหม้อ (เต็มตัว ครึ่งล่างถูกลาวาด้านหน้าบังไว้)")]
        [SerializeField] private SpriteRenderer inPotRenderer;
        [Tooltip("รูปสไลม์เต็มตัว (ใช้เป็นหน้าตากระสุนตอนกระโดดออกไป)")]
        [SerializeField] private Sprite fullBodySprite;

        [Header("Anger (ก่อนกระโดด: ตาโกรธ + แดงจากล่างขึ้นบน)")]
        [Tooltip("ตาโกรธ (วางทับตาเดิม)")]
        [SerializeField] private SpriteRenderer angryEyes;
        [Tooltip("ตัวสีแดงที่ไล่สูงขึ้น (ระบบตัดรูปแดงเป็นชั้นๆ ให้เอง)")]
        [SerializeField] private SpriteRenderer redFill;
        [Tooltip("รูปสไลม์แดงตาโกรธ (ใช้ทั้งตอนไล่สีแดง และเป็นหน้าตากระสุน - ว่าง = ไม่แดง)")]
        [SerializeField] private Sprite angryBodySprite;
        [Tooltip("ความละเอียดของการไล่สี (จำนวนชั้น)")]
        [SerializeField] private int fillSteps = 32;
        [Tooltip("สีแดงเริ่มไล่ขึ้นตอนกี่ % ของเวลาตั้งท่า")]
        [Range(0f, 1f)]
        [SerializeField] private float fillStart = 0.05f;
        [Tooltip("สีแดงเต็มตัวตอนกี่ % ของเวลาตั้งท่า")]
        [Range(0f, 1f)]
        [SerializeField] private float fillEnd = 0.8f;
        [Tooltip("เริ่มย่อตัวตอนกี่ % ของเวลาตั้งท่า")]
        [Range(0f, 1f)]
        [SerializeField] private float squashStart = 0.5f;

        [Header("Idle (แช่ในหม้อ)")]
        [Tooltip("ลอยขึ้นลงกี่หน่วย")]
        [SerializeField] private float bobHeight = 0.02f;
        [SerializeField] private float bobSpeed = 0.7f;
        [Tooltip("ยืดหดหายใจกี่ % (0.05 = 5%)")]
        [SerializeField] private float breathe = 0.05f;
        [Tooltip("เด้งตัวเล่นทุกๆ กี่วินาที (0 = ปิด)")]
        [SerializeField] private float idleHopEvery = 4f;
        [Tooltip("เด้งสูงกี่หน่วย")]
        [SerializeField] private float idleHopHeight = 0.07f;
        [Range(0f, 1f)]
        [SerializeField] private float phase = 0f;

        [Header("Attack (กระโดดใส่ศัตรู)")]
        [Tooltip("เวลาตั้งท่าทั้งหมด (โกรธ + แดง + ย่อตัว) วินาที = ดีเลย์ก่อนกระโดดออก")]
        [SerializeField] private float crouchTime = 0.6f;
        [Tooltip("ย่อตัว: กว้าง x / สูง y เท่า")]
        [SerializeField] private Vector2 crouchScale = new Vector2(1.25f, 0.65f);
        [Tooltip("หลังกระโดดออก รอกี่วินาทีตัวใหม่ค่อยผุดขึ้น")]
        [SerializeField] private float respawnDelay = 0.35f;
        [Tooltip("เวลาผุดขึ้นจากลาวา (วินาที)")]
        [SerializeField] private float emergeTime = 0.35f;

        private Vector3 _pos, _scale;
        private bool _captured;
        private float _seed;

        private enum State { Idle, Crouch, Gone, Emerge }
        private State _state = State.Idle;
        private float _t, _hopTimer, _hopT = -1f;

        public float CrouchTime => crouchTime;
        /// <summary>รูปที่ใช้เป็นกระสุน (แดงตาโกรธ ถ้ามี)</summary>
        public Sprite FullBodySprite => angryBodySprite != null ? angryBodySprite : fullBodySprite;

        /// <summary>ตำแหน่งที่กระสุนเริ่มบิน (กลางตัวสไลม์)</summary>
        public Vector2 LaunchPosition => inPotRenderer != null ? (Vector2)inPotRenderer.bounds.center : (Vector2)transform.position;

        /// <summary>ขนาดจริงในโลก (ใช้ให้กระสุนตัวเท่าสไลม์ในหม้อ)</summary>
        public Vector3 WorldScale
        {
            get
            {
                // วัดตอนยืนปกติ (ไม่เอาตอนย่อตัว): สเกลของพ่อ x สเกลปกติของตัวนี้ x สเกลรูป
                Capture();
                Vector3 parent = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
                Vector3 child = inPotRenderer != null ? inPotRenderer.transform.localScale : Vector3.one;
                Vector3 s = Vector3.Scale(Vector3.Scale(parent, _scale), child);
                s.z = 1f;
                return s;
            }
        }

        private void Awake()
        {
            Capture();
            _seed = Random.value * 10f;
            _hopTimer = idleHopEvery * (0.5f + Random.value);
        }

        private void Capture()
        {
            if (_captured) return;
            _pos = transform.localPosition;
            _scale = transform.localScale;
            _captured = true;
        }

        private void OnDisable()
        {
            if (!_captured) return;
            _state = State.Idle;
            transform.localPosition = _pos;
            transform.localScale = _scale;
            if (inPotRenderer != null) inPotRenderer.enabled = true;
            SetAnger(false, 0f);
        }

        // รูปแดงที่ตัดเป็นชั้นจากล่างขึ้นบน (ใช้ร่วมกันทุกป้อม)
        private static readonly Dictionary<Sprite, Sprite[]> _sliceCache = new Dictionary<Sprite, Sprite[]>();
        private Sprite[] _slices;
        private int _sliceShown = -1;

        private void BuildSlices()
        {
            if (_slices != null || angryBodySprite == null || redFill == null) return;
            if (_sliceCache.TryGetValue(angryBodySprite, out _slices)) return;
            int n = Mathf.Max(2, fillSteps);
            _slices = new Sprite[n + 1];
            Rect r = angryBodySprite.rect;
            for (int i = 1; i <= n; i++)
            {
                float h = Mathf.Max(1f, Mathf.Round(r.height * i / n));
                _slices[i] = Sprite.Create(angryBodySprite.texture, new Rect(r.x, r.y, r.width, h),
                    new Vector2(0.5f, 0f), angryBodySprite.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                _slices[i].name = angryBodySprite.name + "_fill" + i;
            }
            _sliceCache[angryBodySprite] = _slices;
        }

        /// <summary>เปิด/ปิดตาโกรธ + ความสูงสีแดง (0 = ไม่แดง, 1 = แดงเต็มตัว) ไล่จากผิวลาวาขึ้นไปจนสุดหัว</summary>
        private void SetAnger(bool angry, float fill)
        {
            if (angryEyes != null) angryEyes.enabled = angry;
            if (redFill == null) return;
            BuildSlices();
            if (!angry || fill <= 0f || _slices == null || inPotRenderer == null) { redFill.enabled = false; _sliceShown = -1; return; }

            // วางรูปแดงให้ขอบล่างตรงกับขอบล่างของตัวสไลม์
            Transform body = inPotRenderer.transform;
            Bounds b = angryBodySprite.bounds;
            float bottomLocal = (b.center.y - b.extents.y) * body.localScale.y;
            redFill.transform.localPosition = body.localPosition + new Vector3(0f, bottomLocal, 0f);
            redFill.transform.localScale = body.localScale;

            // เริ่มจากส่วนที่จมใต้ลาวา (มองไม่เห็นอยู่แล้ว) จะได้เห็นแดงขึ้นทันที
            float bottomRig = body.localPosition.y + bottomLocal;
            float height = b.size.y * body.localScale.y;
            float start = height > 0.0001f ? Mathf.Clamp01(-bottomRig / height) : 0f;
            float k = Mathf.Lerp(start, 1f, Mathf.Clamp01(fill));
            int idx = Mathf.Clamp(Mathf.CeilToInt(k * (_slices.Length - 1)), 1, _slices.Length - 1);
            if (idx != _sliceShown) { redFill.sprite = _slices[idx]; _sliceShown = idx; }
            redFill.enabled = true;
        }

        public void BeginCrouch()
        {
            if (_state == State.Gone) return; // ยังไม่ผุดกลับมา (ป้อมยิงถี่มาก) ไม่ต้องย่อ
            _state = State.Crouch;
            _t = 0f;
            SetAnger(true, 0f);
        }

        /// <summary>กระโดดออกไปแล้ว: ซ่อนตัวในหม้อ แล้วผุดตัวใหม่</summary>
        public void Launch()
        {
            _state = State.Gone;
            _t = 0f;
            if (inPotRenderer != null) inPotRenderer.enabled = false;
            SetAnger(false, 0f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            float tau = Mathf.PI * 2f;
            float tt = Time.time + _seed;
            float p = phase * tau;

            float y = Mathf.Sin(tt * bobSpeed * tau + p) * bobHeight;
            float br = Mathf.Sin(tt * bobSpeed * 1.3f * tau + p) * breathe;
            float sx = 1f - br * 0.5f, sy = 1f + br;

            switch (_state)
            {
                case State.Crouch:
                {
                    float c = Mathf.Clamp01(_t / Mathf.Max(0.01f, crouchTime));
                    // แดงไล่จากล่างขึ้นบน
                    float f = Mathf.Clamp01((c - fillStart) / Mathf.Max(0.01f, fillEnd - fillStart));
                    SetAnger(true, f * f * (3f - 2f * f));
                    // ย่อตัวช่วงท้าย
                    float sc = Mathf.Clamp01((c - squashStart) / Mathf.Max(0.01f, 1f - squashStart));
                    float e = sc * sc * (3f - 2f * sc);
                    sx = Mathf.Lerp(sx, crouchScale.x, e);
                    sy = Mathf.Lerp(sy, crouchScale.y, e);
                    // สั่นเพิ่มขึ้นเรื่อยๆ ตอนโกรธ
                    y += Mathf.Sin(_t * 70f) * 0.006f * Mathf.Max(f, e);
                    if (_t > crouchTime + 0.4f) { _state = State.Idle; SetAnger(false, 0f); } // ไม่ได้กระโดด (เป้าหาย)
                    break;
                }
                case State.Gone:
                    if (_t >= respawnDelay)
                    {
                        _state = State.Emerge;
                        _t = 0f;
                        if (inPotRenderer != null) inPotRenderer.enabled = true;
                    }
                    sy = 0f;
                    break;
                case State.Emerge:
                {
                    float c = Mathf.Clamp01(_t / Mathf.Max(0.01f, emergeTime));
                    float back = EaseOutBack(c);
                    sy *= back;
                    sx *= Mathf.Lerp(1.25f, 1f, c);
                    if (c >= 1f) _state = State.Idle;
                    break;
                }
                default:
                    if (idleHopEvery > 0f)
                    {
                        _hopTimer -= dt;
                        if (_hopTimer <= 0f) { _hopT = 0f; _hopTimer = idleHopEvery * (0.7f + Random.value * 0.6f); }
                    }
                    if (_hopT >= 0f)
                    {
                        _hopT += dt;
                        const float hopDur = 0.45f;
                        float h = _hopT / hopDur;
                        if (h < 0.25f) { float k = h / 0.25f; sx *= 1f + 0.15f * k; sy *= 1f - 0.2f * k; }   // ย่อ
                        else if (h < 1f)
                        {
                            float k = (h - 0.25f) / 0.75f;
                            y += Mathf.Sin(k * Mathf.PI) * idleHopHeight;                                   // เด้ง
                            sy *= 1f + 0.12f * Mathf.Sin(k * Mathf.PI); sx *= 1f - 0.06f * Mathf.Sin(k * Mathf.PI);
                        }
                        else _hopT = -1f;
                    }
                    break;
            }

            transform.localPosition = _pos + new Vector3(0f, y, 0f);
            transform.localScale = new Vector3(_scale.x * sx, _scale.y * Mathf.Max(0f, sy), _scale.z);
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float a = x - 1f;
            return 1f + c3 * a * a * a + c1 * a * a;
        }
    }
}
