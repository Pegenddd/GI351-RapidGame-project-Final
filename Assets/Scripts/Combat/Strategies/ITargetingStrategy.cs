using NecroDefense.Core;
using NecroDefense.Characters.Base;

namespace NecroDefense.Combat.Strategies
{
    /// <summary>
    /// Strategy Pattern Interface for AI Target Selection.
    /// Allows changing targeting behavior dynamically at runtime without modifying Unit classes.
    /// </summary>
    public interface ITargetingStrategy
    {
        ITargetable FindTarget(Unit self, float searchRadius);
    }
}

