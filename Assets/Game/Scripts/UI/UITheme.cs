using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NecroDefense
{
    /// <summary>
    /// สีของธีม UI ขาว-ดำ-เทา (2-Color Palette) ใช้ร่วมกันทั้งโค้ดและตัวแต่ง UI ใน Editor
    /// </summary>
    public static class UITheme
    {
        public static readonly Color Ink = new Color(0.035f, 0.035f, 0.04f, 1f);      // ดำสุด
        public static readonly Color Panel = new Color(0.075f, 0.075f, 0.08f, 1f);    // พื้นแผง
        public static readonly Color Slot = new Color(0.13f, 0.13f, 0.135f, 1f);      // ช่อง/ปุ่มปกติ
        public static readonly Color SlotHover = new Color(0.22f, 0.22f, 0.225f, 1f);
        public static readonly Color Highlight = new Color(0.45f, 0.45f, 0.46f, 1f);  // ช่องที่เลือกอยู่
        public static readonly Color Disabled = new Color(0.05f, 0.05f, 0.05f, 1f);   // ทำไม่ได้ / เงินไม่พอ
        public static readonly Color Line = new Color(0.55f, 0.55f, 0.56f, 1f);       // เส้นขอบ
        public static readonly Color Muted = new Color(0.62f, 0.62f, 0.63f, 1f);      // ตัวหนังสือรอง
        public static readonly Color Bone = new Color(0.88f, 0.88f, 0.87f, 1f);       // ขาวกระดูก
        public static readonly Color Paper = new Color(0.97f, 0.97f, 0.96f, 1f);      // ขาวสุด

        // แดงเลือด (สีเน้นแบบ Othercide): ใช้กับ HP, ของที่เลือกอยู่, คำเตือน, ปุ่มหลัก
        public static readonly Color Red = new Color(0.80f, 0.07f, 0.09f, 1f);
        public static readonly Color RedBright = new Color(1f, 0.2f, 0.2f, 1f);
        public static readonly Color RedDark = new Color(0.32f, 0.02f, 0.03f, 1f);

        // สีย้อมสำหรับรูปช่อง/ปุ่ม (รูปเป็นพื้นเทา+ขอบขาว สีย้อมคุมความมืด)
        public static readonly Color SlotTint = new Color(0.62f, 0.62f, 0.63f, 1f);
        public static readonly Color SlotHighlightTint = new Color(0.86f, 0.1f, 0.12f, 1f);   // เลือกอยู่ = แดง
        public static readonly Color SlotDisabledTint = new Color(0.26f, 0.26f, 0.26f, 1f);

        /// <summary>แปลงสีใดๆ เป็นเทา (คงความสว่างและ Alpha เดิม)</summary>
        public static Color Mono(Color c)
        {
            float l = c.r * 0.299f + c.g * 0.587f + c.b * 0.114f;
            return new Color(l, l, l, c.a);
        }
    }
}
