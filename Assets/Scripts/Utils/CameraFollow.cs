using UnityEngine;
using NecroDefense.Characters.Player;

namespace NecroDefense.Utils
{
    /// <summary>
    /// Smooth camera follow script for the Necromancer player.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Camera Tracking (Editable in Inspector)")]
        [Tooltip("Target transform to follow (defaults to Player if null)")]
        [SerializeField] private Transform target;
        [Tooltip("Interpolation speed for smooth following")]
        [SerializeField] private float smoothSpeed = 5f;
        [Tooltip("Camera offset relative to target position")]
        [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

        private void Start()
        {
            if (target == null)
            {
                var player = FindAnyObjectByType<PlayerNecromancer>();
                if (player != null) target = player.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;
        }
    }
}

