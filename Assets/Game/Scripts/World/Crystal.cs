using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// คริสตัลหลัก (ฐาน) - HP เหลือ 0 = แพ้
    /// - Passive Healing: ทุก X วินาที ฮีลผู้เล่นและมินเนียนทุกตัว Y% ของเลือดสูงสุด
    /// </summary>
    public class Crystal : Unit
    {
        [Header("Passive Healing")]
        [Tooltip("ฮีลทุกกี่วินาที")]
        [SerializeField] private float healInterval = 30f;
        [Tooltip("ฮีลกี่ % ของเลือดสูงสุด (0.1 = 10%)")]
        [Range(0f, 1f)]
        [SerializeField] private float healPercent = 0.1f;
        [Tooltip("ฮีลตัวป้อมหลักเองด้วย (นอกจากผู้เล่น + มินเนียน)")]
        [SerializeField] private bool healSelf = true;
        [Tooltip("การ์ด Base Architect ลดเวลาฮีลได้ต่ำสุดเท่านี้ (วินาที)")]
        [SerializeField] private float minHealInterval = 10f;
        [SerializeField] private RingEffect healEffect;
        [SerializeField] private float healEffectRadius = 6f;

        private float _healTimer;
        private float _hitSoundTimer;
        private int _alertedWave = -1;

        public override Team Team => Team.Player;
        public float HealTimeRemaining => Mathf.Max(0f, healInterval - _healTimer);
        public float HealInterval => healInterval;

        /// <summary>การ์ด Base Architect: ฮีลถี่ขึ้น (ลดเวลาฮีลกี่วินาที)</summary>
        public void ReduceHealInterval(float seconds) => healInterval = Mathf.Max(minHealInterval, healInterval - seconds);

        protected override void OnEnable()
        {
            base.OnEnable();
            Registry.Crystal = this;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (Registry.Crystal == this) Registry.Crystal = null;
        }

        protected override void Update()
        {
            base.Update();
            if (!IsAlive || !GameManager.IsGameplayRunning) return;

            if (_hitSoundTimer > 0f) _hitSoundTimer -= Time.deltaTime;
            _healTimer += Time.deltaTime;
            if (_healTimer >= healInterval)
            {
                _healTimer = 0f;
                PassiveHeal();
            }
        }

        private void PassiveHeal()
        {
            Sfx.At("SFX_Base_Heal", Position);
            if (Registry.Player != null) Registry.Player.HealPercent(healPercent);
            if (healSelf) HealPercent(healPercent);
            for (int i = 0; i < Registry.Minions.Count; i++) Registry.Minions[i].HealPercent(healPercent);
            if (healEffect != null)
            {
                RingEffect fx = Instantiate(healEffect, Position, Quaternion.identity, GameManager.GetFolder(SpawnFolder.Effects));
                fx.Play(healEffectRadius);
            }
        }

        protected override void OnDamaged(float amount)
        {
            // ศัตรูตีฐานครั้งแรกของเวฟ = สัญญาณเตือน
            int wave = GameManager.Instance != null ? GameManager.Instance.Waves.CurrentWaveNumber : 0;
            if (wave != _alertedWave)
            {
                _alertedWave = wave;
                Sfx.Play("SFX_Enemy_ReachBase");
                if (GameManager.Instance != null) GameManager.Instance.UI.ShowError("The Crystal is under attack!");
            }
            if (_hitSoundTimer > 0f) return;
            _hitSoundTimer = 0.55f;
            Sfx.At("SFX_Base_Hit", Position);
            CameraShake.Shake(0.07f, 0.15f);
        }

        protected override void OnDeath()
        {
            Sfx.Play("SFX_Base_Destroyed");
            CameraShake.Shake(0.4f, 1.0f);
            if (bodyRenderer != null) bodyRenderer.color = Color.gray;
            if (GameManager.Instance != null) GameManager.Instance.Defeat("The Crystal has been destroyed");
        }

    }
}
