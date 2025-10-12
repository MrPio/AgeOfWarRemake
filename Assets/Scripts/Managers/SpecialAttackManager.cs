using System;
using System.Collections;
using System.Collections.Generic;
using Model.Bases;
using Partials.Behaviour;
using Partials.Camera;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Managers
{
    public class SpecialAttackManager : NetworkBehaviour
    {
        private static SceneManager _sm;
        private float spawnXMargin = 2f, spawnY = 12f, spawnZ = 0f;

        [NonSerialized] public bool IsAttacking;
        private float _spawnX1, _spawnX2;

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            IsAttacking = false;
            _spawnX1 = -(_sm.fieldLenght / 2 - spawnXMargin);
            _spawnX2 = _sm.fieldLenght / 2 - spawnXMargin;
        }

        private Base GetBaseModel(ulong attackerId) => attackerId == NetworkManager.Singleton.LocalClientId
            ? _sm.GameManager.BaseAlly.Model.Value
            : _sm.GameManager.BaseEnemy.Model.Value;

        // Server-only
        [ServerRpc(RequireOwnership = false)]
        public void RainAttackServerRpc(ServerRpcParams rpcParams = default)
        {
            if (IsAttacking) return;
            IsAttacking = true;
            var attackerId = rpcParams.Receive.SenderClientId;
            var baseModel = GetBaseModel(attackerId);
            var model = baseModel.Special;

            InitializeSpecialAttackRpc(model);
            StartCoroutine(SpawnRandomBullet());
            return;

            IEnumerator SpawnRandomBullet()
            {
                var start = Time.time;
                while (start + model.Duration > Time.time)
                {
                    SpawnBulletRpc(attackerId);
                    yield return new WaitForSeconds(1 / model.Rate);
                }

                IsAttacking = false;
            }
        }

        // Host & Client
        [Rpc(SendTo.Everyone)]
        private void InitializeSpecialAttackRpc(SpecialAttack model)
        {
            _sm.cam.GetComponent<CameraShake>().Shake(model.Duration);
            // TODO eruption sound
        }


        // Host & Client
        [Rpc(SendTo.Everyone)]
        private void SpawnBulletRpc(ulong attackerId)
        {
            var baseModel = GetBaseModel(attackerId);
            var model = baseModel.Special;

            // Spawn bullet
            var spawnX = Random.Range(_spawnX1, _spawnX2);
            var bulletPrefab = Resources.Load<GameObject>(model.Prefab);
            var bullet = Instantiate(bulletPrefab, transform);
            bullet.transform.localPosition = new Vector3(spawnX, spawnY, spawnZ);

            // Add initial force
            var maxAngle = Mathf.Sin(model.MaxAngle * Mathf.Deg2Rad);
            var dx = Random.Range(-maxAngle, maxAngle);
            var rb = bullet.GetComponentInChildren<Rigidbody>();
            rb.AddForce((-bullet.transform.up + new Vector3(dx, 0, 0)) * model.Speed);

            // Add explodable behaviour
            var explodable = bullet.GetComponentInChildren<Explodable>();
            var explosionPrefab = Resources.Load<GameObject>(model.ExplosionPrefab);
            explodable.Initialize(
                targets: new List<string> { "Ground", "Unit", "Base" },
                range: model.Range,
                damage: model.Damage,
                attackerId: attackerId,
                explosion: explosionPrefab,
                onExplode: () => _sm.musicManager.PlaySpecial(baseModel.Level)
            );
        }
    }
}