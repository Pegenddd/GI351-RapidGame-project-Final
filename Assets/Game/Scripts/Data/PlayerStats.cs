using UnityEngine;

namespace NecroDefense
{
    /// <summary>ค่าของผู้เล่น (Necromancer) - เลือด / เดิน / ยิงอัตโนมัติ / สกิล</summary>
    [CreateAssetMenu(menuName = "NecroDefense/Player Stats", fileName = "Player_Stats")]
    public class PlayerStats : ScriptableObject
    {
        [Header("Health & Movement")]
        public float maxHealth = 100f;
        public float armor = 0f;
        [Tooltip("ความเร็วเดิน")]
        public float moveSpeed = 4.5f;

        [Header("Auto Attack - Magic Bolt")]
        public float boltDamage = 12f;
        [Tooltip("วินาทีต่อการยิง 1 ครั้ง (ใช้เมื่อปิด Orbs Fire As Bolts)")]
        public float attackInterval = 0.6f;
        public float attackRange = 7f;

        [Header("Auto Attack - Orbit Orbs (หมุนรอบตัว / พุ่งออกไปเป็นกระสุน)")]
        public int orbCount = 2;
        public float orbRadius = 1.4f;
        [Tooltip("องศา/วินาที")]
        public float orbRotateSpeed = 220f;
        public float orbDamage = 6f;
        public float orbHitRadius = 0.35f;
        [Tooltip("ตีตัวเดิมซ้ำได้ทุกกี่วินาที")]
        public float orbHitCooldown = 0.5f;
        [Tooltip("มีศัตรูในระยะ: ลูกบอลที่หมุนอยู่พุ่งออกไปเป็นกระสุนทีละลูก")]
        public bool orbsFireAsBolts = true;
        [Tooltip("ลูกแต่ละลูกที่ยิงไปแล้ว ใช้เวลากี่วินาทีเกิดใหม่ (4 ลูก = ยิงได้ 4 นัดต่อรอบ)")]
        public float orbRechargeTime = 1.2f;
        [Tooltip("ระยะห่างระหว่างลูกที่พุ่งออกไปติดกัน (วินาที)")]
        public float orbFireGap = 0.15f;

        [Header("Teleport (คลิกขวา)")]
        public float teleportCooldown = 10f;
        [Tooltip("0 = ไม่จำกัดระยะ")]
        public float teleportMaxDistance = 0f;

        [Header("Raise Dead")]
        [Tooltip("โอกาสชุบเพิ่ม (0.1 = +10%)")]
        public float raiseChanceBonus = 0f;

        [Header("Skill Q - Mass Raise")]
        public float massRaiseCooldown = 10f;
        public float massRaiseRadius = 5f;
        public float massRaiseCastTime = 0.6f;

        [Header("Skill E - Arcane Nova")]
        public float novaCooldown = 8f;
        public float novaCastTime = 0.8f;
        public float novaRadius = 3.5f;
        public float novaDamage = 45f;
        public bool novaHitsFlying = true;
    }
}
