using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Combat;

namespace NecroDefense.Characters.Enemies
{
    /// <summary>
    /// Skeleton Archer Enemy: Ranged attacker that keeps distance.
    /// Demonstrates Polymorphic Ranged Execution.
    /// </summary>
    public class SkeletonArcherEnemy : EnemyUnit
    {
        [Header("Archer Settings")]
        [SerializeField] private GameObject arrowPrefab;

        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            if (arrowPrefab == null)
            {
                arrowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/Proj_BoneArrow.prefab");
            }
#endif
        }

        protected override void ExecuteAttack(ITargetable target)
        {
            Vector2 dir = (target.Position - Position).normalized;

            GameObject arrowObj;
            if (arrowPrefab != null)
            {
                arrowObj = Instantiate(arrowPrefab, Position, Quaternion.identity);
            }
            else
            {
                arrowObj = Utils.ProceduralVisuals.CreateProjectileGameObject("BoneArrow", new Color(0.9f, 0.9f, 0.8f));
                arrowObj.transform.position = Position;
            }

            if (arrowObj.TryGetComponent<Projectile>(out var proj))
            {
                proj.EnsureVisuals();
                proj.Initialize(dir, AttackPower, DamageType.Physical, Team, this);
            }
        }
    }
}

