using UnityEngine;
using NecroDefense.Core;
using NecroDefense.Characters.Enemies;

namespace NecroDefense.Factories
{
    /// <summary>
    /// Factory Pattern Implementation:
    /// Encapsulates enemy monster generation and parameter scaling across waves.
    /// </summary>
    public class EnemyFactory : MonoBehaviour
    {
        public static EnemyFactory Instance { get; private set; }

        [Header("Enemy Prefabs (Optional - Fallbacks available)")]
        [SerializeField] private GameObject zombiePrefab;
        [SerializeField] private GameObject archerPrefab;
        [SerializeField] private GameObject golemPrefab;
        [SerializeField] private GameObject ghostPrefab;

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
            if (zombiePrefab == null)
                zombiePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Zombie.prefab");
            if (archerPrefab == null)
                archerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_SkeletonArcher.prefab");
            if (golemPrefab == null)
                golemPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Golem.prefab");
            if (ghostPrefab == null)
                ghostPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemies/Enemy_Ghost.prefab");
#endif
        }

        public EnemyUnit CreateEnemy(EnemyType type, Vector3 position, float healthMult = 1f, float damageMult = 1f)
        {
            GameObject prefab = GetPrefabForType(type);
            GameObject enemyObj;

            if (prefab != null)
            {
                enemyObj = Instantiate(prefab, position, Quaternion.identity);
            }
            else
            {
                enemyObj = Utils.ProceduralVisuals.CreateEnemyGameObject(type);
                enemyObj.transform.position = position;
            }

            EnemyUnit enemy = enemyObj.GetComponent<EnemyUnit>();
            if (enemy == null)
            {
                enemy = AttachEnemyComponent(enemyObj, type);
            }

            // Apply master configuration from GameManager if enabled
            if (Managers.GameManager.Instance != null && Managers.GameManager.Instance.OverrideAllSettings)
            {
                Managers.GameManager.Instance.ApplyEnemyStats(enemy, type);
            }

            // Scale stats polymorphically
            if (healthMult > 1f)
            {
                enemy.SetMaxHealth(enemy.MaxHealth * healthMult);
            }
            if (damageMult > 1f)
            {
                enemy.SetAttackPower(enemy.AttackPower * damageMult);
            }

            return enemy;
        }

        private GameObject GetPrefabForType(EnemyType type)
        {
            EnsurePrefabsLoaded();
            switch (type)
            {
                case EnemyType.Zombie: return zombiePrefab;
                case EnemyType.SkeletonArcher: return archerPrefab;
                case EnemyType.Golem: return golemPrefab;
                case EnemyType.Ghost: return ghostPrefab;
                default: return zombiePrefab;
            }
        }

        private EnemyUnit AttachEnemyComponent(GameObject obj, EnemyType type)
        {
            switch (type)
            {
                case EnemyType.Zombie:
                    return obj.AddComponent<ZombieEnemy>();
                case EnemyType.SkeletonArcher:
                    return obj.AddComponent<SkeletonArcherEnemy>();
                case EnemyType.Golem:
                    return obj.AddComponent<GolemEnemy>();
                case EnemyType.Ghost:
                    return obj.AddComponent<GhostEnemy>();
                default:
                    return obj.AddComponent<ZombieEnemy>();
            }
        }
    }
}

