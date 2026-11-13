using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// คลาสแม่ของ "ภาพเคลื่อนไหวของป้อม" (ใส่ในช่อง Visual ของ Tower)
    /// Tower จะบอกเหตุการณ์ต่างๆ มาที่นี่: เลเวลเปลี่ยน / กำลังก่อสร้าง / เริ่มโจมตี / ยิงออกไป
    /// ป้อมที่ไม่มี Visual ก็ยังทำงานแบบเดิม (ใช้ Body รูปเดียว)
    /// </summary>
    public abstract class TowerVisual : MonoBehaviour
    {
        /// <summary>เวลาตั้งท่าก่อนกระสุนออก (วินาที) เช่น อ้าปากก่อนยิง</summary>
        public virtual float FireDelay => 0f;

        /// <summary>เลเวลเปลี่ยน (0 = เพิ่งสร้าง, 1-3 = Tier)</summary>
        public virtual void OnLevelChanged(int level, bool isUpgrade) { }

        /// <summary>ความคืบหน้าการก่อสร้าง 0..1 (1 = เสร็จ)</summary>
        public virtual void OnConstruction(float progress) { }

        /// <summary>เริ่มโจมตี (ก่อนกระสุนออก FireDelay วินาที)</summary>
        public virtual void OnAttackStart(IReadOnlyList<Unit> targets) { }

        /// <summary>กระสุนลูกที่ shotIndex ออกไปแล้ว</summary>
        public virtual void OnShotFired(int shotIndex, Unit target) { }

        /// <summary>กระสุนเพิ่งเกิด (ใช้เปลี่ยนหน้าตากระสุน เช่น เป็นรูปสไลม์)</summary>
        public virtual void OnProjectileSpawned(int shotIndex, Projectile projectile) { }

        /// <summary>ตำแหน่งปล่อยกระสุนลูกที่ shotIndex (null = ใช้ FirePoint ของ Tower)</summary>
        public virtual Vector2? GetFirePosition(int shotIndex) => null;

        /// <summary>รูปไอคอนของเลเวลนี้ (null = ใช้ Icon ของ Tower)</summary>
        public virtual Sprite GetIcon(int level) => null;
    }
}
