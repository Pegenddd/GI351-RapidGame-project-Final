using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;

namespace NecroDefense.Combat
{
    /// <summary>
    /// Base class for all projectiles (Arrows, Magic bolts, Phantasmic orbs).
    /// Demonstrates Inheritance and Encapsulation.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class Projectile : Entity
    {
        [Header("Projectile Properties (Editable in Inspector)")]
        [Tooltip("Travel speed in units per second")]
        [SerializeField] private float speed = 10f;
        [Tooltip("Maximum lifetime before self-destruction in seconds")]
        [SerializeField] private float maxLifetime = 5f;
        [Tooltip("Number of enemies this projectile can pierce through")]
        [SerializeField] private int maxPierce = 1;

        [Header("Visual Representation (Editable in Inspector)")]
        [Tooltip("Identifier for procedural 1-bit sprite generation (e.g. DarkBolt, BoneArrow, SpectralArrow)")]
        [SerializeField] private string projectileType = "DarkBolt";
        [Tooltip("Scale multiplier for projectile visual size")]
        [SerializeField] private float visualScale = 1.3f;

        protected SpriteRenderer _spriteRenderer;
        protected float _damage;
        protected DamageType _damageType;
        protected Faction _sourceFaction;
        protected Unit _sourceUnit;
        protected Vector2 _moveDirection;
        protected int _currentPierceCount = 0;
        protected float _spawnTime;

        public float Speed => speed;
        public float MaxLifetime => maxLifetime;

        protected override void Awake()
        {
            base.Awake();
            EnsureVisuals();
        }

        protected virtual void Start()
        {
            _spawnTime = Time.time;
            EnsureVisuals();
        }

        /// <summary>
        /// Guarantees that the projectile has a valid 1-bit sprite, pure white color,
        /// and correct sorting order so it is always 100% visible on screen.
        /// </summary>
        public void EnsureVisuals()
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
                if (_spriteRenderer == null)
                {
                    _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (_spriteRenderer != null)
            {
                if (_spriteRenderer.sprite == null)
                {
                    _spriteRenderer.sprite = Utils.ProceduralVisuals.GetProjectileSprite(projectileType);
                }
                _spriteRenderer.color = Color.white;
                if (_spriteRenderer.sortingOrder < 12)
                {
                    _spriteRenderer.sortingOrder = 12; // Above units and ground
                }
            }

            if (visualScale > 0.05f)
            {
                transform.localScale = Vector3.one * visualScale;
            }
        }

        public virtual void Initialize(Vector2 direction, float damage, DamageType type, Faction sourceFaction, Unit sourceUnit = null)
        {
            _moveDirection = direction.normalized;
            _damage = damage;
            _damageType = type;
            _sourceFaction = sourceFaction;
            _sourceUnit = sourceUnit;

            EnsureVisuals();

            // Rotate projectile to face travel direction
            float angle = Mathf.Atan2(_moveDirection.y, _moveDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        protected virtual void Update()
        {
            // Move linearly in direction
            transform.position += (Vector3)(_moveDirection * speed * Time.deltaTime);

            if (Time.time >= _spawnTime + maxLifetime)
            {
                OnExpired();
            }
        }

        protected virtual void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<IDamageable>(out var damageable) &&
                other.TryGetComponent<ITargetable>(out var targetable))
            {
                // Damage opposite faction
                if (targetable.Team != _sourceFaction && targetable.IsValidTarget)
                {
                    DamageInfo info = new DamageInfo(_damage, _damageType, _sourceUnit, _moveDirection, 2f);
                    damageable.TakeDamage(info);
                    _currentPierceCount++;

                    OnHitTarget(targetable);

                    if (_currentPierceCount >= maxPierce)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }

        protected virtual void OnHitTarget(ITargetable target) { }

        protected virtual void OnExpired()
        {
            Destroy(gameObject);
        }
    }
}

