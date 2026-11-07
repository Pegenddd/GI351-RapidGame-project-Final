using UnityEngine;

namespace NecroDefense
{
    /// <summary>เอฟเฟกต์วงแหวนขยายแล้วจางหาย (ใช้กับสกิล E, R, ระเบิด, วาร์ป)</summary>
    public class RingEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float duration = 0.35f;
        [Tooltip("ขนาดเริ่ม (สัดส่วนของรัศมีสุดท้าย)")]
        [SerializeField] private float startScaleFraction = 0.2f;

        private float _age;
        private float _endDiameter = 1f;
        private Color _color = Color.white;

        /// <summary>เริ่มเล่นเอฟเฟกต์ ให้ขยายจนถึงรัศมี radius</summary>
        public void Play(float radius)
        {
            _endDiameter = Mathf.Max(0.1f, radius * 2f);
            _age = 0f;
            if (spriteRenderer != null) _color = spriteRenderer.color;
            transform.localScale = Vector3.one * _endDiameter * startScaleFraction;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / duration);
            transform.localScale = Vector3.one * Mathf.Lerp(_endDiameter * startScaleFraction, _endDiameter, t);
            if (spriteRenderer != null)
            {
                Color c = _color;
                c.a = _color.a * (1f - t);
                spriteRenderer.color = c;
            }
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
