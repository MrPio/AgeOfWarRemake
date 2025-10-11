using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ExtensionFunctions;
using Managers;
using Model.Bases;
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
        [SerializeField] private float[] phaseDurations = { 60f, 60f }; // [0]=melee, [1]=melee+range, [2]=melee+range+tank
        [SerializeField] private float initialAgeInterval = 180f;
        [SerializeField] private float initialTurretInterval = 10f;
        private Base _base;
        private SceneManager _sm;

        public event Action<int> OnAgeChanged;

        private int age = 0;
        private Phase phase = Phase.Melee;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            _base = GetComponent<Base>();
        }

        private void Start()
        {
            StartCoroutine(SpawnLoop());
            StartCoroutine(PhaseLoop());
            // StartCoroutine(AgeLoop());
            StartCoroutine(TurretLoop());
        }

        private IEnumerator SpawnLoop()
        {
            yield return new WaitForSeconds(3f);
            while (true)
            {
                if (_sm.GameManager.IsGameOver) yield break;
                var ran = Random.value;
                _base.BuyUnitServerRpc((byte)(phase switch
                {
                    Phase.Melee => 0,
                    Phase.Range => ran < .5f ? 0 : 1,
                    Phase.Tank => ran < .3f ? 0
                        : ran < .7f ? 1
                        : 2,
                    _ => 0
                }));
                var delay = phase switch
                {
                    Phase.Melee => Random.Range(2f, 6f),
                    Phase.Range => Random.Range(2f, 6f),
                    Phase.Tank => Random.Range(2f, 6f),
                    _ => Random.Range(2f, 7f)
                };
                yield return new WaitForSeconds(delay);
            }
        }

        private IEnumerator PhaseLoop()
        {
            while (true)
            {
                if (_sm.GameManager.IsGameOver) yield break;
                phase = Phase.Melee;
                yield return new WaitForSeconds(phaseDurations[0]);
                phase = Phase.Range;
                yield return new WaitForSeconds(phaseDurations[1]);
                phase = Phase.Tank;
                // now stay in Tank until age resets it
                yield return new WaitUntil(() => phase != Phase.Tank);
            }
        }

        private IEnumerator AgeLoop()
        {
            var interval = initialAgeInterval;
            while (age < BaseFactory.Bases.Count - 1)
            {
                yield return new WaitForSeconds(interval);
                if (_sm.GameManager.IsGameOver) yield break;
                age++;
                _base.EvolveServerRpc();
                interval += 20f;
                phase = Phase.Melee; // reset phase immediately
            }
        }

        private IEnumerator TurretLoop()
        {
            var interval = initialTurretInterval;
            while (true)
            {
                yield return new WaitForSeconds(interval);
                interval = initialTurretInterval;

                if (_sm.GameManager.IsGameOver) yield break;

                var currentTurrets = _base.Model.Value.Turrets.ToList();
                var validTurrets = currentTurrets.Where(turret => turret.HasValue).OrderBy(turret => turret.Cost).ToList();
                var baseModel = _base.Model.Value;

                switch (Random.Range(0, 3))
                {
                    // Upgrade a turret
                    case 0 when validTurrets.Count > 0:
                        var toUpgrade = validTurrets.Where(turret => turret.Age < baseModel.Level || turret.Level < 3)
                            .ToList().RandomItem();
                        var toUpgradePos = (byte)currentTurrets.IndexOf(toUpgrade);
                        _base.SellTurretServerRpc(toUpgradePos);

                        // The new turret is a lower or same level if changing age, else the next level of the same age
                        var upgradedChoice = toUpgrade.Age < baseModel.Level
                            ? Random.Range(0, toUpgrade.Level)
                            : toUpgrade.Level;
                        _base.BuyTurretServerRpc(toUpgradePos, (byte)upgradedChoice);
                        interval += 15f;
                        break;

                    // Buy a turret on an empty slot
                    case 1 when validTurrets.Count < 4:
                        var spot = currentTurrets.FindIndex(turret => !turret.HasValue);
                        var choice = new List<int> { 0, 1, 2 }.RandomWeighted(new List<float> { 1f, 0.75f, 0.5f });
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