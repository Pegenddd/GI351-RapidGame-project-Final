using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// กล้องสั่น (ระเบิด / Giant ทุบพื้น / ฐานโดนตี) - CameraFollow เป็นคนนำค่าไปใช้
    /// เรียก CameraShake.Shake(แรง, เวลา) จากที่ไหนก็ได้ / ปิดทั้งเกมได้ที่ CameraShake.Enabled
    /// </summary>
    public static class CameraShake
    {
        public static bool Enabled = true;
        private static float _strength, _time, _duration, _seed;

        public static void Shake(float strength, float duration)
        {
            if (!Enabled) return;
            // แรงใหม่ไม่ทับแรงที่มากกว่าที่สั่นอยู่
            float remaining = _duration > 0f ? _strength * (_time / _duration) : 0f;
            if (strength < remaining) return;
            _strength = strength;
            _duration = _time = Mathf.Max(0.01f, duration);
            _seed = Random.value * 100f;
        }

        /// <summary>ค่าเลื่อนกล้องของเฟรมนี้ (ใช้เวลาเกม: เกมหยุด = ไม่สั่น)</summary>
        public static Vector2 Sample(float dt)
        {
            if (_time <= 0f || dt <= 0f) return Vector2.zero;
            _time -= dt;
            float k = Mathf.Clamp01(_time / _duration);
            float a = _strength * k * k;
            float t = Time.time * 35f + _seed;
            return new Vector2(Mathf.PerlinNoise(t, 0.3f) - 0.5f, Mathf.PerlinNoise(0.7f, t) - 0.5f) * 2f * a;
        }
    }
}
