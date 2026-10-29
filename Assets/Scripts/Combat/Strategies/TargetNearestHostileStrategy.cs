using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;

namespace NecroDefense.Combat.Strategies
{
    /// <summary>
    /// Strategy Pattern Implementation:
    /// Searches for the closest enemy of the opposite faction within a given radius.
    /// Used by Resurrected Minions and auto-attacking spells.
    /// </summary>
    public class TargetNearestHostileStrategy : ITargetingStrategy
    {
        public ITargetable FindTarget(Unit self, float searchRadius)
        {
            if (self == null) return null;

            Collider2D[] colliders = Physics2D.OverlapCircleAll(self.Position, searchRadius);
            ITargetable bestTarget = null;
            float minDistance = float.MaxValue;
            Faction opposingFaction = (self.Team == Faction.Player) ? Faction.Enemy : Faction.Player;

            foreach (var col in colliders)
            {
                if (col.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team == opposingFaction && targetable.IsValidTarget)
                    {
                        float dist = Vector2.Distance(self.Position, targetable.Position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestTarget = targetable;
                        }
                    }
                }
            }

            return bestTarget;
        }
    }
}

