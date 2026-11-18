using System;
using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// กระสุนทุกชนิด (ลูกบอลเวทผู้เล่น, ธนู, ลูกป้อม)
    /// ค่าพื้นฐานตั้งใน Prefab / ค่าดาเมจ-ทีม-เป้าหมาย ถูกส่งมาตอนยิง (Launch)
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [Header("Flight")]
        [SerializeField] private float speed = 10f;
        [Tooltip("ตามเป้าหมาย (ถ้าเป้าตายจะบินตรงต่อ)")]
        [SerializeField] private bool homing = true;
        [Tooltip("รัศมีชน")]
        [SerializeField] private float hitRadius = 0.2f;
        [Tooltip("อายุกระสุน (วินาที)")]
        [SerializeField] private float lifetime = 3f;
        [SerializeField] private bool rotateToDirection = true;

        [Header("Arc (โยนโค้ง เช่น สไลม์กระโดด)")]
        [Tooltip("ความสูงโค้ง (หน่วย) 0 = บินตรงแบบเดิม / มากกว่า 0 = ลอยโค้งไปตกที่เป้า (ชนตอนตกเท่านั้น)")]
        [SerializeField] private float arcHeight = 0f;
        [Tooltip("โค้งสูงเพิ่มตามระยะ (ต่อ 1 หน่วยระยะทาง)")]
        [SerializeField] private float arcHeightPerDistance = 0.15f;
        [Tooltip("เวลาบินขั้นต่ำ (วินาที) ระยะใกล้จะได้ไม่พุ่งเร็วเกิน")]
        [SerializeField] private float arcMinFlightTime = 0.35f;
        [Tooltip("ยืด-หดตามความเร็วตอนลอย (0 = ไม่ยืด)")]
        [SerializeField] private float arcStretch = 0.25f;
        [Tooltip("หมุนกลางอากาศ (องศา/วินาที, 0 = ไม่หมุน)")]
        [SerializeField] private float spinSpeed = 0f;

        [Header("On Hit")]
        [Tooltip("รัศมีระเบิด (0 = โดนตัวเดียว)")]
        [SerializeField] private float areaRadius = 0f;
        [Tooltip("เอฟเฟกต์ตอนชน (ไม่ใส่ก็ได้)")]
        [SerializeField] private RingEffect impactEffect;
        [Tooltip("ของที่เกิดตอนชนเพิ่ม เช่น สไลม์แตกกระจาย (FX_SlimeSplat) - ไม่ใส่ก็ได้")]
        [SerializeField] private GameObject impactSpawn;
        [Tooltip("เสียงตอนกระทบ (ว่าง = เดาจากชื่อ Prefab: PlayerBolt / MagicOrb / Explosive / Arrow)")]
        [SerializeField] private string impactSound = "";

        private Team _team;
        private Unit _target;
        private Vector2 _direction = Vector2.right;
        private float _damage;
        private bool _canHitFlying = true;
        private Tower _sourceTower;
        private float _slowPercent, _slowDuration;
        private float _age;
        private bool _launched;
        private bool _arc;
        private Vector2 _arcStart, _arcEnd;
        private float _arcT, _arcTime, _arcPeak;
        private Vector3 _baseScale = Vector3.one;
        private SpriteRenderer _sr;

        /// <summary>เรียกตอนชน (ใช้ทำ Poison Pool ของป้อม Explosive)</summary>
        public Action<Vector2> OnImpact;

        private static readonly List<Unit> _buffer = new List<Unit>();

        public void Launch(Team ownerTeam, Unit target, float damage, bool canHitFlying, Tower sourceTower = null)
        {
            _team = ownerTeam;
            _target = target;
            _damage = damage;
            _canHitFlying = canHitFlying;
            _sourceTower = sourceTower;
            if (target != null) _direction = (target.Position - (Vector2)transform.position).normalized;
            if (_direction.sqrMagnitude < 0.001f) _direction = Vector2.right;
            _launched = true;
            _baseScale = transform.localScale;

            _arc = arcHeight > 0f;
            if (_arc)
            {
                _arcStart = transform.position;
                _arcEnd = target != null ? target.Position : _arcStart + _direction * 3f;
                float dist = Vector2.Distance(_arcStart, _arcEnd);
                _arcTime = Mathf.Max(arcMinFlightTime, dist / Mathf.Max(0.1f, speed));
                _arcPeak = arcHeight + arcHeightPerDistance * dist;
                _arcT = 0f;
            }
        }

        /// <summary>เปลี่ยนหน้าตากระสุน (เช่น ใช้รูปสไลม์ของป้อม) ขนาด = ขนาดจริงในโลก</summary>
        public void SetLook(Sprite sprite, Vector3 worldScale)
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null && sprite != null) { _sr.sprite = sprite; _sr.color = Color.white; }
            transform.localScale = worldScale;
            _baseScale = worldScale;
        }

        public void SetAreaRadius(float radius) => areaRadius = radius;
        public void SetSlow(float percent, float duration) { _slowPercent = percent; _slowDuration = duration; }

        private void Update()
        {
            if (!_launched || !GameManager.IsGameplayRunning) return;
            float dt = Time.deltaTime;
            _age += dt;

            if (_arc) { UpdateArc(dt); return; }

            if (homing && _target != null && _target.IsAlive)
                _direction = (_target.Position - (Vector2)transform.position).normalized;

            transform.position += (Vector3)(_direction * speed * dt);
            if (rotateToDirection)
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);

            Unit hit = Registry.FindNearestHostile(transform.position, _team, hitRadius, _canHitFlying);
            if (hit != null)
            {
                Impact(hit);
                return;
            }

            if (_age >= lifetime)
            {
                if (areaRadius > 0f) Impact(null);
                else Destroy(gameObject);
            }
        }

        /// <summary>ลอยโค้งจากจุดยิงไปตกที่เป้า (ตามเป้าถ้ายังไม่ตาย) แล้วระเบิดตอนตก</summary>
        private void UpdateArc(float dt)
        {
            if (homing && _target != null && _target.IsAlive) _arcEnd = _target.Position;
            _arcT += dt / Mathf.Max(0.01f, _arcTime);
            float t = Mathf.Clamp01(_arcT);

            Vector2 ground = Vector2.Lerp(_arcStart, _arcEnd, t);
            float h = 4f * _arcPeak * t * (1f - t);
            transform.position = new Vector3(ground.x, ground.y + h, transform.position.z);

            // ยืดตอนพุ่งขึ้น/ดิ่งลง หดตอนลอยค้างบนยอด
            float v = Mathf.Abs(1f - 2f * t);
            float st = arcStretch * v;
            transform.localScale = new Vector3(_baseScale.x * (1f - st * 0.5f), _baseScale.y * (1f + st), _baseScale.z);
            if (spinSpeed != 0f) transform.rotation = Quaternion.Euler(0f, 0f, transform.eulerAngles.z + spinSpeed * dt);
            else transform.rotation = Quaternion.identity;

            if (t >= 1f) Impact(null);
        }

        private void Impact(Unit directHit)
        {
            Vector2 pos = transform.position;
            if (areaRadius > 0f)
            {
                Registry.GetHostilesInRadius(pos, areaRadius, _team, _canHitFlying, _buffer);
                for (int i = 0; i < _buffer.Count; i++) HitUnit(_buffer[i]);
            }
            else if (directHit != null)
            {
                HitUnit(directHit);
            }

            if (impactEffect != null)
            {
                RingEffect fx = Instantiate(impactEffect, pos, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Effects));
                fx.Play(areaRadius > 0f ? areaRadius : 0.4f);
            }
            if (impactSpawn != null)
            {
                GameObject spawned = Instantiate(impactSpawn, pos, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Effects));
                if (spawned.TryGetComponent(out SpriteSplatEffect splat))
                {
                    if (_sr == null) _sr = GetComponent<SpriteRenderer>();
                    splat.Play(_sr != null ? _sr.sprite : null, _baseScale, areaRadius);
                }
            }
            PlayImpactSound(pos);
            OnImpact?.Invoke(pos);
            Destroy(gameObject);
        }

        private void PlayImpactSound(Vector2 pos)
        {
            string id = impactSound;
            if (string.IsNullOrEmpty(id))
            {
                string n = gameObject.name;
                if (n.Contains("PlayerBolt")) id = "SFX_Player_Bolt_Hit";
                else if (n.Contains("MagicOrb")) id = "SFX_Tower_Orb_Hit";
                else if (n.Contains("Explosive")) id = "SFX_Tower_Bomb_Explode";
                else if (n.Contains("Arrow")) id = "SFX_Enemy_Arrow_Hit";
            }
            if (string.IsNullOrEmpty(id)) return;
            Sfx.At(id, pos, 0.75f);
            if (id == "SFX_Tower_Bomb_Explode") CameraShake.Shake(0.06f, 0.15f);
        }

        private void HitUnit(Unit u)
        {
            u.TakeDamage(_damage, _sourceTower);
            if (_slowPercent > 0f) u.ApplySlow(_slowPercent, _slowDuration);
        }
    }
}
