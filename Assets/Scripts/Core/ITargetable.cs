using UnityEngine;

namespace NecroDefense.Core
{
    /// <summary>
    /// Abstraction for any entity that can be acquired as a target by AI or projectiles.
    /// Demonstrates Abstraction and Interface Segregation.
    /// </summary>
    public interface ITargetable
    {
        Transform TargetTransform { get; }
        Vector3 Position { get; }
        Faction Team { get; }
        bool IsValidTarget { get; }
    }
}

