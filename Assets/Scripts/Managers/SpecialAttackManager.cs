using System;
using System.Collections;
using System.Collections.Generic;
using Partials.Behaviour;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Managers
{
    public class SpecialAttackManager : NetworkBehaviour
    {
        [SerializeField] private float spawnXMargin = 1f,
            spawnY = 10f,
            spawnZ = 0f,
            maxAngle = 0.5f,
            duration = 15,
            rate = 5f,
            speed = 25f;

        [SerializeField] private List<GameObject> bullets;

        private SceneManager _sm;
        private bool _isAttacking;
        private float _spawnX1, _spawnX2;

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            _isAttacking = false;
            _spawnX1 = -_sm.fieldLenght / 2 * spawnXMargin;
            _spawnX2 = _sm.fieldLenght / 2 * spawnXMargin;
        }

        // Server-only
        public void RainAttack(int bulletIdx, ulong attackerId)
        {
            if (!IsServer || _isAttacking) return;
            _isAttacking = true;
            StartCoroutine(SpawnRandomBullet());
            return;

            IEnumerator SpawnRandomBullet()
            {
                var start = Time.time;
                while (start + duration > Time.time)
                {
                    SpawnBulletRpc(bulletIdx, attackerId);
                    yield return new WaitForSeconds(1 / rate);
                }

                _isAttacking = false;
            }
        }

        // Host & Client
        [Rpc(SendTo.Everyone)]
        private void SpawnBulletRpc(int bulletIdx, ulong attackerId)
        {
            var spawnX = Random.Range(_spawnX1, _spawnX2);
            var dx = Random.Range(-maxAngle, maxAngle);
            var bullet = Instantiate(bullets[bulletIdx], transform);
            bullet.transform.localPosition = new Vector3(spawnX, spawnY, spawnZ);
            var rb = bullet.GetComponentInChildren<Rigidbody>();
            rb.AddForce((-bullet.transform.up + new Vector3(dx, 0, 0)) * speed);

            var destroyable = bullet.GetComponentInChildren<Destroyable>();
            destroyable.TargetOwner =
                attackerId == _sm.GameManager.HostId ? _sm.GameManager.ClientId : _sm.GameManager.HostId;
            if (!_sm.isMultiplayer)
                destroyable.TargetOwner = 2;
            
            // TODO exclude enemy BASE
            // TODO add explosion prefab
            
            if (IsServer)
                destroyable.OnDestroyCallback = target =>
                    target.Damage(60f);
        }
    }
}