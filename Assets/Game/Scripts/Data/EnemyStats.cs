using UnityEngine;

namespace NecroDefense
{
    /// <summary>ค่าของศัตรู 1 ชนิด</summary>
    [CreateAssetMenu(menuName = "NecroDefense/Enemy Stats", fileName = "Enemy_Stats")]
    public class EnemyStats : UnitStats
    {
        [Header("Targeting (ตีอะไรได้บ้าง)")]
        public bool attackPlayer = true;
        public bool attackMinions = true;
        public bool attackMainBase = true;
        [Tooltip("เป้าหมายหลัก")]
        public TargetPriority priority = TargetPriority.Nearest;
        [Tooltip("ระยะมองเห็นเป้า")]
        public float aggroRange = 5f;
        [Tooltip("ระยะไล่เป้าหมายหลัก")]
        public float priorityChaseRange = 9f;

        [Header("Lane Behaviour")]
        [Tooltip("บนทางมอน สนแค่มินเนียน จนกว่าจะเข้าพื้นที่อิสระ")]
        public bool onlyMinionsOnLane = true;
        [Tooltip("เดินไปยิงไป (Skeleton Archer)")]
        public bool shootWhileWalking = false;

        [Header("Rewards")]
        [Tooltip("Souls ที่ได้เมื่อฆ่า")]
        public int soulReward = 2;
        [Tooltip("XP ที่ดรอป")]
        public int xpReward = 1;
        [Tooltip("โอกาสชุบชีวิตสำเร็จ (0.3 = 30%)")]
        [Range(0f, 1f)] public float raiseChance = 0.3f;

        [Header("Corpse Souls (ศพที่ชุบไม่ได้ -> พลังวิญญาณ)")]
        [Tooltip("Souls ที่ได้เมื่อคลิก / กด Q ที่ศพของศัตรูนี้ ตอนกองทัพชนิดนั้นเต็ม (ชุบไม่ได้)")]
        public int corpseHarvestSouls = 3;
        [Tooltip("Souls ที่ได้เมื่อศพของศัตรูนี้หมดเวลาหายไปเอง (0 = ไม่ได้)")]
        public int corpseExpireSouls = 1;

        [Header("Split On Death (Slime)")]
        [Tooltip("ตายแล้วแตกเป็นกี่ตัว (ชนิดที่แตก ตั้งใน Prefab: Split Prefab)")]
        public int splitCount = 2;
    }
}
