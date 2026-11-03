using System.Collections.Generic;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;

namespace NecroDefense.Skills.CombatEntities
{
    /// <summary>
    /// Boomerang projectile: Flies outwards, decelerates, then returns to player.
    /// Deals damage on both outward and return trips.
    /// Demonstrates 1-Bit Monochrome Visuals and Encapsulated Flight Mechanics.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class BoneScytheProjectile : MonoBehaviour
    {
        private Vector2 _direction;
        private float _outwardDamage;
        private float _returnDamage;
        private Transform _playerTransform;
        private Unit _attacker;
        private CircleCollider2D _collider;
        private SpriteRenderer _spriteRenderer;

        private float _maxFlightDistance = 6.5f;
        private float _speed = 10f;
        private Vector2 _startPosition;
        private bool _isReturning = false;
        private float _rotationSpeed = 800f;
        private float _lifetime = 4f;

        private readonly HashSet<IDamageable> _hitEnemiesOutward = new HashSet<IDamageable>();
        private readonly HashSet<IDamageable> _hitEnemiesReturn = new HashSet<IDamageable>();

        public void Initialize(Vector2 direction, float outwardDmg, float returnDmg, float sizeScale, Transform playerTrans, Unit attacker)
        {
            _direction = direction.normalized;
            _outwardDamage = outwardDmg;
            _returnDamage = returnDmg;
            _playerTransform = playerTrans;
            _attacker = attacker;
            _startPosition = transform.position;

            transform.localScale = Vector3.one * sizeScale;

            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.radius = 0.45f;

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _spriteRenderer.sprite = Utils.ProceduralVisuals.GetProjectileSprite("BoneScythe");
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = 12;

            Destroy(gameObject, _lifetime);
        }

        private void Update()
        {
            // Rotate scythe spinning effect
            transform.Rotate(0, 0, _rotationSpeed * Time.deltaTime);

            if (!_isReturning)
            {
                // Moving outward
                transform.position += (Vector3)(_direction * _speed * Time.deltaTime);
                float distTraveled = Vector2.Distance(_startPosition, transform.position);

                if (distTraveled >= _maxFlightDistance)
                {
                    _isReturning = true;
                }
            }
            else
            {
                // Returning towards player
                if (_playerTransform != null)
                {
                    Vector2 toPlayer = ((Vector2)_playerTransform.position - (Vector2)transform.position).normalized;
                    _speed = Mathf.Min(_speed + 8f * Time.deltaTime, 16f); // Accelerates on return!
                    transform.position += (Vector3)(toPlayer * _speed * Time.deltaTime);

                    if (Vector2.Distance(transform.position, _playerTransform.position) < 0.6f)
                    {
                        Destroy(gameObject);
                    }
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.TryGetComponent<IDamageable>(out var damageable) &&
                collision.TryGetComponent<ITargetable>(out var targetable))
            {
                if (targetable.Team != Faction.Player && targetable.IsValidTarget)
                {
                    var targetSet = _isReturning ? _hitEnemiesReturn : _hitEnemiesOutward;
                    if (!targetSet.Contains(damageable))
                    {
                        targetSet.Add(damageable);
                        float dmg = _isReturning ? _returnDamage : _outwardDamage;
                        DamageInfo info = new DamageInfo(dmg, DamageType.Physical, _attacker, _direction, 0.4f);
                        damageable.TakeDamage(info);
                    }
                }
            }
        }
    }
}

