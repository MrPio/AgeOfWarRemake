using System;
using UnityEngine;

namespace Partials
{
    public class Destroyable : MonoBehaviour
    {
        [SerializeField] private bool onStart, onTrigger;
        [SerializeField] private GameObject spawnOnDestroy;
        [SerializeField] private float lifespan = 30;
        private float _spawnTime;

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
                Destroy();
        }

        private void Destroy()
        {
            if (spawnOnDestroy is not null)
                Instantiate(spawnOnDestroy, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}