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
        [SerializeField] private List<GameObject> expansions, turretsPos, turretsPrefabs;
        [NonSerialized] public readonly GameObject[] turrets = { null, null, null, null };
        [NonSerialized] private readonly int?[] turretsIndex = { null, null, null, null };

        private void Awake()
        {
            UpdateState(0, new int?[4]);
        }

        // Update the state of the base. If the state is unchanged, nothing is done.
        public void UpdateState(int numExpansions, int?[] newTurrets)
        {
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
                        turrets[i] = Instantiate(turretsPrefabs[newTurret.Value], turretsPos[i].transform.position,
                            Quaternion.identity);
                        turretsIndex[i] = newTurret.Value;
                    }
                }
            }
        }
    }
}