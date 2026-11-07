using UnityEngine.InputSystem;

namespace NecroDefense
{
    public static class UIUtil
    {
        /// <summary>แปลงชื่อปุ่มให้อ่านง่าย (Digit1 -> 1)</summary>
        public static string KeyLabel(Key key)
        {
            if (key == Key.None) return "";
            string s = key.ToString();
            if (s.StartsWith("Digit")) return s.Substring(5);
            if (s.StartsWith("Numpad")) return "Num" + s.Substring(6);
            return s;
        }
    }
}
