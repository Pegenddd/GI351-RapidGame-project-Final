using UnityEngine;

namespace NecroDefense
{
    /// <summary>ลูกแก้ว XP ที่ศัตรูดรอป เดินเข้าใกล้แล้วจะถูกดูดเข้าหาผู้เล่น</summary>
    public class XpOrb : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [Tooltip("ระยะดูดเข้าหาผู้เล่น")]
        [SerializeField] private float magnetRadius = 2.5f;
        [Tooltip("ระยะที่ถือว่าเก็บแล้ว")]
        [SerializeField] private float pickupRadius = 0.4f;
        [SerializeField] private float flySpeed = 9f;
        [Tooltip("อยู่ได้กี่วินาที (0 = ตลอดไป)")]
        [SerializeField] private float lifetime = 40f;

        private bool _magnetized;
        private float _age;

        public void SetValue(int xp) => value = xp;

        private void Update()
        {
            if (!GameManager.IsGameplayRunning) return;
            _age += Time.deltaTime;
            if (lifetime > 0f && _age > lifetime) { Destroy(gameObject); return; }

            PlayerNecromancer player = Registry.Player;
            if (player == null || !player.IsAlive) return;

            Vector2 pos = transform.position;
            float dist = Vector2.Distance(pos, player.Position);
            if (dist <= magnetRadius) _magnetized = true;

            if (_magnetized)
                transform.position = Vector2.MoveTowards(pos, player.Position, flySpeed * Time.deltaTime);

            if (dist <= pickupRadius)
            {
                if (GameManager.Instance != null) GameManager.Instance.LevelUp.AddXp(value);
                Destroy(gameObject);
            }
        }
    }
}
