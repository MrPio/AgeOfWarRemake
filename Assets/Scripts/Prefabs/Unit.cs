using System;
using Interfaces;
using Managers;
using Partials.State.Unit;
using Partials.Unit;
using UI;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using IState = Partials.State.IState;
using LogType = UI.LogType;

namespace Prefabs
{
    [RequireComponent(typeof(UnitMovement))]
    public class Unit : NetworkBehaviour, IDamageable
    {
        #region IDamageable implementation

        public Transform PrefabTransform => _isDestroyed ? null : _unitPrefab?.transform;
        public string Name => Model.Value.DisplayName;
        public ulong Owner => OwnerClientId;

        // Sever-only
        public void Damage(float damage)
        {
            if (!IsServer || damage <= 0 || !Model.Value.HasValue || _state is DieState || _isDestroyed ||
                Sm.GameManager.IsGameOver) return;
            var newModel = Model.Value;
            newModel.Hp = Mathf.Clamp(newModel.Hp - damage, 0, newModel.MaxHp);
            Model.Value = newModel;
        }

        #endregion

        #region Constants

        // Unit constants (Server-only)
        private const float SpawnWalkDelay = 0.25f;
        private float _colliderWidth;

        // Animation trigger hashes
        public static readonly int IdleTrigger = Animator.StringToHash("idle");
        public static readonly int WalkTrigger = Animator.StringToHash("walk");
        public static readonly int AttackTrigger = Animator.StringToHash("attack");
        public static readonly int ShootTrigger = Animator.StringToHash("shoot");
        public static readonly int DieTrigger = Animator.StringToHash("die");

        #endregion

        #region References & Components

        // Scene references
        [NonSerialized] public SceneManager Sm;
        [NonSerialized] public Base AllyBase, EnemyBase;

        // Components & Partials        
        private Animator _animator;
        private Transform _hpBarPoint;
        private HpBar _hpBar;
        private IState _state;
        private IDamageable _target;
        [NonSerialized] public UnitMovement Movement;
        private UnitPrefab _unitPrefab;
        private UnitAnimationEvents _animationNotify;

        #endregion

        #region Data

        private float _spawnTime;
        private bool _spawnBlocked, _isDestroyed;

        #endregion

        #region NetVars

        // Used to activate extrapolation in client-side unit movement
        public readonly NetworkVariable<bool> IsWalking = new();
        public readonly NetworkVariable<Model.Units.Unit> Model = new();
        private readonly NetworkVariable<int> _playingAnimation = new(-1);
        private readonly NetworkVariable<NetworkObjectReference> _targetRef = new();

        #region Listeners

        // Host & Client
        private void OnModelChanged(Model.Units.Unit value, Model.Units.Unit newValue)
        {
            if (!newValue.HasValue) return;

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

            // Server-only constraint delegated to method
            if (newValue.Hp <= 0)
                ChangeState(new DieState());
        }

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
            Sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            Movement = GetComponent<UnitMovement>();
        }

        public override void OnNetworkSpawn()
        {
            _spawnTime = Time.time;
            AllyBase = IsOwner ? Sm.GameManager.BaseAlly : Sm.GameManager.BaseEnemy;
            EnemyBase = IsOwner ? Sm.GameManager.BaseEnemy : Sm.GameManager.BaseAlly;
            (IsOwner ? Sm.GameManager.UnitsAlly : Sm.GameManager.UnitsEnemy).Add(this);
            transform.localScale = new Vector3(IsOwner ? 1 : -1, 1, 1); // Rendering concern

            # region NetVars listening

            Model.OnValueChanged += OnModelChanged;
            OnModelChanged(default, Model.Value);

            // Server directly plays animation when setting the variable.
            if (!IsServer)
            {
                _playingAnimation.OnValueChanged += OnPlayingAnimationChanged;
                OnPlayingAnimationChanged(-1, _playingAnimation.Value);

                _targetRef.OnValueChanged += OnTargetRefChanged;
                OnTargetRefChanged(default, _targetRef.Value);
            }

            #endregion

            // Initializing state
            ChangeState(new IdleState(shooting: false));
        }

        public override void OnNetworkDespawn()
        {
            // NetVars unsubscription
            Model.OnValueChanged -= OnModelChanged;
            _playingAnimation.OnValueChanged -= OnPlayingAnimationChanged;
            _targetRef.OnValueChanged -= OnTargetRefChanged;

            // (GameManager. ...).Remove(this); // Done by DieState::Enter()
            _isDestroyed = true;
        }

        // Server only
        private void Update()
        {
            if (IsServer && !Sm.GameManager.IsGameOver)
                _state?.Update(this);
        }

        // Server-only
        private void FixedUpdate()
        {
            if (IsServer && !Sm.GameManager.IsGameOver && Time.time - _spawnTime > SpawnWalkDelay)
                CheckCollision();
        }

        #endregion

        #region Methods

