using UnityEngine;

namespace NecroDefense
{
    public enum CardCategory { Player, Minion, Support }

    /// <summary>ผลของการ์ด (LevelUpManager.ApplyCard จะ switch ตามค่านี้)</summary>
    public enum CardEffect
    {
        NecromancyMastery, // value1 = +โอกาสชุบชีวิต (0.1 = +10%)
        TeleportMastery,   // value1 = ลดคูลดาวน์วาร์ป % (0.15)
        NovaBoost,         // value1 = +รัศมี Arcane Nova (E) %, value2 = +ดาเมจ %
        Removed_3,         // (เลิกใช้ - เดิม Shockwave Boost) เว้นไว้ไม่ให้เลขของการ์ดอื่นเลื่อน
        OrbitalMagic,      // value1 = +จำนวนลูก, value2 = +ดาเมจ %
        MinionPower,       // value1 = +ATK มินเนียน % (0.15)
        MinionHaste,       // value1 = +ความเร็วเดิน/ตี มินเนียน % (0.2)
        MinionVitality,    // value1 = +Max HP มินเนียน % (0.25)
        ManaFont,          // value1 = +Max Mana, value2 = +Mana Regen
        BaseArchitect,     // value1 = +Max HP คริสตัล, value2 = +เกราะคริสตัล
        SwiftBoots,        // value1 = +ความเร็วผู้เล่น % (0.1)
        SoulHarvester,     // value1 = ลดราคาป้อม % (0.15)
        Removed_12,        // (เลิกใช้ - เดิม Unlock Enigma)
        Removed_13,        // (เลิกใช้ - เดิม Unlock Shockwave)
        RaiseRadius,       // value1 = +รัศมีสกิล Q (Mass Raise) %
        RaiseCooldown,     // value1 = ลดคูลดาวน์สกิล Q %
        ArmySize           // value1 = +จำนวนกองทัพสูงสุด ทุกชนิดมินเนียน (+1)
    }

    /// <summary>
    /// การ์ดเลเวลอัป 1 ใบ (ScriptableObject) - สร้างใหม่ได้ที่ Project > Create > NecroDefense > Upgrade Card
    /// แล้วลากใส่ LevelUpManager > Card Pool
    /// </summary>
    [CreateAssetMenu(menuName = "NecroDefense/Upgrade Card", fileName = "Card_New")]
    public class UpgradeCard : ScriptableObject
    {
        public string title = "New Card";
        public CardCategory category = CardCategory.Player;
        [TextArea(2, 4)] public string description;
        public CardEffect effect;
        public float value1;
        public float value2;
        [Tooltip("เลือกได้สูงสุดกี่ครั้ง (0 = ไม่จำกัด)")]
        public int maxStacks = 5;
        [Tooltip("การ์ดนี้จะเริ่มสุ่มออกเมื่อผู้เล่นเลเวลเท่านี้ขึ้นไป")]
        public int minLevel = 1;
        [Tooltip("ต้องเคยเลือกการ์ดนี้มาก่อนถึงจะสุ่มออก (ว่าง = ไม่ต้อง)")]
        public UpgradeCard requires;
        public Color cardColor = Color.white;
    }
}
