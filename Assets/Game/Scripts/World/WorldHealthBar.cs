using UnityEngine;

namespace NecroDefense
{
    /// <summary>หลอดเลือดลอยบนหัวยูนิต (ใช้ Sprite 2 ชิ้น: Background + Fill ที่ pivot ซ้าย)</summary>
    public class WorldHealthBar : MonoBehaviour
    {
        [Tooltip("ชิ้นที่ยืด/หดตามเลือด (pivot ต้องอยู่ซ้าย)")]
        [SerializeField] private Transform fill;
        [Tooltip("ซ่อนหลอดตอนเลือดเต็ม")]
        [SerializeField] private bool hideWhenFull = true;

        private Vector3 _fullScale = Vector3.one;
        private bool _init;

        private void Init()
        {
            if (_init || fill == null) return;
            _fullScale = fill.localScale;
            _init = true;
        }

        public void SetValue(float normalized)
        {
            Init();
            normalized = Mathf.Clamp01(normalized);
            if (fill != null) fill.localScale = new Vector3(_fullScale.x * normalized, _fullScale.y, _fullScale.z);
            gameObject.SetActive(!(hideWhenFull && normalized >= 0.999f));
        }
    }
}
