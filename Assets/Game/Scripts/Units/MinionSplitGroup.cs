namespace NecroDefense
{
    /// <summary>กลุ่มมินเนียนตัวเล็กที่แตกออกมาจากตัวใหญ่ 1 ตัว (ตายครบทุกตัว = วิญญาณตัวใหญ่กลับเข้ากระเป๋า)</summary>
    public class MinionSplitGroup
    {
        /// <summary>Prefab ตัวใหญ่ (ที่จะกลับเข้ากระเป๋า)</summary>
        public MinionUnit prefab;
        /// <summary>ตัวเล็กที่ยังไม่ตาย</summary>
        public int alive;
        public bool done;
    }
}
