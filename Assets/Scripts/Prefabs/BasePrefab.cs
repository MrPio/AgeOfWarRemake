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
        [NonSerialized] private Base _base;

        private void Awake()
        {
            _base = transform.parent.GetComponent<Base>();
            UpdateTurretConfiguration(0, new Model.Turrets.Turret[] { default, default, default, default });
        }

        // Host & Client
        // Update the state of the base. If the state is unchanged, nothing is done (lazy).
        public void UpdateTurretConfiguration(int numExpansions, Model.Turrets.Turret[] newTurrets)
        {
            // Checking one expansion slot at a time
            for (var i = 0; i < 4; i++)
            {
                // Show/Hide expansions
                expansions[i].SetActive(i < numExpansions);

                // Remove invalid turrets (Server-only)
                if (_base.IsServer)
                {
                    var newTurret = newTurrets[i];
                    if (!_base.Turrets[i] || _base.Turrets[i]!.Model.Value != newTurret)
                    {
                        _base.Turrets[i]?.GetComponent<NetworkObject>().Despawn(destroy: true);

                        // Instantiate new turrets
                        if (newTurret.HasValue)
                        {
                            var turretPrefab = Resources.Load<GameObject>(newTurret.Prefab);
                            _base.Turrets[i] = Instantiate(turretPrefab).GetComponent<Turret>();

                            // Assigning before spawning to ensure having the value in onNetworkSpawn()
                            // ...it works, but is it safe?
                            _base.Turrets[i].Index.Value = (byte)i;
                            _base.Turrets[i].Model.Value = newTurret;
                            _base.Turrets[i].GetComponent<NetworkObject>().SpawnWithOwnership(_base.OwnerClientId);
                        }
                    }
                }
            }
        }
    }
}