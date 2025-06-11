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
            if (lifespan > 0 && Time.time - _spawnTime > lifespan)
                Destroy();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (onTrigger)
            {
                Destroy(delay: 0.065f);
                if (other.transform.parent && other.transform.parent.TryGetComponent<IDamageable>(out var damageable))
                    OnDestroyCallback?.Invoke(damageable);
            }
        }

        private void Destroy(float delay = 0f)
        {
            if (_destroyed) return;
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

        private void OnDestroy()
        {
            _destroyed = true;
        }
    }
}