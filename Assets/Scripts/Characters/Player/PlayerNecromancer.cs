using System;
using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using NecroDefense.Core;
using NecroDefense.Characters.Base;
using NecroDefense.Characters.Enemies;
using NecroDefense.Combat;
using NecroDefense.Combat.Strategies;
using NecroDefense.Factories;

namespace NecroDefense.Characters.Player
{
    /// <summary>
    /// The Player Character: Necromancer Mage.
    /// Features: Click to cast, locks position on initial click, stops moving and casts for exact duration, 
    /// and automatically regenerates souls over time (+1 every 1 second).
    /// </summary>
    public class PlayerNecromancer : Unit
    {
        [Header("Necromancy Resources (Editable in Inspector)")]
        [Tooltip("Maximum soul essence capacity")]
        [SerializeField] private int maxSouls = 100;
        [Tooltip("Initial soul essence at game start")]
        [SerializeField] private int initialSouls = 20;
        [Tooltip("Radius within which fallen corpses can be resurrected")]
        [SerializeField] private float resurrectionRadius = 5f;
        [Tooltip("If enabled, automatically raises dead when standing near a corpse")]
        [SerializeField] private bool autoResurrect = false;

        [Header("Necromancy Regeneration Settings")]
        [Tooltip("Amount of soul regenerated per interval")]
        [SerializeField] private int soulRegenAmount = 1;
        [Tooltip("Time interval in seconds for soul regeneration")]
        [SerializeField] private float regenInterval = 1.0f;

        [Header("Necromancy Risk Settings")]
        [Tooltip("Success rate probability for resurrection (0.0 to 1.0, e.g., 0.4 = 40% chance)")]
        [Range(0f, 1f)]
        [SerializeField] private float resurrectionSuccessRate = 0.4f;

        [Header("Area of Effect (AoE) Wave Spell Settings")]
        [Tooltip("Prefab for AoE explosion effect (leave empty for procedural wave fallback)")]
        [SerializeField] private GameObject aoeExplosionPrefab;
        [Tooltip("Maximum range for casting AoE spell")]
        [SerializeField] private float spellRange = 8f;
        [Tooltip("Radius of the AoE explosion damage and visual wave")]
        [SerializeField] private float aoeRadius = 3.5f;
        [Tooltip("Exact time required to stop and cast the spell before triggering")]
        [SerializeField] private float castDuration = 1.0f;

        private int _currentSouls;
        private Vector2 _inputDirection;

        // AoE casting states
        private bool isCasting = false;
        private Vector2 targetCastPosition;

        public int CurrentSouls => _currentSouls;
        public int MaxSouls => maxSouls;
        public float ResurrectionRadius => resurrectionRadius;

        public event Action<int, int> OnSoulsChanged;
        public event Action<IResurrectable> OnMinionResurrected;

        protected override void Awake()
        {
            base.Awake();
            SetFaction(Faction.Player);
            _currentSouls = Mathf.Clamp(initialSouls, 0, maxSouls);
        }

        protected override void Start()
        {
            base.Start();
            OnSoulsChanged?.Invoke(_currentSouls, maxSouls);

            // เริ่มต้นระบบรีมานารายวินาที
            StartCoroutine(SoulRegenerationRoutine());
        }

        private void Update()
        {
            if (!IsAlive) return;

            ReadInput();
            HandleAoeSpellInput();
            HandleResurrectionInput();
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;

            // ล็อกห้ามเดินเด็ดขาดในระหว่างร่ายเวท
            if (isCasting)
            {
                StopMovement();
                return;
            }

            if (_inputDirection.sqrMagnitude > 0.01f)
            {
                MoveTowards((Vector2)transform.position + _inputDirection);
            }
            else
            {
                StopMovement();
            }
        }

        private void ReadInput()
        {
            float x = 0f;
            float y = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y -= 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
            }
#else
            x = Input.GetAxisRaw("Horizontal");
            y = Input.GetAxisRaw("Vertical");
#endif
            _inputDirection = new Vector2(x, y).normalized;
        }

        private void HandleResurrectionInput()
        {
            bool triggerCast = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.eKey.wasPressedThisFrame))
            {
                triggerCast = true;
            }
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                triggerCast = true;
            }
#else
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(1))
            {
                triggerCast = true;
            }
