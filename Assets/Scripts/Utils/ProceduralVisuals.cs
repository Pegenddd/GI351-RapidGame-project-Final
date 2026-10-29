using System.Collections.Generic;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Combat;
using NecroDefense.Characters.Enemies;
using NecroDefense.Characters.Minions;

namespace NecroDefense.Utils
{
    /// <summary>
    /// Utility class that procedurally creates 2-Color (1-bit Black & White) pixel art sprites
    /// and GameObjects. Everything adheres strictly to the 2-Color Monochrome Palette!
    /// </summary>
    public static class ProceduralVisuals
    {
        public static readonly Color Black = Color.black;
        public static readonly Color White = Color.white;
        public static readonly Color Clear = Color.clear;

        private static readonly Dictionary<string, Sprite> _spriteCache = new Dictionary<string, Sprite>();

        private static Texture2D CreateEmptyTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            Color[] clears = new Color[width * height];
            for (int i = 0; i < clears.Length; i++) clears[i] = Clear;
            tex.SetPixels(clears);
            return tex;
        }

        private static Sprite CacheAndCreateSprite(string key, Texture2D tex, int pixelsPerUnit = 16)
        {
            tex.Apply();
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
            _spriteCache[key] = sprite;
            return sprite;
        }

        // ==========================================
        // 1-BIT PLAYER: NECROMANCER MAGE (32x32)
        // ==========================================
        public static Sprite GetNecromancerSprite()
        {
            const string key = "2Color_Necromancer";
            if (_spriteCache.TryGetValue(key, out var s) && s != null) return s;

            int size = 32;
            Texture2D tex = CreateEmptyTexture(size, size);

            // Draw Robe & Body (White with Black details)
            for (int y = 4; y <= 24; y++)
            {
                int halfWidth = (y < 16) ? (24 - y) / 2 + 3 : 6;
                for (int x = 16 - halfWidth; x <= 16 + halfWidth; x++)
                {
                    tex.SetPixel(x, y, White);
                }
            }

            // Hood & Head
            for (int y = 20; y <= 28; y++)
            {
                int hw = 28 - y + 2;
                for (int x = 16 - hw; x <= 16 + hw; x++)
                {
                    tex.SetPixel(x, y, White);
                }
            }

            // Hollow Shadow Face (Black)
            for (int y = 22; y <= 25; y++)
            {
                for (int x = 14; x <= 18; x++)
                {
                    tex.SetPixel(x, y, Black);
                }
            }

            // Piercing Eyes (White dots in shadow)
            tex.SetPixel(14, 24, White);
            tex.SetPixel(18, 24, White);

            // Magic Staff on the side
            for (int y = 2; y <= 29; y++)
            {
                tex.SetPixel(24, y, White);
            }
            // Staff Skull / Orb topper
            for (int sy = 27; sy <= 30; sy++)
            {
                for (int sx = 22; sx <= 26; sx++)
                {
                    tex.SetPixel(sx, sy, White);
                }
            }
            tex.SetPixel(23, 29, Black);
            tex.SetPixel(25, 29, Black);

            return CacheAndCreateSprite(key, tex);
        }

