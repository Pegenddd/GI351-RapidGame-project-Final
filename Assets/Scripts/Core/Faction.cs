namespace NecroDefense.Core
{
    /// <summary>
    /// Faction identifier to distinguish friends from foes.
    /// Used by targeting systems and damage calculations.
    /// </summary>
    public enum Faction
    {
        Player,   // Player and resurrected minions
        Enemy,    // Attacking monsters
        Neutral   // Props, neutral obstacles, corpses
    }

    /// <summary>
    /// Types of enemies and their corresponding resurrected minions.
    /// </summary>
    public enum EnemyType
    {
        Zombie,          // Melee brawler / swarm
        SkeletonArcher,  // Ranged shooter
        Golem,           // Heavy tank
        Ghost            // Fast phantasm / debuffer
    }

    /// <summary>
    /// Elemental or behavioral damage categories.
    /// Demonstrates polymorphism in damage mitigation.
    /// </summary>
    public enum DamageType
    {
        Physical,
        Necrotic,
        Void,
        Poison
    }
}

