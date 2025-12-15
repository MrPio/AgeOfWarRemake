using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using Managers.Singletons;
using Model.Units;
using Prefabs;
using UI;
using Unity.Mathematics;
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
        public readonly Dictionary<ulong, float> SpeedPowerupCollectedTime = new();
        public const float SpeedPowerupDuration = 30f;


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
            var baseModelAlly = _sm.GameManager.BaseAlly.Model.Value;
            var baseModelEnemy = _sm.GameManager.BaseEnemy.Model.Value;
            if (baseModelAlly.Age == 5 && baseModelEnemy.Age == 5)
                prefabs.RemoveAll(prefab => prefab.powerupType == PowerupType.Exp);

            var xPos = Random.Range(-spawnRange / 2, spawnRange / 2);
            var currentAge = Math.Max(_sm.GameManager.BaseAlly.Model.Value.Age,
                _sm.GameManager.BaseEnemy.Model.Value.Age);
            var firstUnitModel = UnitFactory.Units[currentAge - 1][0]();
            var maxExp = (int)(5000f * Math.Pow(3, currentAge - 1));

            var powerupIdx = RandomPowerupIdx;
            var powerupType = prefabs[powerupIdx].powerupType;

            // The value is used to add resources and is shown in the floating text
            var powerupValue = powerupType switch
            {
                PowerupType.Coin => (int)Random.Range(firstUnitModel.Cost * 0.5f, firstUnitModel.Cost * 7f),
                PowerupType.Exp => (int)Random.Range(maxExp * 0.0025f, maxExp * 0.035f),
                PowerupType.Special => 1,
                PowerupType.Speed => 1,
                PowerupType.Health => (int)(baseModelAlly.MaxHp * 0.15f),
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
            var collectorBase = _sm.GameManager.Owner2Base(collectorId);
            var collectorUnits = _sm.GameManager.Owner2Units(collectorId);
            var newModel = collectorBase.Model.Value;
            if (powerup.Type == PowerupType.Coin)
                newModel.Money += powerup.Value;
            else if (powerup.Type == PowerupType.Exp)
                newModel.Exp += powerup.Value;
            else if (powerup.Type == PowerupType.Special)
                _sm.SpecialAttackManager.HasSpecialPowerup[collectorId] = true;
            else if (powerup.Type == PowerupType.Speed)
                SpeedPowerupCollectedTime[collectorId] = Time.time;
            else if (powerup.Type == PowerupType.Health)
            {
                newModel.Hp = math.min(newModel.Hp + powerup.Value, newModel.MaxHp);
                foreach (var allyUnit in collectorUnits)
                {
                    var unitModel = allyUnit.Model.Value;
                    unitModel.Hp = math.min(unitModel.Hp + unitModel.MaxHp * 0.5f, unitModel.MaxHp);
                    allyUnit.Model.Value = unitModel;
                }
            }

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
            MusicManager.Instance.PlayPopPowerup(isCollector, type: prefab.powerupType);

            // Floating text for collector
            if (isCollector)
            {
                void SpawnFloatingText()
                {
                    var go = Instantiate(_sm.floatingText, _sm.canvas.transform);
                    go.transform.position = _sm.cam.WorldToScreenPoint(new Vector2(IsServer ? posX : -posX, 1f));
                    var floatingText = go.GetComponent<FloatingText>();
                    floatingText.Initialize($"+ {value.To3Digits()}", prefab.sprite);
                }

                // Set special powerup UI
                if (prefab.powerupType is PowerupType.Special && !_sm.actionMenu.SetSpecialPowerup(true))
                    SpawnFloatingText();
                else if (prefab.powerupType is PowerupType.Speed)
                {
                    _sm.actionMenu.SetSpeedPowerup(true);
                    SpawnFloatingText();
                    StartCoroutine(DelayedDisableSpeedPowerup());

                    IEnumerator DelayedDisableSpeedPowerup()
                    {
                        yield return new WaitForSeconds(SpeedPowerupDuration);
                        _sm.actionMenu.SetSpeedPowerup(false);
                    }
                }
                else
                    SpawnFloatingText();
            }
        }
    }
}