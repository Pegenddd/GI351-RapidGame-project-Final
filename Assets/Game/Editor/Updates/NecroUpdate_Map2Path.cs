using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NecroDefense.EditorTools
{
    /// <summary>
    /// เปลี่ยนพื้นทางมอน + ลานฝั่งเรา ในซีน Map2 เป็นทางดินขอบขรุขระ + ก้อนกรวด (ค่าเริ่มต้น) หรือพื้นอิฐ (รูป Path_Bricks)
    /// เมนู: Tools > NecroDefense > Updates > Bake Map2 Brick Path
    /// - อ่านรูปทรงทางจากของเดิม (Map > Ground_Map2: ทางมอน / ลานฝั่งเรา) แล้ว "อบ" เป็นรูปเดียว Map2_Path.png
    ///   ทางมอนอิฐโทนเทาอ่อน / ลานฝั่งเราอิฐโทนขาว / ขอบเข้ม + ขอบในมืดนิดๆ ให้ดูนูน + เงาทอดลงพื้น
    /// - ปรับสี / ขนาดอิฐ / ขอบ ได้ที่ Assets/Game/Data/Map2_PathLook.asset แล้วกดเมนูนี้ใหม่
    /// - ย้ายทางมอน (เส้นขาวเดิม) แล้วกดเมนูนี้ใหม่ = พื้นอิฐตามไปเอง
    /// - ของเดิม (พื้นขาว + เส้นขอบเดิม) แค่ปิดไว้ ไม่ลบ
    /// </summary>
    public static class NecroUpdate_Map2Path
    {
        private const string ScenePath = "Assets/Scenes/Map2.unity";
        private const string BrickFile = "Assets/Game/Art/Map/Path_Bricks.png";
        private const string OutFile = "Assets/Game/Art/Map/Map2_Path.png";
        private const string StylePath = "Assets/Game/Data/Map2_PathLook.asset";
        private const string ObjName = "Path_Bricks";

        private struct Shape { public bool circle, zone; public Vector2 c; public float angle, hx, hy, r; }

        [MenuItem("Tools/NecroDefense/Updates/Bake Map2 Brick Path")]
        public static void Apply()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                EditorUtility.DisplayDialog("Brick Path", "ไม่เจอซีน Map2", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            AssetDatabase.Refresh();

            PathPaintStyle style = AssetDatabase.LoadAssetAtPath<PathPaintStyle>(StylePath);
            if (style == null)
            {
                NecroDataTools.EnsureFolder("Assets/Game/Data");
                style = ScriptableObject.CreateInstance<PathPaintStyle>();
                AssetDatabase.CreateAsset(style, StylePath);
                AssetDatabase.SaveAssets();
            }

            Texture2D bricks = LoadReadable(BrickFile);
            if (bricks == null) { EditorUtility.DisplayDialog("Brick Path", "ไม่เจอรูป " + BrickFile, "OK"); return; }

            string start = SceneManager.GetActiveScene().path;
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            MapBounds bounds = Object.FindAnyObjectByType<MapBounds>(FindObjectsInactive.Include);
            Transform map = bounds != null ? bounds.transform : null;
            Transform ground = map != null ? map.Find("Ground_Map2") : null;
            if (ground == null) { EditorUtility.DisplayDialog("Brick Path", "ไม่เจอ Map > Ground_Map2 (กด Create Map2 Scene ก่อน)", "OK"); return; }

            // ----- อ่านรูปทรงทางจากของเดิม -----
            List<Shape> shapes = new List<Shape>();
            List<GameObject> toHide = new List<GameObject>();
            foreach (SpriteRenderer sr in ground.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string n = sr.name;
                if (n == "Ground_Gray" || n == ObjName || sr.sprite == null) continue;
                Vector3 ls = sr.transform.lossyScale;
                Vector3 ext = sr.sprite.bounds.extents;
                Shape s = new Shape
                {
                    circle = sr.sprite.name.Contains("Circle"),
                    zone = n.StartsWith("OurZone"),
                    c = sr.transform.position,
                    angle = sr.transform.eulerAngles.z * Mathf.Deg2Rad,
                    hx = Mathf.Abs(ls.x) * ext.x,
                    hy = Mathf.Abs(ls.y) * ext.y,
                };
                s.r = s.hx;
                shapes.Add(s);
                toHide.Add(sr.gameObject);
            }
            if (shapes.Count == 0) { EditorUtility.DisplayDialog("Brick Path", "ไม่เจอรูปทรงทางใน Ground_Map2", "OK"); return; }

            // ----- พื้นที่ที่จะอบ (ขอบเขตแมพ + เผื่อ) -----
            Vector2 bc = bounds.Center, bs = bounds.Size + new Vector2(6f, 6f);
            int ppu = Mathf.Clamp(style.pixelsPerUnit, 16, 128);
            int W = Mathf.Min(4096, Mathf.RoundToInt(bs.x * ppu)), H = Mathf.Min(4096, Mathf.RoundToInt(bs.y * ppu));
            float upp = bs.x / W; // หน่วยต่อพิกเซล
            Vector2 origin = bc - bs * 0.5f;

            Color32[] src = bricks.GetPixels32();
            int bw = bricks.width, bh = bricks.height;
            float tileH = Mathf.Max(0.5f, style.tileHeightUnits), tileW = tileH * bw / bh;
            float maxL = 0.01f;
            for (int i = 0; i < src.Length; i += 7) maxL = Mathf.Max(maxL, Mathf.Max(src[i].r, Mathf.Max(src[i].g, src[i].b)) / 255f);

            Color32[] outPx = new Color32[W * H];
            float aa = upp;
            for (int y = 0; y < H; y++)
            {
                if ((y & 63) == 0 && EditorUtility.DisplayCancelableProgressBar("Brick Path", "กำลังอบพื้นอิฐ...", (float)y / H))
                {
                    EditorUtility.ClearProgressBar();
                    return;
                }
                float wy = origin.y + (y + 0.5f) * upp;
                for (int x = 0; x < W; x++)
                {
                    float wx = origin.x + (x + 0.5f) * upp;
                    float bestLane = -999f, bestZone = -999f;
                    for (int i = 0; i < shapes.Count; i++)
                    {
                        float d = Inside(shapes[i], wx, wy);
                        if (shapes[i].zone) { if (d > bestZone) bestZone = d; }
                        else if (d > bestLane) bestLane = d;
                    }
                    float best = Mathf.Max(bestLane, bestZone);
                    bool zone = bestZone > 0f && !style.zoneSameAsLane;
                    bool dirt = style.look == PathLook.Dirt;
                    if (dirt && style.dirtEdgeRoughness > 0f)   // ขอบขรุขระ
                        best += ((VNoise(wx * 1.6f, wy * 1.6f, style.seed) - 0.5f) * 1.1f + (VNoise(wx * 5f, wy * 5f, style.seed + 1) - 0.5f) * 0.5f) * style.dirtEdgeRoughness;

                    // เงาทอดนอกทาง
                    Color col = new Color(0f, 0f, 0f, 0f);
                    if (best < 0f && style.dropShadowWidth > 0f)
                    {
                        float t = 1f - Mathf.Clamp01(-best / style.dropShadowWidth);
                        col = new Color(0f, 0f, 0f, style.dropShadowAlpha * t * t);
                    }
                    if (best > -aa && dirt)
                    {
                        float grain = VNoise(wx * ppu * 0.5f, wy * ppu * 0.5f, style.seed + 2) - 0.5f;
                        float mottle = (VNoise(wx * 0.7f, wy * 0.7f, style.seed + 3) - 0.5f) * 0.75f + (VNoise(wx * 2.2f, wy * 2.2f, style.seed + 4) - 0.5f) * 0.5f;
                        Color c = style.dirtColor * (1f + mottle * style.dirtMottle * 2f + grain * style.dirtGrain * 2f);
                        float d = Mathf.Max(0f, best);
                        Color e = style.dirtEdgeColor * (1f + grain * 0.2f);
                        c = Color.Lerp(e, c, Mathf.Clamp01((d - style.dirtEdgeWidth) / (aa * 3f) + 0.5f));
                        if (style.edgeShadeWidth > 0f && d > style.dirtEdgeWidth)
                            c *= 1f - style.edgeShadeStrength * (1f - Mathf.Clamp01((d - style.dirtEdgeWidth) / style.edgeShadeWidth));
                        if (style.borderWidth > 0f)
                            c = Color.Lerp(style.borderColor, c, Mathf.Clamp01((d - style.borderWidth) / aa + 0.5f));
                        c.a = 1f;
                        float a = Mathf.Clamp01(best / aa + 0.5f);
                        col = Color.Lerp(col, c, a);
                        col.a = Mathf.Max(col.a, a);
                        outPx[y * W + x] = col;
                        continue;
                    }
                    if (best > -aa)
                    {
                        // ลายอิฐ (กลับด้านสลับกันทุกแผ่น = ต่อกันเนียน)
                        float u = wx / tileW, v = wy / tileH;
                        int iu = Mathf.FloorToInt(u), iv = Mathf.FloorToInt(v);
                        float fu = u - iu, fv = v - iv;
                        if ((iu & 1) != 0) fu = 1f - fu;
                        if ((iv & 1) != 0) fv = 1f - fv;
                        Color32 b = src[Mathf.Clamp((int)(fv * (bh - 1)), 0, bh - 1) * bw + Mathf.Clamp((int)(fu * (bw - 1)), 0, bw - 1)];
                        float L = Mathf.Clamp01(Mathf.Max(b.r, Mathf.Max(b.g, b.b)) / 255f / maxL);
                        Color c = zone ? Color.Lerp(style.zoneMortar, style.zoneStone, L) : Color.Lerp(style.laneMortar, style.laneStone, L);

                        float d = Mathf.Max(0f, best);
                        if (style.edgeShadeWidth > 0f)
                            c *= 1f - style.edgeShadeStrength * (1f - Mathf.Clamp01(d / style.edgeShadeWidth));
                        if (style.borderWidth > 0f)
                            c = Color.Lerp(style.borderColor, c, Mathf.Clamp01((d - style.borderWidth) / aa + 0.5f));
                        c.a = 1f;
                        float a = Mathf.Clamp01(best / aa + 0.5f);
                        col = Color.Lerp(col, c, a);
                        col.a = Mathf.Max(col.a, a);
                    }
                    outPx[y * W + x] = col;
                }
            }
            EditorUtility.ClearProgressBar();

            // ----- ก้อนกรวด (ทางดิน) -----
            if (style.look == PathLook.Dirt && style.pebbleDensity > 0f)
            {
                System.Random rnd = new System.Random(style.seed);
                float cell = 1f;
                for (float gy = origin.y; gy < origin.y + H * upp; gy += cell)
                    for (float gx = origin.x; gx < origin.x + W * upp; gx += cell)
                    {
                        int count = (int)style.pebbleDensity + (rnd.NextDouble() < style.pebbleDensity % 1f ? 1 : 0);
                        for (int k = 0; k < count; k++)
                        {
                            float px = gx + (float)rnd.NextDouble() * cell, py = gy + (float)rnd.NextDouble() * cell;
                            float r = Mathf.Lerp(style.pebbleSize.x, style.pebbleSize.y, (float)rnd.NextDouble());
                            float stretch = 1f + (float)rnd.NextDouble() * 0.5f;
                            float shade = 0.9f + (float)rnd.NextDouble() * 0.2f;
                            float inside = -999f;
                            foreach (Shape s in shapes) inside = Mathf.Max(inside, Inside(s, px, py));
                            if (inside < style.dirtEdgeWidth + 0.15f) continue;
                            DrawPebble(outPx, W, H, origin, upp, px, py, r * stretch, r, style.pebbleColor * shade, style.pebbleOutline);
                        }
                    }
            }

            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(outPx);
            tex.Apply();
            File.WriteAllBytes(OutFile, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(OutFile);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(OutFile);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = ppu;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.maxTextureSize = 4096;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(OutFile);

            // ----- วางในซีน -----
            Transform old = ground.Find(ObjName);
            if (old != null) Object.DestroyImmediate(old.gameObject);
            GameObject go = new GameObject(ObjName);
            go.transform.SetParent(ground, false);
            go.transform.position = new Vector3(origin.x + W * upp * 0.5f, origin.y + H * upp * 0.5f, 0f);
            SpriteRenderer psr = go.AddComponent<SpriteRenderer>();
            psr.sprite = sprite;
            psr.sortingOrder = -28;

            foreach (GameObject g in toHide) g.SetActive(false);
            Transform decor = map.Find("Decor_Map2");
            Transform borders = decor != null ? decor.Find("LaneBorders") : null;
            if (borders != null) borders.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (!string.IsNullOrEmpty(start) && start != ScenePath) EditorSceneManager.OpenScene(start, OpenSceneMode.Single);

            string log = "อบพื้นอิฐแล้ว (" + W + "x" + H + " px, รูปทรง " + shapes.Count + " ชิ้น)\n" +
                         "พื้นขาวเดิม + เส้นขอบเดิม ปิดไว้ ไม่ลบ\n\n" +
                         "ปรับสี / ขนาดอิฐ / ขอบ: " + StylePath + " แล้วกดเมนูนี้ใหม่\n" +
                         "(ย้ายทางมอนใน Ground_Map2 แล้วกดใหม่ = พื้นตามไปเอง)";
            Debug.Log("[NecroDefense] Bake Map2 Brick Path\n" + log);
            EditorUtility.DisplayDialog("Brick Path", log, "OK");
            Selection.activeObject = style;
        }

        private static void DrawPebble(Color32[] px, int W, int H, Vector2 origin, float upp, float cx, float cy, float rx, float ry, Color fill, Color outline)
        {
            float ol = upp * 1.3f;
            int x0 = Mathf.Max(0, (int)((cx - rx - ol - origin.x) / upp) - 1), x1 = Mathf.Min(W - 1, (int)((cx + rx + ol - origin.x) / upp) + 1);
            int y0 = Mathf.Max(0, (int)((cy - ry - ol - origin.y) / upp) - 1), y1 = Mathf.Min(H - 1, (int)((cy + ry + ol - origin.y) / upp) + 1);
            Color hi = Color.Lerp(fill, Color.white, 0.3f);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    float wx = origin.x + (x + 0.5f) * upp, wy = origin.y + (y + 0.5f) * upp;
                    float nx = (wx - cx) / (rx + ol), ny = (wy - cy) / (ry + ol);
                    if (nx * nx + ny * ny > 1f) continue;
                    float ix = (wx - cx) / rx, iy = (wy - cy) / ry;
                    Color c = ix * ix + iy * iy <= 1f ? (iy > 0.2f && ix < 0.2f ? hi : fill) : outline;
                    c.a = 1f;
                    px[y * W + x] = c;
                }
        }

        private static float Hash(int ix, int iy, int seed)
        {
            unchecked
            {
                uint n = (uint)(ix * 374761393 + iy * 668265263 + seed * 144665);
                n = (n ^ (n >> 13)) * 1274126177u;
                return ((n ^ (n >> 16)) & 0xffff) / 65535f;
            }
        }

        private static float VNoise(float x, float y, int seed)
        {
            int ix = Mathf.FloorToInt(x), iy = Mathf.FloorToInt(y);
            float fx = x - ix, fy = y - iy;
            fx = fx * fx * (3f - 2f * fx); fy = fy * fy * (3f - 2f * fy);
            float a = Hash(ix, iy, seed), b = Hash(ix + 1, iy, seed), c = Hash(ix, iy + 1, seed), d = Hash(ix + 1, iy + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        /// <summary>ระยะจากขอบเข้าไปด้านใน (บวก = อยู่ในรูปทรง, ลบ = อยู่นอก)</summary>
        private static float Inside(Shape s, float x, float y)
        {
            float dx = x - s.c.x, dy = y - s.c.y;
            if (s.circle) return s.r - Mathf.Sqrt(dx * dx + dy * dy);
            float cs = Mathf.Cos(-s.angle), sn = Mathf.Sin(-s.angle);
            float u = Mathf.Abs(dx * cs - dy * sn), v = Mathf.Abs(dx * sn + dy * cs);
            float ox = u - s.hx, oy = v - s.hy;
            if (ox <= 0f && oy <= 0f) return Mathf.Min(-ox, -oy);
            return -Mathf.Sqrt(Mathf.Max(ox, 0f) * Mathf.Max(ox, 0f) + Mathf.Max(oy, 0f) * Mathf.Max(oy, 0f));
        }

        private static Texture2D LoadReadable(string file)
        {
            TextureImporter ti = AssetImporter.GetAtPath(file) as TextureImporter;
            if (ti == null) return null;
            if (!ti.isReadable || ti.textureCompression != TextureImporterCompression.Uncompressed || ti.mipmapEnabled)
            {
                ti.isReadable = true;
                ti.textureCompression = TextureImporterCompression.Uncompressed;
                ti.mipmapEnabled = false;
                ti.maxTextureSize = 2048;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(file);
        }
    }
}
