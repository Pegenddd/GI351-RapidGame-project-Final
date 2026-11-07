using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ซากศพที่ศัตรูทิ้งไว้ -> คลิกซ้ายเพื่อลอง Raise Dead (สุ่มตามโอกาส)
    /// สำเร็จ = มินเนียนเข้า Minion Bag / ไม่สำเร็จ = ซากสลายไป
    /// </summary>
    public class Corpse : MonoBehaviour
    {
        [Tooltip("ซากอยู่ได้กี่วินาทีก่อนหายไป")]
        [SerializeField] private float lifetime = 10f;
        [Tooltip("กี่วินาทีสุดท้ายที่จะกระพริบเตือน")]
        [SerializeField] private float blinkTime = 3f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Look (ใช้รูปของศัตรูตัวที่ตาย)")]
        [Tooltip("สีของศพ (คูณกับรูปศัตรู)")]
        [SerializeField] private Color corpseTint = new Color(0.55f, 0.55f, 0.58f, 0.9f);
        [Tooltip("มุมล้มนอน (องศา)")]
        [SerializeField] private float fallenAngle = 80f;
        [Tooltip("บีบความสูงให้ดูแบนติดพื้น")]
        [Range(0.3f, 1f)]
        [SerializeField] private float flatten = 0.8f;

        public string SourceName { get; private set; } = "Corpse";
        public MinionUnit MinionPrefab { get; private set; }
        public float RaiseChance { get; private set; }
        public Vector2 Position => transform.position;

        private float _timer;

        public void Setup(string sourceName, MinionUnit minionPrefab, float raiseChance)
        {
            SourceName = sourceName;
            MinionPrefab = minionPrefab;
            RaiseChance = raiseChance;
        }

        /// <summary>ใช้รูปศัตรูที่ตายเป็นศพ (ล้มนอน สีเทา)</summary>
        public void SetLook(Sprite sprite, Vector3 worldScale, bool facingLeft)
        {
            if (spriteRenderer == null || sprite == null) return;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = corpseTint;
            spriteRenderer.flipX = facingLeft;
            Transform t = spriteRenderer.transform;
            t.localScale = new Vector3(Mathf.Abs(worldScale.x), Mathf.Abs(worldScale.y) * flatten, 1f);
            t.localRotation = Quaternion.Euler(0f, 0f, facingLeft ? -fallenAngle : fallenAngle);
        }

        private void OnEnable() => Registry.Corpses.Add(this);
        private void OnDisable() => Registry.Corpses.Remove(this);

        private void Update()
        {
            _timer += Time.deltaTime;
            float remaining = lifetime - _timer;
            if (spriteRenderer != null && remaining < blinkTime)
                spriteRenderer.enabled = Mathf.Repeat(_timer * 8f, 1f) > 0.3f;
            if (remaining <= 0f) Destroy(gameObject);
        }

        /// <summary>ใช้ซากนี้แล้ว (ลองชุบไปแล้ว ไม่ว่าจะสำเร็จหรือไม่)</summary>
        public void Consume() => Destroy(gameObject);
    }
}
