using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// รูปที่ตกพื้นแล้วแบนแผ่ออกแล้วจางหาย (เช่น สไลม์ป้อม Explosive ตกใส่ศัตรู)
    /// Projectile จะส่งรูปของตัวเองมาให้ (Play) - ไม่ส่งก็ใช้รูปที่ใส่ไว้ใน Prefab
    /// </summary>
    public class SpriteSplatEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [Tooltip("เวลาทั้งหมด (วินาที)")]
        [SerializeField] private float duration = 0.45f;
        [Tooltip("ตอนตกกระแทก: กว้างกี่เท่า / สูงกี่เท่า ของขนาดเดิม")]
        [SerializeField] private Vector2 impactScale = new Vector2(1.5f, 0.45f);
        [Tooltip("ตอนจบ: แผ่ออกกว้างกี่เท่า / แบนเหลือกี่เท่า")]
        [SerializeField] private Vector2 endScale = new Vector2(2.1f, 0.12f);
        [Tooltip("แผ่ตามรัศมีระเบิดด้วย (0 = ไม่สน, 1 = กว้างเท่ารัศมี)")]
        [Range(0f, 1f)]
        [SerializeField] private float matchAreaRadius = 0.5f;

        private Vector3 _baseScale = Vector3.one;
        private float _age, _areaWidth;
        private Color _color = Color.white;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) _color = spriteRenderer.color;
            _baseScale = transform.localScale;
        }

        public void Play(Sprite sprite, Vector3 worldScale, float areaRadius)
        {
            if (spriteRenderer != null && sprite != null) spriteRenderer.sprite = sprite;
            _baseScale = worldScale;
            float spriteW = spriteRenderer != null && spriteRenderer.sprite != null ? spriteRenderer.sprite.bounds.size.x * worldScale.x : 1f;
            _areaWidth = areaRadius > 0f && spriteW > 0.001f ? areaRadius * 2f / spriteW : 0f;
            _age = 0f;
            Apply(0f);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / Mathf.Max(0.01f, duration));
            Apply(t);
            if (t >= 1f) Destroy(gameObject);
        }

        private void Apply(float t)
        {
            float e = 1f - (1f - t) * (1f - t);
            float endX = Mathf.Lerp(endScale.x, Mathf.Max(endScale.x, _areaWidth), matchAreaRadius);
            float sx = Mathf.Lerp(impactScale.x, endX, e);
            float sy = Mathf.Lerp(impactScale.y, endScale.y, e);
            transform.localScale = new Vector3(_baseScale.x * sx, _baseScale.y * sy, 1f);
            if (spriteRenderer != null)
            {
                Color c = _color;
                c.a = _color.a * (1f - t * t);
                spriteRenderer.color = c;
            }
        }
    }
}
