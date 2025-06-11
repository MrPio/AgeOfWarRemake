using System;
using Interfaces;
using Managers;
using Model;
using Model.Units;
using Partials;
using Partials.State.Unit;
using UI;
using Unity.Netcode;
using UnityEngine;
using IState = Partials.State.IState;

namespace Prefabs
{
    // [RequireComponent(typeof(Observable))]
    public class Unit : NetworkBehaviour, IDamageable
    {
        private const float SpawnWalkDelay = 0.25f;
        private float _minUnitsDistanceBase = 0.75f;

        public static readonly int IdleTrigger = Animator.StringToHash("idle");
        public static readonly int WalkTrigger = Animator.StringToHash("walk");
        public static readonly int AttackTrigger = Animator.StringToHash("attack");
        public static readonly int ShootTrigger = Animator.StringToHash("shoot");
        public static readonly int DieTrigger = Animator.StringToHash("die");

        [SerializeField] private float zPos = -0.14f;
        [NonSerialized] public SceneManager Sm;
        [NonSerialized] public Animator Animator;
        private Transform _hpBarPoint;
        private IState _state;
        private IDamageable _target;
        private UnitAnimationEvents _animationNotify;
        private HpBar _hpBar;
        private float _spawnTime;
        private Base _ownerBase;
        private UnitPrefab _unitPrefab;
        private bool _spawnBlocked, _isDestroyed;

        public Transform Transform => _unitPrefab.transform;
        public bool IsDamageable => !_isDestroyed && State.Value.State is not DieState && !IsOwner;

        #region NetworkVariables

        [NonSerialized]
        public readonly NetworkVariable<Model.Units.Unit> Model = new(writePerm: NetworkVariableWritePermission.Owner);

        private void OnModelChanged(Model.Units.Unit value, Model.Units.Unit newValue)
        {
            if (!newValue.HasValue) return;
            // _sm.logger.Log($"Obtaining {(IsOwner ? "Ally" : "Enemy")} unit state, HP={newValue.Hp}");

            // Reload the unit prefab if the unit type has changed
            if (!value.HasValue || value.Prefab != newValue.Prefab)
                LoadPrefab(newValue.Prefab);

            // Update the unit's HP bar if the unit's HP has changed
            if (newValue.Hp < newValue.MaxHp)
            {
                // If it's the first time, spawn the Hp bar.
                if (_hpBar is null)
                {
                    var go = Instantiate(Sm.hpBarHorizontal, Sm.canvas.transform);
                    _hpBar = go.GetComponent<HpBar>();
                    _hpBar.Target = _hpBarPoint;
                }

                _hpBar.SetValue(newValue.Hp, newValue.MaxHp, alsoText: false);

                // Show blood
                _unitPrefab.SpawnBlood();
            }

            if (IsOwner && newValue.Hp <= 0)
                State.Value = UnitState.FromIState(new DieState());
        }

        [NonSerialized] public readonly NetworkVariable<UnitState> State =
            new(writePerm: NetworkVariableWritePermission.Owner);

        private void OnStateChanged(UnitState value, UnitState newValue)
        {
            if (!newValue.HasValue) return;
            var newState = newValue.State;
            
            // Remove shooting lag between 2 shooting states
            var wasShooting = value is { HasValue: true, IsShooting: true };
            var isShooting = newValue is { HasValue: true, IsShooting: true };
            if (wasShooting && isShooting)
                if (newState is WalkState walkState)
                    walkState.LastShoot = 0;
                else if (newState is IdleState idleState)
                    idleState.LastShoot = 0;

            // State Design Pattern
            _state?.Exit(this);
            _state = newState;
            _state?.Enter(this);
        }

        [NonSerialized]
        public readonly NetworkVariable<float> DeltaX = new(0f, writePerm: NetworkVariableWritePermission.Owner);

        private void OnDeltaXChanged(float value, float newValue)
        {
            // if (!_ownerBase.IsDamageable) return;
            var dir = IsOwner ? 1 : -1;
            // TODO interpolate this
            transform.position = new Vector3(x: _ownerBase.BasePrefab.unitSpawnPointX.position.x + dir * newValue, y: 0,
                z: zPos);
        }

        [NonSerialized] public readonly NetworkVariable<int>
            PlayingAnimation = new(-1, writePerm: NetworkVariableWritePermission.Owner);

        private void OnPlayingAnimationChanged(int _, int newValue)
        {
            // Owner directly plays animation when setting the variable.
            if (IsOwner || newValue == -1) return;
            Animator.SetTrigger(newValue);
        }

        #endregion

        #region Events

        private void Awake()
        {
            Sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            // Observable = GetComponent<Observable>();
        }

