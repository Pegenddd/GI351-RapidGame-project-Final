using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Base;
using NecroDefense.Base;

namespace NecroDefense.Combat.Strategies
{
    /// <summary>
    /// Strategy Pattern Implementation:
    /// Minions prioritizing enemies that are threatening the Base Crystal perimeter.
    /// </summary>
    public class GuardBasePerimeterStrategy : ITargetingStrategy
    {
        private BaseCrystal _cachedBase;

        public ITargetable FindTarget(Unit self, float searchRadius)
        {
            if (_cachedBase == null || !_cachedBase.IsValidTarget)
            {
                _cachedBase = Object.FindAnyObjectByType<BaseCrystal>();
            }

            Vector3 center = (_cachedBase != null) ? _cachedBase.Position : self.Position;
            Collider2D[] threats = Physics2D.OverlapCircleAll(center, searchRadius);

            ITargetable nearestThreat = null;
            float shortestDist = float.MaxValue;

            foreach (var col in threats)
            {
                if (col.TryGetComponent<ITargetable>(out var targetable))
                {
                    if (targetable.Team == Faction.Enemy && targetable.IsValidTarget)
                    {
                        float d = Vector2.Distance(center, targetable.Position);
                        if (d < shortestDist)
                        {
                            shortestDist = d;
                            nearestThreat = targetable;
                        }
                    }
                }
            }

            return nearestThreat;
        }
    }
}

