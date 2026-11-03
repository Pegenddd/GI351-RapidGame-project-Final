using System.Collections.Generic;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;

namespace NecroDefense.Skills.CombatEntities
{
    /// <summary>
    /// Orbiting Skull projectile that circles around the PlayerNecromancer.
    /// Deals contact damage to enemies.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class OrbitingSkullEntity : MonoBehaviour
    {
        private Transform _centerTransform;
        private Unit _attacker;
        private float _orbitRadius = 2.2f;
        private float _rotationSpeed = 180f; // degrees per sec
        private float _currentAngle = 0f;
        private float _damage = 12f;

        private CircleCollider2D _collider;
        private SpriteRenderer _spriteRenderer;
        private readonly Dictionary<IDamageable, float> _hitTimers = new Dictionary<IDamageable, float>();
        private const float HitCooldown = 0.35f;

        public void Initialize(Transform center, float angleOffset, float radius, float speed, float damage, Unit attacker)
        {
            _centerTransform = center;
            _currentAngle = angleOffset;
            _orbitRadius = radius;
            _rotationSpeed = speed;
            _damage = damage;
            _attacker = attacker;

            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.radius = 0.4f;

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _spriteRenderer.sprite = Utils.ProceduralVisuals.GetProjectileSprite("OrbitSkull");
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = 13;

            UpdatePosition();
        }

        public void UpdateParameters(float radius, float speed, float damage)
        {
            _orbitRadius = radius;
            _rotationSpeed = speed;
            _damage = damage;
        }

        private void Update()
        {
            if (_centerTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            _currentAngle += _rotationSpeed * Time.deltaTime;
            if (_currentAngle >= 360f) _currentAngle -= 360f;

            UpdatePosition();

            // Self rotation for extra spooky visual effect
            transform.Rotate(0, 0, 360f * Time.deltaTime);

            // Clean up hit cooldowns
            List<IDamageable> keys = new List<IDamageable>(_hitTimers.Keys);
            foreach (var k in keys)
            {
                _hitTimers[k] -= Time.deltaTime;
                if (_hitTimers[k] <= 0f)
                {
                    _hitTimers.Remove(k);
                }
            }
        }

        private void UpdatePosition()
        {
            if (_centerTransform == null) return;
            float rad = _currentAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _orbitRadius;
            transform.position = _centerTransform.position + offset;
        }

        private void OnTriggerStay2D(Collider2D collision)
        {
            if (collision.TryGetComponent<IDamageable>(out var damageable) &&
                collision.TryGetComponent<ITargetable>(out var targetable))
            {
                if (targetable.Team != Faction.Player && targetable.IsValidTarget)
                {
                    if (!_hitTimers.ContainsKey(damageable))
                    {
                        _hitTimers[damageable] = HitCooldown;
                        Vector2 hitDir = ((Vector2)targetable.Position - (Vector2)transform.position).normalized;
                        DamageInfo info = new DamageInfo(_damage, DamageType.Necrotic, _attacker, hitDir, 0.3f);
                        damageable.TakeDamage(info);
                    }
                }
            }
        }
    }
}

