using UnityEngine;
using NecroDefense.Characters.Player;

namespace NecroDefense.Skills.CombatEntities
{
    /// <summary>
    /// Dropped by slain enemies. Attracted to Player by Mana Magnet and grants EXP upon collection.
    /// 1-Bit Monochrome Sprite with sparkling diamond/soul shard design.
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class ExperienceOrb : MonoBehaviour
    {
        [SerializeField] private int expValue = 1;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float acceleration = 12f;

        private Transform _playerTransform;
        private PlayerProgression _playerProgression;
        private CircleCollider2D _collider;
        private SpriteRenderer _spriteRenderer;
        private bool _isAttracted = false;
        private float _currentSpeed = 0f;
        private float _spawnTime;

        public void Initialize(int value)
        {
            expValue = Mathf.Max(1, value);
        }

        private void Awake()
        {
            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.radius = 0.3f;

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer == null)
            {
                _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            _spriteRenderer.sprite = Utils.ProceduralVisuals.GetProjectileSprite("ExpOrb");
            _spriteRenderer.color = Color.white;
            _spriteRenderer.sortingOrder = 4;
        }

        private void Start()
        {
            _spawnTime = Time.time;
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerNecromancer>();
            if (player != null)
            {
                _playerTransform = player.transform;
                _playerProgression = player.GetComponent<PlayerProgression>();
            }
        }

        private void Update()
        {
            if (_playerTransform == null) return;

            // Subtle bobbing animation before pickup
            if (!_isAttracted)
            {
                float bob = Mathf.Sin((Time.time - _spawnTime) * 4f) * 0.05f;
                transform.position += new Vector3(0, bob * Time.deltaTime, 0);

                // Check distance against player magnet radius
                float magnetRadius = _playerProgression != null ? _playerProgression.MagnetRadius : 3.0f;
                float dist = Vector2.Distance(transform.position, _playerTransform.position);
                if (dist <= magnetRadius)
                {
                    _isAttracted = true;
                    _currentSpeed = moveSpeed;
                }
            }
            else
            {
                // Accelerate smoothly towards player
                _currentSpeed += acceleration * Time.deltaTime;
                transform.position = Vector2.MoveTowards(transform.position, _playerTransform.position, _currentSpeed * Time.deltaTime);

                // Collect check
                if (Vector2.Distance(transform.position, _playerTransform.position) < 0.4f)
                {
                    Collect();
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponent<PlayerNecromancer>() != null)
            {
                Collect();
            }
        }

        private void Collect()
        {
            if (_playerProgression != null)
            {
                _playerProgression.AddExp(expValue);
            }
            Destroy(gameObject);
        }
    }
}

