using System;
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using Managers;
using Unity.Netcode;
using UnityEngine;

namespace Partials.Behaviour
{
    public class Explodable : MonoBehaviour
    {
        private static SceneManager _sm;
        private List<string> _targets;
        private float _range, _damage;
        private GameObject _explosion;
        private ulong _attackerId;
        private Action _onExplode;

        public void Initialize(List<string> targets, float range, float damage, ulong attackerId,
                               GameObject explosion = null, Action onExplode = null)
        {
            _targets = targets;
            _range = range;
            _damage = damage;
            _attackerId = attackerId;
            _explosion = explosion;
            _onExplode = onExplode;
        }

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        /// <summary>
        /// When detects a collision with an allowed target, explode
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (_targets.Where(other.CompareTag).Any())
                Explode();
        }

        /// <summary>
        /// Spawn explosion and damage all damageable in range.
        /// </summary>
        private void Explode()
        {
            // Spawn explosion
            if (_explosion != null)
                Instantiate(_explosion, transform.position, Quaternion.identity);

            // Damage units (Server-only)
            if (NetworkManager.Singleton.IsServer)
            {
                var enemies = NetworkManager.Singleton.LocalClientId == _attackerId
                    ? _sm.GameManager.UnitsEnemy
                    : _sm.GameManager.UnitsAlly;
                for (var i = 0; i < enemies.Count; i++)
                    if (Mathf.Abs(enemies[i].transform.position.x - transform.position.x) < _range)
                        enemies[i].Damage(_damage);
            }

            _onExplode?.Invoke();
            
            GetComponent<Collider>().enabled = false;
            GetComponent<MeshRenderer>().enabled = false;
            Destroy(gameObject,10f);
        }
    }
}