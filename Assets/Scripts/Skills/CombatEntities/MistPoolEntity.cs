using System.Collections.Generic;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;

namespace NecroDefense.Skills.CombatEntities
{
    /// <summary>
    /// Dark Mist Pool deposited behind the player.
    /// Ticks damage every 0.5s to all enemies walking through the pool.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class MistPoolEntity : MonoBehaviour
    {
        private float _damagePerTick;
        private float _duration;
        private float _radius;
        private Unit _attacker;

        private CircleCollider2D _collider;
        private SpriteRenderer _spriteRenderer;
        private float _elapsed = 0f;
        private float _tickTimer = 0f;
        private const float TickInterval = 0.5f;

        private readonly HashSet<IDamageable> _enemiesInside = new HashSet<IDamageable>();

        public void Initialize(float damagePerTick, float duration, float radius, Unit attacker)
        {
            _damagePerTick = damagePerTick;
            _duration = duration;
            _radius = radius;
            _attacker = attacker;

            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.radius = _radius;

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _spriteRenderer.sprite = Utils.ProceduralVisuals.GetProjectileSprite("MistPool");
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = 1; // On the ground

            transform.localScale = Vector3.one * (_radius * 2f);
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            _tickTimer += Time.deltaTime;

            // Tick damage every 0.5s
            if (_tickTimer >= TickInterval)
            {
                _tickTimer = 0f;
                ApplyTickDamage();
            }

            // Visual fade near end of life
            if (_elapsed >= _duration - 0.5f)
            {
                float remain = Mathf.Clamp01((_duration - _elapsed) / 0.5f);
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.color = new Color(1f, 1f, 1f, remain);
                }
            }

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
            }
        }

        private void ApplyTickDamage()
        {
            _enemiesInside.RemoveWhere(e => e == null || !e.IsAlive);

            foreach (var damageable in _enemiesInside)
            {
                if (damageable != null && damageable.IsAlive)
                {
                    DamageInfo info = new DamageInfo(_damagePerTick, DamageType.Poison, _attacker, Vector2.zero, 0f);
                    damageable.TakeDamage(info);
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
                    _enemiesInside.Add(damageable);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            if (collision.TryGetComponent<IDamageable>(out var damageable))
            {
                _enemiesInside.Remove(damageable);
            }
        }
    }
}

