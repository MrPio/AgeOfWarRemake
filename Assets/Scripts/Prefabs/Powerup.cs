using System;
using ExtensionFunctions;
using Managers;
using Managers.Singletons;
using Managers.Statics;
using UI;
using Unity.Netcode;
using UnityEngine;

namespace Prefabs
{
    public enum PowerupType
    {
        Coin,
        Exp
    }

    public class Powerup : NetworkBehaviour
    {
        private static SceneManager _sm;
        private static readonly int PopTrigger = Animator.StringToHash("pop");
        private Animator _animator;
        private PowerupType _powerupType;
        private bool _collected;
        private int _value;


        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            _animator = GetComponent<Animator>();
        }

        // Server & Client
        public void Init(PowerupType powerupType, int value)
        {
            if (!DataManager.IsMultiplayer)
                throw new Exception("Powerup works only in multiplayer!");

            _powerupType = powerupType;
            _value = value;
        }

        // Server-only
        private void OnTriggerStay(Collider other)
        {
            if (_collected) return; // Collected
            if (!NetworkManager.Singleton.IsServer) return; // Server-only
            if (!other.CompareTag("Unit")) return; // Only units can collect
            var collectorId = other.transform.parent.GetComponent<Unit>().OwnerClientId;
            DestroyRpc(collectorId);
            CollectServerRpc(collectorId);
            _collected = true;
        }

        // Server & Client
        [Rpc(SendTo.Everyone)]
        private void DestroyRpc(ulong collectorId)
        {
            _animator.SetTrigger(PopTrigger);
            MusicManager.Instance.PlayCollectPowerup();

            // Floating text for collector
            if (collectorId == NetworkManager.Singleton.LocalClientId)
            {
                var go = Instantiate(_sm.floatingText, _sm.canvas.transform);
                go.transform.position = _sm.cam.WorldToScreenPoint(transform.position + Vector3.up * 0.25f);
                var floatingText = go.GetComponent<FloatingText>();
                floatingText.Initialize($"+ {_value.To3Digits()}");
            }
        }

        // Server-only
        [ServerRpc(RequireOwnership = false)]
        private void CollectServerRpc(ulong collectorId)
        {
            // Add money/exp to collector's base
            var collectorBase = _sm.GameManager.OwnerId2Base(collectorId);
            var newModel = collectorBase.Model.Value;
            if (_powerupType == PowerupType.Coin)
                newModel.Money += _value;
            else if (_powerupType == PowerupType.Exp)
                newModel.Exp += _value;
            collectorBase.Model.Value = newModel;
        }
    }
}