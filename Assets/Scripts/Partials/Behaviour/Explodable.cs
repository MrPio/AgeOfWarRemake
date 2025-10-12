using System;
using System.Collections.Generic;
using System.Linq;
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
        private Action<string> _onExplode;
        private bool _hideOnExplode;

        public void Initialize(List<string> targets, float range, float damage, ulong attackerId,
                               GameObject explosion = null, Action<string> onExplode = null, bool hideOnExplode = true)
        {
            _targets = targets;
            _range = range;
            _damage = damage;
            _attackerId = attackerId;
            _explosion = explosion;
            _onExplode = onExplode;
            _hideOnExplode = hideOnExplode;
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
                Explode(other.tag);
        }

        /// <summary>
        /// Spawn explosion and damage all damageable in range.
        /// </summary>
        private void Explode(string collisionTag)
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
                {
                    var maxDistance = enemies[i].ColliderWidth / 2 + _range;
                    if (Mathf.Abs(enemies[i].transform.position.x - transform.position.x) < maxDistance)
                        enemies[i].Damage(_damage);
                }
            }

            _onExplode?.Invoke(collisionTag);

            GetComponent<BoxCollider>().enabled = false;
            GetComponent<Rigidbody>().isKinematic = true;
            foreach (var particleSystem in transform.GetComponentsInChildren<ParticleSystem>())
                particleSystem.Stop();
            if (_hideOnExplode)
                GetComponent<MeshRenderer>().enabled = false;
            Destroy(gameObject, 10f);
        }
    }
}