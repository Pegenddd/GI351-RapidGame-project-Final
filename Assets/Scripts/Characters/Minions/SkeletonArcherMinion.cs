using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Combat;

namespace NecroDefense.Characters.Minions
{
    /// <summary>
    /// Resurrected Skeleton Archer: Undead Spectral Marksman.
    /// Acts as a defensive tower/sniper defending the Citadel from distance.
    /// Demonstrates Polymorphic Ranged Execution.
    /// </summary>
    public class SkeletonArcherMinion : MinionUnit
    {
        [Header("Spectral Marksman Settings")]
        [Tooltip("Prefab for spectral arrow (leave empty for procedural fallback)")]
        [SerializeField] private GameObject spectralArrowPrefab;

        protected override void Awake()
        {
            base.Awake();
#if UNITY_EDITOR
            if (spectralArrowPrefab == null)
            {
                spectralArrowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectiles/Proj_SpectralArrow.prefab");
            }
#endif
        }

        protected override void ExecuteAttack(ITargetable target)
        {
            Vector2 dir = (target.Position - Position).normalized;

            GameObject arrowObj;
            if (spectralArrowPrefab != null)
            {
                arrowObj = Instantiate(spectralArrowPrefab, Position, Quaternion.identity);
            }
            else
            {
                arrowObj = Utils.ProceduralVisuals.CreateProjectileGameObject("SpectralArrow", new Color(0.2f, 0.9f, 1f));
                arrowObj.transform.position = Position;
            }

            if (arrowObj.TryGetComponent<Projectile>(out var proj))
            {
                proj.EnsureVisuals();
                proj.Initialize(dir, AttackPower, DamageType.Necrotic, Team, this);
            }
        }
    }
}

