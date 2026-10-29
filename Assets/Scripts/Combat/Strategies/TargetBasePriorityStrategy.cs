using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;
using NecroDefense.Base;

namespace NecroDefense.Combat.Strategies
{
    /// <summary>
    /// Strategy Pattern Implementation:
    /// Targets the player's Base Crystal first. If the player gets dangerously close, switches to the player.
    /// Used by standard wave enemies.
    /// </summary>
    public class TargetBasePriorityStrategy : ITargetingStrategy
    {
        private readonly float _playerAggroRadius;
        private BaseCrystal _cachedBase;

        public TargetBasePriorityStrategy(float playerAggroRadius = 3.5f)
        {
            _playerAggroRadius = playerAggroRadius;
        }

        public ITargetable FindTarget(Unit self, float searchRadius)
        {
            if (self == null) return null;

            // 1. Check if Player or nearby Minion is within close aggro range
            Collider2D[] nearby = Physics2D.OverlapCircleAll(self.Position, _playerAggroRadius);
            ITargetable closestThreat = null;
            float closestDist = float.MaxValue;

            foreach (var col in nearby)
            {
                if (col.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team == Faction.Player && targetable.IsValidTarget)
                    {
                        float d = Vector2.Distance(self.Position, targetable.Position);
                        if (d < closestDist)
                        {
                            closestDist = d;
                            closestThreat = targetable;
                        }
                    }
                }
            }

            if (closestThreat != null)
            {
                return closestThreat;
            }

            // 2. Otherwise march directly toward the Base Crystal
            if (_cachedBase == null || !_cachedBase.IsValidTarget)
            {
                _cachedBase = Object.FindAnyObjectByType<BaseCrystal>();
            }

            return _cachedBase;
        }
    }
}

