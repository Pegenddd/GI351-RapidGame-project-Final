using UnityEngine;

namespace NecroDefense
{
    /// <summary>ทรัพยากร: Souls (สร้าง/อัปป้อม) และ Mana (อัญเชิญมินเนียน/ร่ายสกิล)</summary>
    public class EconomyManager : MonoBehaviour
    {
        [Header("Souls")]
        [Tooltip("Souls ตอนเริ่มเกม (GDD = 100)")]
        [SerializeField] private int startingSouls = 100;

        [Header("Mana")]
        [Tooltip("Mana สูงสุด (GDD = 100)")]
        [SerializeField] private float maxMana = 100f;
        [Tooltip("Mana ตอนเริ่มเกม")]
        [SerializeField] private float startingMana = 100f;
        [Tooltip("ฟื้น Mana ต่อวินาที (GDD = 5)")]
        [SerializeField] private float manaRegenPerSecond = 5f;

        public int Souls { get; private set; }
        public float Mana { get; private set; }
        public float MaxMana => maxMana;
        public float ManaRegen => manaRegenPerSecond;

        private void Awake()
        {
            Souls = startingSouls;
            Mana = Mathf.Min(startingMana, maxMana);
        }

        private void Update()
        {
            if (!GameManager.IsGameplayRunning) return;
            Mana = Mathf.Min(maxMana, Mana + manaRegenPerSecond * Time.deltaTime);
        }

        // ---- Souls ----
        public void AddSouls(int amount) => Souls += Mathf.Max(0, amount);

        // ---- ใช้ในซีนเทส ----
        public void SetSouls(int value) => Souls = Mathf.Max(0, value);
        public void SetMana(float value) => Mana = Mathf.Clamp(value, 0f, maxMana);
        public void SetMaxMana(float value) { maxMana = Mathf.Max(1f, value); Mana = Mathf.Min(Mana, maxMana); }
        public void FillMana() => Mana = maxMana;

        public bool CanAffordSouls(int amount) => Souls >= amount;

        public bool TrySpendSouls(int amount, bool showMessage = true)
        {
            if (Souls < amount)
            {
                if (showMessage && GameManager.Instance != null) GameManager.Instance.UI.ShowMessage("Not enough Souls");
                return false;
            }
            Souls -= amount;
            return true;
        }

        // ---- Mana ----
        public bool TrySpendMana(float amount, bool showMessage = true)
        {
            if (Mana < amount)
            {
                if (showMessage && GameManager.Instance != null) GameManager.Instance.UI.ShowMessage("Not enough Mana");
                return false;
            }
            Mana -= amount;
            return true;
        }

        /// <summary>การ์ด Mana Font</summary>
        public void UpgradeMana(float extraMax, float extraRegen)
        {
            maxMana += extraMax;
            Mana += extraMax;
            manaRegenPerSecond += extraRegen;
        }
    }
}
