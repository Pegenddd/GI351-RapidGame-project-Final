using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>พื้นที่ทำดาเมจต่อเนื่อง เช่น บ่อพิษ (Poison Pool) ของป้อม Explosive Tier 3</summary>
    public class DamageZone : MonoBehaviour
    {
        [SerializeField] private Team team = Team.Player;
        [SerializeField] private float radius = 1.5f;
        [Tooltip("ดาเมจต่อวินาที")]
        [SerializeField] private float damagePerSecond = 8f;
        [Tooltip("อยู่ได้กี่วินาที")]
        [SerializeField] private float duration = 3f;
        [SerializeField] private float tickInterval = 0.5f;
        [SerializeField] private bool canHitFlying = false;
        [Tooltip("ปรับขนาดรูปให้เท่ารัศมีอัตโนมัติ (รูปวงกลมขนาด 1 หน่วย)")]
        [SerializeField] private bool scaleVisualToRadius = true;

        private float _age, _tick;
        private static readonly List<Unit> _buffer = new List<Unit>();

        private void Start()
        {
            if (scaleVisualToRadius) transform.localScale = Vector3.one * radius * 2f;
        }

        private void Update()
        {
            if (!GameManager.IsGameplayRunning) return;
            float dt = Time.deltaTime;
            _age += dt;
            _tick += dt;
            if (_tick >= tickInterval)
            {
                _tick -= tickInterval;
                Registry.GetHostilesInRadius(transform.position, radius, team, canHitFlying, _buffer);
                for (int i = 0; i < _buffer.Count; i++) _buffer[i].TakeDamage(damagePerSecond * tickInterval);
            }
            if (_age >= duration) Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
