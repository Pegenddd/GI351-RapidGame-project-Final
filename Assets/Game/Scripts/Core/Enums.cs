namespace NecroDefense
{
    /// <summary>ฝั่งของยูนิต (ฝ่ายเรา = Player, Minion, Crystal / ฝ่ายศัตรู = Enemy)</summary>
    public enum Team { Player, Enemy }

    /// <summary>สถานะของเกม (GameManager เป็นคนเปลี่ยน)</summary>
    public enum GameState { Playing, Paused, LevelUp, Victory, Defeat }

    /// <summary>
    /// คำสั่งมินเนียน - ใช้จริง 3 แบบ: SelfDefense (= Hold Area ปกป้องบริเวณนั้น, ค่าเริ่มต้น), MoveTo, FollowPlayer
    /// GuardBase / AttackNearest เลิกใช้ (เก็บไว้ไม่ให้เลขเลื่อน) ถ้าถูกตั้งจะถูกแปลงเป็น SelfDefense
    /// </summary>
    public enum MinionCommand { GuardBase, SelfDefense, FollowPlayer, AttackNearest, MoveTo }

    /// <summary>รูปแบบการโจมตีของยูนิต (ศัตรู/มินเนียน)</summary>
    public enum AttackStyle
    {
        Melee,      // ตีเป้าหมายตัวเดียว
        MeleeArea,  // ทุบพื้นรอบตัว (Giant)
        Ranged      // ยิง Projectile (Skeleton Archer)
    }

    /// <summary>เป้าหมายหลักของศัตรู (ตั้งใน Prefab)</summary>
    public enum TargetPriority
    {
        Nearest,   // อะไรก็ได้ที่ใกล้สุด (ในบรรดาที่ติ๊กให้ตีได้)
        Player,    // เล็ง Necromancer ก่อน
        Minions,   // เล็งมินเนียนก่อน
        MainBase   // พุ่งไปป้อมหลัก
    }

    /// <summary>จุดเกิดศัตรู</summary>
    public enum SpawnSide { Random, North, East, South, West, AllSides }

    /// <summary>โฟลเดอร์ใน Hierarchy (ใต้ "Runtime") ที่ของที่เกิดระหว่างเล่นจะไปอยู่</summary>
    public enum SpawnFolder { Enemies, Minions, Projectiles, Effects, Pickups }
}
