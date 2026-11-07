using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// เตือนก่อนเริ่มเวฟว่าศัตรูจะมาทางไหน (แบบธงเวฟใน Kingdom Rush)
    /// - เปิดป้ายเตือนที่ปากทางมอน (EnemyPath > Warning Marker)
    /// - โชว์ไอคอนบนจอ ถ้าทางนั้นอยู่นอกจอจะติดขอบจอพร้อมลูกศรชี้
    /// - คลิกไอคอน = เรียกเวฟทันที
    /// </summary>
    public class WaveWarningUI : MonoBehaviour
    {
        [SerializeField] private List<WaveWarningIndicator> indicators = new List<WaveWarningIndicator>();
        [Tooltip("ระยะห่างจากขอบจอ (หน่วย UI)")]
        [SerializeField] private float screenMargin = 70f;
        [Tooltip("ยกไอคอนขึ้นจากป้ายในฉาก (หน่วย UI) ตอนอยู่ในจอ")]
        [SerializeField] private float onScreenLift = 70f;

        private readonly HashSet<EnemyPath> _warned = new HashSet<EnemyPath>();
        private readonly HashSet<EnemyPath> _now = new HashSet<EnemyPath>();

        private void Awake()
        {
            foreach (WaveWarningIndicator ind in indicators)
                if (ind != null) ind.Bind(() => { if (GameManager.Instance != null) GameManager.Instance.Waves.StartNextWave(); });
        }

        private void LateUpdate()
        {
            GameManager gm = GameManager.Instance;
            _now.Clear();
            IReadOnlyList<PathPreview> list = null;
            bool show = gm != null && gm.Waves.IsCountingDown && gm.State == GameState.Playing;
            if (show) list = gm.Waves.UpcomingPreview;

            Camera cam = Camera.main;
            float scale = transform.lossyScale.x;
            int used = 0;
            if (list != null && cam != null)
            {
                foreach (PathPreview pv in list)
                {
                    if (pv.path == null || used >= indicators.Count) continue;
                    _now.Add(pv.path);
                    pv.path.SetWarning(true);
                    Place(indicators[used], pv, cam, scale, gm.Waves.Countdown);
                    used++;
                }
            }
            for (int i = used; i < indicators.Count; i++) if (indicators[i] != null) indicators[i].Hide();

            foreach (EnemyPath p in _warned) if (p != null && !_now.Contains(p)) p.SetWarning(false);
            _warned.Clear();
            foreach (EnemyPath p in _now) _warned.Add(p);
        }

        private void Place(WaveWarningIndicator ind, PathPreview pv, Camera cam, float scale, float countdown)
        {
            if (ind == null) return;
            Vector3 sp = cam.WorldToScreenPoint(pv.path.WarningPosition);
            float m = screenMargin * scale;
            bool onScreen = sp.z > 0f && sp.x >= m && sp.x <= Screen.width - m && sp.y >= m && sp.y <= Screen.height - m;

            Vector2 pos;
            float arrowAngle;
            if (onScreen)
            {
                pos = new Vector2(sp.x, sp.y + onScreenLift * scale);
                arrowAngle = -90f; // ชี้ลงไปที่ป้าย
            }
            else
            {
                Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                Vector2 dir = (Vector2)sp - center;
                if (sp.z < 0f) dir = -dir;
                float hx = Screen.width * 0.5f - m, hy = Screen.height * 0.5f - m;
                float t = Mathf.Min(hx / Mathf.Max(0.001f, Mathf.Abs(dir.x)), hy / Mathf.Max(0.001f, Mathf.Abs(dir.y)));
                pos = center + dir * t;
                arrowAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("Next wave in ").Append(Mathf.CeilToInt(countdown)).Append("s\n");
            foreach (var kv in pv.counts) sb.Append(kv.Key.DisplayName).Append(" x").Append(kv.Value).Append('\n');
            sb.Append("Click to call the wave now");
            ind.Show(pos, arrowAngle, pv, sb.ToString());
        }
    }
}
