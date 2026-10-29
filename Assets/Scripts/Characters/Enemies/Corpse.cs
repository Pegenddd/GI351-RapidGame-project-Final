using System;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;
using NecroDefense.Factories;

namespace NecroDefense.Characters.Enemies
{
    /// <summary>
    /// Represents the remains of a fallen enemy on the battlefield.
    /// Implements IResurrectable for Polymorphic Minion Creation.
    /// Demonstrates Abstraction & Interface Realization.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class Corpse : Entity, IResurrectable
    {
        [Header("Corpse / Soul Remnant (Editable in Inspector)")]
        [Tooltip("Original monster type this corpse represents")]
        [SerializeField] private EnemyType sourceType = EnemyType.Zombie;
        [Tooltip("Soul cost required for the Necromancer to reanimate this corpse")]
        [SerializeField] private int soulCost = 5;
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
            // Visual fade over decay duration
            float elapsed = Time.time - _spawnTime;
            if (elapsed >= decayDuration && !_isResurrected)
            {
                Destroy(gameObject);
            }
            else if (_spriteRenderer != null)
            {
                // Pulsing necromantic soul glow
                float alpha = Mathf.Lerp(1f, 0.2f, elapsed / decayDuration);
                float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * 4f);
                Color c = _spriteRenderer.color;
                c.a = alpha * pulse;
                _spriteRenderer.color = c;
            }
        }

        public void Initialize(EnemyType type, int cost, Sprite sprite = null)
        {
            sourceType = type;
            soulCost = cost;
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = (sprite != null) ? sprite : Utils.ProceduralVisuals.GetCorpseSprite();
                _spriteRenderer.color = Color.white; // 2-color monochrome
            }
        }

        /// <summary>
        /// Polymorphic trigger called when the Necromancer casts resurrection on this corpse.
        /// </summary>
        public void OnResurrected()
        {
            if (_isResurrected) return;
            _isResurrected = true;

            // Spawn particle / feedback if desired, then cleanup corpse
            Destroy(gameObject);
        }
    }
}

