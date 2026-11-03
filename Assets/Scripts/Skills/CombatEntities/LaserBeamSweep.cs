using System.Collections.Generic;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;

namespace NecroDefense.Skills.CombatEntities
{
    /// <summary>
    /// Sweeping Piercing Death Laser.
    /// Sweeps an angular arc in front of the caster, piercing all enemy monsters.
    /// Pure 2-Color High-Contrast Visual using LineRenderer.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LaserBeamSweep : MonoBehaviour
    {
        private Transform _caster;
        private Unit _attacker;
        private float _startAngle;
        private float _endAngle;
        private float _duration;
        private float _damage;
        private float _beamLength = 12f;

        private LineRenderer _line;
        private float _elapsed = 0f;
        private readonly Dictionary<IDamageable, float> _hitCooldowns = new Dictionary<IDamageable, float>();

        public void Initialize(Transform caster, float startAngle, float endAngle, float duration, float damage, Unit attacker)
        {
            _caster = caster;
            _startAngle = startAngle;
            _endAngle = endAngle;
            _duration = Mathf.Max(0.1f, duration);
            _damage = damage;
            _attacker = attacker;

            _line = GetComponent<LineRenderer>();
            _line.positionCount = 2;
            _line.startWidth = 0.28f;
            _line.endWidth = 0.28f;
            _line.material = new Material(Shader.Find("Sprites/Default"));
            _line.startColor = Color.white;
            _line.endColor = Color.white;
            _line.sortingOrder = 14;

            UpdateLaser(0f);
        }

        private void Update()
        {
            if (_caster == null)
            {
                Destroy(gameObject);
                return;
            }

            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / _duration);

            UpdateLaser(t);

            // Update hit cooldowns
            List<IDamageable> keys = new List<IDamageable>(_hitCooldowns.Keys);
            foreach (var k in keys)
            {
                _hitCooldowns[k] -= Time.deltaTime;
                if (_hitCooldowns[k] <= 0f) _hitCooldowns.Remove(k);
            }

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
            }
        }

        private void UpdateLaser(float t)
        {
            float currentAngle = Mathf.Lerp(_startAngle, _endAngle, t);
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            Vector3 startPos = _caster.position;
            Vector3 endPos = startPos + (Vector3)(dir * _beamLength);

            _line.SetPosition(0, startPos);
            _line.SetPosition(1, endPos);

            // RaycastAll along the beam to pierce all enemies
            RaycastHit2D[] hits = Physics2D.RaycastAll(startPos, dir, _beamLength);
            foreach (var hit in hits)
            {
                if (hit.collider != null &&
                    hit.collider.TryGetComponent<IDamageable>(out var damageable) &&
                    hit.collider.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team != Faction.Player && targetable.IsValidTarget)
                    {
                        if (!_hitCooldowns.ContainsKey(damageable))
                        {
                            _hitCooldowns[damageable] = 0.2f;
                            DamageInfo info = new DamageInfo(_damage, DamageType.Void, _attacker, dir, 0.2f);
                            damageable.TakeDamage(info);
                        }
                    }
                }
            }
        }
    }
}

