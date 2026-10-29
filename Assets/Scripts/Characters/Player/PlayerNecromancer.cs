using System;
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
    /// Demonstrates:
    /// - Inheritance: Inherits from Unit.
    /// - Encapsulation: Souls resource, attack cooldowns, input bindings.
    /// - Polymorphism: Overrides ExecuteAttack (Survivor-like auto bolt) and Die().
    /// - Core Mechanic: Raises fallen enemies from corpses into allied minions.
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

        [Header("Combat & Spells (Editable in Inspector)")]
        [Tooltip("Prefab for Homing Dark Bolt (leave empty for procedural fallback)")]
        [SerializeField] private GameObject darkBoltPrefab;
        [Tooltip("Maximum range for auto-targeting and firing Dark Bolt")]
        [SerializeField] private float spellRange = 8f;
        [Tooltip("Visual scale multiplier for Dark Bolt (Ball)")]
        [SerializeField] private float boltVisualScale = 1.3f;
        [Tooltip("Enable manual click-to-fire on Left Click")]
        [SerializeField] private bool allowManualFire = true;

        private int _currentSouls;
        private ITargetingStrategy _targetStrategy;
        private Vector2 _inputDirection;

        public int CurrentSouls => _currentSouls;
        public int MaxSouls => maxSouls;
        public float ResurrectionRadius => resurrectionRadius;

        public event Action<int, int> OnSoulsChanged; // current, max
        public event Action<IResurrectable> OnMinionResurrected;

        protected override void Awake()
        {
            base.Awake();
            SetFaction(Faction.Player);
            _currentSouls = Mathf.Clamp(initialSouls, 0, maxSouls);
            _targetStrategy = new TargetNearestHostileStrategy();

#if UNITY_EDITOR
            if (darkBoltPrefab == null)
            {
                darkBoltPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/Proj_DarkBolt.prefab");
            }
#endif
        }

        protected override void Start()
        {
            base.Start();
            OnSoulsChanged?.Invoke(_currentSouls, maxSouls);
        }

        private void Update()
        {
            if (!IsAlive) return;

            ReadInput();
            HandleManualFireInput();
            HandleAutoAttack();
            HandleResurrectionInput();
        }

        private void FixedUpdate()
        {
            if (!IsAlive) return;

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

        /// <summary>
        /// Scans for nearby corpses and resurrects them if player has enough souls.
        /// </summary>
        public void TryRaiseDead()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(Position, resurrectionRadius);
            foreach (var hit in hits)
            {
                if (hit.TryGetComponent<Corpse>(out var corpse))
                {
                    if (corpse.CanBeResurrected && _currentSouls >= corpse.SoulCost)
                    {
                        SpendSouls(corpse.SoulCost);

                        // Polymorphic factory creates the appropriate Minion for the corpse type
                        if (MinionFactory.Instance != null)
                        {
                            MinionFactory.Instance.CreateMinionFromCorpse(corpse);
                        }

                        corpse.OnResurrected();
                        OnMinionResurrected?.Invoke(corpse);
                        break; // Resurrect one per pulse/press for pacing
                    }
                }
            }
        }

        private void HandleManualFireInput()
        {
            if (!allowManualFire || !CanAttack) return;

            bool firePressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                firePressed = true;
            }
#else
            if (Input.GetMouseButtonDown(0))
            {
                firePressed = true;
            }
#endif
            if (firePressed)
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
                Vector2 dir = ((Vector2)mouseWorld - (Vector2)Position).normalized;
                if (dir.sqrMagnitude > 0.001f)
                {
                    // Target clicked hostile if any, else fire straight bolt in click direction
                    ITargetable clickedTarget = null;
                    Collider2D hit = Physics2D.OverlapPoint(mouseWorld);
                    if (hit != null && hit.TryGetComponent<ITargetable>(out var targetable))
                    {
                        if (targetable.Team != Team && targetable.IsValidTarget)
                        {
                            clickedTarget = targetable;
                        }
                    }

                    _lastAttackTime = Time.time;
                    FireDarkBolt(dir, clickedTarget);
                }
            }
        }

        /// <summary>
        /// Survivor-like auto-attack: finds nearest hostile and fires Dark Bolt.
        /// </summary>
        private void HandleAutoAttack()
        {
            if (!CanAttack) return;

            ITargetable target = _targetStrategy.FindTarget(this, spellRange);
            if (target != null && target.IsValidTarget)
            {
                Attack(target);
            }
        }

        /// <summary>
        /// Polymorphic attack implementation: Spawns Homing Dark Bolt towards target.
        /// </summary>
        protected override void ExecuteAttack(ITargetable target)
        {
            Vector2 direction = (target.Position - Position).normalized;
            FireDarkBolt(direction, target);
        }

        public void FireDarkBolt(Vector2 direction, ITargetable target)
        {
            GameObject boltObj;
            if (darkBoltPrefab != null)
            {
                boltObj = Instantiate(darkBoltPrefab, Position, Quaternion.identity);
            }
            else
            {
                // Procedural projectile fallback if no prefab assigned
                boltObj = Utils.ProceduralVisuals.CreateProjectileGameObject("DarkBolt", Color.white);
                boltObj.transform.position = Position;
            }

            // Ensure 1-bit visual sprite is guaranteed to render
            if (boltObj.TryGetComponent<SpriteRenderer>(out var sr))
            {
                if (sr.sprite == null)
                {
                    sr.sprite = Utils.ProceduralVisuals.GetProjectileSprite("DarkBolt");
                }
                sr.color = Color.white;
                sr.sortingOrder = 12;
            }

            if (boltVisualScale > 0.05f)
            {
                boltObj.transform.localScale = Vector3.one * boltVisualScale;
            }

            if (boltObj.TryGetComponent<HomingProjectile>(out var homing))
            {
                homing.EnsureVisuals();
                homing.Initialize(direction, AttackPower, DamageType.Necrotic, Team, this);
                if (target != null) homing.SetTarget(target);
            }
            else if (boltObj.TryGetComponent<Projectile>(out var proj))
            {
                proj.EnsureVisuals();
                proj.Initialize(direction, AttackPower, DamageType.Necrotic, Team, this);
            }
        }

        public void ConfigureStats(float hp, float speed, float power, float cooldown, float range, int maxSoul, int initSoul, float resurrectRadius, float boltScale, bool manualFire)
        {
            SetMaxHealth(hp);
            SetMoveSpeed(speed);
            SetAttackPower(power);
            SetAttackInterval(cooldown);
            spellRange = range;
            maxSouls = maxSoul;
            _currentSouls = Mathf.Clamp(initSoul, 0, maxSouls);
            resurrectionRadius = resurrectRadius;
            boltVisualScale = boltScale;
            allowManualFire = manualFire;
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
            // Let GameManager handle Game Over
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.5f, 0f, 0.8f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, resurrectionRadius);
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, spellRange);
        }
    }
}

