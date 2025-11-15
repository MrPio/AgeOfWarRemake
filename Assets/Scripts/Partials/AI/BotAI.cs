using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Model.Bases;
using Unity.Netcode;
using UnityEngine;
using Base = Prefabs.Base;
using Random = UnityEngine.Random;

namespace Partials.AI
{
    public enum Phase
    {
        Melee,
        Range,
        Tank
    }

    public class BotAI : MonoBehaviour
    {
        public const float BotIncomeMultiplier = 1.75f;

        [SerializeField]
        private float[] phaseDurations = { 60f, 60f }; // [0]=melee, [1]=melee+range, [2]=melee+range+tank

        private readonly Dictionary<Phase, List<float>> _turretWeights = new()
        {
            { Phase.Melee, new List<float> { 1f, 0.33f, 0f } },
            { Phase.Range, new List<float> { 1f, 0.75f, 0.5f } },
            { Phase.Tank, new List<float> { 1f, 1f, 1f } }
        };

        [SerializeField] private float[] initialAgeIntervals = { 360f, 380f, 400f, 420f };
        [SerializeField] private float initialTurretInterval = 30f;
        private Base _base;
        private SceneManager _sm;

        private int _age;
        private Phase _phase = Phase.Melee;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            _base = GetComponent<Base>();
        }

        private void Start()
        {
            StartCoroutine(SpawnLoop());
            StartCoroutine(PhaseLoop());
            StartCoroutine(AgeLoop());
            StartCoroutine(TurretLoop());
        }

        private IEnumerator SpawnLoop()
        {
            yield return new WaitForSeconds(3f);
            
            while (true)
            {
                if (_sm.GameManager.IsGameOver) yield break;
                yield return new WaitUntil( () => !_sm.GameManager.IsGamePaused );
                
                var ran = Random.value;
                var unitIdx = _phase switch
                {
                    Phase.Melee => 0,
                    Phase.Range => ran < .5f ? 0 : 1,
                    Phase.Tank => ran < .25f ? 0
                        : ran < .5f ? 1
                        : 2,
                    _ => 0
                };
                var fakeSenderParams = new ServerRpcParams() { Receive = new ServerRpcReceiveParams() { SenderClientId = 2 } };
                _base.BuyUnitServerRpc((byte)unitIdx, rpcParams: fakeSenderParams);
                var delay = _phase switch
                {
                    Phase.Melee => Random.Range(2f, 8f),
                    Phase.Range => unitIdx switch
                    {
                        0 => Random.Range(2f, 6f),
                        1 => Random.Range(3f, 6f),
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    Phase.Tank => unitIdx switch
                    {
                        0 => Random.Range(2f, 4f),
                        1 => Random.Range(2f, 4f),
                        2 => Random.Range(4f, 6f),
                        _ => throw new ArgumentOutOfRangeException()
                    },
                    _ => throw new ArgumentOutOfRangeException()
                };
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator PhaseLoop()
        {
            while (true)
            {
                if (_sm.GameManager.IsGameOver) yield break;
                yield return new WaitUntil( () => !_sm.GameManager.IsGamePaused );

                _phase = Phase.Melee;
                yield return new WaitForSeconds(phaseDurations[0]);
                _phase = Phase.Range;
                yield return new WaitForSeconds(phaseDurations[1]);
                _phase = Phase.Tank;
                // now stay in Tank until age resets it
                yield return new WaitUntil(() => _phase != Phase.Tank);
            }
        }

        private IEnumerator AgeLoop()
        {
            while (_age < BaseFactory.Bases.Count - 1)
            {
                yield return new WaitUntil( () => !_sm.GameManager.IsGamePaused );
                yield return new WaitForSeconds(initialAgeIntervals[_age]);
                if (_sm.GameManager.IsGameOver) yield break;
                _age++;
                _base.EvolveServerRpc();
                _phase = Phase.Melee; // reset phase immediately
            }
        }

        private IEnumerator TurretLoop()
        {
            var interval = initialTurretInterval;
            while (true)
            {
                yield return new WaitUntil( () => !_sm.GameManager.IsGamePaused );
                yield return new WaitForSeconds(interval);
                interval = initialTurretInterval;

                if (_sm.GameManager.IsGameOver) yield break;

                var currentTurrets = _base.Model.Value.Turrets.ToList();
                var validTurrets = currentTurrets.Where(turret => turret.HasValue).OrderBy(turret => turret.Cost)
                    .ToList();
                var baseModel = _base.Model.Value;

                // # 1 add turret -> Will only add a turret if space is available
                // # 2 upgrade turret -> Will only upgrade turrets if it's current Age is greater than the age of the turret. Upgrading a turret will not go above the age of the turret
                // # 3 add a turret spot
                // # 4 sell a turret that is of previous age.
                switch (Random.Range(0, 3))
                {
                    // Upgrade a turret
                    case 0 when validTurrets.Count > 0:
                        var toUpgrades = validTurrets.Where(turret => turret.Age < baseModel.Age || turret.Level < 3)
                            .ToList();
                        if (toUpgrades.Count > 0)
                        {
                            var toUpgrade = toUpgrades.RandomItem();
                            var toUpgradePos = (byte)currentTurrets.IndexOf(toUpgrade);
                            _base.SellTurretServerRpc(toUpgradePos);

                            // The new turret is a lower or same level if changing age, else the next level of the same age
                            var upgradedChoice = toUpgrade.Age < baseModel.Age
                                ? Random.Range(0, toUpgrade.Level)
                                : toUpgrade.Level;
                            _base.BuyTurretServerRpc(toUpgradePos, (byte)upgradedChoice);
                            interval += 15f;
                        }

                        break;

                    // Buy a turret on an empty slot
                    case 1 when validTurrets.Count < 4:
                        var spot = currentTurrets.FindIndex(turret => !turret.HasValue);
                        var choice = new List<int> { 0, 1, 2 }.RandomWeighted(_turretWeights[_phase]);
                        if (baseModel.UnlockedExpansions - 1 < spot)
                            _base.BuyExpansionServerRpc();
                        _base.BuyTurretServerRpc((byte)spot, (byte)choice);
                        interval += 15f;
                        break;

                    // Sell the worst turret
                    case 2 when validTurrets.Count > 1:
                        _base.SellTurretServerRpc((byte)currentTurrets.IndexOf(validTurrets[0]));
                        break;
                }
            }
        }
    }
}