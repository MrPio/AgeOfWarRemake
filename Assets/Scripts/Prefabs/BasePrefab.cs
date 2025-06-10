using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prefabs
{
    /// <summary>
    /// Refers to the base prefab instantiated inside the base game object.
    /// </summary>
    public class BasePrefab : MonoBehaviour
    {
        [SerializeField] public Transform unitSpawnPointX;
        [SerializeField] private List<GameObject> expansions, turretsPos, turretsPrefabs;
        [NonSerialized] public readonly Turret[] turrets = { null, null, null, null };
        [NonSerialized] private readonly int?[] turretsIndex = { null, null, null, null };
        [NonSerialized] public Base Base;

        private void Awake()
        {
            Base = transform.parent.GetComponent<Base>();
            UpdateState(0, new int?[4]);
        }

        // Update the state of the base. If the state is unchanged, nothing is done.
        public void UpdateState(int numExpansions, int?[] newTurrets)
        {
            // Checking one expansion slot at a time
            for (var i = 0; i < 4; i++)
            {
                // Show/Hide expansions
                expansions[i].SetActive(i < numExpansions);

                var newTurret = newTurrets[i];

                // Remove invalid turrets
                if (turretsIndex[i] != newTurret)
                {
                    Destroy(turrets[i]);
                    turrets[i] = null;
                    turretsIndex[i] = null;

                    // Instantiate new turrets
                    if (newTurret is not null)
                    {
                        turrets[i] = Instantiate(turretsPrefabs[newTurret.Value], transform).GetComponent<Turret>();
                        turrets[i].Model = Base.Model.Value.Turrets[newTurret.Value];
                        turrets[i].transform.position = turretsPos[i].transform.position;
                        turretsIndex[i] = newTurret.Value;
                    }
                }
            }
        }
    }
}