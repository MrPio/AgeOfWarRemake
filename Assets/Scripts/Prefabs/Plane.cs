using System;
using System.Collections;
using System.Collections.Generic;
using Managers;
using Managers.Statics;
using Model.Bases;
using Partials.Behaviour;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Prefabs
{
    public class Plane : MonoBehaviour
    {
        private static SceneManager _sm;
        [SerializeField] private Vector3 spawnPosition;
        [SerializeField] private float deltaX, dropMarginFromBase;
        [SerializeField] private GameObject bomb;
        [SerializeField] private Transform bombSpawnPoint;
        private Rigidbody _rb;

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        // Host&Client
        public void Initialize(SpecialAttack model, bool isLeft, ulong attackerId, Action<GameObject> onBombSpawn,
                               Action<GameObject> onBombExplode)
        {
            // Set dynamics
            transform.position = new Vector3(
                x: isLeft ? spawnPosition.x : -spawnPosition.x,
                y: spawnPosition.y,
                z: spawnPosition.z
            );
            transform.localScale = new Vector3(
                x: math.abs(transform.localScale.x) * (isLeft ? 1 : -1),
                y: transform.localScale.y,
                z: transform.localScale.z
            );
            _rb = GetComponent<Rigidbody>();
            _rb.linearVelocity = (isLeft ? Vector3.right : Vector3.left) * deltaX / model.Duration;

            // Bomb drop routines
            StartCoroutine(DropBomb());
            return;

            IEnumerator DropBomb()
            {
                while (true)
                {
                    var rate = model.Rate;
                    if (_sm.SpecialAttackManager.HasSpecialPowerup.TryGetValue(attackerId, out var hasSpecialPowerup) &&
                        hasSpecialPowerup)
                        rate *= 1.666f;
                    
                    yield return new WaitForSeconds(1f / rate);
                    // Check allowed drop zone
                    if (Mathf.Abs(bombSpawnPoint.position.x) > _sm.fieldLenght / 2 - dropMarginFromBase) continue;

                    var bombGo = Instantiate(bomb, bombSpawnPoint.position, Quaternion.identity);
                    onBombSpawn?.Invoke(bombGo);
                    var destroyable = bombGo.GetComponent<Destroyable>();
                    destroyable.AllowedTags = new List<string> { "Unit", "Ground" };
                    destroyable.TargetOwner = DataManager.GameMode is GameMode.Singleplayer && isLeft ? 2 :
                        attackerId == _sm.GameManager.HostId ? _sm.GameManager.ClientId : _sm.GameManager.HostId;
                    destroyable.OnDestroy = () =>
                    {
                        onBombExplode?.Invoke(bombGo);
                        _sm.MusicManager.PlayHitSpecial(model.Age);
                    };

                    // Server-only
                    if (NetworkManager.Singleton.IsServer)
                        destroyable.OnDamage = target =>
                        {
                            // This avoids hitting just one enemy unit
                            var enemies = attackerId == _sm.GameManager.HostId
                                ? _sm.GameManager.UnitsEnemy
                                : _sm.GameManager.UnitsAlly;
                            for (var j = 0; j < enemies.Count; j++)
                            {
                                var maxDistance = enemies[j].ColliderWidth / 2 + model.Range;
                                if (Mathf.Abs(enemies[j].transform.position.x - bombGo.transform.position.x) <
                                    maxDistance)
                                    enemies[j].Damage(model.Damage);
                            }
                        };
                }
            }
        }
    }
}