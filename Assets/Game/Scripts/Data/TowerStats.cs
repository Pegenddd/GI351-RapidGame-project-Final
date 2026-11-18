using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>ค่าของป้อม 1 แบบ (ราคา / ดาเมจ / ระยะ แต่ละ Tier)</summary>
    [CreateAssetMenu(menuName = "NecroDefense/Tower Stats", fileName = "Tower_Stats")]
    public class TowerStats : ScriptableObject
    {
        [Header("Info")]
        public string towerName = "Tower";
        [TextArea(2, 4)] public string description;

        [Header("Attack")]
        [Tooltip("ยิงตัวบินได้ไหม")]
        public bool canHitFlying = true;
        [Tooltip("ระยะกระโดดของลำแสง (Chain)")]
        public float chainRange = 3f;
        [Tooltip("เวลาก่อสร้าง/อัปเกรด (วินาที)")]
        public float constructionTime = 1f;

        [Header("Levels (0 = สร้าง, 1-3 = อัปเกรด)")]
        public List<TowerLevel> levels = new List<TowerLevel>();
    }
}
