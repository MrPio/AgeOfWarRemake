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

namespace Prefabs
{
    public class Turret : NetworkBehaviour
    {
        #region Constants

        // Animation trigger hashes
        private static readonly int IdleTrigger = Animator.StringToHash("idle");
        private static readonly int AttackTrigger = Animator.StringToHash("attack");

        #endregion

        #region References & Components

        private SceneManager _sm;
        [SerializeField] private Transform bulletSpawnPoint;
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
            transform.localScale = new Vector3(_isLeft ? 1 : -1, 1, 1);

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
            var angle = 0f;
            if (_target?.PrefabTransform is not null)
            {
                var dir = ((_target.PrefabTransform.position + Vector3.up * 0.75f) - transform.position) *
                          (_isLeft ? 1 : -1);
                angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }

            var currentEuler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, -angle);
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
        private void SpawnBullet()
        {
            if (_target is null) return;
            var bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
            var rb = bullet.GetComponent<Rigidbody>();
            rb.linearVelocity = ((_target.PrefabTransform.position + Vector3.up * 0.75f) - bulletSpawnPoint.position)
                                .normalized *
                                Model.Value.BulletSpeed;

            var destroyable = bullet.GetComponent<Destroyable>();
            destroyable.AllowedTags = new List<string> { "Base", "Unit" };
            destroyable.TargetOwner = IsOwnedByServer ? _sm.GameManager.ClientId : _sm.GameManager.HostId;
            if (!_sm.isMultiplayer && !IsBot.Value)
                destroyable.TargetOwner = 2;

            if (IsServer)
                destroyable.OnDestroyCallback = target =>
                    target.Damage(Model.Value.Damage);
        }

        private void PlaySound() =>
            _sm.musicManager.PlayTurret(_base.Model.Value.Level, Model.Value.Level);

        #endregion
    }
}