using System;
using Interfaces;
using Managers;
using Model.Bases;
using UI;
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

        private SceneManager _sm;
        private HpBar _hpBar;
        private GameObject _baseGo;
        private bool _isDestroyed;
        // private Observable _observable;
        [NonSerialized] public Transform UnitSpawnPoint;

        // public Observable Observable { get; private set; }


        #region NetworkVariables

        [NonSerialized] public readonly NetworkVariable<Model.Bases.Base> Model = new(BaseFactory.Cave(),
            writePerm: NetworkVariableWritePermission.Owner);

        #endregion

        public bool IsDamageable => !_isDestroyed;

        #region Events

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            // Observable = GetComponent<Observable>();
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

            // Only the host can despawn the destroyed base. _sm.EndGame() is called in OnNetworkDespawn()
            if (newValue.Hp <= 0)
            {
                // _observable.Notify("death");
                if (IsServer)
                    gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
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
            {
                SpawnUnitServerRpc();
            }
        }

        #endregion

        #region RPCs

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