using System.Collections.Generic;
using UnityEngine;

namespace NecroDefense
{
    /// <summary>
    /// รายชื่อทุกอย่างที่อยู่ในฉากตอนเล่น (ยูนิตลงทะเบียนตัวเองใน OnEnable / ถอนตัวใน OnDisable)
    /// ใช้แทน FindObjectsOfType ให้เร็วและหาเป้าหมายได้ง่าย
    /// </summary>
    public static class Registry
    {
        public static readonly List<EnemyUnit> Enemies = new List<EnemyUnit>();
        public static readonly List<MinionUnit> Minions = new List<MinionUnit>();
        public static readonly List<Corpse> Corpses = new List<Corpse>();
        public static readonly List<TowerSlot> TowerSlots = new List<TowerSlot>();
        public static PlayerNecromancer Player;
        public static Crystal Crystal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Enemies.Clear();
            Minions.Clear();
            Corpses.Clear();
            TowerSlots.Clear();
            Player = null;
            Crystal = null;
        }

        /// <summary>ยูนิตนี้เป็นศัตรูกับทีม attackerTeam หรือไม่ และยังโจมตีได้</summary>
        public static bool IsValidHostile(Unit u, Team attackerTeam, bool canHitFlying)
        {
            if (u == null || !u.IsAlive || u.Team == attackerTeam) return false;
            if (u.IsFlying && !canHitFlying) return false;
            return true;
        }

        /// <summary>หา "ศัตรูของทีม attackerTeam" ที่ใกล้ที่สุดภายในระยะ (วัดถึงขอบตัว)</summary>
        public static Unit FindNearestHostile(Vector2 from, Team attackerTeam, float range, bool canHitFlying = true)
        {
            Unit best = null;
            float bestDist = float.MaxValue;

            if (attackerTeam == Team.Player)
            {
                for (int i = 0; i < Enemies.Count; i++)
                    Consider(Enemies[i], from, attackerTeam, range, canHitFlying, ref best, ref bestDist);
            }
            else
            {
                Consider(Player, from, attackerTeam, range, canHitFlying, ref best, ref bestDist);
                Consider(Crystal, from, attackerTeam, range, canHitFlying, ref best, ref bestDist);
                for (int i = 0; i < Minions.Count; i++)
                    Consider(Minions[i], from, attackerTeam, range, canHitFlying, ref best, ref bestDist);
            }
            return best;
        }

        private static void Consider(Unit u, Vector2 from, Team team, float range, bool hitFlying, ref Unit best, ref float bestDist)
        {
            if (!IsValidHostile(u, team, hitFlying)) return;
            float d = Vector2.Distance(from, u.Position) - u.BodyRadius;
            if (d <= range && d < bestDist)
            {
                bestDist = d;
                best = u;
            }
        }

        /// <summary>เก็บศัตรูทั้งหมดของ attackerTeam ที่อยู่ในวงกลม ใส่ลง results</summary>
        public static void GetHostilesInRadius(Vector2 center, float radius, Team attackerTeam, bool canHitFlying, List<Unit> results)
        {
            results.Clear();
            if (attackerTeam == Team.Player)
            {
                for (int i = 0; i < Enemies.Count; i++)
                    AddIfInside(Enemies[i], center, radius, attackerTeam, canHitFlying, results);
            }
            else
            {
                AddIfInside(Player, center, radius, attackerTeam, canHitFlying, results);
                AddIfInside(Crystal, center, radius, attackerTeam, canHitFlying, results);
                for (int i = 0; i < Minions.Count; i++)
                    AddIfInside(Minions[i], center, radius, attackerTeam, canHitFlying, results);
            }
        }

        private static void AddIfInside(Unit u, Vector2 center, float radius, Team team, bool hitFlying, List<Unit> results)
        {
            if (!IsValidHostile(u, team, hitFlying)) return;
            if (Vector2.Distance(center, u.Position) - u.BodyRadius <= radius) results.Add(u);
        }
    }
}
