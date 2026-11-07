using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace NecroDefense.EditorTools
{
    /// <summary>ตัวช่วยตั้งค่าฟิลด์ [SerializeField] แบบ private ผ่าน SerializedObject (ใช้เฉพาะตอนสร้างซีน)</summary>
    internal class SO
    {
        private readonly SerializedObject _so;

        public SO(Object target) { _so = new SerializedObject(target); }

        public SerializedProperty Prop(string name)
        {
            SerializedProperty p = _so.FindProperty(name);
            if (p == null) throw new Exception("Field '" + name + "' not found on " + _so.targetObject.GetType().Name);
            return p;
        }

        public SO Set(string name, object value)
        {
            Assign(Prop(name), value);
            return this;
        }

        public SO List(string name, int count, Action<SerializedProperty, int> fill)
        {
            SerializedProperty p = Prop(name);
            p.arraySize = count;
            for (int i = 0; i < count; i++) fill(p.GetArrayElementAtIndex(i), i);
            return this;
        }

        public SO ObjList(string name, params Object[] items)
        {
            return List(name, items.Length, (e, i) => e.objectReferenceValue = items[i]);
        }

        public void Apply() => _so.ApplyModifiedPropertiesWithoutUndo();

        public static void Rel(SerializedProperty element, string name, object value)
        {
            SerializedProperty p = element.FindPropertyRelative(name);
            if (p == null) throw new Exception("Relative field '" + name + "' not found");
            Assign(p, value);
        }

        public static void Assign(SerializedProperty p, object v)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Float: p.floatValue = Convert.ToSingle(v); break;
                case SerializedPropertyType.Integer: p.intValue = Convert.ToInt32(v); break;
                case SerializedPropertyType.Boolean: p.boolValue = (bool)v; break;
                case SerializedPropertyType.String: p.stringValue = (string)v; break;
                case SerializedPropertyType.Enum: p.intValue = Convert.ToInt32(v); break;
                case SerializedPropertyType.ObjectReference: p.objectReferenceValue = v as Object; break;
                case SerializedPropertyType.Color: p.colorValue = (Color)v; break;
                case SerializedPropertyType.Vector2: p.vector2Value = (Vector2)v; break;
                case SerializedPropertyType.Vector3: p.vector3Value = (Vector3)v; break;
                default: throw new Exception("Unsupported property type " + p.propertyType + " for " + p.name);
            }
        }
    }

    /// <summary>สร้างรูป placeholder (วงกลม/สี่เหลี่ยม/ฯลฯ) และ helper สร้าง GameObject / UI</summary>
    internal static class BU
    {
        public const string ArtDir = "Assets/Game/Art/Placeholder";

        public const string UserArtDir = "Assets/picture";

        public static Sprite Circle, Ring, Square, SquareLeft, Diamond, Triangle;
        public static Sprite Flag, Lock, Check, ArrowUp, Coin, Heart, Shield, Castle, Bag, Star, Sword, Clock, Skull, Waves, Tree, Question;
        public static Sprite TowerOrb, TowerBomb, TowerBeam;

        // ------------------------------------------------------------------
        // Folders & sprites
        // ------------------------------------------------------------------

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static void CreateSprites()
        {
            EnsureFolder(ArtDir);
            Circle = MakeSprite("Circle", (x, y) => Dist(x, y) <= 0.5f, new Vector2(0.5f, 0.5f));
            Ring = MakeSprite("Ring", (x, y) => { float d = Dist(x, y); return d <= 0.5f && d >= 0.42f; }, new Vector2(0.5f, 0.5f));
            Square = MakeSprite("Square", (x, y) => true, new Vector2(0.5f, 0.5f));
            SquareLeft = MakeSprite("SquareLeftPivot", (x, y) => true, new Vector2(0f, 0.5f));
            Diamond = MakeSprite("Diamond", (x, y) => Mathf.Abs(x - 0.5f) + Mathf.Abs(y - 0.5f) <= 0.5f, new Vector2(0.5f, 0.5f));
            Triangle = MakeSprite("Triangle", (x, y) => y >= 0.02f && Mathf.Abs(x - 0.5f) <= (1f - y) * 0.5f, new Vector2(0.5f, 0.5f));

            Vector2 c = new Vector2(0.5f, 0.5f);
            // ---- ไอคอน UI (placeholder เปลี่ยนเป็นรูปจริงได้ใน Assets/Game/Art/Placeholder) ----
            Check = MakeSprite("Icon_Check", (x, y) => SegDist(x, y, 0.15f, 0.52f, 0.4f, 0.25f) < 0.09f || SegDist(x, y, 0.4f, 0.25f, 0.86f, 0.78f) < 0.09f, c);
            ArrowUp = MakeSprite("Icon_ArrowUp", (x, y) => (y >= 0.5f && y <= 0.95f && Mathf.Abs(x - 0.5f) <= (0.95f - y) * 0.85f) || (Mathf.Abs(x - 0.5f) < 0.13f && y > 0.08f && y < 0.55f), c);
            Coin = MakeSprite("Icon_Coin", (x, y) => { float d = Dist(x, y); return d <= 0.48f && !(d < 0.36f && d > 0.3f); }, c);
            Heart = MakeSprite("Icon_Heart", (x, y) => { float u = (x - 0.5f) * 2.6f, v = (y - 0.45f) * 2.6f; float a = u * u + v * v - 1f; return a * a * a - u * u * v * v * v <= 0f; }, c);
            Shield = MakeSprite("Icon_Shield", (x, y) => y <= 0.9f && y >= 0.05f && Mathf.Abs(x - 0.5f) <= (y >= 0.45f ? 0.36f : 0.36f * (y - 0.05f) / 0.4f), c);
            Castle = MakeSprite("Icon_Castle", (x, y) =>
            {
                bool gate = Mathf.Abs(x - 0.5f) < 0.1f && y < 0.32f;
                bool body = x > 0.12f && x < 0.88f && y > 0.05f && y < 0.6f;
                bool merlons = y >= 0.6f && y < 0.72f && x > 0.12f && x < 0.88f && Mathf.Repeat((x - 0.12f) / 0.152f, 1f) < 0.55f;
                bool keep = Mathf.Abs(x - 0.5f) < 0.14f && y >= 0.6f && y < 0.95f;
                return (body || merlons || keep) && !gate;
            }, c);
            Bag = MakeSprite("Icon_Bag", (x, y) => Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.38f)) < 0.34f || (Mathf.Abs(x - 0.5f) < 0.12f && y >= 0.66f && y < 0.8f) || (Mathf.Abs(x - 0.5f) < 0.22f && y >= 0.8f && y < 0.9f), c);
            Star = MakeSprite("Icon_Star", (x, y) => { float dx = x - 0.5f, dy = y - 0.5f; float a = Mathf.Atan2(dy, dx); float r = Mathf.Sqrt(dx * dx + dy * dy); return r < 0.2f + 0.27f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2.5f + 1.5708f * 0.2f)), 3f); }, c);
            Sword = MakeSprite("Icon_Sword", (x, y) => (Mathf.Abs(x - 0.5f) < 0.07f && y > 0.32f && y < 0.95f) || (Mathf.Abs(x - 0.5f) < 0.25f && y > 0.25f && y < 0.33f) || (Mathf.Abs(x - 0.5f) < 0.05f && y > 0.06f && y <= 0.25f), c);
            Clock = MakeSprite("Icon_Clock", (x, y) => { float d = Dist(x, y); return (d < 0.48f && d > 0.38f) || SegDist(x, y, 0.5f, 0.5f, 0.5f, 0.82f) < 0.05f || SegDist(x, y, 0.5f, 0.5f, 0.72f, 0.5f) < 0.05f; }, c);
            Skull = MakeSprite("Icon_Skull", (x, y) =>
            {
                bool head = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.58f)) < 0.36f;
                bool jaw = Mathf.Abs(x - 0.5f) < 0.2f && y > 0.1f && y < 0.3f;
                bool eyes = Vector2.Distance(new Vector2(x, y), new Vector2(0.36f, 0.55f)) < 0.09f || Vector2.Distance(new Vector2(x, y), new Vector2(0.64f, 0.55f)) < 0.09f;
                return (head || jaw) && !eyes;
            }, c);
            Waves = MakeSprite("Icon_Waves", (x, y) => { float d = Dist(x, y); return (d < 0.48f && d > 0.41f) || (d < 0.32f && d > 0.25f) || d < 0.12f; }, c);
            Tree = MakeSprite("Deco_Tree", (x, y) => (Mathf.Abs(x - 0.5f) < 0.06f && y < 0.3f) || (y >= 0.2f && Mathf.Abs(x - 0.5f) <= (1f - y) * 0.45f), new Vector2(0.5f, 0f));
            Flag = MakeSprite("Icon_Flag", (x, y) => (x > 0.2f && x < 0.28f && y > 0.04f && y < 0.95f) ||
                (x >= 0.28f && x < 0.85f && y > 0.55f && y < 0.92f && Mathf.Abs(y - 0.735f) <= (0.85f - x) * 0.33f), new Vector2(0.24f, 0.05f));
            Lock = MakeSprite("Icon_Lock", (x, y) =>
            {
                bool body = x > 0.2f && x < 0.8f && y > 0.05f && y < 0.55f;
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.55f));
                bool shackle = y >= 0.55f && d > 0.17f && d < 0.27f;
                bool hole = Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.33f)) < 0.07f || (Mathf.Abs(x - 0.5f) < 0.03f && y > 0.15f && y < 0.33f);
                return (body || shackle) && !hole;
            }, c);
            Question = MakeSprite("Icon_Help", (x, y) => { float d = Dist(x, y); return d < 0.48f && d > 0.4f; }, c);

            // ---- ป้อม placeholder ----
            TowerOrb = MakeSprite("Tower_Orb", (x, y) => (x > 0.28f && x < 0.72f && y > 0.03f && y < 0.6f) || (y >= 0.6f && y < 0.66f && x > 0.22f && x < 0.78f) || Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.8f)) < 0.15f, new Vector2(0.5f, 0.1f));
            TowerBomb = MakeSprite("Tower_Bomb", (x, y) => (x > 0.15f && x < 0.85f && y > 0.03f && y < 0.42f) || Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.42f)) < 0.27f || (x > 0.5f && x < 0.92f && y > 0.55f && y < 0.66f), new Vector2(0.5f, 0.1f));
            TowerBeam = MakeSprite("Tower_Beam", (x, y) => (Mathf.Abs(x - 0.5f) < 0.26f && y > 0.03f && y < 0.25f) || (y >= 0.25f && y < 0.97f && Mathf.Abs(x - 0.5f) <= 0.2f * (1f - (y - 0.25f) / 0.72f) + 0.02f), new Vector2(0.5f, 0.1f));
        }

        private static float SegDist(float x, float y, float ax, float ay, float bx, float by)
        {
            Vector2 p = new Vector2(x, y), a = new Vector2(ax, ay), b = new Vector2(bx, by);
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        /// <summary>
        /// โหลดรูปของผู้ใช้จาก Assets/picture/&lt;name&gt;.pt.png (ไม่มีไฟล์ = null)
        /// ถ้ารูปถูกตั้งเป็น Sprite Mode = Multiple จะเปลี่ยนเป็น Single ให้ (ใช้ทั้งภาพเป็นรูปเดียว)
        /// </summary>
        public static Sprite Art(string name)
        {
            string path = UserArtDir + "/" + name + ".pt.png";
            if (!File.Exists(path)) return null;
            TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>สเกลให้ด้านที่ยาวที่สุดของรูปเท่ากับ size หน่วย</summary>
        public static float FitScale(Sprite s, float size)
        {
            if (s == null) return size;
            Vector3 b = s.bounds.size;
            float m = Mathf.Max(b.x, b.y);
            return m > 0.0001f ? size / m : size;
        }

        private static float Dist(float x, float y) => Vector2.Distance(new Vector2(x, y), new Vector2(0.5f, 0.5f));

        private static Sprite MakeSprite(string name, Func<float, float, bool> inside, Vector2 pivot)
        {
            string path = ArtDir + "/" + name + ".png";
            if (!File.Exists(path))
            {
                const int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                for (int y = 0; y < size; y++)
                    for (int x = 0; x < size; x++)
                    {
                        float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
                        tex.SetPixel(x, y, inside(u, v) ? Color.white : new Color(1f, 1f, 1f, 0f));
                    }
                tex.Apply();
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

                TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 64;
                imp.mipmapEnabled = false;
                imp.alphaIsTransparency = true;
                imp.filterMode = FilterMode.Bilinear;
                TextureImporterSettings s = new TextureImporterSettings();
                imp.ReadTextureSettings(s);
                s.spriteAlignment = (int)SpriteAlignment.Custom;
                s.spritePivot = pivot;
                s.spriteMeshType = SpriteMeshType.FullRect;
                imp.SetTextureSettings(s);
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ------------------------------------------------------------------
        // World objects
        // ------------------------------------------------------------------

        public static GameObject Go(string name, Transform parent = null, Vector3? localPos = null)
        {
            GameObject go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            if (localPos.HasValue) go.transform.localPosition = localPos.Value;
            return go;
        }

        public static SpriteRenderer Sprite(string name, Transform parent, Sprite sprite, Color color, Vector2 scale, int order, Vector3? localPos = null)
        {
            GameObject go = Go(name, parent, localPos);
            go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        public static WorldHealthBar HealthBar(Transform parent, float y, float width, bool hideWhenFull)
        {
            GameObject root = Go("HealthBar", parent, new Vector3(0f, y, 0f));
            Sprite("Background", root.transform, Square, new Color(0f, 0f, 0f, 0.8f), new Vector2(width + 0.06f, 0.14f), 20);
            SpriteRenderer fill = Sprite("Fill", root.transform, SquareLeft, new Color(0.3f, 1f, 0.35f), new Vector2(width, 0.09f), 21, new Vector3(-width * 0.5f, 0f, 0f));
            WorldHealthBar hb = root.AddComponent<WorldHealthBar>();
            new SO(hb).Set("fill", fill.transform).Set("hideWhenFull", hideWhenFull).Apply();
            return hb;
        }

        public static Rigidbody2D Body2D(GameObject go, float colliderRadius, bool trigger, float mass = 1f)
        {
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.mass = mass;
            rb.linearDamping = 0f;
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.radius = colliderRadius;
            col.isTrigger = trigger;
            return rb;
        }

        // ------------------------------------------------------------------
        // UI
        // ------------------------------------------------------------------

        public static readonly Color PanelColor = new Color(0.05f, 0.05f, 0.08f, 0.82f);
        public static readonly Color ButtonColor = new Color(0.2f, 0.2f, 0.26f, 0.95f);

        public static RectTransform UI(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float padding = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
            return rt;
        }

        public static Image Img(RectTransform rt, Color color, bool raycast = true, Sprite sprite = null)
        {
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (sprite != null) img.sprite = sprite;
            return img;
        }

        public static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color? color = null)
        {
            RectTransform rt = Place(UI(name, parent), anchor, pivot, pos, size);
            Img(rt, color ?? PanelColor);
            return rt;
        }

        public static TextMeshProUGUI Text(string name, Transform parent, string text, float fontSize,
            Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
            TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null)
        {
            RectTransform rt = Place(UI(name, parent), anchor, pivot, pos, size);
            TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) t.font = TMP_Settings.defaultFontAsset;
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color ?? Color.white;
            t.raycastTarget = false;
            return t;
        }

        public static TextMeshProUGUI StretchText(string name, Transform parent, string text, float fontSize,
            TextAlignmentOptions align = TextAlignmentOptions.Center, Color? color = null, float padding = 4f)
        {
            TextMeshProUGUI t = Text(name, parent, text, fontSize, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, align, color);
            Stretch(t.rectTransform, padding);
            return t;
        }

        public static Button Button(string name, Transform parent, string label, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
            out TextMeshProUGUI labelText, float fontSize = 22f, Color? color = null)
        {
            RectTransform rt = Place(UI(name, parent), anchor, pivot, pos, size);
            Image img = Img(rt, color ?? ButtonColor);
            Button b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            labelText = StretchText("Label", rt, label, fontSize);
            return b;
        }

        public static Image Icon(string name, Transform parent, Sprite sprite, Color color, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            RectTransform rt = Place(UI(name, parent), anchor, pivot, pos, size);
            Image img = Img(rt, color, false, sprite);
            img.preserveAspect = true;
            return img;
        }

        public static Button MakeButton(GameObject go, Graphic target)
        {
            Button b = go.AddComponent<Button>();
            b.targetGraphic = target;
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            return b;
        }

        /// <summary>ป้ายเล็ก (พื้นเข้ม + ตัวเลข) เช่น ราคา / จำนวน / ปุ่มลัด</summary>
        public static TextMeshProUGUI Badge(string name, Transform parent, string text, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size,
            Color? bg = null, Color? fg = null, float fontSize = 16f, Sprite icon = null, Color? iconColor = null)
        {
            RectTransform rt = Place(UI(name, parent), anchor, pivot, pos, size);
            Img(rt, bg ?? new Color(0f, 0f, 0f, 0.85f), false);
            if (icon != null)
            {
                Icon("Icon", rt, icon, iconColor ?? Color.white, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(size.y - 6f, size.y - 6f));
                TextMeshProUGUI t = Text("Text", rt, text, fontSize, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, fg ?? Color.white);
                t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
                t.rectTransform.offsetMin = new Vector2(size.y - 2f, 0f); t.rectTransform.offsetMax = new Vector2(-2f, 0f);
                t.fontStyle = FontStyles.Bold;
                return t;
            }
            TextMeshProUGUI txt = StretchText("Text", rt, text, fontSize, TextAlignmentOptions.Center, fg ?? Color.white, 1f);
            txt.fontStyle = FontStyles.Bold;
            return txt;
        }

        public static BarUI Bar(string name, Transform parent, string title, Color fillColor, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            RectTransform rt = Place(UI(name, parent), anchor, pivot, pos, size);
            Img(rt, new Color(0f, 0f, 0f, 0.7f), false);

            RectTransform fillRt = Stretch(UI("Fill", rt), 3f);
            Image fill = Img(fillRt, fillColor, false, Square);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            fill.fillAmount = 1f;

            TextMeshProUGUI label = StretchText("Label", rt, title, 18f);

            BarUI bar = rt.gameObject.AddComponent<BarUI>();
            new SO(bar).Set("fill", fill).Set("label", label).Set("title", title).Set("format", "{1}/{2}").Apply();
            return bar;
        }
    }
}
