using Partials.Behaviour;
using Unity.Netcode;
using UnityEngine;

namespace Partials
{
    public class ClusterSpawn : MonoBehaviour
    {
        [SerializeField] private GameObject toSpawn;
        [SerializeField] private float impulse = 5f, spawnDistance = 0.55f;

        [Range(0, 8)] [SerializeField] private int howMany = 5;

        private float? _damage;
        private ulong _targetOwner;

        public void Initialize(ulong targetOwner, float? damage = null)
        {
            _damage = damage;
            _targetOwner = targetOwner;
        }

        private void OnDestroy()
        {
            if (toSpawn is null || howMany < 1) return;

            var step = Mathf.PI / (howMany + 1);
            // for (var rad = step; rad < Mathf.PI - 0.1; rad += step)
            for (var i = 1; i <= howMany; i++)
            {
                var rad = Random.Range(Mathf.PI / 6, Mathf.PI * 5 / 6);
                var direction = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
                var go = Instantiate(toSpawn, transform.position + direction * spawnDistance, Quaternion.identity);
                var rb = go.GetComponent<Rigidbody>() ?? go.AddComponent<Rigidbody>();
                rb.AddForce(direction * impulse, ForceMode.Impulse);
                rb.AddTorque(Random.insideUnitSphere * Random.Range(5f, 20f), ForceMode.Impulse);

                if (_damage.HasValue)
                {
                    var destroyable = go.GetComponent<Destroyable>();
                    destroyable.TargetOwner = _targetOwner;
                    if (NetworkManager.Singleton.IsServer)
                        destroyable.OnDamage = target =>
                            target.Damage(_damage.Value);
                }
            }
        }
    }
}