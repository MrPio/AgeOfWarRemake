using System;
using Interfaces;
using Managers;
using Model.Units;
using Partials;
using Partials.State.Unit;
using UI;
using Unity.Netcode;
using UnityEngine;
using IState = Partials.State.IState;

namespace Prefabs
{
    public enum UnitState
    {
        Idling,
        Walking,
        Attacking,
        Dying,
    }

    // [RequireComponent(typeof(Observable))]
    public class Unit : NetworkBehaviour, IDamageable
    {
        private const float SpawnWalkDelay = 0.25f;
        private float _minUnitsDistance = 0.75f;

        public static readonly int IdleTrigger = Animator.StringToHash("idle");
        public static readonly int WalkTrigger = Animator.StringToHash("walk");
        public static readonly int AttackTrigger = Animator.StringToHash("attack");
        public static readonly int DieTrigger = Animator.StringToHash("die");

        [SerializeField] private float zPos = -0.14f;
        [NonSerialized] public SceneManager Sm;
        [NonSerialized] public Animator Animator;
        [NonSerialized] public float LastTargetTime;
        private Transform _hpBarPoint;
        private IState _state;
        private IDamageable _target;
        private UnitAnimationEvents _animationNotify;
        private HpBar _hpBar;
        private float _spawnTime;
        private Base _ownerBase;
        private UnitPrefab _unitPrefab;
        private bool _spawnBlocked, _isDestroyed;

        public bool IsDamageable => !_isDestroyed && State.Value != (byte)UnitState.Dying && !IsOwner;
        // public Observable Observable { get; private set; }

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
                State.Value = (byte)UnitState.Dying;
        }

        [NonSerialized] public readonly NetworkVariable<byte> State =
            new(byte.MaxValue, writePerm: NetworkVariableWritePermission.Owner);

        private void OnStateChanged(byte value, byte newValue)
        {
            if (newValue == byte.MaxValue) return;
            // _sm.logger.Log($"{(IsOwner ? "Ally" : "Enemy")}={state.GetType()}"); OK, PASSED

            IState state = (UnitState)newValue switch
            {
                UnitState.Idling => new IdleState(),
                UnitState.Attacking => new AttackingState(),
                UnitState.Walking => new WalkingState(),
                UnitState.Dying => new DyingState(),
                _ => throw new ArgumentOutOfRangeException(nameof(newValue), newValue, null)
            };

            // Follows the State Design Pattern
            _state?.Exit(this);
            _state = state;
            _state?.Enter(this);
        }

        [NonSerialized]
        public readonly NetworkVariable<float> DeltaX = new(0f, writePerm: NetworkVariableWritePermission.Owner);

        private void OnDeltaXChanged(float value, float newValue)
        {
            if (!_ownerBase.IsDamageable) return;
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
            Sm.logger.Log($"{(IsOwner ? "Ally" : "Enemy")}={newValue}");
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
            OnStateChanged(byte.MaxValue, State.Value);

            DeltaX.OnValueChanged += OnDeltaXChanged;
            OnDeltaXChanged(0f, DeltaX.Value);

            PlayingAnimation.OnValueChanged += OnPlayingAnimationChanged;
            OnPlayingAnimationChanged(-1, PlayingAnimation.Value);

            if (IsOwner)
            {
                Sm.GameManager.UnitsAlly.Add(this);

                // Instantiate unit
                var model = UnitFactory.Caveman1();
                Model.Value = model;

                // Initializing state
                State.Value = (byte)UnitState.Idling;
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

        // Owner only
        private void CheckCollision()
        {
            if (!IsOwner || State.Value == (byte)UnitState.Dying) return;

            // [unit_0, unit_1 (this), unit_2]
            var allies = Sm.GameManager.UnitsAlly;
            var enemies = Sm.GameManager.UnitsEnemy;

            // Get the ally and enemy in front of this unit
            var thisIndex = Sm.GameManager.UnitsAlly.IndexOf(this);
            var inFrontAlly = thisIndex > 0 ? allies[thisIndex - 1] : null;
            var inFrontEnemy = enemies.Count > 0 ? enemies[0] : null;
            var enemyBase = Sm.GameManager.BaseEnemy;

            // The ally has precedence over the enemy which has in turn precedence over base
            if (inFrontAlly is not null && inFrontAlly.transform.position.x - transform.position.x < _minUnitsDistance)
                State.Value = (byte)UnitState.Idling;
            else if (inFrontEnemy is not null &&
                     inFrontEnemy.transform.position.x - transform.position.x < _minUnitsDistance)
            {
                // Don't change target to a unit if attacking the base
                if (_target is Base) return;

                State.Value = (byte)UnitState.Attacking;
                _target = inFrontEnemy;
            }
            else if (enemyBase is not null &&
                     enemyBase.BasePrefab.unitSpawnPointX.position.x - transform.position.x < _minUnitsDistance / 2)
            {
                State.Value = (byte)UnitState.Attacking;
                _target = enemyBase;
            }
            else
                State.Value = (byte)UnitState.Walking;
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
            _minUnitsDistance = _unitPrefab.GetComponent<BoxCollider>().size.x;
            _animationNotify = _unitPrefab.GetComponent<UnitAnimationEvents>();

            // Animations events =============================
            _animationNotify.OnAttack = () =>
            {
                if (!IsOwner || _target is not { IsDamageable: true }) return;
                _target.DamageRpc(Model.Value.Damage);
            };
            _animationNotify.OnDie = () =>
            {
                if (IsServer)
                    gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            };
        }
    }
}