using UnityEngine;

namespace NecroDefense
{
    // ============================================================================
    // ไฟล์ข้อมูล (Data) ของยูนิต / ผู้เล่น / ป้อม - ปรับบาลานซ์ที่นี่ที่เดียว
    // - สร้างไฟล์: Project > คลิกขวา > Create > NecroDefense > ...
    //   หรือเมนู Tools > NecroDefense > Data > Create & Link Stats From Prefabs (ย้ายค่าเดิมจาก Prefab ให้อัตโนมัติ)
    // - ลากไฟล์ใส่ช่อง "Stats" ของ Prefab -> เกมจะใช้ค่าจากไฟล์นี้แทนค่าใน Prefab
    // - ดู/แก้ทุกตัวเป็นตาราง: Tools > NecroDefense > Data > Balance Table
    // ชื่อตัวแปรตั้งให้ตรงกับใน Prefab (ใช้ย้ายค่าอัตโนมัติ) - อย่าเปลี่ยนชื่อ
    // ============================================================================

    /// <summary>ค่าพื้นฐานของยูนิตที่สู้ได้ (ศัตรู / มินเนียน)</summary>
    public abstract class UnitStats : ScriptableObject
    {
        [Header("Info")]
        [Tooltip("ชื่อที่โชว์ในเกม")]
        public string displayName = "Unit";

        [Header("Health")]
        [Tooltip("เลือดสูงสุด")]
        public float maxHealth = 100f;
        [Tooltip("เกราะ: ลดดาเมจต่อครั้งแบบตรงๆ (ขั้นต่ำโดน 1)")]
        public float armor = 0f;
        [Tooltip("บินได้ไหม (ป้อม Explosive ยิงตัวบินไม่ได้)")]
        public bool isFlying = false;

        [Header("Movement")]
        [Tooltip("ความเร็วเดิน (หน่วย/วินาที)")]
        public float moveSpeed = 2f;

        [Header("Attack")]
        public AttackStyle attackStyle = AttackStyle.Melee;
        [Tooltip("ดาเมจต่อการโจมตี 1 ครั้ง")]
        public float attackDamage = 5f;
        [Tooltip("ระยะโจมตี (วัดจากขอบตัวถึงขอบตัวเป้าหมาย)")]
        public float attackRange = 0.5f;
        [Tooltip("วินาทีต่อการโจมตี 1 ครั้ง")]
        public float attackInterval = 1f;
        [Tooltip("รัศมีทุบพื้น (ใช้เมื่อ Attack Style = MeleeArea)")]
        public float areaRadius = 1.5f;
        [Tooltip("ตีตัวบินได้ไหม")]
        public bool canAttackFlying = true;
    }
}
