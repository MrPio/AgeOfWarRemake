using System;
using System.Collections;
using Interfaces;
using Managers;
using Model.Units;
using Partials;
using Partials.State.Unit;
using UI;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using IState = Partials.State.IState;
using LogType = UI.LogType;

namespace Prefabs
{
    public enum UnitState
    {
        Idling,
        Walking,
        Attacking,
        Dying,
    }

    public class Unit : NetworkBehaviour, IDamageable
    {
        private const float SpawnWalkDelay = 0.25f;

        public static readonly int IdleTrigger = Animator.StringToHash("idle");
        public static readonly int WalkTrigger = Animator.StringToHash("walk");
        public static readonly int AttackTrigger = Animator.StringToHash("attack");
        public static readonly int DieTrigger = Animator.StringToHash("die");

        [NonSerialized] public Animator Animator;
        private Transform _hpBarPoint;
        private SceneManager _sm;
        private IState _state;
        private IDamageable _target;
        private UnitAnimationEvents _animationNotify;
        private HpBar _hpBar;
        private float _spawnTime;
        private Base _ownerBase;
        private GameObject _unitGo;
        private bool _spawnBlocked, _isDestroyed;

        public bool IsActive => !_isDestroyed || State.Value == (byte)UnitState.Dying;

        #region NetworkVariables

        [NonSerialized]
        public readonly NetworkVariable<Model.Units.Unit> Model = new(writePerm: NetworkVariableWritePermission.Owner);

        [NonSerialized] public readonly NetworkVariable<byte> State =
            new(byte.MaxValue, writePerm: NetworkVariableWritePermission.Owner);

        [NonSerialized]
        public readonly NetworkVariable<float> DeltaX = new(0f, writePerm: NetworkVariableWritePermission.Owner);

        #endregion


        #region Events

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        #region NetworkVariablesChanges

        private void OnModelChanged(Model.Units.Unit value, Model.Units.Unit newValue)
        {
            if (!newValue.HasValue) return;
            _sm.logger.Log($"Obtaining {(IsOwner ? "Ally" : "Enemy")} unit state, HP={newValue.Hp}");

            // Reload the unit prefab if the unit type has changed
            if (!value.HasValue || value.Prefab != newValue.Prefab)
                LoadPrefab(newValue.Prefab);

            // Update the unit's HP bar if the unit's HP has changed
            if (newValue.Hp < newValue.MaxHp)
            {
                // If it's the first time, spawn the Hp bar.
                if (_hpBar is null)
                {
                    var go = Instantiate(_sm.hpBarHorizontal, _sm.canvas.transform);
                    _hpBar = go.GetComponent<HpBar>();
                    _hpBar.Target = _hpBarPoint;
                }

                _hpBar.SetValue(newValue.Hp, newValue.MaxHp, alsoText: false);
            }

            if (IsOwner && newValue.Hp <= 0)
                State.Value = (byte)UnitState.Dying;
        }

        private void OnStateChanged(byte value, byte newValue)
        {
            if (newValue == byte.MaxValue) return;
            _sm.logger.Log($"Obtaining {(IsOwner ? "Ally" : "Enemy")} unit IState={newValue}");

            IState state = (UnitState)newValue switch
            {
                UnitState.Idling => new IdleState(),
                UnitState.Attacking => new AttackingState(),
                UnitState.Walking => new WalkingState(),
                UnitState.Dying => new DyingState(),
                _ => throw new ArgumentOutOfRangeException(nameof(newValue), newValue, null)
            };
            SetState(state);
        }

        private void OnDeltaXChanged(float value, float newValue)
        {
            var dir = IsOwner ? Vector3.right : Vector3.left;
            transform.position = _ownerBase.UnitSpawnPoint.position + dir * newValue;
        }

        #endregion

        public override void OnNetworkSpawn()
        {
            _sm.logger.Log("Spawning a Unit, isOwner=" + IsOwner, LogType.NetworkSpawn);
            _spawnTime = Time.time;
            _ownerBase = IsOwner ? _sm.BaseAlly : _sm.BaseEnemy;

            Model.OnValueChanged += OnModelChanged;
            OnModelChanged(default, Model.Value);

            State.OnValueChanged += OnStateChanged;
            OnStateChanged(byte.MaxValue, State.Value);

            DeltaX.OnValueChanged += OnDeltaXChanged;
            OnDeltaXChanged(0f, DeltaX.Value);

            if (IsOwner)
            {
                _sm.GameManager.UnitsAlly.Add(this);

                // Instantiate unit
                var model = UnitFactory.Caveman1();
                Model.Value = model;

                // Initializing state
                State.Value = (byte)UnitState.Idling;
                StartCoroutine(DelayedWalk());

                IEnumerator DelayedWalk()
                {
                    yield return new WaitForSeconds(SpawnWalkDelay);
                    if (!_spawnBlocked && State.Value != (byte)UnitState.Dying)
                        State.Value = (byte)UnitState.Walking;
                }

                transform.localScale = new Vector3(1, 1, 1);
            }
            else
            {
                _sm.GameManager.UnitsEnemy.Add(this);
                transform.localScale = new Vector3(-1, 1, 1);
            }
        }

