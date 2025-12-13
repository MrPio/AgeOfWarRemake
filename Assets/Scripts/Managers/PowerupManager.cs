using System;
using System.Collections.Generic;
using System.Linq;
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
        public int probabilityWeight;
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

        private int RandomPowerupIdx
        {
            get
            {
                var maxWeight = (float)prefabs.Select(it => it.probabilityWeight).Sum();
                var rand = Random.Range(0, maxWeight);
                var cumulative = 0f;
                for (var i = 0; i < prefabs.Count; i++)
                {
                    cumulative += prefabs[i].probabilityWeight;
                    if (cumulative >= rand)
                        return i;
                }

                return prefabs.Count - 1;
            }
        }

        // Server-only
        public void SpawnPowerup()
        {
            if (!NetworkManager.Singleton.IsServer)
                throw new Exception("Only server can spawn powerups!");

            // Remove exp powerup at future age
            if (_sm.GameManager.BaseAlly.Model.Value.Age == 5 && _sm.GameManager.BaseEnemy.Model.Value.Age == 5)
                prefabs.RemoveAll(prefab => prefab.powerupType == PowerupType.Exp);

            var xPos = Random.Range(-spawnRange / 2, spawnRange / 2);
            var currentAge = Math.Max(_sm.GameManager.BaseAlly.Model.Value.Age,
                _sm.GameManager.BaseEnemy.Model.Value.Age);
            var firstUnitModel = UnitFactory.Units[currentAge - 1][0]();
            var maxExp = (int)(5000f * Math.Pow(3, currentAge - 1));

            var powerupIdx = RandomPowerupIdx;
            var powerupType = prefabs[powerupIdx].powerupType;
            var powerupValue = powerupType switch
            {
                PowerupType.Coin => (int)Random.Range(firstUnitModel.Cost * 0.5f, firstUnitModel.Cost * 7f),
                PowerupType.Exp => (int)Random.Range(maxExp * 0.0025f, maxExp * 0.035f),
                PowerupType.Special => 1,
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
            else if (powerup.Type == PowerupType.Special)
                _sm.SpecialAttackManager.HasSpecialPowerup[collectorId] = true;

            collectorBase.Model.Value = newModel;
            var powerupIdx = prefabs.FindIndex(x => x.powerupType == powerup.Type);

            DestroyPowerupRpc(collectorId, powerup.Value, powerup.transform.position.x, powerupIdx);
        }

        // Server & Client
        [Rpc(SendTo.Everyone)]
        private void DestroyPowerupRpc(ulong collectorId, int value, float posX, int powerupIdx)
        {
            var prefab = prefabs[powerupIdx];
            var isCollector = collectorId == NetworkManager.Singleton.LocalClientId;
            MusicManager.Instance.PlayPopPowerup(isCollector);

            // Floating text for collector
            if (isCollector)
            {
                var go = Instantiate(_sm.floatingText, _sm.canvas.transform);
                go.transform.position = _sm.cam.WorldToScreenPoint(new Vector2(IsServer ? posX : -posX, 1f));
                var floatingText = go.GetComponent<FloatingText>();
                floatingText.Initialize($"+ {value.To3Digits()}", prefab.sprite);

                // Set special powerup UI
                if (prefab.powerupType == PowerupType.Special)
                    _sm.actionMenu.SetSpecialPowerup(true);
            }
        }
    }
}