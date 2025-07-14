using System;
using System.Collections;
using Interfaces;
using UnityEngine;

namespace Partials
{
    public class Destroyable : MonoBehaviour
    {
        [SerializeField] private bool onStart, onTrigger;
        [SerializeField] private GameObject spawnOnDestroy;
        [SerializeField] private float lifespan = 30;
        [NonSerialized] public Action<IDamageable> OnDestroyCallback;
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

            if (other.transform.parent && other.transform.parent.TryGetComponent<IDamageable>(out var damageable))
            {
                if (TargetOwner is not null && damageable.Owner != TargetOwner) return; // Includes !IsOwner check
                OnDestroyCallback?.Invoke(damageable);
            }

            Destroy(delay: 0.04f);
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