        public override void OnNetworkDespawn()
        {
            Model.OnValueChanged -= OnModelChanged;
            State.OnValueChanged -= OnStateChanged;
            DeltaX.OnValueChanged -= OnDeltaXChanged;

            if (IsOwner)
                _sm.GameManager.UnitsAlly.Remove(this);
            else
                _sm.GameManager.UnitsEnemy.Remove(this);
            _isDestroyed = true;
        }


        private void Update()
        {
            _state?.Update(this);
            if (IsOwner)
            {
                if (State.Value == (byte)UnitState.Attacking && _target is not null && !_target.IsActive)
                {
                    State.Value = (byte)UnitState.Walking;
                    _target = null;
                }
            }
        }

        // Owner only
        private void OnChildTriggerStay(Collider other)
        {
            if (!IsOwner || State.Value == (byte)UnitState.Dying) return;
            if (Time.time - _spawnTime > SpawnWalkDelay) return;
            if (other.CompareTag("Unit") && other.transform.parent.TryGetComponent<Unit>(out var unit) && unit.IsOwner)
            {
                var dx = other.transform.position.x - transform.position.x;
                if (dx <= 0.01) return;
                if (unit.IsOwner && unit.State.Value == (byte)UnitState.Walking) return;
                // TODO: when dying remove collider
                _spawnBlocked = true;
            }
        }

        // Owner only
        private void OnChildTriggerEnter(Collider other)
        {
            print($"{other.name} {other.name} {IsOwner}");
            if (!IsOwner || State.Value == (byte)UnitState.Dying) return;

            // Exit if already in attacking someone or if colliding with self
            if (_target is not null || other.gameObject == _unitGo) return;

            // If colliding with an ally or enemy unit, wait or attack respectively
            if (other.gameObject.CompareTag("Unit") &&
                other.transform.parent.TryGetComponent<Unit>(out var otherUnit))
            {
                var dx = otherUnit.transform.position.x - transform.position.x;
                if (dx <= 0.01) return;
                State.Value = (byte)(otherUnit.IsOwner ? UnitState.Idling : UnitState.Attacking);
                _target = otherUnit;
            }
            // If colliding with the enemy base, attack it
            else if (other.gameObject.CompareTag("Base") &&
                     other.transform.parent.TryGetComponent<Base>(out var otherBase) && !otherBase.IsOwner)
            {
                State.Value = (byte)UnitState.Attacking;
                _target = otherBase;
            }
        }

        // Owner only
        private void OnChildTriggerExit(Collider other)
        {
            if (!IsOwner || State.Value == (byte)UnitState.Dying) return;

            // If exiting from a unit collision, re-begin walking.
            if (_target is not null && other.gameObject.CompareTag("Unit") && other.gameObject != _unitGo &&
                other.transform.parent.TryGetComponent<Unit>(out var otherUnit))
            {
                State.Value = (byte)UnitState.Walking;
                _target = null;
            }
        }

        #endregion

        #region RPC

        [Rpc(SendTo.Owner)]
        public void DamageRpc(float damage)
        {
            if (damage <= 0 || !Model.Value.HasValue) return;
            var newModel = Model.Value;
            newModel.Hp = Mathf.Clamp(newModel.Hp - damage, 0, newModel.MaxHp);
            Model.Value = newModel;
        }

        #endregion

        // Follows the State Design Pattern
        private void SetState(IState newState)
        {
            _state?.Exit(this);
            _state = newState;
            _state?.Enter(this);
        }

        // Reload the Unit prefab
        private void LoadPrefab(string prefab)
        {
            if (_unitGo is not null)
                Destroy(_unitGo);
            _unitGo = Instantiate(Resources.Load<GameObject>(prefab), transform);
            Animator = _unitGo.GetComponent<Animator>();
            _hpBarPoint = _unitGo.transform.Find("HpBarPoint");

            // Relay trigger events
            var triggerable = _unitGo.GetComponent<Triggerable>();
            triggerable.OnChildTriggerEnter = OnChildTriggerEnter;
            triggerable.OnChildTriggerExit = OnChildTriggerExit;
            triggerable.OnChildTriggerStay = OnChildTriggerStay;

            _animationNotify = _unitGo.GetComponent<UnitAnimationEvents>();

            // Relay attack event
            _animationNotify.OnAttack = () =>
            {
                if (!IsOwner) return;
                _target.DamageRpc(Model.Value.Damage);
            };

            // Relay die event
            _animationNotify.OnDie = () =>
            {
                (IsOwner ? _sm.GameManager.UnitsAlly : _sm.GameManager.UnitsEnemy).Remove(this);
                if (IsServer)
                    gameObject.GetComponent<NetworkObject>().Despawn(destroy: true);
            };
        }
    }
}