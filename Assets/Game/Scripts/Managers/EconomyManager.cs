using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// ทรัพยากร 2 อย่าง:
    /// - Souls : เงินใช้สร้าง/อัปเกรดป้อม (ได้จากการฆ่าศัตรู)
    /// - Soul  : พลังชีวิตของผู้เล่น + ทรัพยากรอัญเชิญมินเนียน
    ///           ลดลงเองตลอดเวลา, เก็บลูกแก้ว Soul ที่ศัตรูดรอปเพื่อเติม
    ///           ถ้าหมดหลอด -> HP ของผู้เล่นจะลดลงเรื่อยๆ
    /// </summary>
    public class EconomyManager : MonoBehaviour
    {
        [Header("Souls (เงินสร้างป้อม)")]
        [Tooltip("Souls ตอนเริ่มเกม (GDD = 100)")]
        [SerializeField] private int startingSouls = 100;

        [Header("Soul (พลังชีวิต + ทรัพยากรอัญเชิญ)")]
        [Tooltip("Soul สูงสุด")]
        [SerializeField] private float maxSoul = 100f;
        [Tooltip("Soul ตอนเริ่มเกม")]
        [SerializeField] private float startingSoul = 100f;
        [Tooltip("Soul ลดลงเองต่อวินาที (0 = ไม่ลด)")]
        [SerializeField] private float soulDrainPerSecond = 1.5f;
        [Tooltip("Soul หมดหลอด: HP ผู้เล่นลดลงต่อวินาที (0 = ไม่ลด)")]
        [SerializeField] private float emptySoulHealthDrainPerSecond = 3f;

        public int Souls { get; private set; }
        public float Soul { get; private set; }
        public float MaxSoul => maxSoul;
        public float SoulDrainPerSecond => soulDrainPerSecond * _drainMultiplier;
        public float EmptySoulHealthDrain => emptySoulHealthDrainPerSecond;
        /// <summary>true = หลอด Soul หมดแล้ว (HP กำลังลด)</summary>
        public bool IsSoulEmpty => Soul <= 0.001f;
        /// <summary>เปิด/ปิดการลดลงของ Soul (ใช้ในแผงเทส)</summary>
        public bool SoulDrainEnabled { get; set; } = true;

        private float _drainMultiplier = 1f;
        private float _lastSoulSound = -1f;      // เสียง Souls (เงิน)
        private float _lastSoulPickup = -1f;     // เสียงเก็บ Soul (หลอด)
        private static int _soulChain;
        private bool _emptyWarned;

        /// <summary>การ์ด Soul Font: ลดอัตราการลดลงของ Soul % (0.2 = ช้าลง 20%)</summary>
        public void AddSoulDrainReductionPercent(float percent) =>
            _drainMultiplier = Mathf.Max(0.1f, _drainMultiplier - Mathf.Max(0f, percent));

        private void Awake()
        {
            Souls = startingSouls;
            Soul = Mathf.Clamp(startingSoul, 0f, maxSoul);
        }

        private void Update()
        {
            if (!GameManager.IsGameplayRunning) return;

            PlayerNecromancer player = GameManager.Instance != null ? GameManager.Instance.Player : null;
            if (player == null || !player.IsAlive) return;

            float before = Soul;
            if (SoulDrainEnabled && soulDrainPerSecond > 0f)
                Soul = Mathf.Max(0f, Soul - SoulDrainPerSecond * Time.deltaTime);

            // เพิ่งหมดหลอด -> เตือนครั้งเดียว
            if (Soul > 0f) _emptyWarned = false;
            else if (before > 0f && !_emptyWarned)
            {
                _emptyWarned = true;
                if (GameManager.Instance != null && GameManager.Instance.UI != null)
                    GameManager.Instance.UI.ShowError("Soul depleted! HP is draining");
            }

            // หลอด Soul หมด -> เลือดลดลงเรื่อยๆ จนกว่าจะเก็บ Soul มาเติม
            if (IsSoulEmpty && emptySoulHealthDrainPerSecond > 0f)
                player.DrainHealth(emptySoulHealthDrainPerSecond * Time.deltaTime);
        }

        // ---- Souls (เงินสร้างป้อม) ----
        public void AddSouls(int amount)
        {
            Souls += Mathf.Max(0, amount);
            if (amount > 0 && Time.unscaledTime - _lastSoulSound > 0.155f)
            {
                _lastSoulSound = Time.unscaledTime;
                Sfx.UI("SFX_Souls_Gain", 0.5f);
            }
        }

        public void SetSouls(int value) => Souls = Mathf.Max(0, value);

        public bool CanAffordSouls(int amount) => Souls >= amount;

        public bool TrySpendSouls(int amount, bool showMessage = true)
        {
            if (Souls < amount)
            {
                if (showMessage && GameManager.Instance != null) GameManager.Instance.UI.ShowError("Not enough Souls");
                return false;
            }
            Souls -= amount;
            return true;
        }

        // ---- Soul (พลังชีวิต/ทรัพยากร) ----
        /// <summary>เติม Soul (เก็บลูกแก้ว / การ์ด / แผงเทส)</summary>
        public void AddSoul(float amount)
        {
            if (amount <= 0f) return;
            Soul = Mathf.Min(maxSoul, Soul + amount);

            // เก็บติดกันเร็วๆ เสียงไล่โน้ตสูงขึ้น
            _soulChain = Time.unscaledTime - _lastSoulPickup < 0.6f ? Mathf.Min(_soulChain + 1, 8) : 0;
            _lastSoulPickup = Time.unscaledTime;
            Sfx.Play("SFX_Soul_Arrive", 0.65f, Mathf.Pow(1.059463f, _soulChain * 2));
        }

        public bool CanAffordSoul(float amount) => Soul >= amount;

        public bool TrySpendSoul(float amount, bool showMessage = true)
        {
            if (Soul < amount)
            {
                if (showMessage && GameManager.Instance != null) GameManager.Instance.UI.ShowError("Not enough Soul");
                return false;
            }
            Soul -= amount;
            return true;
        }

        /// <summary>การ์ด Soul Font: +Max Soul และลดอัตราการลดลง</summary>
        public void UpgradeSoul(float extraMax, float drainReductionPercent)
        {
            maxSoul += extraMax;
            Soul += extraMax;
            if (drainReductionPercent > 0f) AddSoulDrainReductionPercent(drainReductionPercent);
        }

        // ---- ใช้ในซีนเทส ----
        public void SetSoul(float value) => Soul = Mathf.Clamp(value, 0f, maxSoul);
        public void SetMaxSoul(float value) { maxSoul = Mathf.Max(1f, value); Soul = Mathf.Min(Soul, maxSoul); }
        public void FillSoul() => Soul = maxSoul;
    }
}
