using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;

namespace NecroDefense.Combat
{
    /// <summary>
    /// Homing Projectile: Demonstrates Polymorphism by overriding Update behavior
    /// to dynamically steer towards a target.
    /// Used by the Necromancer's Dark Bolt and Ghost's Soul Orbs.
    /// </summary>
    public class HomingProjectile : Projectile
    {
        [Header("Homing Settings (Editable in Inspector)")]
        [Tooltip("Turn rate in degrees per second towards target")]
        [SerializeField] private float turnRate = 360f;
        [Tooltip("Continuous spin animation for magical energy ball effect")]
        [SerializeField] private bool spinEffect = true;
        [Tooltip("Spin speed in degrees per second")]
        [SerializeField] private float spinSpeed = 540f;

        private ITargetable _target;

        public void SetTarget(ITargetable target)
        {
            _target = target;
        }

        protected override void Update()
        {
            if (Time.time >= _spawnTime + MaxLifetime)
            {
                OnExpired();
                return;
            }

            if (_target != null && _target.IsValidTarget)
            {
                Vector2 targetDir = (_target.Position - transform.position).normalized;
                _moveDirection = Vector3.RotateTowards(_moveDirection, targetDir, turnRate * Mathf.Deg2Rad * Time.deltaTime, 0f);
                transform.position += (Vector3)(_moveDirection * Speed * Time.deltaTime);

                if (spinEffect)
                {
                    transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
                }
                else
                {
                    float angle = Mathf.Atan2(_moveDirection.y, _moveDirection.x) * Mathf.Rad2Deg;
                    transform.rotation = Quaternion.Euler(0, 0, angle);
                }
            }
            else
            {
                // Fallback to base straight movement if target is lost/dead
                base.Update();
            }
        }
    }
}

