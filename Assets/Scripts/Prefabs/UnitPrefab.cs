using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Prefabs
{
    /// <summary>
    /// Refers to the unit prefab instantiated inside the unit game object.
    /// </summary>
    public class UnitPrefab : MonoBehaviour
    {
        private static readonly Vector2 BloodSpawnBounds = new(0.15f, 0.25f);
        private static readonly float minBloodDelay = 1;
        [SerializeField] public Transform hpBarPoint;
        [SerializeField] private Transform bloodSpawnPoint;
        [SerializeField] private GameObject bloodPrefab;
        [NonSerialized] public Unit Unit;
        private float _lastBlood;

        private void Awake()
        {
            Unit = transform.parent.GetComponent<Unit>();
        }

        public void SpawnBlood()
        {
            if (Time.time - _lastBlood < minBloodDelay) return;
            _lastBlood = Time.time;
            var spawnPoint = bloodSpawnPoint.position +
                             Vector3.right * Random.Range(-BloodSpawnBounds.x, BloodSpawnBounds.x) +
                             Vector3.up * Random.Range(-BloodSpawnBounds.y, BloodSpawnBounds.y);
            var go = Instantiate(bloodPrefab, transform);
            go.transform.position = spawnPoint;
        }
    }
}