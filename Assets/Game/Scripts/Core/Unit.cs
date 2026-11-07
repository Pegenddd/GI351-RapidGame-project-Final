using System;
using System.Collections;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// คลาสแม่ของทุกอย่างที่ "มีเลือด" : Player, Crystal, Enemy, Minion
    /// - เลือด / เกราะ / ขนาดตัว / บินได้ไหม
    /// - สถานะ Slow และ Poison (DoT)
    /// - กระพริบตอนโดนตี + อัปเดตหลอดเลือดบนหัว
    /// </summary>
    public abstract class Unit : MonoBehaviour
    {
        [Header("Health")]
        [Tooltip("เลือดสูงสุด")]
        [SerializeField] protected float maxHealth = 100f;
        [Tooltip("เกราะ: ลดดาเมจที่ได้รับแบบตรงๆ ต่อการโดน 1 ครั้ง (ขั้นต่ำโดน 1)")]
        [SerializeField] protected float armor = 0f;

        [Header("Body")]
        [Tooltip("รัศมีตัว (ใช้คำนวณระยะโจมตีถึงขอบตัว)")]
        [SerializeField] protected float bodyRadius = 0.4f;
        [Tooltip("บินได้ไหม (ป้อม Explosive ยิงตัวบินไม่ได้)")]
        [SerializeField] protected bool isFlying = false;

        [Header("Visual")]
        [Tooltip("SpriteRenderer ของตัว (ใช้กระพริบตอนโดนตี)")]
        [SerializeField] protected SpriteRenderer bodyRenderer;
        [Tooltip("กลุ่มภาพ (Body + เส้นขอบ) ที่จะกลับซ้าย-ขวาตามทิศเดิน (ว่าง = กลับแค่ Body)")]
        [SerializeField] protected Transform visualRoot;
        [Tooltip("หลอดเลือดบนหัว (ไม่ใส่ก็ได้)")]
        [SerializeField] protected WorldHealthBar healthBar;
        [SerializeField] protected Color hitFlashColor = new Color(1f, 0.25f, 0.25f);
        [SerializeField] protected float hitFlashTime = 0.08f;

        protected float currentHealth;
        protected bool isDead;
        private Color _baseColor = Color.white;
        private Coroutine _flashRoutine;

        // Status effects
        private float _slowPercent, _slowTimer;
        private float _poisonDps, _poisonTimer;

        public abstract Team Team { get; }
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float Armor => armor;
        public bool IsAlive => !isDead && currentHealth > 0f;
        public bool IsFlying => isFlying;
        public float BodyRadius => bodyRadius;
        public Vector2 Position => transform.position;
        public bool IsSlowed => _slowTimer > 0f;
        /// <summary>ตัวคูณความเร็วจากการโดน Slow (1 = ปกติ)</summary>
        public float SlowMultiplier => _slowTimer > 0f ? 1f - _slowPercent : 1f;

        /// <summary>ป้อมที่ตีโดนล่าสุด (ใช้กับ Soul Capture ของ Magic Orb Tower)</summary>
        public Tower LastTowerAttacker { get; private set; }

        public event Action<Unit> Died;

        protected virtual void Awake()
        {
            currentHealth = maxHealth;
            if (bodyRenderer != null) _baseColor = bodyRenderer.color;
            RefreshHealthBar();
        }

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }

        protected virtual void Update()
        {
            if (isDead) return;
            float dt = Time.deltaTime;

            if (_slowTimer > 0f) _slowTimer -= dt;

            if (_poisonTimer > 0f)
            {
                _poisonTimer -= dt;
                ApplyRawDamage(_poisonDps * dt, false);
            }
        }

        // ---------------- Damage / Heal ----------------

        /// <summary>รับดาเมจ (หักเกราะแล้ว ขั้นต่ำ 1)</summary>
        /// <summary>อมตะ (ใช้ในซีนเทส: God Mode)</summary>
        public bool Invincible { get; set; }

        /// <summary>ฆ่าทันที (ใช้ในซีนเทส)</summary>
        public void DebugKill()
        {
            if (!IsAlive) return;
            currentHealth = 0f;
            RefreshHealthBar();
            Die();
        }

        public virtual void TakeDamage(float amount, Tower sourceTower = null)
        {
            if (!IsAlive || amount <= 0f || Invincible) return;
            if (sourceTower != null) LastTowerAttacker = sourceTower;
            float final = Mathf.Max(1f, amount - armor);
            ApplyRawDamage(final, true);
        }

        private void ApplyRawDamage(float amount, bool flash)
        {
            if (!IsAlive || Invincible) return;
            currentHealth -= amount;
            if (flash) Flash();
            RefreshHealthBar();
            if (currentHealth <= 0f)
            {
                currentHealth = 0f;
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f) return;
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            RefreshHealthBar();
        }

        /// <summary>ฮีลเป็น % ของเลือดสูงสุด (0.1 = 10%)</summary>
        public void HealPercent(float percent) => Heal(maxHealth * percent);

        /// <summary>เพิ่มเลือดสูงสุด (เลือดปัจจุบันเพิ่มตามด้วย)</summary>
        public void AddMaxHealth(float bonus)
        {
            maxHealth += bonus;
            currentHealth += bonus;
            RefreshHealthBar();
        }

        /// <summary>ตั้งเลือดสูงสุดใหม่และเติมเต็ม (ใช้ตอนเสกมินเนียน)</summary>
        public void SetMaxHealthAndFill(float newMax)
        {
            maxHealth = Mathf.Max(1f, newMax);
            currentHealth = maxHealth;
            RefreshHealthBar();
        }

        public void AddArmor(float amount) => armor += amount;

        // ---------------- Status effects ----------------

        /// <summary>ทำให้ช้าลง percent (0.5 = ช้าลง 50%) นาน duration วินาที</summary>
        public void ApplySlow(float percent, float duration)
        {
            if (!IsAlive || percent <= 0f || duration <= 0f) return;
            _slowPercent = Mathf.Clamp01(Mathf.Max(_slowTimer > 0f ? _slowPercent : 0f, percent));
            _slowTimer = Mathf.Max(_slowTimer, duration);
        }

        /// <summary>ติดพิษ ดาเมจ dps ต่อวินาที นาน duration วินาที (ไม่หักเกราะ)</summary>
        public void ApplyPoison(float dps, float duration)
        {
            if (!IsAlive || dps <= 0f || duration <= 0f) return;
            _poisonDps = Mathf.Max(_poisonTimer > 0f ? _poisonDps : 0f, dps);
            _poisonTimer = Mathf.Max(_poisonTimer, duration);
        }

        // ---------------- Death ----------------

        protected void Die()
        {
            if (isDead) return;
            isDead = true;
            Died?.Invoke(this);
            OnDeath();
        }

        /// <summary>คลาสลูก override เพื่อทำสิ่งที่ต้องทำตอนตาย (ดรอปของ, จบเกม ฯลฯ)</summary>
        protected virtual void OnDeath()
        {
            Destroy(gameObject);
        }

        // ---------------- Visual ----------------

        protected void RefreshHealthBar()
        {
            if (healthBar != null) healthBar.SetValue(maxHealth > 0f ? currentHealth / maxHealth : 0f);
        }

        private void Flash()
        {
            if (bodyRenderer == null || !gameObject.activeInHierarchy) return;
            if (_flashRoutine != null) StopCoroutine(_flashRoutine);
            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            bodyRenderer.color = hitFlashColor;
            yield return new WaitForSeconds(hitFlashTime);
            if (bodyRenderer != null) bodyRenderer.color = _baseColor;
            _flashRoutine = null;
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, bodyRadius);
        }
    }
}
