using System;
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using Managers;
using Model.Bases;
using Model.Turrets;
using Model.Units;
using Partials.AI;
using UI;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;

namespace Prefabs
{
    public class Base : NetworkBehaviour, IDamageable
    {
        #region IDamageable implementation

        public Transform PrefabTransform => _isDestroyed ? null : BasePrefab?.transform;
        public string Name => Model.Value.Name;
        public ulong Owner => IsBot.Value ? 2 : OwnerClientId;

        // Server-only
        public void Damage(float damage)
        {
            if (!IsServer || damage <= 0 || !Model.Value.HasValue || _isDestroyed || _sm.GameManager.IsGameOver) return;

            // Bot resistance
            if (!DataManager.IsMultiplayer && IsBot.Value)
                damage *= 0.5f;

            var newModel = Model.Value;
            newModel.Hp = Mathf.Clamp(newModel.Hp - damage, 0, newModel.MaxHp);
            Model.Value = newModel;
        }

        #endregion

        #region References & Components

        private SceneManager _sm;
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private Transform hpBarPoint;
        [NonSerialized] public BasePrefab BasePrefab;
        private HpBar _hpBar;

        #endregion

        #region Data

        private const bool IsCheating = true;
        [NonSerialized] public readonly List<Turret> Turrets = new() { null, null, null, null };
        private bool _isDestroyed;
        private bool _isLeft;

        #endregion

        #region NetVars

        public readonly NetworkVariable<Model.Bases.Base> Model = new(BaseFactory.Cave());
        public readonly NetworkVariable<bool> IsBot = new(); // Readonly

        #region Listeners

        // Host & Client
        private void OnModelChanged(Model.Bases.Base value, Model.Bases.Base newValue)
        {
            if (!newValue.HasValue) return;

            // Reload the unit prefab if the unit type has changed
            if (BasePrefab is null || !value.HasValue || value.Prefab != newValue.Prefab)
                LoadPrefab(newValue.Prefab);

            // Update the unit's HP bar if the unit's HP has changed
            if (newValue.Hp < newValue.MaxHp)
            {
                // If it's the first time, spawn the Hp bar.
                if (_hpBar is null)
                {
                    var go = Instantiate(_sm.hpBarVertical, _sm.canvas.transform);
                    go.transform.position = Vector3.down * 100;
                    _hpBar = go.GetComponent<HpBar>();
                    _hpBar.Target = hpBarPoint;
                    _hpBar.Initialize(!DataManager.IsMultiplayer
                        ? null
                        : (OwnerClientId == _sm.GameManager.HostId
                            ? _sm.GameManager.UsernameHost
                            : _sm.GameManager.UsernameClient).Value);
                }

                _hpBar.SetValue(newValue.Hp, newValue.MaxHp, alsoText: true);
            }

            // Only the host can despawn the destroyed base. _sm.EndGame() is called in OnNetworkDespawn()
            if (newValue.Hp <= 0)
            {
                _isDestroyed = true;

                // Hide turrets
                foreach (var turret in Turrets)
                    turret?.gameObject.SetActive(false);

                // TODO: spawn explosion
                _sm.EndGame();

                // Hide base and hpBar
                _hpBar.gameObject.SetActive(false);
                gameObject.SetActive(false);
            }

            // Update the turret configuration (lazy)
            // Eager only when evolving
            BasePrefab?.UpdateTurretConfiguration(newValue.UnlockedExpansions, newValue.Turrets,
                force: value.Age != newValue.Age);

            if (_isLeft)
                _sm.statsMenu.UpdateUI(Model.Value.Money, Model.Value.Exp);
            else if (IsBot.Value)
                _sm.logger.Log($"[BOT Status] Money={Model.Value.Money}, Exp={Model.Value.Exp}.");
        }

        #endregion

        #endregion

        #region Events

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();

            // Spawn HP bar
            // var go = Instantiate(_sm.hpBarVertical, _sm.canvas.transform);
            // go.transform.position = Vector3.down * 100;
            // _hpBar = go.GetComponent<HpBar>();
            // _hpBar.Target = hpBarPoint;
        }

