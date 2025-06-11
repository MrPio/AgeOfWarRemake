using System;
using System.Collections.Generic;
using System.Linq;
using EasyButtons;
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
            UpdateState(0, new Model.Turrets.Turret[] { default, default, default, default });
        }

        // Update the state of the base. If the state is unchanged, nothing is done (lazy).
        public void UpdateState(int numExpansions, Model.Turrets.Turret[] newTurrets)
        {
            // Checking one expansion slot at a time
            for (var i = 0; i < 4; i++)
            {
                // Show/Hide expansions
                expansions[i].SetActive(i < numExpansions);

                // Remove invalid turrets
                if (_base.IsServer)
                {
                    var newTurret = newTurrets[i];
                    if (!_base.Turrets[i] || _base.Turrets[i]!.Model.Value != newTurret)
                    {
                        print($"Destroying {(!_base.Turrets[i] ? "null" : _base.Turrets[i].Index.Value)}");
                        print(string.Join(",", _base.Turrets.ToList()));
                        _base.Turrets[i]?.GetComponent<NetworkObject>().Despawn(destroy: true);

                        // Instantiate new turrets
                        if (newTurret.HasValue)
                        {
                            print($"Instantiating {newTurret.Prefab}");
                            var prefab = Resources.Load<GameObject>(newTurret.Prefab);
                            _base.Turrets[i] = Instantiate(prefab).GetComponent<Turret>();

                            // Assigning before spawning... it works, but is it safe?
                            _base.Turrets[i].Index.Value = (byte)i; // To ensure having the value in onNetworkSpawn()
                            _base.Turrets[i].Model.Value = newTurret;
                            _base.Turrets[i].GetComponent<NetworkObject>().SpawnWithOwnership(_base.OwnerClientId);
                        }
                    }
                }
            }
        }

        [Button]
        private void Debug()
        {
            foreach (var turret in GameObject.FindGameObjectsWithTag("Turret"))
            {
                print(turret);
                print(turret == null);
            }
        }
    }
}