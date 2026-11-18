using UnityEngine;

namespace NecroDefense
{
    /// <summary>ค่าของมินเนียน 1 ชนิด</summary>
    [CreateAssetMenu(menuName = "NecroDefense/Minion Stats", fileName = "Minion_Stats")]
    public class MinionStats : UnitStats
    {
        [Header("Minion")]
        [Tooltip("Mana ที่ใช้อัญเชิญ")]
        public int manaCost = 10;

        [Header("AI Ranges")]
        [Tooltip("ระยะมองหาศัตรู")]
        public float aggroRange = 5f;
        [Tooltip("ไล่ได้ไกลสุดจากจุดเฝ้า")]
        public float chaseLeash = 8f;
        [Tooltip("รัศมีเฝ้าพื้นที่")]
        public float guardRadius = 6f;
        [Tooltip("ระยะตามผู้เล่น (คำสั่ง Follow)")]
        public float followDistance = 1.8f;
    }
}
