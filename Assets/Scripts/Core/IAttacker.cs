namespace NecroDefense.Core
{
    /// <summary>
    /// Contract for entities capable of engaging in combat.
    /// Encapsulates offensive capabilities.
    /// </summary>
    public interface IAttacker
    {
        float AttackPower { get; }
        float AttackRange { get; }
        float AttackInterval { get; }
        bool CanAttack { get; }

        void Attack(ITargetable target);
    }
}

