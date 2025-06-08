using System;
using System.Collections;
using Interfaces;
using Managers;
using Unity.Netcode;
using UnityEngine;
using Model.Bases;
using UI;
using UnityEditor;
using LogType = UI.LogType;
using EasyButtons;

namespace Prefabs
{
    public class Base : NetworkBehaviour, IDamageable
    {
        [SerializeField] private GameObject unitPrefab;
        [SerializeField] private Transform hpBarPoint;
        [SerializeField] private float posX = 11;

        private SceneManager _sm;
        private HpBar _hpBar;
        private GameObject _baseGo;
        [NonSerialized] public Transform UnitSpawnPoint;

        #region NetworkVariables

        [NonSerialized] public readonly NetworkVariable<Model.Bases.Base> Model = new(BaseFactory.Cave(),
            writePerm: NetworkVariableWritePermission.Owner);

        #endregion

        #region Events

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }


        #region NetworkVariablesChanges

        private void OnModelChanged(Model.Bases.Base value, Model.Bases.Base newValue)
        {
            if (!newValue.HasValue) return;
            _sm.logger.Log($"Obtaining {(IsOwner ? "Ally" : "Enemy")} base state");

            // Reload the unit prefab if the unit type has changed
            if (_baseGo is null || !value.HasValue || value.Prefab != newValue.Prefab)
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
        }

        #endregion

        public override void OnNetworkSpawn()
        {
            _sm.logger.Log("Spawning a Base, isOwner=" + IsOwner, LogType.NetworkSpawn);
            if (IsOwner) _sm.BaseAlly = this;
            else _sm.BaseEnemy = this;

            Model.OnValueChanged += OnModelChanged;
            OnModelChanged(default, Model.Value);

            // Initialize the base position based on ownership.
            if (IsOwner)
            {
                transform.position = new Vector3(-posX, transform.position.y, transform.position.z);
                transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                transform.position = new Vector3(posX, transform.position.y, transform.position.z);
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }

        public override void OnNetworkDespawn()
        {
            Model.OnValueChanged -= OnModelChanged;
        }

        private void Update()
        {
            if (!IsOwner) return;

            // Owner only ================================
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SpawnUnitServerRpc();
            }
        }

        #endregion

        #region RPC

        [ServerRpc]
        public void SpawnUnitServerRpc(ServerRpcParams rpcParams = default)
        {
            ulong senderClientId = rpcParams.Receive.SenderClientId;
            var unit = Instantiate(unitPrefab);
            var unitNo = unit.GetComponent<NetworkObject>();
            unitNo.SpawnWithOwnership(senderClientId);
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
            if (_baseGo is not null)
                Destroy(_baseGo);
            _baseGo = Instantiate(Resources.Load<GameObject>(prefab), transform);
            UnitSpawnPoint = _baseGo.transform.Find("spawnPoint");
        }
    }
}