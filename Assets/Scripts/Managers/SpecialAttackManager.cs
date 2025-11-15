using System;
using System.Collections;
using System.Collections.Generic;
using Managers.Statics;
using Model.Bases;
using Partials.Behaviour;
using Partials.Camera;
using Prefabs;
using Unity.Netcode;
using UnityEngine;
using Base = Model.Bases.Base;
using Plane = Prefabs.Plane;
using Random = UnityEngine.Random;

namespace Managers
{
    /// <summary>
    /// Note: Doesn't work if the attacker is the bot.
    /// But this is not the case in the original game.
    /// </summary>
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
        private SpecialAttack _currentSpecialModel;
        [SerializeField] private GameObject halo, plane, beam;
        private readonly List<PausableRigidbody> _bulletRBs = new();
        private bool _isStopped;

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            IsAttacking = false;
            _spawnX1 = -(_sm.fieldLenght / 2 - SpawnXMargin);
            _spawnX2 = _sm.fieldLenght / 2 - SpawnXMargin;
        }

        private void FixedUpdate()
        {
            // Handling pause (singleplayer-only)
            if (_sm.GameManager.IsGamePaused)
            {
                // Extend cooldown (in singleplayer the player is the host 0)
                if (_lastAttacks.ContainsKey(0))
                    _lastAttacks[0] += Time.fixedDeltaTime;

                // Stop RBs
                if (!_isStopped)
                {
                    _isStopped = true;
                    foreach (var rb in _bulletRBs)
                        rb.Pause();
                }
            }
            else
            {
                // Resume RBs
                if (_isStopped)
                {
                    _isStopped = false;
                    foreach (var rb in _bulletRBs)
                        rb.Resume();
                }
            }
        }

        private Base GetBaseModel(ulong attackerId) => attackerId == NetworkManager.Singleton.LocalClientId
            ? _sm.GameManager.BaseAlly.Model.Value
            : _sm.GameManager.BaseEnemy.Model.Value;

        #region Server-only

        [ServerRpc(RequireOwnership = false)]
        public void RunSpecialServerRpc(ServerRpcParams rpcParams = default)
        {
            if (IsAttacking) return;
            var attackerId = rpcParams.Receive.SenderClientId;
            var model = GetBaseModel(attackerId).Special;

            // Check cooldown requirement
            if (_lastAttacks.ContainsKey(attackerId) &&
                Time.time - _lastAttacks[attackerId] < model.Cooldown) return;
            _lastAttacks[attackerId] = Time.time;
            IsAttacking = true; // Must go after any return

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
                    if (model.Age == 4)
                        SpawnPlaneRpc(model, attackerId);
                    else if (model.Age == 5)
                        RunSatelliteRpc(model, attackerId);
                    break;
            }

            StartCoroutine(DelayedSetIsAttacking());
            return;

            IEnumerator DelayedSetIsAttacking()
            {
                yield return new WaitForSeconds(model.Duration);
                IsAttacking = false;
            }
        }

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
            }
        }

        #endregion

        #region Host & Client

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
            rb.linearVelocity = -bullet.transform.up * model.Speed;
            var pausableRb = rb.gameObject.AddComponent<PausableRigidbody>();
            _bulletRBs.Add(pausableRb);
            if (_sm.GameManager.IsGamePaused)
                pausableRb.Pause();

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
                    _bulletRBs.Remove(pausableRb);
                    if (collisionTag == "Unit")
                        _sm.musicManager.PlayHitSpecial(model.Age);
                },
                hideOnExplode: _hideOnExplode[model.Age - 1]
            );
        }

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
                // TODO: wait for pause (ignore)
                yield return new WaitForSeconds(model.Duration);
                (isAlly ? _sm.GameManager.OnAllySpawn : _sm.GameManager.OnEnemySpawn).Remove(AddHalo);
            }
        }

        [Rpc(SendTo.Everyone)]
        private void SpawnPlaneRpc(SpecialAttack model, ulong attackerId)
        {
            var isAlly = NetworkManager.Singleton.LocalClientId == attackerId;
            var planeGo = Instantiate(plane, Vector3.up * 999f, Quaternion.identity).GetComponent<Plane>();
            planeGo.Initialize(
                model: model,
                isLeft: isAlly,
                attackerId: attackerId,
                onBombSpawn: bomb =>
                {
                    var pausableRb = bomb.GetComponentInChildren<Rigidbody>().gameObject
                        .AddComponent<PausableRigidbody>();
                    _bulletRBs.Add(pausableRb);
                    if (_sm.GameManager.IsGamePaused)
                        pausableRb.Pause();
                },
                onBombExplode: bomb => { _bulletRBs.Remove(bomb.GetComponentInChildren<PausableRigidbody>()); }
            );
        }

        [Rpc(SendTo.Everyone)]
        private void RunSatelliteRpc(SpecialAttack model, ulong attackerId)
        {
            var steps = model.Duration * model.Rate;
            var xMin = _sm.fieldLenght / 2 - 1.5f;
            var stepLength = xMin * 2 / steps;
            var isAlly = NetworkManager.Singleton.LocalClientId == attackerId;
            StartCoroutine(SatelliteCoroutine());
            return;

            IEnumerator SatelliteCoroutine()
            {
                for (var i = 0; i < steps; i++)
                {
                    yield return new WaitForSeconds(1f / model.Rate);
                    var spawnPos = new Vector3(x: (-xMin + i * stepLength) * (isAlly ? 1f : -1f), 20f, 0f);
                    var beamGo = Instantiate(beam, spawnPos, Quaternion.identity);
                    var rb = beamGo.GetComponent<Rigidbody>();
                    rb.linearVelocity = Vector3.down * model.Speed;
                    var pausableRb = rb.gameObject.AddComponent<PausableRigidbody>();
                    _bulletRBs.Add(pausableRb);
                    if (_sm.GameManager.IsGamePaused)
                        pausableRb.Pause();

                    var destroyable = beamGo.GetComponent<Destroyable>();
                    destroyable.AllowedTags = new List<string> { "Unit", "Ground" };
                    destroyable.TargetOwner = !DataManager.IsMultiplayer && isAlly ? 2 :
                        attackerId == _sm.GameManager.HostId ? _sm.GameManager.ClientId : _sm.GameManager.HostId;
                    destroyable.OnDestroy = () =>
                    {
                        _sm.musicManager.PlayHitSpecial(model.Age);
                        _bulletRBs.Remove(pausableRb);
                    };

                    // Server-only
                    if (NetworkManager.Singleton.IsServer)
                        destroyable.OnDamage = target =>
                        {
                            // This avoids hitting just one enemy unit
                            var enemies = attackerId == _sm.GameManager.HostId
                                ? _sm.GameManager.UnitsEnemy
                                : _sm.GameManager.UnitsAlly;
                            for (var j = 0; j < enemies.Count; j++)
                            {
                                var maxDistance = enemies[j].ColliderWidth / 2 + model.Range;
                                if (Mathf.Abs(enemies[j].transform.position.x - beamGo.transform.position.x) <
                                    maxDistance)
                                    enemies[j].Damage(model.Damage);
                            }
                        };
                }
            }
        }

        #endregion
    }
}