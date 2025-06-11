using System;
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using Managers;
using Model.Bases;
using Model.Units;
using UI;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using LogType = UI.LogType;

namespace Prefabs
{
    // [RequireComponent(typeof(Observable))]
    public class Base : NetworkBehaviour, IDamageable
    {
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private Transform hpBarPoint;

        [NonSerialized] public BasePrefab BasePrefab;
        [NonSerialized] public readonly List<Turret> Turrets = new() { null, null, null, null };
        private SceneManager _sm;
        private HpBar _hpBar;
        private bool _isDestroyed;

        #region NetworkVariables

        [NonSerialized] public readonly NetworkVariable<Model.Bases.Base> Model = new(BaseFactory.Cave(),
            writePerm: NetworkVariableWritePermission.Owner);

        private void OnModelChanged(Model.Bases.Base value, Model.Bases.Base newValue)
        {
            if (!newValue.HasValue) return;
            _sm.logger.Log($"Obtaining {(IsOwner ? "Ally" : "Enemy")} base state");

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
                    _hpBar = go.GetComponent<HpBar>();
                    _hpBar.Target = hpBarPoint;
                }

                _hpBar.SetValue(newValue.Hp, newValue.MaxHp, alsoText: true);
            }

            // Only the host can despawn the destroyed base. _sm.EndGame() is called in OnNetworkDespawn()
            if (newValue.Hp <= 0)
            {
                if (IsServer)
                    gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            }

            // Update the turret configuration (lazy)
            BasePrefab?.UpdateState(newValue.UnlockedExpansions, newValue.Turrets);
        }

        #endregion

        public Transform Transform => BasePrefab.transform;
        public bool IsDamageable => !_isDestroyed && !IsOwner;

        #region Events

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        public override void OnNetworkSpawn()
        {
            _sm.logger.Log("Spawning a Base, isOwner=" + IsOwner, LogType.NetworkSpawn);
            if (IsOwner) _sm.GameManager.BaseAlly = this;
            else _sm.GameManager.BaseEnemy = this;

            Model.OnValueChanged += OnModelChanged;
            OnModelChanged(default, Model.Value);

            // Initialize the base position based on ownership.
            if (IsOwner)
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
            _isDestroyed = true;
            _sm.EndGame();
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Owner only ================================
            if (Input.GetKeyDown(KeyCode.Space))
                SpawnUnitServerRpc(0);
            if (Input.GetKeyDown(KeyCode.LeftShift))
                SpawnUnitServerRpc(1);
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                var model = Model.Value;
                model.UnlockedExpansions = math.max(model.UnlockedExpansions, 1);
                model.Turrets[0] = BaseFactory.BaseTurrets[BaseFactory.Cave][0]();
                model.Turrets = (Model.Turrets.Turret[])model.Turrets.Clone();
                Model.Value = model;
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                var model = Model.Value;
                model.UnlockedExpansions = math.max(model.UnlockedExpansions, 2);
                model.Turrets[1] = BaseFactory.BaseTurrets[BaseFactory.Cave][1]();
                model.Turrets = (Model.Turrets.Turret[])model.Turrets.Clone();
                Model.Value = model;
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                var model = Model.Value;
                model.UnlockedExpansions = math.max(model.UnlockedExpansions, 3);
                model.Turrets[2] = BaseFactory.BaseTurrets[BaseFactory.Cave][2]();
                model.Turrets = (Model.Turrets.Turret[])model.Turrets.Clone();
                Model.Value = model;
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                var model = Model.Value;
                model.UnlockedExpansions = 0;
                model.Turrets = new Model.Turrets.Turret[] { default, default, default, default };
                Model.Value = model;
            }
        }

        #endregion

        #region RPCs

        [ServerRpc]
        public void SpawnUnitServerRpc(byte unitIndex, ServerRpcParams rpcParams = default)
        {
            var senderClientId = rpcParams.Receive.SenderClientId;
            var model = UnitFactory.Units[Model.Value.Level - 1][unitIndex]();
            var unit = Instantiate(unitPrefab, Vector3.up * 999f, Quaternion.identity)
                .GetComponent<Unit>(); // Spawn out of map
            unit.Model.Value = model;
            unit.GetComponent<NetworkObject>().SpawnWithOwnership(senderClientId);
        }

        [Rpc(SendTo.Owner)]
        public void DamageRpc(float damage)
        {
            if (damage <= 0 || !Model.Value.HasValue) return;
            var newModel = Model.Value;
            newModel.Hp = Mathf.Clamp(newModel.Hp - damage, 0, newModel.MaxHp);
            Model.Value = newModel;
        }

        [Rpc(SendTo.Owner)]
        public void EvolveRpc()
        {
            //TODO evolve base
        }

        #endregion

        // Reload the Base prefab
        private void LoadPrefab(string prefab)
        {
            if (BasePrefab is not null)
                Destroy(BasePrefab);
            BasePrefab = Instantiate(Resources.Load<GameObject>(prefab), transform).GetComponent<BasePrefab>();
        }
    }
}