        // ==========================================
        // 1-BIT BASE: CITADEL CORE CRYSTAL (64x64)
        // ==========================================
        public static Sprite GetCitadelSprite()
        {
            const string key = "2Color_CitadelCrystal";
            if (_spriteCache.TryGetValue(key, out var s) && s != null) return s;

            int size = 64;
            Texture2D tex = CreateEmptyTexture(size, size);
            int half = size / 2;

            // Outer Runic Perimeter Ring (White 1px dashed ring)
            for (int angle = 0; angle < 360; angle += 4)
            {
                float rad = angle * Mathf.Deg2Rad;
                int rx = half + Mathf.RoundToInt(Mathf.Cos(rad) * 28);
                int ry = half + Mathf.RoundToInt(Mathf.Sin(rad) * 28);
                if (angle % 12 != 0) tex.SetPixel(rx, ry, White);
            }

            // Central Monolithic Diamond Crystal
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int manhattan = Mathf.Abs(x - half) + Mathf.Abs(y - half);
                    if (manhattan <= 22)
                    {
                        // Inner facet lines
                        if (manhattan == 22 || manhattan == 21 || x == half || y == half || manhattan == 11)
                        {
                            tex.SetPixel(x, y, White);
                        }
                        else if (manhattan <= 6)
                        {
                            tex.SetPixel(x, y, White); // Core eye
                        }
                        else
                        {
                            tex.SetPixel(x, y, Black);
                        }
                    }
                }
            }

            return CacheAndCreateSprite(key, tex, 24);
        }

        // ==========================================
        // 1-BIT ENEMIES: HOSTILE MONSTERS (White silhouette with black evil eyes)
        // ==========================================
        public static Sprite GetEnemySprite(EnemyType type)
        {
            string key = $"2Color_Enemy_{type}";
            if (_spriteCache.TryGetValue(key, out var s) && s != null) return s;

            int size = (type == EnemyType.Golem) ? 48 : 32;
            Texture2D tex = CreateEmptyTexture(size, size);
            int half = size / 2;

            switch (type)
            {
                case EnemyType.Zombie:
                    // Ragged clawed ghoul
                    for (int y = 4; y <= 24; y++)
                    {
                        for (int x = 11; x <= 21; x++)
                        {
                            tex.SetPixel(x, y, White);
                        }
                    }
                    // Arms reaching out
                    for (int x = 6; x <= 26; x++)
                    {
                        tex.SetPixel(x, 15, White);
                        tex.SetPixel(x, 16, White);
                    }
                    // Claws
                    tex.SetPixel(6, 17, White);
                    tex.SetPixel(26, 17, White);
                    // Dark hollow eyes
                    tex.SetPixel(13, 20, Black);
                    tex.SetPixel(18, 20, Black);
                    tex.SetPixel(15, 17, Black); // gaping mouth
                    break;

                case EnemyType.SkeletonArcher:
                    // Ribcage and bow
                    for (int y = 6; y <= 22; y++) tex.SetPixel(16, y, White); // spine
                    for (int r = 10; r <= 18; r += 2) // ribs
                    {
                        for (int rx = 12; rx <= 20; rx++) tex.SetPixel(rx, r, White);
                    }
                    // Skull
                    for (int y = 22; y <= 28; y++)
                    {
                        for (int x = 13; x <= 19; x++) tex.SetPixel(x, y, White);
                    }
                    tex.SetPixel(14, 25, Black); // eye socket
                    tex.SetPixel(18, 25, Black);
                    // Bone Bow
                    for (int y = 8; y <= 24; y++)
                    {
                        int bx = 24 - Mathf.Abs(y - 16) / 2;
                        tex.SetPixel(bx, y, White);
                    }
                    break;

                case EnemyType.Golem:
                    // Massive blocky stone colossus
                    for (int y = 4; y <= 40; y++)
                    {
                        int hw = (y < 28) ? 14 : 10;
                        for (int x = half - hw; x <= half + hw; x++)
                        {
                            // Brick mortar lines (Black)
                            if (y % 6 == 0 || (y % 12 < 6 && (x == half - 6 || x == half + 6)) || (y % 12 >= 6 && x == half))
                            {
                                tex.SetPixel(x, y, Black);
                            }
                            else
                            {
                                tex.SetPixel(x, y, White);
                            }
                        }
                    }
                    // Heavy glowing eyes
                    tex.SetPixel(half - 5, 34, Black);
                    tex.SetPixel(half - 4, 34, Black);
                    tex.SetPixel(half + 4, 34, Black);
                    tex.SetPixel(half + 5, 34, Black);
                    break;

                case EnemyType.Ghost:
                    // Floating ethereal shroud
                    for (int y = 6; y <= 26; y++)
                    {
                        int hw = (y < 18) ? 8 : (26 - y) + 2;
                        for (int x = half - hw; x <= half + hw; x++)
                        {
                            // Dithered bottom edges
                            if (y <= 8 && (x + y) % 2 == 0)
                            {
                                tex.SetPixel(x, y, Clear);
                            }
                            else
                            {
                                tex.SetPixel(x, y, White);
                            }
                        }
                    }
                    // Void eyes
                    tex.SetPixel(half - 3, 20, Black);
                    tex.SetPixel(half + 3, 20, Black);
                    tex.SetPixel(half, 16, Black);
                    break;
            }

            return CacheAndCreateSprite(key, tex);
        }

        // ==========================================
        // 1-BIT MINIONS: ALLIED UNDEAD (With Floating Halo/Crest & Inverted Shield)
        // Distinguishes friend from foe in pure 2-color black & white!
        // ==========================================
        public static Sprite GetMinionSprite(EnemyType type)
        {
            string key = $"2Color_Minion_{type}";
            if (_spriteCache.TryGetValue(key, out var s) && s != null) return s;

            // Start from base enemy shape
            Sprite baseEnemySprite = GetEnemySprite(type);
            Texture2D baseTex = baseEnemySprite.texture;
            int size = baseTex.width;
            int half = size / 2;

            Texture2D tex = CreateEmptyTexture(size, size);

            // Copy base pixels
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, baseTex.GetPixel(x, y));
                }
            }

            // Add ALLIED RECOGNITION FEATURES:
            // 1. Floating Runic Halo / Crown above head (Pure White Ring)
            int haloY = (type == EnemyType.Golem) ? 44 : 30;
            for (int x = half - 5; x <= half + 5; x++)
            {
                if (x == half - 5 || x == half + 5)
                {
                    tex.SetPixel(x, haloY, White);
                }
                else
                {
                    tex.SetPixel(x, haloY + 1, White);
                    tex.SetPixel(x, haloY - 1, White);
                    tex.SetPixel(x, haloY, Black); // Hollow halo center
                }
            }

            // 2. Allied Inverted Crest on chest (Hollow Diamond/Cross)
            int chestY = (type == EnemyType.Golem) ? 22 : 12;
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 2)
                    {
                        tex.SetPixel(half + dx, chestY + dy, Black);
                    }
                }
            }
            tex.SetPixel(half, chestY, White); // white spark inside black crest

            return CacheAndCreateSprite(key, tex);
        }

        // ==========================================
        // 1-BIT CORPSE: GRAVESTONE / SOUL MARKER (24x24)
        // ==========================================
        public static Sprite GetCorpseSprite()
        {
            const string key = "2Color_Corpse";
            if (_spriteCache.TryGetValue(key, out var s) && s != null) return s;

            int size = 24;
            Texture2D tex = CreateEmptyTexture(size, size);
            int half = size / 2;

            // Base ground mound (dashed)
            for (int x = 4; x <= 20; x++)
            {
                if (x % 2 == 0) tex.SetPixel(x, 3, White);
                tex.SetPixel(x, 4, White);
            }

            // Tombstone Arch (White with black cross)
            for (int y = 5; y <= 16; y++)
            {
                int hw = (y > 13) ? 16 - y + 3 : 5;
                for (int x = half - hw; x <= half + hw; x++)
                {
                    tex.SetPixel(x, y, White);
                }
            }

            // Inverted Cross in center of Tombstone (Black)
            for (int y = 7; y <= 13; y++) tex.SetPixel(half, y, Black);
            for (int x = half - 2; x <= half + 2; x++) tex.SetPixel(x, 11, Black);

            // Floating Soul Spark
            tex.SetPixel(half, 19, White);
            tex.SetPixel(half - 1, 20, White);
            tex.SetPixel(half + 1, 20, White);
            tex.SetPixel(half, 21, White);

            return CacheAndCreateSprite(key, tex);
        }

        // ==========================================
        // 1-BIT PROJECTILES (20x20)
        // ==========================================
        public static Sprite GetProjectileSprite(string name)
        {
            string key = $"2Color_Proj_{name}";
            if (_spriteCache.TryGetValue(key, out var s) && s != null) return s;

            int size = 20;
            Texture2D tex = CreateEmptyTexture(size, size);
            int half = size / 2;

            if (name.Contains("DarkBolt"))
            {
                // 1-Bit Magical Soul Orb / Dark Energy Sphere (Solid White with runic core & radiant flares)
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float distSq = (x - half) * (x - half) + (y - half) * (y - half);
                        if (distSq <= 28f) // radius ~5.3 solid circular orb
                        {
                            // Inner runic ring contrast for mystical depth
                            if (distSq >= 4f && distSq <= 9f)
                            {
                                tex.SetPixel(x, y, Black);
                            }
                            else
                            {
                                tex.SetPixel(x, y, White);
                            }
                        }
                        // 4 cardinal magical flare rays extending outward (Up, Down, Left, Right)
                        else if ((Mathf.Abs(x - half) <= 1 && Mathf.Abs(y - half) <= 8) ||
                                 (Mathf.Abs(y - half) <= 1 && Mathf.Abs(x - half) <= 8))
                        {
                            tex.SetPixel(x, y, White);
                        }
                        // 4 diagonal flares
                        else if (Mathf.Abs(Mathf.Abs(x - half) - Mathf.Abs(y - half)) <= 1 && distSq <= 42f)
                        {
                            tex.SetPixel(x, y, White);
                        }
                    }
                }
            }
            else if (name.Contains("BoneArrow"))
            {
                // Sharp Bone Arrow (Solid 2px white shaft and arrowhead)
                for (int x = 2; x <= 15; x++)
                {
                    tex.SetPixel(x, half, White);
                    tex.SetPixel(x, half - 1, White);
                }
                // Solid Arrowhead
                tex.SetPixel(16, half, White);
                tex.SetPixel(16, half - 1, White);
                tex.SetPixel(15, half + 1, White);
                tex.SetPixel(15, half - 2, White);
                tex.SetPixel(14, half + 2, White);
                tex.SetPixel(14, half - 3, White);
                // Fletching
                tex.SetPixel(3, half + 1, White);
                tex.SetPixel(3, half - 2, White);
                tex.SetPixel(2, half + 2, White);
                tex.SetPixel(2, half - 3, White);
            }
            else
            {
                // Spectral Arrow (Luminous magical arrow)
                for (int x = 2; x <= 15; x++)
                {
                    if (x % 2 == 0)
                    {
                        tex.SetPixel(x, half, White);
                        tex.SetPixel(x, half - 1, White);
                    }
                }
                // Arrowhead
                tex.SetPixel(16, half, White);
                tex.SetPixel(16, half - 1, White);
                tex.SetPixel(15, half + 1, White);
                tex.SetPixel(15, half - 2, White);
                tex.SetPixel(14, half + 2, White);
                tex.SetPixel(14, half - 3, White);
            }

            return CacheAndCreateSprite(key, tex);
        }

        // ==========================================
        // RUNTIME CREATION HELPERS
        // ==========================================
        public static GameObject CreateEnemyGameObject(EnemyType type)
        {
            GameObject obj = new GameObject($"Enemy_{type}");
            var sr = obj.AddComponent<SpriteRenderer>();
            var col = obj.AddComponent<CircleCollider2D>();
            var rb = obj.AddComponent<Rigidbody2D>();

            col.isTrigger = false;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            sr.sprite = GetEnemySprite(type);
            sr.color = White;
            sr.sortingOrder = 5;

            col.radius = (type == EnemyType.Golem) ? 0.8f : 0.5f;
            return obj;
        }

        public static GameObject CreateMinionGameObject(EnemyType type)
        {
            GameObject obj = new GameObject($"Minion_{type}");
            var sr = obj.AddComponent<SpriteRenderer>();
            var col = obj.AddComponent<CircleCollider2D>();
            var rb = obj.AddComponent<Rigidbody2D>();

            col.isTrigger = false;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            sr.sprite = GetMinionSprite(type);
            sr.color = White;
            sr.sortingOrder = 6;

            col.radius = (type == EnemyType.Golem) ? 0.8f : 0.5f;
            return obj;
        }

        public static GameObject CreateCorpseGameObject(EnemyType type, int soulCost)
        {
            GameObject obj = new GameObject($"Corpse_{type}");
            var sr = obj.AddComponent<SpriteRenderer>();
            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.6f;

            sr.sprite = GetCorpseSprite();
            sr.color = White;
            sr.sortingOrder = 2;

            var corpseComp = obj.AddComponent<Corpse>();
            corpseComp.Initialize(type, soulCost, sr.sprite);

            return obj;
        }

        public static GameObject CreateProjectileGameObject(string name, Color color)
        {
            GameObject obj = new GameObject($"Proj_{name}");
            var sr = obj.AddComponent<SpriteRenderer>();
            var col = obj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.25f;

            sr.sprite = GetProjectileSprite(name);
            sr.color = White;
            sr.sortingOrder = 10;

            if (name.Contains("DarkBolt"))
            {
                obj.AddComponent<HomingProjectile>();
            }
            else
            {
                obj.AddComponent<Projectile>();
            }

            return obj;
        }
    }
}
