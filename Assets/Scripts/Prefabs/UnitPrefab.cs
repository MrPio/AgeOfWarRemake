using System;
using System.Collections.Generic;
using Managers;
using Partials;
using Partials.Behaviour;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Prefabs
{
    /// <summary>
    /// Refers to the unit prefab instantiated inside the unit game object.
    /// </summary>
    public class UnitPrefab : MonoBehaviour
    {
        #region Constants

        private static readonly Vector2 BloodSpawnBounds = new(0.15f, 0.25f);
        private const float MinBloodDelay = 0.025f;

        #endregion

        [SerializeField] public float bulletSpeed = 3.5f;
        [SerializeField] public Transform hpBarPoint;
        [SerializeField] private Transform bloodSpawnPoint;
        [SerializeField] private GameObject bloodPrefab;
        [SerializeField] private Transform bulletSpawnPoint;
        [SerializeField] private GameObject bulletPrefab, shootEffectPrefab;
        [NonSerialized] public Unit Unit;
        private float _lastBlood;
        private SceneManager _sm;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            Unit = transform.parent.GetComponent<Unit>();
        }

        // Host & Client
        // We don't care if the blood is not synced.
        public void SpawnBlood()
        {
            if (Time.time - _lastBlood < MinBloodDelay) return;
            _lastBlood = Time.time;
            var spawnPoint = bloodSpawnPoint.position +
                             Vector3.right * Random.Range(-BloodSpawnBounds.x, BloodSpawnBounds.x) +
                             Vector3.up * Random.Range(-BloodSpawnBounds.y, BloodSpawnBounds.y);
            Instantiate(bloodPrefab, spawnPoint, Quaternion.identity);
        }

        // Host & Client
        // We don't care if the bullet is not synced. But the collision must be server-side.
        public void SpawnBullet(Transform target)
        {
            if (target is null || _sm.GameManager.IsGameOver) return;

            var bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
            var rb = bullet.GetComponent<Rigidbody>();
            var dir = Unit.IsLeft ? Vector3.right : Vector3.left;
            rb.linearVelocity = dir * bulletSpeed;
            var destroyable = bullet.GetComponent<Destroyable>();

            if (shootEffectPrefab != null)
                Instantiate(shootEffectPrefab, bulletSpawnPoint.transform);

            // destroyable.Target = target.gameObject;
            destroyable.AllowedTags = new List<string> { "Base", "Unit" };
            destroyable.TargetOwner = Unit.IsOwnedByServer ? _sm.GameManager.ClientId : _sm.GameManager.HostId;
            if (!_sm.IsMultiplayer && !Unit.IsBot.Value)
                destroyable.TargetOwner = 2;

            if (Unit.IsServer)
                destroyable.OnDamage = damageable =>
                    damageable.Damage(Unit.Model.Value.ShootDamage);
        }
    }
}