using System;
using UnityEngine;

namespace NecroDefense.Characters.Base
{
    /// <summary>
    /// Abstract base class for all game objects in the arena.
    /// Demonstrates OOP Inheritance and Common State Encapsulation.
    /// </summary>
    public abstract class Entity : MonoBehaviour
    {
        [Header("Entity Base Settings")]
        [SerializeField] private string entityName = "Entity";

        private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform != null ? _cachedTransform : (_cachedTransform = transform);
        public string EntityName => entityName;
        public Vector3 Position => CachedTransform.position;

        protected virtual void Awake()
        {
            _cachedTransform = transform;
        }

        protected virtual void OnEnable() { }
        protected virtual void OnDisable() { }
    }
}

