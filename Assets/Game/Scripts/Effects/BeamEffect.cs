using UnityEngine;

namespace NecroDefense
{
    /// <summary>ลำแสงชั่วครู่ระหว่าง 2 จุด (ใช้กับ Soul Beam Tower) ใช้ Sprite สี่เหลี่ยมยืดเอา</summary>
    public class BeamEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private float width = 0.15f;
        [SerializeField] private float duration = 0.15f;

        private float _age;
        private Color _color = Color.white;

        public void Play(Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            transform.position = (from + to) * 0.5f;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            transform.localScale = new Vector3(d.magnitude, width, 1f);
            if (spriteRenderer != null) _color = spriteRenderer.color;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            float t = Mathf.Clamp01(_age / duration);
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
