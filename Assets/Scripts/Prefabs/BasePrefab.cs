using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Prefabs
{
    /// <summary>
    /// Refers to the base prefab instantiated inside the base game object.
    /// </summary>
    public class BasePrefab : MonoBehaviour
    {
        [SerializeField] public Transform unitSpawnPointX;
        [SerializeField] public List<GameObject> expansions, turretsPos;
        [NonSerialized] public readonly Turret[] Turrets = { null, null, null, null };
        [NonSerialized] private Base _base;

        private void Awake()
        {
            _base = transform.parent.GetComponent<Base>();
            UpdateState(0, new Model.Turrets.Turret[] { default, default, default, default });
        }

        // Update the state of the base. If the state is unchanged, nothing is done.
        public void UpdateState(int numExpansions, Model.Turrets.Turret[] newTurrets)
        {
            // Checking one expansion slot at a time
            for (var i = 0; i < 4; i++)
            {
                // Show/Hide expansions
                expansions[i].SetActive(i < numExpansions);

                // Remove invalid turrets
                var newTurret = newTurrets[i];
                if (_base.IsServer && Turrets[i].Model.Value != newTurret)
                {
                    if (_base.IsServer)
                        Turrets[i].GetComponent<NetworkObject>().Despawn(destroy: true);

                    // Instantiate new turrets
                    if (newTurret.HasValue)
                    {
                        var prefab = Resources.Load<GameObject>(newTurret.Prefab);
                        var turret = Instantiate(prefab, transform).GetComponent<Turret>();
                        turret.Index.Value = (byte)i; // To ensure having the value in onNetworkSpawn()
                        turret.GetComponent<NetworkObject>().SpawnWithOwnership(_base.OwnerClientId);
                    }
                }
            }
        }
    }
}