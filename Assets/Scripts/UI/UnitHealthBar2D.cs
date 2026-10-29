using UnityEngine;
using NecroDefense.Characters.Base;

namespace NecroDefense.UI
{
    /// <summary>
    /// World-space 1-bit mini Health Bar displayed directly above units.
    /// Provides immediate visual recognition of enemies and instant feedback on damage.
    /// </summary>
    public class UnitHealthBar2D : MonoBehaviour
    {
        [Header("Positioning & Size")]
        [Tooltip("Vertical offset above the unit's pivot")]
        [SerializeField] private float yOffset = 0.8f;
        [Tooltip("Width of the health bar in world units")]
        [SerializeField] private float barWidth = 0.8f;
        [Tooltip("Height of the health bar in world units")]
        [SerializeField] private float barHeight = 0.12f;

        [Header("Display Settings")]
        [Tooltip("Toggle health bar visibility")]
        [SerializeField] private bool showHealthBar = true;
        [Tooltip("Sorting layer order for the bar (must be above sprites)")]
        [SerializeField] private int sortingOrder = 18;

        private Unit _unit;
        private Transform _fillTransform;
        private SpriteRenderer _bgRenderer;
        private SpriteRenderer _borderRenderer;
        private SpriteRenderer _fillRenderer;

        private static Sprite _borderSprite;
        private static Sprite _solidWhiteSprite;

        private void Awake()
        {
            _unit = GetComponentInParent<Unit>();
            if (!showHealthBar)
            {
                gameObject.SetActive(false);
                return;
            }
            EnsureSprites();
            BuildHealthBar();
        }

        private void Start()
        {
            if (_unit != null)
            {
                _unit.OnHealthChanged += HandleHealthChanged;
                HandleHealthChanged(_unit.CurrentHealth, _unit.MaxHealth);
            }
        }

        private void OnDestroy()
        {
            if (_unit != null)
            {
                _unit.OnHealthChanged -= HandleHealthChanged;
            }
        }

        private void LateUpdate()
        {
            // Keep health bar upright even if unit sprite flips
            transform.position = transform.parent.position + Vector3.up * yOffset;
            transform.rotation = Quaternion.identity;
        }

        private void HandleHealthChanged(float current, float max)
        {
            if (_fillTransform != null && max > 0f)
            {
                float pct = Mathf.Clamp01(current / max);
                _fillTransform.localScale = new Vector3(pct, 1f, 1f);
            }
        }

        private void EnsureSprites()
        {
            if (_solidWhiteSprite == null)
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                tex.Apply();
                _solidWhiteSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2);
            }

            if (_borderSprite == null)
            {
                int w = 24, h = 6;
                Texture2D bTex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        bool isB = (x == 0 || x == w - 1 || y == 0 || y == h - 1);
                        bTex.SetPixel(x, y, isB ? Color.white : Color.black);
                    }
                }
                bTex.Apply();
                _borderSprite = Sprite.Create(bTex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16);
            }
        }

        private void BuildHealthBar()
        {
            // 1. Frame / Background (Black inside with 1px White border)
            GameObject frameObj = new GameObject("HP_Frame");
            frameObj.transform.SetParent(transform, false);
            frameObj.transform.localScale = new Vector3(barWidth, barHeight, 1f);

            _borderRenderer = frameObj.AddComponent<SpriteRenderer>();
            _borderRenderer.sprite = _borderSprite;
            _borderRenderer.color = Color.white;
            _borderRenderer.sortingOrder = sortingOrder;

            // 2. White Fill bar (Pivot at left edge)
            GameObject fillObj = new GameObject("HP_Fill");
            fillObj.transform.SetParent(transform, false);
            fillObj.transform.localPosition = new Vector3(-barWidth * 0.45f, 0f, 0f);
            fillObj.transform.localScale = Vector3.one;

            _fillTransform = fillObj.transform;

            GameObject fillSpriteObj = new GameObject("Sprite");
            fillSpriteObj.transform.SetParent(fillObj.transform, false);
            fillSpriteObj.transform.localPosition = new Vector3((barWidth * 0.9f) * 0.5f, 0f, 0f);
            fillSpriteObj.transform.localScale = new Vector3(barWidth * 0.85f, barHeight * 0.65f, 1f);

            _fillRenderer = fillSpriteObj.AddComponent<SpriteRenderer>();
            _fillRenderer.sprite = _solidWhiteSprite;
            _fillRenderer.color = Color.white;
            _fillRenderer.sortingOrder = sortingOrder + 1;
        }
    }
}