        // Server-only
        private void ChangeState(IState newState)
        {
            if (!IsServer) return;

            // Prevent re-assigning the same state
            if (_state?.Equals(newState) == true) return;

            // Remove shooting lag between 2 shooting states
            var wasShooting = _state is IdleState { Shooting: true } or WalkState { Shooting: true };
            var isShooting = newState is IdleState { Shooting: true } or WalkState { Shooting: true };
            if (wasShooting && isShooting)
                if (newState is WalkState walkState)
                    walkState.LastShoot = 0;
                else if (newState is IdleState idleState)
                    idleState.LastShoot = 0;

            // Remove attack wait if the attacking unit is different from this
            if (newState is AttackState attackState && _target.Name != Name)
                attackState.LastAttack = 0;

            // State Design Pattern
            _state?.Exit(this);
            _state = newState;
            _state?.Enter(this);
        }

        // Server-only
        /// <summary>
        /// Check if there is an ally to wait or an enemy unit/base to attack/shoot. Otherwise, walk.
        /// </summary>
        private void CheckCollision()
        {
            if (!IsServer || _unitPrefab is null || _state is DieState) return;

            // The units are stored like a FIFO list in GameManager: [unit_0, unit_1 (this), unit_2]
            var allies = IsOwner ? Sm.GameManager.UnitsAlly : Sm.GameManager.UnitsEnemy;
            var enemies = IsOwner ? Sm.GameManager.UnitsEnemy : Sm.GameManager.UnitsAlly;
            var enemyBase = IsOwner ? Sm.GameManager.BaseEnemy : Sm.GameManager.BaseAlly;


            // Get the ally and enemy in front of this unit
            var oldTarget = _target;
            var thisIndex = allies.IndexOf(this);
            var inFrontAlly = thisIndex > 0 ? allies[thisIndex - 1] : null;
            var inFrontEnemy = enemies.Count > 0 ? enemies[0] : null;
            IDamageable shootTarget =
                (inFrontEnemy is not null && math.abs(inFrontEnemy.transform.position.x - transform.position.x) <
                    Model.Value.MaxShootingDistance)
                    ? inFrontEnemy
                    : (enemyBase is not null && math.abs(enemyBase.transform.position.x - transform.position.x) <
                        Model.Value.MaxShootingDistance)
                        ? enemyBase
                        : null;

            // The ally has precedence over the enemy, which has in turn precedence over base
            IState newState;

            // Waiting for ally
            if (inFrontAlly is not null &&
                math.abs(inFrontAlly.transform.position.x - transform.position.x) < _colliderWidth)
            {
                newState = new IdleState(shooting: shootTarget is not null);
                if (shootTarget is not null)
                    _target = shootTarget;
            }

            // Attacking the enemy
            else if (inFrontEnemy is not null &&
                     math.abs(inFrontEnemy.transform.position.x - transform.position.x) < _colliderWidth)
            {
                // Don't change the target to a unit if attacking the base
                if (_target is Base) return;

                newState = new AttackState();
                _target = inFrontEnemy;
            }

            // Attacking the base
            else if (enemyBase is not null &&
                     math.abs(enemyBase.BasePrefab.unitSpawnPointX.position.x - transform.position.x) < _colliderWidth)
            {
                newState = new AttackState();
                _target = enemyBase;
            }

            // Walking
            else
            {
                newState = new WalkState(shooting: shootTarget is not null);
                if (shootTarget is not null)
                    _target = shootTarget;
            }

            // Share the target with the client
            if (_target is not null && oldTarget != _target)
                _targetRef.Value = new NetworkObjectReference(_target.PrefabTransform.parent.gameObject);

            // Server-only
            ChangeState(newState);
        }

        // Server-only
        public void PlayAnimation(int triggerHash)
        {
            if (!IsServer) return;
            _animator.SetTrigger(triggerHash);

            // Force network variable change
            _playingAnimation.Value = IdleTrigger;
            _playingAnimation.Value = triggerHash;
        }

        // Host & Client
        // Reload the Unit prefab and listen to animation events
        private void LoadPrefab(string prefab)
        {
            if (_unitPrefab is not null)
                Destroy(_unitPrefab);
            _unitPrefab = Instantiate(Resources.Load<GameObject>(prefab), transform).GetComponent<UnitPrefab>();

            // Store the unit prefab component references
            _animator = _unitPrefab.GetComponent<Animator>();
            _hpBarPoint = _unitPrefab.hpBarPoint;
            _colliderWidth = _unitPrefab.GetComponent<BoxCollider>().size.x;
            _animationNotify = _unitPrefab.GetComponent<UnitAnimationEvents>();

            #region Animation events listeners

            if (IsServer)
            {
                _animationNotify.OnAttack = () => _target.Damage(Model.Value.Damage);
                _animationNotify.OnDie = () => gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            }

            // This is just a rendering concern.
            // The client can spawn the bullet on its own, what matters is the collision,
            // which is taken care of by the server.
            _animationNotify.OnShoot = () =>
            {
                if (_target?.PrefabTransform is not null)
                    _unitPrefab.SpawnBullet(_target.PrefabTransform);
            };

            #endregion
        }

        #endregion
    }
}