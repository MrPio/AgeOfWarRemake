using System;
using System.Collections;
using System.Collections.Generic;
using Model.Bases;
using Partials.Behaviour;
using Partials.Camera;
using Prefabs;
using Unity.Netcode;
using UnityEngine;
using Base = Model.Bases.Base;
using Random = UnityEngine.Random;

namespace Managers
{
    public class SpecialAttackManager : NetworkBehaviour
    {
        private static SceneManager _sm;
        private const float SpawnXMargin = 2f;
        private const float SpawnY = 12f;
        private const float SpawnZ = 0f;
        private readonly List<bool> _hideOnExplode = new() { true, false };
        [NonSerialized] public bool IsAttacking;
        private float _spawnX1, _spawnX2;
        private readonly Dictionary<ulong, float> _lastAttacks = new();
        [SerializeField] private GameObject halo;
        private SpecialAttack _currentSpecialModel;

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            IsAttacking = false;
            _spawnX1 = -(_sm.fieldLenght / 2 - SpawnXMargin);
            _spawnX2 = _sm.fieldLenght / 2 - SpawnXMargin;
        }

        private Base GetBaseModel(ulong attackerId) => attackerId == NetworkManager.Singleton.LocalClientId
            ? _sm.GameManager.BaseAlly.Model.Value
            : _sm.GameManager.BaseEnemy.Model.Value;

        // Server-only
        [ServerRpc(RequireOwnership = false)]
        public void RunSpecialServerRpc(ServerRpcParams rpcParams = default)
        {
            if (IsAttacking) return;
            IsAttacking = true;
            var attackerId = rpcParams.Receive.SenderClientId;
            var model = GetBaseModel(attackerId).Special;

            // Check cooldown requirement
            if (_lastAttacks.ContainsKey(attackerId) &&
                Time.time - _lastAttacks[attackerId] < model.Cooldown) return;
            _lastAttacks[attackerId] = Time.time;

            InitializeSpecialAttackRpc(model, attackerId);
            switch (model.Type)
            {
                case SpecialType.Rain:
                    RainSpecial(model, attackerId);
                    break;
                case SpecialType.Heal:
                    SpawnHaloRpc(model, attackerId);
                    break;
                case SpecialType.Scan:
                    ScanSpecial(model, attackerId);
                    break;
            }
        }

        #region Special types

        // Server-only
        private void RainSpecial(SpecialAttack model, ulong attackerId)
        {
            StartCoroutine(SpawnRandomBullet());
            return;

            IEnumerator SpawnRandomBullet()
            {
                var start = Time.time;
                while (start + model.Duration > Time.time)
                {
                    var spawnX = Random.Range(_spawnX1, _spawnX2);
                    var angle = Random.Range(-model.MaxAngle, model.MaxAngle);
                    SpawnBulletRpc(attackerId, spawnX, angle);
                    yield return new WaitForSeconds(1 / model.Rate);
                }

                IsAttacking = false;
            }
        }

        // Server-only
        private void ScanSpecial(SpecialAttack model, ulong attackerId)
        {
        }

        #endregion

        // Host & Client
        [Rpc(SendTo.Everyone)]
        private void InitializeSpecialAttackRpc(SpecialAttack model, ulong attackerId)
        {
            _currentSpecialModel = model;

            // Camera Shake effect
            if (model.Type is SpecialType.Rain)
                _sm.cam.GetComponent<CameraShake>().Shake(model.Duration);

            // Recharge bar effect
            var cooldown = attackerId == NetworkManager.Singleton.LocalClientId ? model.Cooldown : model.Duration;
            _sm.specialAttackRechargeBar.Recharge(1, 0, cooldown);

            _sm.musicManager.PlayStartSpecial(model.Age);
        }


        // Host & Client
        [Rpc(SendTo.Everyone)]
        private void SpawnBulletRpc(ulong attackerId, float spawnX, float angle)
        {
            var model = _currentSpecialModel;

            // Spawn bullet
            var bulletPrefab = Resources.Load<GameObject>(model.Prefab);
            var bullet = Instantiate(bulletPrefab, transform);
            bullet.transform.localPosition = new Vector3(spawnX, SpawnY, SpawnZ);
            bullet.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Add initial force
            var rb = bullet.GetComponentInChildren<Rigidbody>();
            rb.AddForce(-bullet.transform.up * model.Speed);

            // Add explodable behaviour
            var explodable = bullet.GetComponentInChildren<Explodable>();
            var explosionPrefab = Resources.Load<GameObject>(model.ExplosionPrefab);
            explodable.Initialize(
                targets: new List<string> { "Ground", "Unit", "Base" },
                range: model.Range,
                damage: model.Damage,
                attackerId: attackerId,
                explosion: explosionPrefab,
                onExplode: collisionTag =>
                {
                    if (collisionTag == "Unit")
                        _sm.musicManager.PlayHitSpecial(model.Age);
                },
                hideOnExplode: _hideOnExplode[model.Age - 1]
            );
        }

        // Host & Client
        [Rpc(SendTo.Everyone)]
        private void SpawnHaloRpc(SpecialAttack model, ulong attackerId)
        {
            var isAlly = NetworkManager.Singleton.LocalClientId == attackerId;
            foreach (var unit in isAlly ? _sm.GameManager.UnitsAlly : _sm.GameManager.UnitsEnemy)
                AddHalo(unit);
            (isAlly ? _sm.GameManager.OnAllySpawn : _sm.GameManager.OnEnemySpawn).Add(AddHalo);
            StartCoroutine(RemoveListener());
            return;

            void AddHalo(Unit unit)
            {
                var elapsed = Time.time - _lastAttacks[attackerId];
                var haloGo = Instantiate(halo, unit.transform);
                haloGo.AddComponent<Destroyable>().Initialize(lifespan: model.Duration - elapsed);

                // Server-only
                if (NetworkManager.Singleton.IsServer)
                    haloGo.AddComponent<Tickable>().Initialize(
                        tickLength: 1f / model.Rate,
                        // Note: the damage for special 3 is negative
                        onTick: () => { unit.Damage(model.Damage / model.Rate); }
                    );
            }

            IEnumerator RemoveListener()
            {
                yield return new WaitForSeconds(model.Duration);
                (isAlly ? _sm.GameManager.OnAllySpawn : _sm.GameManager.OnEnemySpawn).Remove(AddHalo);
            }
        }
    }
}