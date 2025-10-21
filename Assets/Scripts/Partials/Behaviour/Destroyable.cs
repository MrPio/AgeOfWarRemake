using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using UnityEngine;

namespace Partials.Behaviour
{
    public class Destroyable : MonoBehaviour
    {
        [SerializeField] private bool onStart, onTrigger;
        [SerializeField] private GameObject spawnOnDestroy;
        [SerializeField] private float lifespan = 30, destroyDelay = 0.06f;
        [NonSerialized] public Action<IDamageable> OnDestroyCallback;
        [SerializeField] public List<string> AllowedTags;
        [NonSerialized] public GameObject Target;
        [NonSerialized] public ulong? TargetOwner; // Used when there is not a precise Target
        private float _spawnTime;
        private bool _destroyed;

        private void Start()
        {
            _spawnTime = Time.time;
            if (onStart)
                Destroy();
        }

        private void FixedUpdate()
        {
            if (!_destroyed && lifespan > 0 && Time.time - _spawnTime > lifespan)
                Destroy();
        }

        private void OnTriggerStay(Collider other)
        {
            if (_destroyed || !onTrigger || (Target is not null && other.gameObject != Target)) return;
            if (other.CompareTag("Bullet")) return;
            if (AllowedTags is not null && !AllowedTags.Any(other.CompareTag)) return;

            if (other.transform.parent && other.transform.parent.TryGetComponent<IDamageable>(out var damageable))
            {
                if (TargetOwner is not null && damageable.Owner != TargetOwner) return; // Includes !IsOwner check
                OnDestroyCallback?.Invoke(damageable);
            }

            Destroy(delay: other.CompareTag("Ground") ? 0f : destroyDelay);
        }

        private void Destroy(float delay = 0f)
        {
            if (_destroyed) return;
            _destroyed = true;
            if (delay > 0)
                StartCoroutine(DelayedDestroy());
            else
                DestroyHelper();
            return;


            IEnumerator DelayedDestroy()
            {
                yield return new WaitForSeconds(delay);
                DestroyHelper();
            }

            void DestroyHelper()
            {
                if (spawnOnDestroy)
                    Instantiate(spawnOnDestroy, transform.position, Quaternion.identity);
                Destroy(gameObject);
            }
        }
    }
}