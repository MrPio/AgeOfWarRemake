using System;
using Interfaces;
using Managers;
using Model.Bases;
using Partials;
using Unity.Netcode;
using UnityEngine;

namespace Prefabs
{
    public class Turret : NetworkBehaviour
    {
        private static readonly int Idle = Animator.StringToHash("idle");
        private static readonly int Attack = Animator.StringToHash("attack");
        private SceneManager _sm;
        [SerializeField] private Transform bulletSpawnPoint;
        [SerializeField] private GameObject bulletPrefab;
        private Animator _animator;
        [NonSerialized] private Base _base;
        [NonSerialized] private Unit _target;

        #region NetworkVariables

        // Readonly
        [NonSerialized] public readonly NetworkVariable<byte> Index = new(byte.MaxValue,
            writePerm: NetworkVariableWritePermission.Server);

        // Readonly
        [NonSerialized] public readonly NetworkVariable<Model.Turrets.Turret> Model =
            new(writePerm: NetworkVariableWritePermission.Owner);

        [NonSerialized] public readonly NetworkVariable<int>
            PlayingAnimation = new(-1, writePerm: NetworkVariableWritePermission.Owner);

        private void OnPlayingAnimationChanged(int _, int newValue)
        {
            if (newValue == -1) return;
            _animator.SetTrigger(newValue);
        }

        #endregion

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        public override void OnNetworkSpawn()
        {
            _base = IsOwner ? _sm.GameManager.BaseAlly : _sm.GameManager.BaseEnemy;
            var i = Index.Value;
            _base.Turrets[i] = this;
            transform.position = _base.BasePrefab.turretsPos[i].transform.position;

            PlayingAnimation.OnValueChanged += OnPlayingAnimationChanged;
            OnPlayingAnimationChanged(-1, PlayingAnimation.Value);

            transform.localScale = new Vector3(IsOwner ? 1 : -1, 1, 1);
        }

        public override void OnNetworkDespawn()
        {
            PlayingAnimation.OnValueChanged -= OnPlayingAnimationChanged;
            _base.Turrets[Index.Value] = null;
        }

        // Host & Client
        private void FixedUpdate()
        {
            CheckCollision();

            // Rotate if there's a target
            var angle = 0f;
            if (_target is not null)
            {
                var dir = ((_target.transform.position + Vector3.up * 0.75f) - transform.position) * (IsOwner ? 1 : -1);
                angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            }

            var currentEuler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y, -angle);
        }

        // Host & Client
        private void CheckCollision()
        {
            // Get the nearest enemy within reach
            var enemies = IsOwner ? _sm.GameManager.UnitsEnemy : _sm.GameManager.UnitsAlly;
            var inFrontEnemy = enemies.Count > 0 ? enemies[0] : null;
            if (inFrontEnemy is not null &&
                (inFrontEnemy.transform.position.x - transform.position.x) * (IsOwner ? 1 : -1) <
                Model.Value.Range * TurretFactory.ExpansionsRangeMultiplier[Index.Value])
            {
                if (IsOwner)
                    PlayingAnimation.Value = Attack;

                _target = inFrontEnemy;
            }
            else
            {
                if (IsOwner)
                    PlayingAnimation.Value = Idle;

                _target = null;
            }
        }

        // Host & Client
        private void SpawnBullet()
        {
            if (_target is null) return;
            var bullet = Instantiate(bulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
            var rb = bullet.GetComponent<Rigidbody>();
            rb.linearVelocity = ((_target.transform.position + Vector3.up * 0.75f) - bulletSpawnPoint.position) *
                                Model.Value.BulletSpeed;

            if (IsOwner)
                bullet.GetComponent<Destroyable>().OnDestroyCallback = target =>
                {
                    if (target is not { IsDamageable: true }) return;
                    target.DamageRpc(Model.Value.Damage);
                };
        }
    }
}