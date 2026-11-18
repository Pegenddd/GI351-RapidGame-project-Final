using TMPro;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>ตัวเลขลอยขึ้นแล้วจางหาย (เช่น +3 Souls ตอนเก็บพลังวิญญาณจากศพ)</summary>
    public class FloatingText : MonoBehaviour
    {
        private TextMeshPro _text;
        private float _time, _duration;
        private Vector3 _start;
        private float _rise;
        private Color _color;

        /// <summary>สร้างข้อความลอยที่ตำแหน่งนี้ (ไม่ต้องมี Prefab)</summary>
        public static FloatingText Spawn(string text, Vector2 position, Color color, float fontSize = 4.5f, float duration = 0.9f, float rise = 1f)
        {
            GameObject go = new GameObject("FloatingText");
            if (GameManager.Instance != null) go.transform.SetParent(GameManager.GetFolder(SpawnFolder.Effects), true);
            go.transform.position = position;
            FloatingText ft = go.AddComponent<FloatingText>();
            ft._text = go.AddComponent<TextMeshPro>();
            ft._text.text = text;
            ft._text.fontSize = fontSize;
            ft._text.fontStyle = FontStyles.Bold;
            ft._text.alignment = TextAlignmentOptions.Center;
            ft._text.color = color;
            ft._text.sortingOrder = 60;
            ft._text.rectTransform.sizeDelta = new Vector2(4f, 1.5f);
            ft._color = color;
            ft._duration = Mathf.Max(0.1f, duration);
            ft._start = go.transform.position;
            ft._rise = rise;
            return ft;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            float t = Mathf.Clamp01(_time / _duration);
            transform.position = _start + Vector3.up * (_rise * (1f - (1f - t) * (1f - t)));
            float pop = t < 0.15f ? Mathf.Lerp(0.6f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, (t - 0.15f) / 0.2f);
            transform.localScale = Vector3.one * pop;
            Color c = _color;
            c.a = _color.a * (t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f);
            if (_text != null) _text.color = c;
            if (t >= 1f) Destroy(gameObject);
        }
    }
}