        public override void OnNetworkSpawn()
        {
            // _sm.logger.Log("Spawning a Unit, isOwner=" + IsOwner, LogType.NetworkSpawn);
            _spawnTime = Time.time;
            _ownerBase = IsOwner ? Sm.GameManager.BaseAlly : Sm.GameManager.BaseEnemy;

            Model.OnValueChanged += OnModelChanged;
            OnModelChanged(default, Model.Value);

            State.OnValueChanged += OnStateChanged;
            OnStateChanged(default, State.Value);

            DeltaX.OnValueChanged += OnDeltaXChanged;
            OnDeltaXChanged(0f, DeltaX.Value);

            PlayingAnimation.OnValueChanged += OnPlayingAnimationChanged;
            OnPlayingAnimationChanged(-1, PlayingAnimation.Value);

            if (IsOwner)
            {
                Sm.GameManager.UnitsAlly.Add(this);

                // Instantiate unit (Set in Base)
                // var model = UnitFactory.Caveman1();
                // Model.Value = model;

                // Initializing state
                State.Value = UnitState.FromIState(new IdleState(shooting: false));
                transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                Sm.GameManager.UnitsEnemy.Add(this);
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }

        public override void OnNetworkDespawn()
        {
            Model.OnValueChanged -= OnModelChanged;
            State.OnValueChanged -= OnStateChanged;
            DeltaX.OnValueChanged -= OnDeltaXChanged;
            PlayingAnimation.OnValueChanged -= OnPlayingAnimationChanged;

            if (IsOwner)
                Sm.GameManager.UnitsAlly.Remove(this);
            else
                Sm.GameManager.UnitsEnemy.Remove(this);
            _isDestroyed = true;
        }

        private void Update()
        {
            _state?.Update(this);
        }

        private void FixedUpdate()
        {
            if (Time.time - _spawnTime > SpawnWalkDelay)
                CheckCollision();
        }

        #endregion

        #region RPCs

        [Rpc(SendTo.Owner)]
        public void DamageRpc(float damage)
        {
            if (damage <= 0 || !Model.Value.HasValue) return;
            var newModel = Model.Value;
            newModel.Hp = Mathf.Clamp(newModel.Hp - damage, 0, newModel.MaxHp);
            Model.Value = newModel;
        }

        #endregion

        // Reload the Unit prefab
        private void LoadPrefab(string prefab)
        {
            if (_unitPrefab is not null)
                Destroy(_unitPrefab);
            _unitPrefab = Instantiate(Resources.Load<GameObject>(prefab), transform).GetComponent<UnitPrefab>();
            Animator = _unitPrefab.GetComponent<Animator>();
            _hpBarPoint = _unitPrefab.hpBarPoint;
            _minUnitsDistanceBase = _unitPrefab.GetComponent<BoxCollider>().size.x;
            _animationNotify = _unitPrefab.GetComponent<UnitAnimationEvents>();

            // Animations events =============================
            _animationNotify.OnAttack = () =>
            {
                if (!IsOwner || _target is not { IsDamageable: true }) return;
                _target.DamageRpc(Model.Value.Damage);
            };
            _animationNotify.OnShoot = () =>
            {
                if (_target is not null)
                    _unitPrefab.SpawnBullet(_target.Transform);
            };
            _animationNotify.OnDie = () =>
            {
                if (IsServer)
                    gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            };
        }

        // Host & Client
        /// <summary>
        /// Check if there is an ally to wait or an enemy unit/base to attack/shoot. Otherwise, walk.
        /// </summary>
        private void CheckCollision()
        {
            if (State.Value.State is DieState) return;

            // [unit_0, unit_1 (this), unit_2]
            var allies = IsOwner ? Sm.GameManager.UnitsAlly : Sm.GameManager.UnitsEnemy;
            var enemies = IsOwner ? Sm.GameManager.UnitsEnemy : Sm.GameManager.UnitsAlly;
            var enemyBase = IsOwner ? Sm.GameManager.BaseEnemy : Sm.GameManager.BaseAlly;


            // Get the ally and enemy in front of this unit
            var thisIndex = allies.IndexOf(this);
            var inFrontAlly = thisIndex > 0 ? allies[thisIndex - 1] : null;
            var inFrontEnemy = enemies.Count > 0 ? enemies[0] : null;
            IDamageable shootTarget =
                (inFrontEnemy is not null && inFrontEnemy.transform.position.x - transform.position.x <
                    Model.Value.MaxShootingDistance)
                    ? inFrontEnemy
                    : (enemyBase is not null && enemyBase.transform.position.x - transform.position.x <
                        Model.Value.MaxShootingDistance)
                        ? enemyBase
                        : null;

            // The ally has precedence over the enemy which has in turn precedence over base
            UnitState newState;

            // Waiting for ally
            if (inFrontAlly is not null &&
                inFrontAlly.transform.position.x - transform.position.x < _minUnitsDistanceBase)
            {
                newState = UnitState.FromIState(new IdleState(shooting: shootTarget is not null));
                if (shootTarget is not null)
                    _target = shootTarget;
            }

            // Attacking the enemy
            else if (inFrontEnemy is not null &&
                     inFrontEnemy.transform.position.x - transform.position.x < _minUnitsDistanceBase)
            {
                // Don't change target to a unit if attacking the base
                if (_target is Base) return;

                newState = UnitState.FromIState(new AttackState());
                _target = inFrontEnemy;
            }

            // Attacking the base
            else if (enemyBase is not null &&
                     enemyBase.BasePrefab.unitSpawnPointX.position.x - transform.position.x < _minUnitsDistanceBase)
            {
                newState = UnitState.FromIState(new AttackState());
                _target = enemyBase;
            }

            // Walking
            else
            {
                newState = UnitState.FromIState(new WalkState(shooting: shootTarget is not null));
                if (shootTarget is not null)
                    _target = shootTarget;
            }

            if (IsOwner)
                State.Value = newState;
        }
    }
}