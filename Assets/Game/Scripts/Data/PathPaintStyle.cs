using UnityEngine;

namespace NecroDefense
{
    public enum PathLook { Dirt, Bricks }

    /// <summary>
    /// หน้าตาพื้นทางเดิน (ทางมอน + ลานฝั่งเรา) ของ Map2 - แก้ค่าแล้วกดเมนู Tools > NecroDefense > Updates > Bake Map2 Brick Path ใหม่
    /// </summary>
    [CreateAssetMenu(menuName = "NecroDefense/Path Paint Style", fileName = "PathPaintStyle")]
    public class PathPaintStyle : ScriptableObject
    {
        [Tooltip("Dirt = ทางดินขอบขรุขระ + ก้อนกรวด / Bricks = พื้นอิฐ")]
        public PathLook look = PathLook.Dirt;
        [Tooltip("ลานฝั่งเราใช้สีเดียวกับทางมอน (ไม่เห็นเป็นกรอบสี่เหลี่ยมสีต่าง)")]
        public bool zoneSameAsLane = true;

        [Header("Dirt (ทางดิน)")]
        public Color dirtColor = new Color(0.80f, 0.80f, 0.80f);
        [Tooltip("ความด่างของพื้น (จุดอ่อน-เข้มใหญ่ๆ)")]
        [Range(0f, 0.2f)] public float dirtMottle = 0.08f;
        [Tooltip("ความหยาบละเอียดของพื้น")]
        [Range(0f, 0.2f)] public float dirtGrain = 0.03f;
        [Tooltip("สีแถบขอบทาง")]
        public Color dirtEdgeColor = new Color(0.56f, 0.56f, 0.56f);
        [Tooltip("ความกว้างแถบขอบทาง (หน่วย)")]
        public float dirtEdgeWidth = 0.22f;
        [Tooltip("ความขรุขระของขอบ (หน่วย)")]
        public float dirtEdgeRoughness = 0.2f;
        [Tooltip("จำนวนก้อนกรวด (ต่อ 1 ตารางหน่วย)")]
        [Range(0f, 2f)] public float pebbleDensity = 0.45f;
        public Color pebbleColor = new Color(0.6f, 0.6f, 0.6f);
        public Color pebbleOutline = new Color(0.3f, 0.3f, 0.3f);
        [Tooltip("ขนาดก้อนกรวด (หน่วย) ต่ำสุด-สูงสุด")]
        public Vector2 pebbleSize = new Vector2(0.03f, 0.07f);
        public int seed = 7;

        [Header("Bricks (พื้นอิฐ)")]
        [Tooltip("สีร่องอิฐ (ทางมอน)")]
        public Color laneMortar = new Color(0.45f, 0.45f, 0.45f);
        [Tooltip("สีก้อนอิฐ (ทางมอน)")]
        public Color laneStone = new Color(0.80f, 0.80f, 0.80f);
        [Tooltip("สีร่อง / ก้อน ของลานฝั่งเรา (ใช้เมื่อปิด Zone Same As Lane)")]
        public Color zoneMortar = new Color(0.70f, 0.70f, 0.70f);
        public Color zoneStone = new Color(0.93f, 0.93f, 0.93f);
        [Tooltip("รูปอิฐ 1 แผ่น (สูง 1080px) สูงกี่หน่วยในโลก - ยิ่งน้อยอิฐยิ่งเล็ก")]
        public float tileHeightUnits = 3.2f;

        [Header("ขอบทาง (ทั้ง 2 แบบ)")]
        [Tooltip("เส้นขอบเข้มรอบทาง (หน่วย)")]
        public float borderWidth = 0.04f;
        public Color borderColor = new Color(0.22f, 0.22f, 0.22f);
        [Tooltip("ขอบด้านในมืดลงนิดๆ (หน่วย)")]
        public float edgeShadeWidth = 0.4f;
        [Range(0f, 1f)] public float edgeShadeStrength = 0.1f;
        [Tooltip("เงาทอดออกนอกทางลงบนพื้น (หน่วย)")]
        public float dropShadowWidth = 0.3f;
        [Range(0f, 1f)] public float dropShadowAlpha = 0.38f;

        [Header("ความละเอียด")]
        [Tooltip("พิกเซลต่อ 1 หน่วย (ยิ่งมากยิ่งคม แต่ไฟล์ใหญ่ขึ้น)")]
        public int pixelsPerUnit = 64;
    }
}
