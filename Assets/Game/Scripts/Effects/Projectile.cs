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

        [Header("On Hit")]
        [Tooltip("รัศมีระเบิด (0 = โดนตัวเดียว)")]
        [SerializeField] private float areaRadius = 0f;
        [Tooltip("เอฟเฟกต์ตอนชน (ไม่ใส่ก็ได้)")]
        [SerializeField] private RingEffect impactEffect;

        private Team _team;
        private Unit _target;
        private Vector2 _direction = Vector2.right;
        private float _damage;
        private bool _canHitFlying = true;
        private Tower _sourceTower;
        private float _slowPercent, _slowDuration;
        private float _age;
        private bool _launched;

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
        }

        public void SetAreaRadius(float radius) => areaRadius = radius;
        public void SetSlow(float percent, float duration) { _slowPercent = percent; _slowDuration = duration; }

        private void Update()
        {
            if (!_launched || !GameManager.IsGameplayRunning) return;
            float dt = Time.deltaTime;
            _age += dt;

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
            OnImpact?.Invoke(pos);
            Destroy(gameObject);
        }

        private void HitUnit(Unit u)
        {
            u.TakeDamage(_damage, _sourceTower);
            if (_slowPercent > 0f) u.ApplySlow(_slowPercent, _slowDuration);
        }
    }
}
