using System;
using Managers;
using Managers.Statics;
using Unity.Netcode;
using UnityEngine;

namespace Prefabs
{
    public enum PowerupType
    {
        Coin,
        Exp,
        Special
    }

    public class Powerup : MonoBehaviour
    {
        private static SceneManager _sm;
        private static readonly int PopTrigger = Animator.StringToHash("pop");
        private Animator _animator;
        [NonSerialized] public PowerupType Type;
        [NonSerialized] public int Value;
        [SerializeField] private float delayBeforeCollision = 2f;
        private bool _collected;
        private float _spawnTime;


        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            _animator = GetComponent<Animator>();
        }
        private void Start()
        {
            _spawnTime = Time.time;
        }

        // Server & Client
        public void Init(PowerupType powerupType, int value)
        {
            if (!DataManager.IsMultiplayer)
                throw new Exception("Powerup works only in multiplayer!");

            Type = powerupType;
            Value = value;
        }

        // Server-only
        private void OnTriggerStay(Collider other)
        {
            if (Time.time - _spawnTime < delayBeforeCollision) return; // Wait before colliding
            if (_collected) return; // Collected
            if (!other.CompareTag("Unit")) return; // Only units can collect

            _animator.SetTrigger(PopTrigger);
            _collected = true;

            // Server-only
            if (NetworkManager.Singleton.IsServer)
            {
                var collectorId = other.transform.parent.GetComponent<Unit>().OwnerClientId;
                _sm.PowerupManager.Collect(this, collectorId);
            }
        }


        // Animation event
        private void OnPopEnd() => Destroy(gameObject);
    }
}