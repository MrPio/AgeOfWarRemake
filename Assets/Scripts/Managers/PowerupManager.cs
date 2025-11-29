using System;
using System.Collections.Generic;
using Model.Units;
using Prefabs;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Managers
{
    [Serializable]
    internal class PowerupPrefab
    {
        public PowerupType powerupType;
        public GameObject prefab;
    }

    public class PowerupManager : NetworkBehaviour
    {
        private static SceneManager _sm;
        [SerializeField] private List<PowerupPrefab> prefabs;
        private GameObject spawnPowerup;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        // Server-only
        public void SpawnPowerup()
        {
            if (!NetworkManager.Singleton.IsServer)
                throw new Exception("Only server can spawn powerups!");
            var xPos = Random.Range(-2f, 2f);
            var currentAge = Math.Max(_sm.GameManager.BaseAlly.Model.Value.Age,
                _sm.GameManager.BaseEnemy.Model.Value.Age);
            var firstUnitModel = UnitFactory.Units[currentAge - 1][0]();
            var maxExp = (int)(5000f * Math.Pow(3, currentAge - 1));
            var powerupIdx = Random.Range(0, prefabs.Count);
            var powerupType = prefabs[powerupIdx].powerupType;
            var powerupValue = powerupType switch
            {
                PowerupType.Coin => Random.Range(firstUnitModel.Cost, firstUnitModel.Cost * 4),
                PowerupType.Exp => (int)Random.Range(maxExp * 0.05f, maxExp * 0.2f),
                _ => throw new ArgumentOutOfRangeException()
            };
            SpawnPowerupRpc(xPos, powerupIdx, powerupValue);
        }

        // Host & Client
        [Rpc(SendTo.Everyone)]
        private void SpawnPowerupRpc(float xPos, int powerupIdx, int powerupValue)
        {
            var prefab = prefabs[powerupIdx].prefab;
            var powerupType = prefabs[powerupIdx].powerupType;
            spawnPowerup = Instantiate(prefab, transform);
            spawnPowerup.transform.position = new Vector3(
                x: xPos,
                y: spawnPowerup.transform.position.y,
                z: spawnPowerup.transform.position.z
            );
            spawnPowerup.GetComponent<Powerup>().Init(powerupType, powerupValue);
        }
    }
}