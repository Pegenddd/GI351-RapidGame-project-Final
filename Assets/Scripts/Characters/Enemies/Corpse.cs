using System;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;
using NecroDefense.Factories;

namespace NecroDefense.Characters.Enemies
{
    [RequireComponent(typeof(CircleCollider2D))]
    public class Corpse : Entity, IResurrectable
    {
        [Header("Corpse / Soul Remnant (Editable in Inspector)")]
        [Tooltip("Original monster type this corpse represents")]
        [SerializeField] private EnemyType sourceType = EnemyType.Zombie;
        [Tooltip("Soul cost required for the Necromancer to reanimate this corpse")]
        [SerializeField] private int soulCost = 20; // <--- ปรับค่า Default ตรงนี้ให้เป็น 20 เผื่อเหนียวไว้ก่อนเลยครับ!
        [Tooltip("Seconds before this corpse decays and disappears from the arena")]
        [SerializeField] private float decayDuration = 25f;

        private float _spawnTime;
        private bool _isResurrected = false;
        private SpriteRenderer _spriteRenderer;

        public EnemyType SourceType => sourceType;
        public int SoulCost => soulCost;
        public bool CanBeResurrected => !_isResurrected && (Time.time - _spawnTime < decayDuration);

        public static event Action<Corpse> OnCorpseSpawned;
        public static event Action<Corpse> OnCorpseRemoved;

        protected override void Awake()
        {
            base.Awake();
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _spawnTime = Time.time;
            _isResurrected = false;
            OnCorpseSpawned?.Invoke(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnCorpseRemoved?.Invoke(this);
        }

        private void Update()
        {
            float elapsed = Time.time - _spawnTime;
            if (elapsed >= decayDuration && !_isResurrected)
            {
                Destroy(gameObject);
            }
            else if (_spriteRenderer != null)
            {
                float alpha = Mathf.Lerp(1f, 0.2f, elapsed / decayDuration);
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 4f);
                Color c = _spriteRenderer.color;
                c.a = alpha * pulse;
                _spriteRenderer.color = c;
            }
        }

        public void Initialize(EnemyType type, int cost, Sprite sprite = null, Color? customColor = null)
        {
            sourceType = type;

            // บังคับใช้ค่า cost ที่ส่งมาจากศัตรูทันที ถ้า cost เป็น 0 หรือต่ำกว่า ให้ใช้ 20 แทน
            soulCost = cost > 0 ? cost : 20;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = (sprite != null) ? sprite : Utils.ProceduralVisuals.GetCorpseSprite();
                _spriteRenderer.color = customColor ?? Color.black;
            }
        }

        public void OnResurrected()
        {
            if (_isResurrected) return;
            _isResurrected = true;
            Destroy(gameObject);
        }
    }
}