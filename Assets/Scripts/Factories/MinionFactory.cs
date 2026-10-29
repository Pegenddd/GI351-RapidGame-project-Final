using System;
using System.Collections.Generic;
using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Minions;

namespace NecroDefense.Factories
{
    /// <summary>
    /// Factory Pattern Implementation:
    /// Encapsulates the instantiation and configuration of resurrected minions.
    /// Maps enemy corpse types to their corresponding allied minion classes polymorphically.
    /// </summary>
    public class MinionFactory : MonoBehaviour
    {
        public static MinionFactory Instance { get; private set; }

        [Header("Minion Prefabs (Optional - Fallbacks available)")]
        [SerializeField] private GameObject zombieMinionPrefab;
        [SerializeField] private GameObject archerMinionPrefab;
        [SerializeField] private GameObject golemMinionPrefab;
        [SerializeField] private GameObject ghostMinionPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsurePrefabsLoaded();
        }

        public void EnsurePrefabsLoaded()
        {
#if UNITY_EDITOR
            if (zombieMinionPrefab == null)
                zombieMinionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minions/Minion_Zombie.prefab");
            if (archerMinionPrefab == null)
                archerMinionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minions/Minion_SkeletonArcher.prefab");
            if (golemMinionPrefab == null)
                golemMinionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minions/Minion_Golem.prefab");
            if (ghostMinionPrefab == null)
                ghostMinionPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Minions/Minion_Ghost.prefab");
#endif
        }

        /// <summary>
        /// Creates a MinionUnit from a fallen enemy corpse based on its SourceType.
        /// Demonstrates Polymorphic Factory Creation.
        /// </summary>
        public MinionUnit CreateMinionFromCorpse(IResurrectable corpse)
        {
            if (corpse == null) return null;

            return CreateMinion(corpse.SourceType, corpse.Position);
        }

        /// <summary>
        /// Instantiates and initializes a specific minion type at the given position.
        /// </summary>
        public MinionUnit CreateMinion(EnemyType type, Vector3 spawnPosition)
        {
            GameObject prefab = GetPrefabForType(type);
            GameObject minionObj;

            if (prefab != null)
            {
                minionObj = Instantiate(prefab, spawnPosition, Quaternion.identity);
            }
            else
            {
                // Fallback procedural creation
                minionObj = Utils.ProceduralVisuals.CreateMinionGameObject(type);
                minionObj.transform.position = spawnPosition;
            }

            MinionUnit minion = minionObj.GetComponent<MinionUnit>();
            if (minion == null)
            {
                // Attach component polymorphically if not present
                minion = AttachMinionComponent(minionObj, type);
            }

            // Apply master configuration from GameManager if enabled
            if (Managers.GameManager.Instance != null && Managers.GameManager.Instance.OverrideAllSettings)
            {
                Managers.GameManager.Instance.ApplyMinionStats(minion, type);
            }

            return minion;
        }

        private GameObject GetPrefabForType(EnemyType type)
        {
            EnsurePrefabsLoaded();
            switch (type)
            {
                case EnemyType.Zombie: return zombieMinionPrefab;
                case EnemyType.SkeletonArcher: return archerMinionPrefab;
                case EnemyType.Golem: return golemMinionPrefab;
                case EnemyType.Ghost: return ghostMinionPrefab;
                default: return zombieMinionPrefab;
            }
        }

        private MinionUnit AttachMinionComponent(GameObject obj, EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Zombie:
                    return obj.AddComponent<ZombieMinion>();
                case EnemyType.SkeletonArcher:
                    return obj.AddComponent<SkeletonArcherMinion>();
                case EnemyType.Golem:
                    return obj.AddComponent<GolemMinion>();
                case EnemyType.Ghost:
                    return obj.AddComponent<GhostMinion>();
                default:
                    return obj.AddComponent<ZombieMinion>();
            }
        }
    }
}

