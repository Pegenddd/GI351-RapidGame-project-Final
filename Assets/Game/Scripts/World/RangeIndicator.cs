using UnityEngine;

namespace NecroDefense
{
    /// <summary>วงระยะยิงที่โชว์ตอนเลือกป้อม/แท่น (วงกลมจางๆ + ขอบ)</summary>
    public class RangeIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer fill;
        [SerializeField] private SpriteRenderer outline;

        private void Awake() => Hide();

        public void Show(Vector2 center, float radius)
        {
            transform.position = center;
            transform.localScale = Vector3.one * Mathf.Max(0.1f, radius * 2f);
            if (fill != null) fill.enabled = true;
            if (outline != null) outline.enabled = true;
        }

        public void Hide()
        {
            if (fill != null) fill.enabled = false;
            if (outline != null) outline.enabled = false;
        }
    }
}