        public override void OnNetworkSpawn()
        {
            _isLeft = IsOwner && !IsBot.Value;
            _sm.logger.Log($"Spawning {name}, IsOwner={IsOwner},  IsBot={IsBot.Value}, _isLeft={_isLeft}");

            if (_isLeft) _sm.GameManager.BaseAlly = this;
            else _sm.GameManager.BaseEnemy = this;

            if (IsBot.Value)
            {
                gameObject.AddComponent<BotAI>();

                // Add infinite exp to bot
                var newModel = Model.Value;
                // newModel.Money = (int)(newModel.Money * BotAI.BotIncomeMultiplier); // 9_999_999;
                newModel.Exp = 9_999_999;
                Model.Value = newModel;
            }

            // Add cheats to both players
            if (IsCheating && IsHost && !IsBot.Value)
            {
                // Add infinite money and exp
                var newModel = Model.Value;
                // newModel.Money = 9_999_999;
                // newModel.Exp = 9_999_999; 
                Model.Value = newModel;
            }

            Model.OnValueChanged += OnModelChanged;
            OnModelChanged(default, Model.Value);

            // Initialize the base position based on ownership.
            if (_isLeft)
            {
                transform.position = new Vector3(-_sm.fieldLenght / 2, transform.position.y, transform.position.z);
                transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                transform.position = new Vector3(_sm.fieldLenght / 2, transform.position.y, transform.position.z);
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }

        public override void OnNetworkDespawn()
        {
            Model.OnValueChanged -= OnModelChanged;
        }

        #endregion

        #region RPCs

        [ServerRpc]
        // unitIndex is 0-based
        public void BuyUnitServerRpc(byte unitIndex, ServerRpcParams rpcParams = default)
        {
            var senderClientId = rpcParams.Receive.SenderClientId;
            var model = Model.Value;
            var unitModel = UnitFactory.Units[Model.Value.Age - 1][unitIndex]();

            // Money check
            if (model.Money < unitModel.Cost)
                return;
            model.Money -= unitModel.Cost;
            Model.Value = model;

            StartCoroutine(DelayedSpawnUnit());
            return;

            IEnumerator DelayedSpawnUnit()
            {
                yield return new WaitForSeconds(unitModel.SpawnTime);
                var unit = Instantiate(
                    unitPrefab,
                    new Vector3(BasePrefab.unitSpawnPointX.position.x, 0, 0),
                    Quaternion.identity
                ).GetComponent<Unit>();

                // Assigning before spawning to ensure having the value in onNetworkSpawn()
                // ...it works, but is it safe?
                unit.Model.Value = unitModel;
                unit.IsBot.Value = IsBot.Value;
                unit.GetComponent<NetworkObject>().SpawnWithOwnership(senderClientId);
            }
        }

        [ServerRpc]
        public void BuyTurretServerRpc(byte expansionIndex, byte turretIndex)
        {
            var model = Model.Value;
            var turretModel = TurretFactory.Turrets[Model.Value.Age - 1][turretIndex]();

            // The requested expansion has not been bought or the spot is already occupied
            if (model.UnlockedExpansions - 1 < expansionIndex || model.Turrets[expansionIndex].HasValue) return;

            // Money check
            if (model.Money < turretModel.Cost) return;
            model.Money -= turretModel.Cost;

            model.Turrets[expansionIndex] = turretModel;
            model.Turrets = (Model.Turrets.Turret[])model.Turrets.Clone(); // Force trigger the change
            Model.Value = model;
        }

        [ServerRpc]
        public void SellTurretServerRpc(byte expansionIndex)
        {
            var model = Model.Value;
            var turretModel = model.Turrets[expansionIndex];
            model.Turrets[expansionIndex] = default;
            model.Turrets = (Model.Turrets.Turret[])model.Turrets.Clone(); // Force trigger the change
            model.Money += turretModel.SellPrice;
            Model.Value = model;
        }

        [ServerRpc]
        // UnlockedExpansions is 1-based
        public void BuyExpansionServerRpc()
        {
            var model = Model.Value;

            // Availability check
            if (BaseFactory.ExpansionCosts.Count <= model.UnlockedExpansions - 1) return;

            // Money check
            var cost = BaseFactory.ExpansionCosts[model.UnlockedExpansions - 1];
            if (model.Money < cost) return;

            // Commit
            model.Money -= cost;
            model.UnlockedExpansions = math.min(4, model.UnlockedExpansions + 1);
            Model.Value = model;
        }

        [ServerRpc]
        public void EvolveServerRpc()
        {
            if (Model.Value.Exp < Model.Value.EvolveExpRequired || Model.Value.Age >= BaseFactory.Bases.Count) return;

            // No more ages
            if (BaseFactory.Bases.Count <= Model.Value.Age) return;

            var newModel = BaseFactory.Bases[Model.Value.Age]();
            newModel.Hp = Mathf.Min(newModel.MaxHp, Model.Value.Hp + (newModel.MaxHp - Model.Value.MaxHp));
            newModel.Money = Model.Value.Money;
            newModel.Exp = Model.Value.Exp;
            newModel.UnlockedExpansions = Model.Value.UnlockedExpansions;
            newModel.Turrets = Model.Value.Turrets;
            Model.Value = newModel;

            if (!IsBot.Value)
                InitializeActionMenuRpc(newModel.Age - 1);
        }

        [Rpc(SendTo.Owner)]
        private void InitializeActionMenuRpc(int age) =>
            _sm.actionMenu.Initialize(age);

        #endregion

        #region Methods

        // Host & Client
        private void LoadPrefab(string prefab)
        {
            if (BasePrefab is not null)
                Destroy(BasePrefab.gameObject);
            BasePrefab = Instantiate(Resources.Load<GameObject>(prefab), transform).GetComponent<BasePrefab>();
            for (var i = 0; i < Turrets.Count; i++)
                if (Turrets[i] is not null)
                {
                    _sm.logger.Log(
                        $"Setting Pos for turret {i}. From={Turrets[i].transform.position}, to={BasePrefab.turretsPos[i].transform.position}");
                    Turrets[i].transform.position = BasePrefab.turretsPos[i].transform.position;
                }
        }

        #endregion
    }
}