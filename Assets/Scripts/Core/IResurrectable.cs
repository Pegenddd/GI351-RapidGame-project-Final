using UnityEngine;

namespace NecroDefense.Core
{
    /// <summary>
    /// Contract for corpses or spirits that can be raised from the dead by a Necromancer.
    /// Crucial for Polymorphic Minion Creation.
    /// </summary>
    public interface IResurrectable
    {
        EnemyType SourceType { get; }
        int SoulCost { get; }
        bool CanBeResurrected { get; }
        Vector3 Position { get; }
        
        /// <summary>
        /// Polymorphically converts this fallen soul into an allied minion.
        /// </summary>
        void OnResurrected();
    }
}

