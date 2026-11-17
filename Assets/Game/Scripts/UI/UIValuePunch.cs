using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>ตัวเลขเด้งตอนค่าเปลี่ยน (เช่น Souls)</summary>
    [RequireComponent(typeof(TMP_Text))]
    public class UIValuePunch : MonoBehaviour
    {
        public float punch = 0.22f;
        private TMP_Text _t;
        private string _last;
        private float _k;
        private Vector3 _base;

        private void Awake()
        {
            _t = GetComponent<TMP_Text>();
            _base = transform.localScale;
        }

        private void LateUpdate()
        {
            if (_t.text != _last)
            {
                if (_last != null) _k = 1f;
                _last = _t.text;
            }
            _k = Mathf.Max(0f, _k - Time.unscaledDeltaTime * 5f);
            transform.localScale = _base * (1f + punch * _k * _k);
        }
    }
}
