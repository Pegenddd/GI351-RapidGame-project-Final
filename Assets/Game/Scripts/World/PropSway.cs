using UnityEngine;

namespace NecroDefense
{
    /// <summary>ของตกแต่ง (ต้นไม้) ไหวเบาๆ รอบโคน - ให้แมพดูมีชีวิต</summary>
    public class PropSway : MonoBehaviour
    {
        [Tooltip("มุมไหวสูงสุด (องศา)")]
        [SerializeField] private float angle = 1.2f;
        [Tooltip("ความเร็วไหว")]
        [SerializeField] private float speed = 0.8f;
        [Tooltip("ยืด-หดแนวตั้งเล็กน้อย (หายใจ)")]
        [SerializeField] private float breathe = 0.01f;

        private float _phase;
        private Vector3 _baseScale;

        private void Awake()
        {
            _phase = Random.Range(0f, 100f);
            _baseScale = transform.localScale;
        }

        private void Update()
        {
            float t = Time.time * speed + _phase;
            transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t) * angle);
            transform.localScale = new Vector3(_baseScale.x, _baseScale.y * (1f + Mathf.Sin(t * 1.7f) * breathe), _baseScale.z);
        }
    }
}
