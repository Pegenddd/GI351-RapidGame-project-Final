using UnityEngine;
using NecroDefense.Core; // เพิ่มบรรทัดนี้เข้ามาครับ
using NecroDefense.Characters.Base;

namespace NecroDefense.Towers
{
    public class SlowProjectile : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Enemy") || collision.TryGetComponent<IDamageable>(out var damageable))
            {
                Destroy(gameObject);
            }
        }
    }
}