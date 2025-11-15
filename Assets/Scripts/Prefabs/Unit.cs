using System;
using System.Collections;
using Interfaces;
using Managers;
using Managers.Statics;
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
        public ulong Owner => IsBot.Value ? 2 : OwnerClientId;

        public bool IsDead => State is DieState;

        // Sever-only
        public void Damage(float damage)
        {
            if (!IsServer /*|| damage <= 0*/ || !Model.Value.HasValue || State is DieState || _isDestroyed ||
                Sm.GameManager.IsGameOver) return;

            // Bot resistance
            if (!DataManager.IsMultiplayer && IsBot.Value)
                damage *= 0.9f;

            var newModel = Model.Value;
            newModel.Hp = Mathf.Clamp(newModel.Hp - damage, 0, newModel.MaxHp);
            Model.Value = newModel;
        }

        #endregion

        #region Constants

        // Unit constants (Server-only)
        private const float SpawnWalkDelay = 0.25f;
        private const float DeadDelay = 10.0f;
        private const float MinUnitsDistance = 0.2f;
        private const float MinDistanceFromEnemyBase = 1.2f;
        [NonSerialized] public float ColliderWidth;

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
        [NonSerialized] public IState State;
        [NonSerialized] public float StateChangedTime;
        private IDamageable _target;
        [NonSerialized] public UnitMovement Movement;
        private UnitPrefab _unitPrefab;
        private UnitAnimationEvents _animationNotify;
        [NonSerialized] public bool IsLeft;

        #endregion

        #region Data

        private float _spawnTime;
        private bool _spawnBlocked, _isDestroyed;
        private int _lastTrigger;

        #endregion

        #region NetVars

        // Used to activate extrapolation in client-side unit movement
        public readonly NetworkVariable<bool> IsWalking = new();
        public readonly NetworkVariable<Model.Units.Unit> Model = new();
        private readonly NetworkVariable<int> _playingAnimation = new(-1);
        private readonly NetworkVariable<NetworkObjectReference> _targetRef = new();
        public readonly NetworkVariable<bool> IsBot = new(); // Readonly

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
                    go.transform.position = Vector3.down * 999f;
                    _hpBar = go.GetComponent<HpBar>();
                    _hpBar.Target = _hpBarPoint;
                    _hpBar.Initialize();
                }

                _hpBar.SetValue(newValue.Hp, newValue.MaxHp, alsoText: false);

                // Show blood
                _unitPrefab.SpawnBlood();
            }

            _hpBar?.gameObject.SetActive(newValue.Hp < newValue.MaxHp);

            // Server-only constraint delegated to method
            if (newValue.Hp <= 0)
            {
                ChangeState(new DieState());

                // Spawn floating money
                if (!IsLeft)
                {
                    var go = Instantiate(Sm.floatingText, Sm.canvas.transform);
                    go.transform.position = Sm.cam.WorldToScreenPoint(_hpBarPoint.position + Vector3.up * 0.25f);
                    var floatingText = go.GetComponent<FloatingText>();
                    floatingText.Initialize($"+ {newValue.Revenue:N0}");
                }
            }
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
            IsLeft = IsOwner && !IsBot.Value;

            _spawnTime = Time.time;
            AllyBase = IsLeft ? Sm.GameManager.BaseAlly : Sm.GameManager.BaseEnemy;
            EnemyBase = IsLeft ? Sm.GameManager.BaseEnemy : Sm.GameManager.BaseAlly;

            // Observer pattern on unity spawn
            var allyUnits = IsLeft ? Sm.GameManager.UnitsAlly : Sm.GameManager.UnitsEnemy;
            var listeners = IsLeft ? Sm.GameManager.OnAllySpawn : Sm.GameManager.OnEnemySpawn;
            allyUnits.Add(this);
            foreach (var action in listeners)
                action.Invoke(this);

            transform.localScale = new Vector3(
                x: math.abs(transform.localScale.x) * (IsLeft ? 1 : -1),
                y: transform.localScale.y,
                z: transform.localScale.z
            ); // Rendering concern
            name =
                $"Unit {AllyBase.Model.Value.Age}-{Model.Value.Level} ({allyUnits.Count})";
            Sm.logger.Log($"Spawning {name}, IsOwner={IsOwner},  IsBot={IsBot.Value}, _isLeft={IsLeft}");

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
                State?.Update(this);
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
            if (State?.Equals(newState) == true) return;

            if (OwnerClientId == 0)
                Sm.logger.Log($"Set state of {name} to {newState}");

            // State Design Pattern
            State?.Exit(this);
            newState?.Enter(this);

            // Remove shooting lag between 2 shooting states
            var wasShooting = State is IdleState { Shooting: true } or WalkState { Shooting: true };
            var isShooting = newState is IdleState { Shooting: true } or WalkState { Shooting: true };
            if (wasShooting && isShooting)
                if (newState is WalkState walkState && State is IdleState idleStateOld)
                    walkState.LastShoot = idleStateOld.LastShoot;
                else if (newState is IdleState idleState && State is WalkState walkStateOld)
                    idleState.LastShoot = walkStateOld.LastShoot;

            // Remove attack wait if the attacking unit is different from this or if it is already in the attacking loop
            if (newState is AttackState attackState && _target is Unit targetUnit)
            {
                var isSameType = Model.Value.Age == targetUnit.Model.Value.Age &&
                                 Model.Value.Level == targetUnit.Model.Value.Level;
                var isTargetAlreadyAttacking =
                    targetUnit.State is AttackState && Time.time - targetUnit.StateChangedTime > 2f;
                if (!isSameType || isTargetAlreadyAttacking)
                    attackState.LastAttack = Time.time - Model.Value.AttackDuration / 1.35f;
            }

            State = newState;
            StateChangedTime = Time.time;
        }

        // Server-only
        /// <summary>
        /// Check if there is an ally to wait or an enemy unit/base to attack/shoot. Otherwise, walk.
        /// </summary>
        private void CheckCollision()
        {
            // TODO, when shooting to a base, the range unit must switch to new spawn enemy unit
            if (!IsServer || _unitPrefab is null || State is DieState) return;

            // The units are stored like a FIFO list in GameManager: [unit_0, unit_1 (this), unit_2]
            var allies = IsLeft ? Sm.GameManager.UnitsAlly : Sm.GameManager.UnitsEnemy;
            var enemies = IsLeft ? Sm.GameManager.UnitsEnemy : Sm.GameManager.UnitsAlly;
            var enemyBase = IsLeft ? Sm.GameManager.BaseEnemy : Sm.GameManager.BaseAlly;


            // Get the ally and enemy in front of this unit
            var oldTarget = _target;
            var thisIndex = allies.IndexOf(this);
            var inFrontAlly = thisIndex > 0 ? allies[thisIndex - 1] : null;
            var inFrontEnemy = enemies.Count > 0 ? enemies[0] : null;
            IDamageable shootTarget = Model.Value.MaxShootingDistance > 0.1
                ? (inFrontEnemy is not null && math.abs(inFrontEnemy.transform.position.x - transform.position.x) <
                    Model.Value.MaxShootingDistance)
                    ? inFrontEnemy
                    : (enemyBase is not null && math.abs(enemyBase.transform.position.x - transform.position.x) <
                        Model.Value.MaxShootingDistance)
                        ? enemyBase
                        : null
                : null;

            // The ally has precedence over the enemy, which has in turn precedence over base
            IState newState;

            // Waiting for ally
            if (inFrontAlly is not null &&
                math.abs(inFrontAlly.transform.position.x - transform.position.x) <
                ColliderWidth / 2 + inFrontAlly.ColliderWidth / 2 + MinUnitsDistance)
            {
                newState = new IdleState(shooting: shootTarget is not null);
                if (shootTarget is not null)
                    _target = shootTarget;
            }

            // Attacking the enemy
            else if (inFrontEnemy is not null &&
                     math.abs(inFrontEnemy.transform.position.x - transform.position.x) <
                     ColliderWidth / 2 + inFrontEnemy.ColliderWidth / 2)
            {
                // Don't change the target to a unit if attacking the base
                // if (_target is Base) return;

                newState = new AttackState();
                _target = inFrontEnemy;
            }

            // Attacking the base
            else if (enemyBase is not null &&
                     math.abs(enemyBase.BasePrefab.unitSpawnPointX.position.x - transform.position.x) <
                     ColliderWidth / 2 + MinDistanceFromEnemyBase)
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

            ChangeState(newState);
        }

        // Server-only
        public void PlayAnimation(int triggerHash)
        {
            if (!IsServer || _lastTrigger == DieTrigger) return;
            _lastTrigger = triggerHash; // Prevent any animation after death.
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
            ColliderWidth = _unitPrefab.GetComponent<BoxCollider>().size.x *
                            math.abs(_unitPrefab.transform.localScale.x);
            _animationNotify = _unitPrefab.GetComponent<UnitAnimationEvents>();

            #region Animation events listeners

            if (IsServer)
            {
                Sm.logger.Log($"Registering Animation OnAttack events for {name}");
                _animationNotify.OnAttack = () =>
                {
                    // If not dying
                    if ((IsLeft ? Sm.GameManager.UnitsAlly : Sm.GameManager.UnitsEnemy).Contains(this))
                        _target.Damage(Model.Value.Damage);
                    PlaySoundRpc(0, isRanged: false);
                };
            }

            // This is just a rendering concern.
            // The client can spawn the bullet on its own, what matters is the collision,
            // which is taken care of by the server.
            _animationNotify.OnShoot = () =>
            {
                // Host & Client
                if (_target?.PrefabTransform is not null)
                {
                    _unitPrefab.SpawnBullet(_target.PrefabTransform);
                    if (IsServer)
                        PlaySoundRpc(0, isRanged: true);
                }
            };

            #endregion
        }

        // Server-only
        public void DelayedDestroy()
        {
            if (!IsServer) return;
            StartCoroutine(DelayedDead());
            return;

            IEnumerator DelayedDead()
            {
                yield return new WaitForSeconds(DeadDelay);
                gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            }
        }

        #endregion

        // Host & Client
        [Rpc(SendTo.Everyone)]
        public void PlaySoundRpc(byte soundType, bool isRanged = false)
        {
            var model = Model.Value;
            switch (soundType)
            {
                case 0:
                    Sm.musicManager.PlayAttack(AllyBase.Model.Value.Age, Model.Value.Level, isRanged: isRanged);
                    break;
                case 1:
                    Sm.musicManager.PlayDie(model.Age, model.Level);
                    break;
            }
        }
    }
}