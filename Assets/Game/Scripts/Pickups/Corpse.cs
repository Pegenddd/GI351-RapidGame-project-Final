using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ซากศพที่ศัตรูทิ้งไว้ -> คลิกซ้าย / กด Q เพื่อลอง Raise Dead (สุ่มตามโอกาส)
    /// สำเร็จ = ลุกขึ้นสู้ / ไม่สำเร็จ = ซากสลายไป
    /// กองทัพชนิดนั้นเต็ม (ชุบไม่ได้) -> ศพเรืองแสง คลิก / Q = เก็บพลังวิญญาณ (Souls) ทันที
    /// ศพหมดเวลา -> กลายเป็นพลังวิญญาณเอง (ได้น้อยกว่าเก็บเอง) = ไม่มีศพเกลื่อนแมพ
    /// </summary>
    public class Corpse : MonoBehaviour
    {
        [Tooltip("ซากอยู่ได้กี่วินาทีก่อนหายไป")]
        [SerializeField] private float lifetime = 10f;
        [Tooltip("กี่วินาทีสุดท้ายที่จะกระพริบเตือน")]
        [SerializeField] private float blinkTime = 3f;
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Soul Harvest (พลังวิญญาณจากศพ)")]
        [Tooltip("Souls ที่ได้เมื่อคลิก / กด Q ที่ศพที่ชุบไม่ได้ (กองทัพชนิดนั้นเต็ม)")]
        [SerializeField] private int harvestSouls = 3;
        [Tooltip("Souls ที่ได้เมื่อศพหมดเวลาหายไปเอง (ทุกศพ) - ตั้งให้น้อยกว่า Harvest Souls เพื่อให้อยากคลิกเอง / 0 = ไม่ได้")]
        [SerializeField] private int expireSouls = 1;
        [Tooltip("สีศพตอนชุบไม่ได้แล้ว (บอกผู้เล่นว่าคลิกเก็บวิญญาณได้)")]
        [SerializeField] private Color harvestTint = new Color(0.95f, 0.95f, 0.95f, 1f);
        [Tooltip("ศพที่เก็บวิญญาณได้จะกระพริบเบาๆ")]
        [SerializeField] private float harvestPulseSpeed = 4f;
        [Tooltip("สีตัวเลข +Souls ที่ลอยขึ้น")]
        [SerializeField] private Color soulTextColor = new Color(0.9f, 0.15f, 0.15f, 1f);

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
        private float _checkTimer;
        private bool _harvestOnly;

        /// <summary>ชุบไม่ได้แล้ว (กองทัพชนิดนี้เต็ม) -> คลิกเก็บวิญญาณ</summary>
        public bool HarvestOnly => _harvestOnly;
        public int HarvestSouls => harvestSouls;

        public void Setup(string sourceName, MinionUnit minionPrefab, float raiseChance)
        {
            SourceName = sourceName;
            MinionPrefab = minionPrefab;
            RaiseChance = raiseChance;
        }

        /// <summary>ตั้งค่า Souls ตามชนิดศัตรู (เรียกจาก EnemyUnit ตอนตาย) - ทับค่าใน Prefab ศพ</summary>
        public void SetSouls(int harvest, int expire)
        {
            harvestSouls = Mathf.Max(0, harvest);
            expireSouls = Mathf.Max(0, expire);
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

            // เช็คเป็นช่วงๆ ว่ายังชุบได้ไหม
            _checkTimer -= Time.deltaTime;
            if (_checkTimer <= 0f)
            {
                _checkTimer = 0.25f;
                _harvestOnly = MinionPrefab != null && GameManager.Instance != null && !GameManager.Instance.Minions.HasRoom(MinionPrefab);
            }
            if (spriteRenderer != null)
            {
                Color c = corpseTint;
                if (_harvestOnly) c = Color.Lerp(corpseTint, harvestTint, 0.55f + 0.45f * Mathf.Sin(_timer * harvestPulseSpeed));
                spriteRenderer.color = c;
                if (remaining < blinkTime) spriteRenderer.enabled = Mathf.Repeat(_timer * 8f, 1f) > 0.3f;
            }

            if (remaining <= 0f)
            {
                if (expireSouls > 0) Harvest(expireSouls);
                else Destroy(gameObject);
            }
        }

        /// <summary>ใช้ซากนี้แล้ว (ลองชุบไปแล้ว ไม่ว่าจะสำเร็จหรือไม่)</summary>
        public void Consume() => Destroy(gameObject);

        /// <summary>เปลี่ยนศพเป็นพลังวิญญาณ (Souls) แล้วหายไป</summary>
        public void Harvest(int souls)
        {
            if (souls > 0 && GameManager.Instance != null)
            {
                GameManager.Instance.Economy.AddSouls(souls);
                FloatingText.Spawn("+" + souls, Position + Vector2.up * 0.3f, soulTextColor);
            }
            Destroy(gameObject);
        }

        /// <summary>คลิก / Q ที่ศพที่ชุบไม่ได้: เก็บพลังวิญญาณเต็มจำนวน</summary>
        public int HarvestNow()
        {
            int s = harvestSouls;
            Harvest(s);
            return s;
        }
    }
}
