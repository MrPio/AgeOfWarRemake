using System;
using System.Collections.Generic;
using ExtensionFunctions;
using Managers.Singletons;
using Model.Units;
using Prefabs;
using UI;
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
        public Sprite sprite;
    }

    public class PowerupManager : NetworkBehaviour
    {
        private static SceneManager _sm;
        [SerializeField] private List<PowerupPrefab> prefabs;
        [SerializeField] private float spawnRange = 10f;
        private readonly List<Powerup> _spawnedPowerups = new();


        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        // Server-only
        public void SpawnPowerup()
        {
            if (!NetworkManager.Singleton.IsServer)
                throw new Exception("Only server can spawn powerups!");
            var xPos = Random.Range(-spawnRange / 2, spawnRange / 2);
            var currentAge = Math.Max(_sm.GameManager.BaseAlly.Model.Value.Age,
                _sm.GameManager.BaseEnemy.Model.Value.Age);
            var firstUnitModel = UnitFactory.Units[currentAge - 1][0]();
            var maxExp = (int)(5000f * Math.Pow(3, currentAge - 1));
            var powerupIdx = Random.Range(0, prefabs.Count);
            var powerupType = prefabs[powerupIdx].powerupType;
            var powerupValue = powerupType switch
            {
                PowerupType.Coin => (int)Random.Range(firstUnitModel.Cost * 0.5f, firstUnitModel.Cost * 6f),
                PowerupType.Exp => (int)Random.Range(maxExp * 0.0025f, maxExp * 0.0325f),
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
            var powerupGo = Instantiate(prefab, transform);
            powerupGo.transform.position = new Vector3(
                x: IsServer ? xPos : -xPos,
                y: powerupGo.transform.position.y,
                z: powerupGo.transform.position.z
            );
            var powerup = powerupGo.GetComponentInChildren<Powerup>();
            _spawnedPowerups.Add(powerup);
            powerup.Init(powerupType, powerupValue);
        }

        // Server-only
        public void Collect(Powerup powerup, ulong collectorId)
        {
            if (!IsServer) return;
            if (!_spawnedPowerups.Contains(powerup)) return;
            _spawnedPowerups.Remove(powerup);

            // Add money/exp to collector's base

            var collectorBase = _sm.GameManager.OwnerId2Base(collectorId);
            var newModel = collectorBase.Model.Value;
            if (powerup.Type == PowerupType.Coin)
                newModel.Money += powerup.Value;
            else if (powerup.Type == PowerupType.Exp)
                newModel.Exp += powerup.Value;
            collectorBase.Model.Value = newModel;
            var powerupIdx = prefabs.FindIndex(x => x.powerupType == powerup.Type);

            DestroyPowerupRpc(collectorId, powerup.Value, powerup.transform.position.x, powerupIdx);
        }

        // Server & Client
        [Rpc(SendTo.Everyone)]
        private void DestroyPowerupRpc(ulong collectorId, int value, float posX, int powerupIdx)
        {
            var isCollector = collectorId == NetworkManager.Singleton.LocalClientId;
            MusicManager.Instance.PlayPopPowerup(isCollector);

            // Floating text for collector
            if (isCollector)
            {
                var go = Instantiate(_sm.floatingText, _sm.canvas.transform);
                go.transform.position = _sm.cam.WorldToScreenPoint(new Vector2(IsServer ? posX : -posX, 1f));
                var floatingText = go.GetComponent<FloatingText>();
                floatingText.Initialize($"+ {value.To3Digits()}", prefabs[powerupIdx].sprite);
            }
        }
    }
}