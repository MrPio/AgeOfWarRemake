using System;
using System.Collections.Generic;
using System.Linq;
using Interfaces;
using Managers;
using Model.Bases;
using Model.Turrets;
using Model.Units;
using UI;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using LogType = UI.LogType;

namespace Prefabs
{
    public class Base : NetworkBehaviour, IDamageable
    {
        #region IDamageable implementation

        public Transform PrefabTransform => BasePrefab.transform;
        public string Name => Model.Value.Name;
        public ulong Owner => OwnerClientId;

        // Server-only
        public void Damage(float damage)
        {
            if (!IsServer || damage <= 0 || !Model.Value.HasValue || _isDestroyed) return;
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

        [NonSerialized] public readonly List<Turret> Turrets = new() { null, null, null, null };
        private bool _isDestroyed;

        #endregion

        #region NetVars

        public readonly NetworkVariable<Model.Bases.Base> Model = new(BaseFactory.Cave());

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
                    _hpBar = go.GetComponent<HpBar>();
                    _hpBar.Target = hpBarPoint;
                }

                _hpBar.SetValue(newValue.Hp, newValue.MaxHp, alsoText: true);
            }

            // Only the host can despawn the destroyed base. _sm.EndGame() is called in OnNetworkDespawn()
            if (IsServer && newValue.Hp <= 0)
            {
                // Destroy turrets
                foreach (var turret in Turrets.ToList())
                    turret?.GetComponent<NetworkObject>().Despawn(destroy: true);
                gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            }

            // Update the turret configuration (lazy)
            BasePrefab?.UpdateTurretConfiguration(newValue.UnlockedExpansions, newValue.Turrets);
        }

        #endregion

        #endregion

        #region Events

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        public override void OnNetworkSpawn()
        {
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
            // TODO: spawn explosion
            Model.OnValueChanged -= OnModelChanged;

            Destroy(_hpBar.gameObject);
            _isDestroyed = true;
            _sm.EndGame();
        }

        private void Update()
        {
            // Player Input is always owner-only
            // This is for debug purpose only.
            if (IsOwner)
            {
                // Units
                if (Input.GetKeyDown(KeyCode.Space))
                    BuyUnitServerRpc(0);
                if (Input.GetKeyDown(KeyCode.LeftShift))
                    BuyUnitServerRpc(1);

                // Turrets
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    BuyTurretServerRpc(0, 0);
                if (Input.GetKeyDown(KeyCode.Alpha2))
                    BuyTurretServerRpc(0, 1);
                if (Input.GetKeyDown(KeyCode.Alpha3))
                    BuyTurretServerRpc(0, 2);
                if (Input.GetKeyDown(KeyCode.Alpha4))
                    BuyExpansionServerRpc();
                if (Input.GetKeyDown(KeyCode.Alpha0))
                {
                    SellTurretServerRpc(0);
                    SellTurretServerRpc(1);
                    SellTurretServerRpc(2);
                    SellTurretServerRpc(3);
                }
            }
        }

        #endregion

        #region RPCs

        [ServerRpc]
        private void BuyUnitServerRpc(byte unitIndex, ServerRpcParams rpcParams = default)
        {
            var senderClientId = rpcParams.Receive.SenderClientId;
            var model = UnitFactory.Units[Model.Value.Level - 1][unitIndex]();
            var unit = Instantiate(
                unitPrefab,
                new Vector3(BasePrefab.unitSpawnPointX.position.x, 0, 0),
                Quaternion.identity
            ).GetComponent<Unit>();

            // Assigning before spawning to ensure having the value in onNetworkSpawn()
            // ...it works, but is it safe?
            unit.Model.Value = model;
            unit.GetComponent<NetworkObject>().SpawnWithOwnership(senderClientId);
            print(senderClientId);
        }

        [ServerRpc]
        private void BuyTurretServerRpc(byte expansionIndex, byte turretIndex)
        {
            var model = Model.Value;
            var turretModel = TurretFactory.Turrets[Model.Value.Level - 1][turretIndex]();

            // The requested expansion has not been bought
            if (model.UnlockedExpansions - 1 < expansionIndex) return;

            model.Turrets[expansionIndex] = turretModel;
            model.Turrets = (Model.Turrets.Turret[])model.Turrets.Clone(); // Force trigger the change
            Model.Value = model;
        }

        [ServerRpc]
        public void SellTurretServerRpc(byte expansionIndex)
        {
            // TODO money
            var model = Model.Value;
            model.Turrets[expansionIndex] = default;
            model.Turrets = (Model.Turrets.Turret[])model.Turrets.Clone(); // Force trigger the change
            Model.Value = model;
        }

        [ServerRpc]
        public void BuyExpansionServerRpc()
        {
            // TODO money
            var model = Model.Value;
            model.UnlockedExpansions = math.min(4, model.UnlockedExpansions + 1);
            Model.Value = model;
        }

        [ServerRpc]
        public void EvolveServerRpc()
        {
            //TODO evolve base
        }

        #endregion

        #region Methods

        // Host & Client
        private void LoadPrefab(string prefab)
        {
            if (BasePrefab is not null)
                Destroy(BasePrefab);
            BasePrefab = Instantiate(Resources.Load<GameObject>(prefab), transform).GetComponent<BasePrefab>();
        }

        #endregion
    }
}