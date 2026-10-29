using UnityEngine;
using NecroDefense.Base;
using NecroDefense.Characters.Player;
using NecroDefense.Factories;
using NecroDefense.UI;
using NecroDefense.Utils;

namespace NecroDefense.Managers
{
    /// <summary>
    /// Automatic Scene Initializer & Bootstrap.
    /// Ensures that when Play mode is pressed in Unity, all essential systems
    /// (Necromancer Player, Citadel Base, Factories, Spawners, HUD, Camera)
    /// are initialized and hooked up without needing tedious manual hierarchy setup.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameSceneSetup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitializeScene()
        {
            // If the developer already has objects placed in the Hierarchy, do not auto-generate anything!
            if (FindAnyObjectByType<BaseCrystal>() != null || 
                FindAnyObjectByType<PlayerNecromancer>() != null ||
                FindAnyObjectByType<GameManager>() != null)
            {
                return;
            }

            if (FindAnyObjectByType<GameSceneSetup>() == null)
            {
                var setupObj = new GameObject("[AUTO_GAME_SETUP]");
                setupObj.AddComponent<GameSceneSetup>();
            }
        }

        private void Awake()
        {
            SetupCamera();
            SetupCitadelBase();
            SetupPlayerNecromancer();
            SetupFactories();
            SetupManagers();
            SetupHUD();
        }

        private void SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = new GameObject("Main Camera");
                cam = camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.backgroundColor = Color.black; // 2-Color Pure Black
            cam.clearFlags = CameraClearFlags.SolidColor;

            if (cam.GetComponent<CameraFollow>() == null)
            {
                cam.gameObject.AddComponent<CameraFollow>();
            }
        }

        private void SetupCitadelBase()
        {
            var baseCrystal = FindAnyObjectByType<BaseCrystal>();
            if (baseCrystal == null)
            {
                var baseObj = new GameObject("Citadel_BaseCore");
                baseObj.transform.position = Vector3.zero;

                var sr = baseObj.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralVisuals.GetCitadelSprite();
                sr.color = Color.white;
                sr.sortingOrder = 3;

                var col = baseObj.AddComponent<CircleCollider2D>();
                col.radius = 1.2f;

                baseCrystal = baseObj.AddComponent<BaseCrystal>();
            }
        }

        private void SetupPlayerNecromancer()
        {
            var player = FindAnyObjectByType<PlayerNecromancer>();
            if (player == null)
            {
                var playerObj = new GameObject("Player_Necromancer");
                playerObj.transform.position = new Vector3(0, -2.5f, 0);

                var sr = playerObj.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralVisuals.GetNecromancerSprite();
                sr.color = Color.white;
                sr.sortingOrder = 7;

                var col = playerObj.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;

                var rb = playerObj.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.freezeRotation = true;

                player = playerObj.AddComponent<PlayerNecromancer>();
            }
        }

        private void SetupFactories()
        {
            if (MinionFactory.Instance == null && FindAnyObjectByType<MinionFactory>() == null)
            {
                var minionFactObj = new GameObject("MinionFactory");
                minionFactObj.AddComponent<MinionFactory>();
            }

            if (EnemyFactory.Instance == null && FindAnyObjectByType<EnemyFactory>() == null)
            {
                var enemyFactObj = new GameObject("EnemyFactory");
                enemyFactObj.AddComponent<EnemyFactory>();
            }
        }

        private void SetupManagers()
        {
            if (GameManager.Instance == null && FindAnyObjectByType<GameManager>() == null)
            {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }

            if (FindAnyObjectByType<WaveSpawner>() == null)
            {
                var spawnerObj = new GameObject("WaveSpawner");
                spawnerObj.AddComponent<WaveSpawner>();
            }
        }

        private void SetupHUD()
        {
            if (FindAnyObjectByType<GameHUD>() == null)
            {
                var hudObj = new GameObject("GameHUD");
                hudObj.AddComponent<GameHUD>();
            }
        }
    }
}