#endif
            if (autoResurrect || triggerCast)
            {
                TryRaiseDead();
            }
        }

        public void TryRaiseDead()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(Position, resurrectionRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Corpse>(out var corpse))
                {
                    // เช็กเงื่อนไขมานาตาม SoulCost ของซากศพตัวนั้นๆ (ซึ่งรับค่ามาจาก Resurrection Cost ของศัตรูแล้ว)
                    if (corpse.CanBeResurrected && _currentSouls >= corpse.SoulCost)
                    {
                        SpendSouls(corpse.SoulCost);
                        bool isSuccess = UnityEngine.Random.value <= resurrectionSuccessRate;

                        if (isSuccess)
                        {
                            if (MinionFactory.Instance != null)
                            {
                                MinionFactory.Instance.CreateMinionFromCorpse(corpse);
                            }
                            OnMinionResurrected?.Invoke(corpse);
                        }
                        else
                        {
                            Debug.Log("<color=red>[Necromancy]</color> Resurrection failed! The corpse disintegrated into dust.");
                        }

                        corpse.OnResurrected();
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Coroutine to automatically regenerate souls over time.
        /// </summary>
        private IEnumerator SoulRegenerationRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(regenInterval);

                if (IsAlive && _currentSouls < maxSouls)
                {
                    AddSouls(soulRegenAmount);
                }
            }
        }

        private void HandleAoeSpellInput()
        {
            if (isCasting) return;

#if ENABLE_INPUT_SYSTEM
            bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            bool mousePressed = Input.GetMouseButtonDown(0);
#endif

            if (mousePressed)
            {
                targetCastPosition = GetMouseWorldPosition();
                StartCoroutine(CastSpellRoutine());
            }
        }

        private Vector2 GetMouseWorldPosition()
        {
            Vector3 mouseWorld = Vector3.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Camera.main != null)
            {
                Vector2 screenPos = Mouse.current.position.ReadValue();
                mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0f));
            }
#else
            if (Camera.main != null)
            {
                mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }
#endif
            mouseWorld.z = 0f;
            return mouseWorld;
        }

        private IEnumerator CastSpellRoutine()
        {
            isCasting = true;
            float timer = 0f;

            while (timer < castDuration)
            {
                timer += Time.deltaTime;
                yield return null;
            }

            TriggerAoeExplosion(targetCastPosition);

            isCasting = false;
            _lastAttackTime = Time.time;
        }

        private void TriggerAoeExplosion(Vector2 centerPosition)
        {
            if (aoeExplosionPrefab != null)
            {
                Instantiate(aoeExplosionPrefab, centerPosition, Quaternion.identity);
            }
            else
            {
                GameObject waveObj = new GameObject("AoeWave_ProceduralCircle");
                waveObj.transform.position = centerPosition;
                waveObj.transform.localScale = Vector3.zero;

                SpriteRenderer sr = waveObj.AddComponent<SpriteRenderer>();
                sr.sprite = CreateProceduralCircleSprite();
                sr.color = Color.white;
                sr.sortingOrder = 15;

                var waveVisual = waveObj.AddComponent<AoeWaveVisual>();
                waveVisual.Initialize(aoeRadius * 2f, 0.4f);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(centerPosition, aoeRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<IDamageable>(out var damageable) &&
                    hit.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team != Team && targetable.IsValidTarget)
                    {
                        DamageInfo info = new DamageInfo(AttackPower, DamageType.Necrotic, this, centerPosition, 0.5f);
                        damageable.TakeDamage(info);
                    }
                }
            }
        }

        private Sprite CreateProceduralCircleSprite()
        {
            int textureSize = 64;
            Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            Color transparentColor = new Color(0, 0, 0, 0);
            Color whiteColor = Color.white;
            float center = textureSize / 2f;
            float radius = center - 2f;

            for (int x = 0; x < textureSize; x++)
            {
                for (int y = 0; y < textureSize; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius && dist >= radius - 4f)
                    {
                        texture.SetPixel(x, y, whiteColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, transparentColor);
                    }
                }
            }
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
        }

        protected override void ExecuteAttack(ITargetable target)
        {
            if (target != null)
            {
                targetCastPosition = target.Position;
                StartCoroutine(CastSpellRoutine());
            }
        }

        public void ConfigureStats(float hp, float speed, float power, float cooldown, float range, int maxSoul, int initSoul, float resurrectRadius, float boltScale)
        {
            SetMaxHealth(hp);
            SetMoveSpeed(speed);
            SetAttackPower(power);
            SetAttackInterval(cooldown);
            spellRange = range;
            maxSouls = maxSoul;
            _currentSouls = Mathf.Clamp(initSoul, 0, maxSouls);
            resurrectionRadius = resurrectRadius;
            OnSoulsChanged?.Invoke(_currentSouls, maxSouls);
        }

        public void AddSouls(int amount)
        {
            if (amount <= 0) return;
            _currentSouls = Mathf.Min(maxSouls, _currentSouls + amount);
            OnSoulsChanged?.Invoke(_currentSouls, maxSouls);
        }

        public bool SpendSouls(int amount)
        {
            if (_currentSouls >= amount)
            {
                _currentSouls -= amount;
                OnSoulsChanged?.Invoke(_currentSouls, maxSouls);
                return true;
            }
            return false;
        }

        protected override void Die()
        {
            base.Die();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 0f, 0.8f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, resurrectionRadius);
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, spellRange);
        }
    }

    public class AoeWaveVisual : MonoBehaviour
    {
        private float targetScale;
        private float duration;
        private float elapsed;
        private SpriteRenderer sr;

        public void Initialize(float maxScale, float animDuration)
        {
            targetScale = maxScale;
            duration = animDuration;
            elapsed = 0f;
            sr = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float currentScale = Mathf.Lerp(0f, targetScale, t);
            transform.localScale = Vector3.one * currentScale;

            if (sr != null)
            {
                Color c = sr.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                sr.color = c;
            }

            if (elapsed >= duration)
            {
                Destroy(gameObject);
            }
        }
    }
}