using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// วิญญาณบินจากศพเข้ากระเป๋ามินเนียน (เอฟเฟกต์ตอนชุบสำเร็จ)
    /// สร้างจาก Template (ลูกที่ซ่อนอยู่) -> บินโค้งๆ ไปที่ปุ่มกระเป๋า (หรือช่องของมินเนียนนั้นถ้ากระเป๋าเปิดอยู่)
    /// </summary>
    public class SoulFlyUI : MonoBehaviour
    {
        [Tooltip("ต้นแบบวิญญาณ (Image ที่ซ่อนไว้) จะถูก Clone ทุกครั้งที่บิน")]
        [SerializeField] private RectTransform soulTemplate;
        [SerializeField] private MinionBagUI bag;
        [Tooltip("เวลาบิน (วินาที)")]
        [SerializeField] private float flyTime = 0.9f;
        [Tooltip("ความโค้งของทางบิน (หน่วย UI)")]
        [SerializeField] private float arcHeight = 160f;
        [Tooltip("สุ่มดีเลย์เล็กน้อย ให้ชุบหลายตัวพร้อมกันแล้วดูไหลเป็นสาย")]
        [SerializeField] private float maxStartDelay = 0.25f;
        [SerializeField] private Color soulColor = new Color(0.75f, 1f, 0.85f, 0.9f);

        private void Awake()
        {
            if (soulTemplate != null) soulTemplate.gameObject.SetActive(false);
        }

        public void Fly(Sprite icon, Vector2 fromWorld, int bagIndex, Action onArrive)
        {
            Camera cam = Camera.main;
            if (soulTemplate == null || cam == null) { onArrive?.Invoke(); return; }
            Vector2 from = cam.WorldToScreenPoint(fromWorld);
            StartCoroutine(FlyRoutine(icon, from, bagIndex, onArrive));
        }

        private IEnumerator FlyRoutine(Sprite icon, Vector2 from, int bagIndex, Action onArrive)
        {
            float delay = UnityEngine.Random.Range(0f, maxStartDelay);
            while (delay > 0f) { delay -= Time.deltaTime; yield return null; }

            RectTransform soul = Instantiate(soulTemplate, soulTemplate.parent);
            soul.gameObject.SetActive(true);
            Image img = soul.GetComponent<Image>();
            if (img != null) { img.sprite = icon; img.preserveAspect = true; img.color = soulColor; }

            float scale = transform.lossyScale.x;
            float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / Mathf.Max(0.05f, flyTime);
                float e = t * t * (3f - 2f * t); // ease in-out
                Vector2 to = Target(bagIndex);
                Vector2 mid = (from + to) * 0.5f + new Vector2(side * arcHeight * 0.5f, arcHeight) * scale;
                Vector2 p = Vector2.Lerp(Vector2.Lerp(from, mid, e), Vector2.Lerp(mid, to, e), e);
                soul.position = p;
                soul.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.5f, e);
                soul.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 20f) * 10f);
                yield return null;
            }
            Destroy(soul.gameObject);
            onArrive?.Invoke();
            if (bag != null) bag.Pulse();
        }

        private Vector2 Target(int bagIndex)
        {
            RectTransform r = bag != null ? bag.GetSoulTarget(bagIndex) : null;
            return r != null ? (Vector2)r.position : new Vector2(80f, 80f);
        }
    }
}
