using System;
using System.Collections.Generic;
using Interfaces;
using Managers;
using Model.Turrets;
using Partials;
using Partials.Behaviour;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Prefabs
{
    public class Turret : NetworkBehaviour
    {
        #region Constants

        // Animation trigger hashes
        private static readonly int IdleTrigger = Animator.StringToHash("idle");
        private static readonly int AttackTrigger = Animator.StringToHash("attack");
        public static readonly float UnitAimUp = 0.5f;

        #endregion

        #region References & Components

        private SceneManager _sm;
        [SerializeField] private Transform bulletSpawnPoint, bulletSecondarySpawnPoint;
        [SerializeField] private GameObject bulletPrefab;
        private Animator _animator;
        [NonSerialized] private Base _base;

        #endregion

        #region Data

        private IDamageable _target;
        private bool _isLeft;

        #endregion

        #region NetVars

        public readonly NetworkVariable<byte> Index = new(byte.MaxValue); // Readonly
        public readonly NetworkVariable<Model.Turrets.Turret> Model = new(); // Readonly
        private readonly NetworkVariable<int> _playingAnimation = new(-1);
        private readonly NetworkVariable<NetworkObjectReference> _targetRef = new();
        public readonly NetworkVariable<bool> IsBot = new(); // Readonly

        #region Listeners

        // Client-only
        private void OnPlayingAnimationChanged(int _, int newValue)
        {
            if (newValue == -1) return;
            _animator.SetTrigger(newValue);
        }

        // Client-only
        private void OnTargetRefChanged(NetworkObjectReference _, NetworkObjectReference newValue)
        {
            if (newValue.TryGet(out var target))
                _target = target.GetComponent<IDamageable>();
        }

        #endregion

        #endregion

        #region Events

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            _animator = GetComponent<Animator>();
        }

        public override void OnNetworkSpawn()
        {
            _isLeft = IsOwner && !IsBot.Value;
            _base = _isLeft ? _sm.GameManager.BaseAlly : _sm.GameManager.BaseEnemy;
            var i = Index.Value;
            _base.Turrets[i] = this;
            transform.position = _base.BasePrefab.turretsPos[i].transform.position;
            transform.localScale = new Vector3(
                x: math.abs(transform.localScale.x) * (_isLeft ? 1 : -1),
                y: transform.localScale.y,
                z: transform.localScale.z
            );

            # region NetVar listening

            // Server directly plays animation when setting the variable.
            if (!IsServer)
            {
                _playingAnimation.OnValueChanged += OnPlayingAnimationChanged;
                OnPlayingAnimationChanged(-1, _playingAnimation.Value);

                _targetRef.OnValueChanged += OnTargetRefChanged;
                OnTargetRefChanged(default, _targetRef.Value);
            }

            #endregion
        }

        public override void OnNetworkDespawn()
        {
            // NetVars unsubscription
            _playingAnimation.OnValueChanged -= OnPlayingAnimationChanged;
            _targetRef.OnValueChanged -= OnTargetRefChanged;

            _base.Turrets[Index.Value] = null;
        }

        // Host & Client
        private void FixedUpdate()
        {
            if (IsServer)
                CheckCollision();

            // Rotate if there's a target
            if (!Model.Value.IsFluid)
            {
                var angle = 0f;
                if (_target?.PrefabTransform is not null)
                {
                    var dir = ((_target.PrefabTransform.position + Vector3.up * UnitAimUp) - transform.position) *
                              (_isLeft ? 1 : -1);
                    angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                }

                var currentEuler = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, -angle);
            }
        }

        #endregion

        #region Methods

        // Server-only
        private void CheckCollision()
        {
            // Get the nearest enemy within reach
            var oldTarget = _target;
            var enemies = _isLeft ? _sm.GameManager.UnitsEnemy : _sm.GameManager.UnitsAlly;
            var inFrontEnemy = enemies.Count > 0 ? enemies[0] : null;
            if (inFrontEnemy is not null &&
                math.abs(inFrontEnemy.transform.position.x - transform.position.x) <
                Model.Value.Range * TurretFactory.ExpansionsRangeMultiplier[Index.Value])
            {
                PlayAnimation(AttackTrigger);
                _target = inFrontEnemy;
            }
            else
            {
                PlayAnimation(IdleTrigger);
                _target = null;
            }

            // Share the target with the client
            if (_target is not null && oldTarget != _target)
                _targetRef.Value = new NetworkObjectReference(_target.PrefabTransform.parent.gameObject);
        }

        // Server-only
        private void PlayAnimation(int triggerHash)
        {
            if (!IsServer) return;

            // The turret's animations are currently in loop mode.
            // There's no need to play the same animation multiple times.
            if (_playingAnimation.Value == triggerHash) return;
            _animator.SetTrigger(triggerHash);
            _playingAnimation.Value = triggerHash;
        }

        // Host & Client (Called by animation event)
        private void SpawnBullet(int idx = 0)
        {
            if (_target is null) return;
            var spawnPoint = idx == 0 ? bulletSpawnPoint : bulletSecondarySpawnPoint;
            var bullet = Instantiate(bulletPrefab, spawnPoint.position, Quaternion.identity);

            if (Model.Value.IsFluid)
            {
                var rate = 10f;
                bullet.AddComponent<Tickable>().Initialize(tickLength: 1f / rate, startDelay: 0.5f, onTick: () =>
                {
                    if (!IsServer) return;
                    for (var i = 0; i < _sm.GameManager.UnitsEnemy.Count; i++)
                    {
                        var enemy = _sm.GameManager.UnitsEnemy[i];
                        if (Mathf.Abs(bullet.transform.position.x - enemy.transform.position.x) <
                            enemy.ColliderWidth / 2f + 1f)
                        {
                            enemy.Damage(Model.Value.Damage / rate);
                            break; // damage only the first unit
                        }
                    }
                });
            }
            else
            {
                var rb = bullet.GetComponent<Rigidbody>();
                var speed = ((_target.PrefabTransform.position + Vector3.up * UnitAimUp) - spawnPoint.position)
                    .normalized * Model.Value.BulletSpeed;
                if (Model.Value is { Age: 5, Level: > 1 })
                    bullet.AddComponent<Tickable>().Initialize(tickLength: 0,
                        onTick: () => { rb.AddForce(speed, ForceMode.Acceleration); });
                else rb.linearVelocity = speed;

                if (Model.Value.IsFollow)
                    bullet.AddComponent<Followable>().Initialize(
                        target: new FollowableTarget(followLastEnemy: true, isLeft: _isLeft),
                        smoothing: 0.9f,
                        updateAngle: true
                    );
                else
                    rb.AddTorque(Random.insideUnitSphere * Random.Range(5f, 20f), ForceMode.Impulse);

                var destroyable = bullet.GetComponent<Destroyable>();
                destroyable.AllowedTags = new List<string> { "Unit", "Ground" };
                destroyable.TargetOwner = !DataManager.IsMultiplayer && !IsBot.Value ? 2 :
                    IsOwnedByServer ? _sm.GameManager.ClientId : _sm.GameManager.HostId;

                _sm.logger.Log(
                    $"Launch bullet. (TargetOwner={destroyable.TargetOwner})");

                if (IsServer)
                    destroyable.OnDamage = target => target.Damage(Model.Value.Damage);

                // Cluster explosion effect
                if (Model.Value.ClusterDamage > 0f)
                {
                    var clusterSpawn = bullet.GetComponent<ClusterSpawn>() ?? bullet.AddComponent<ClusterSpawn>();
                    clusterSpawn.Initialize(destroyable.TargetOwner.Value, Model.Value.ClusterDamage);
                }
            }
        }

        // Host & Client (Animation event)
        private void PlaySound() =>
            _sm.musicManager.PlayTurret(Model.Value.Age, Model.Value.Level);

        #endregion
    }
}