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

    // [RequireComponent(typeof(Observable))]
    public class Unit : NetworkBehaviour, IDamageable
    {
        private const float SpawnWalkDelay = 0.25f;
        private const float MinUnitsDistance = 0.75f;

        public static readonly int IdleTrigger = Animator.StringToHash("idle");
        public static readonly int WalkTrigger = Animator.StringToHash("walk");
        public static readonly int AttackTrigger = Animator.StringToHash("attack");
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
        private GameObject _unitGo;
        private bool _spawnBlocked, _isDestroyed;

        public bool IsDamageable => !_isDestroyed && State.Value != (byte)UnitState.Dying;
        // public Observable Observable { get; private set; }

        #region NetworkVariables

        [NonSerialized]
        public readonly NetworkVariable<Model.Units.Unit> Model = new(writePerm: NetworkVariableWritePermission.Owner);

        [NonSerialized] public readonly NetworkVariable<byte> State =
            new(byte.MaxValue, writePerm: NetworkVariableWritePermission.Owner);

        [NonSerialized]
        public readonly NetworkVariable<float> DeltaX = new(0f, writePerm: NetworkVariableWritePermission.Owner);

        [NonSerialized] public readonly NetworkVariable<int>
            PlayingAnimation = new(-1, writePerm: NetworkVariableWritePermission.Owner);

        #endregion

        #region Events

        private void Awake()
        {
            Sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            // Observable = GetComponent<Observable>();
        }

        #region NetworkVariablesChanges

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
            }

            if (IsOwner && newValue.Hp <= 0)
                State.Value = (byte)UnitState.Dying;
        }

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

        private void OnDeltaXChanged(float value, float newValue)
        {
            if (!_ownerBase.IsDamageable) return;
            var dir = IsOwner ? 1 : -1;
            // TODO interpolate this
            transform.position = new Vector3(x: _ownerBase.UnitSpawnPointX.position.x + dir * newValue, y: 0, z: zPos);
        }

        private void OnPlayingAnimationChanged(int _, int newValue)
        {
            // Owner directly plays animation when setting the variable.
            if (IsOwner || newValue == -1) return;
            Sm.logger.Log($"{(IsOwner ? "Ally" : "Enemy")}={newValue}");
            Animator.SetTrigger(newValue);
        }

        #endregion

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
                // StartCoroutine(DelayedWalk());
                //
                // IEnumerator DelayedWalk()
                // {
                //     yield return new WaitForSeconds(SpawnWalkDelay);
                //     if (/*!_spawnBlocked*/ && State.Value != (byte)UnitState.Dying)
                //         State.Value = (byte)UnitState.Walking;
                // }

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
            // if (IsOwner)
            // {
            //     if (State.Value is (byte)UnitState.Attacking or (byte)UnitState.Idling && _target is not null &&
            //         !_target.IsDamageable)
            //     {
            //         State.Value = (byte)UnitState.Walking;
            //         _target = null;
            //     }
            // }
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
            if (inFrontAlly is not null && inFrontAlly.transform.position.x - transform.position.x < MinUnitsDistance)
                State.Value = (byte)UnitState.Idling;
            else if (inFrontEnemy is not null &&
                     inFrontEnemy.transform.position.x - transform.position.x < MinUnitsDistance)
            {
                State.Value = (byte)UnitState.Attacking;
                _target = inFrontEnemy;
            }
            else if (enemyBase is not null &&
                     enemyBase.UnitSpawnPointX.position.x - transform.position.x < MinUnitsDistance / 2)
            {
                State.Value = (byte)UnitState.Attacking;
                _target = enemyBase;
            }
            else
                State.Value = (byte)UnitState.Walking;
        }

        // Owner only
        // private void OnChildTriggerStay(Collider other)
        // {
        //     if (!IsOwner || State.Value == (byte)UnitState.Dying) return;
        //
        //     // Only consider just spawned units
        //     if (Time.time - _spawnTime > SpawnWalkDelay) return;
        //
        //     // Only if colliding with another unit
        //     if (!other.CompareTag("Unit")) return;
        //
        //     // Only if that other unit is mine but not this unit...
        //     var otherUnit = other.transform.parent.GetComponent<Unit>();
        //     if (!otherUnit.IsOwner || otherUnit == this) return;
        //
        //     // ...is in front of this unit...
        //     if (otherUnit.transform.position.x <= transform.position.x) return;
        //
        //     // ...and is not dying.
        //     if (otherUnit.State.Value is (byte)UnitState.Dying) return; // TODO: Should I remove this? 
        //
        //     // If so, block the unit from starting to walk
        //     _spawnBlocked = true;
        //
        //     // until the one in front gets out of the way.
        //     _target = otherUnit;
        //     _target.Observable.Subscribe("death", OnTargetDeath);
        // }
        //
        // // Owner only
        // private void OnChildTriggerEnter(Collider other)
        // {
        //     if (!IsOwner || State.Value == (byte)UnitState.Dying) return;
        //
        //     // Exit if already waiting/attacking someone or if colliding with self
        //     if (_target is not null || other.gameObject == _unitGo) return;
        //
        //     // If colliding with an ally or enemy unit, wait or attack respectively
        //     if (other.gameObject.CompareTag("Unit"))
        //     {
        //         var otherUnit = other.transform.parent.GetComponent<Unit>();
        //
        //         // The other unit must be in front of this unit
        //         if (otherUnit.transform.position.x <= transform.position.x) return;
        //
        //         State.Value = (byte)(otherUnit.IsOwner ? UnitState.Idling : UnitState.Attacking);
        //         _target = otherUnit;
        //         _target.Observable.Subscribe("death", OnTargetDeath);
        //     }
        //     // If colliding with the enemy base, attack it
        //     else if (other.gameObject.CompareTag("Base"))
        //     {
        //         var otherBase = other.transform.parent.GetComponent<Base>();
        //         if (otherBase.IsOwner) return;
        //         State.Value = (byte)UnitState.Attacking;
        //         _target = otherBase;
        //         _target.Observable.Subscribe("death", OnTargetDeath);
        //     }
        // }
        //
        // // Owner only
        // private void OnChildTriggerExit(Collider other)
        // {
        //     if (!IsOwner || State.Value == (byte)UnitState.Dying) return;
        //
        //     // If exiting from a unit collision, restart walking. A Base cannot call this event.
        //     if (_target is not null && other.gameObject.CompareTag("Unit") && other.gameObject != _unitGo)
        //     {
        //         State.Value = (byte)UnitState.Walking;
        //         _target.Observable.Unsubscribe("death", OnTargetDeath);
        //         _target = null;
        //     }
        // }

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
            if (_unitGo is not null)
                Destroy(_unitGo);
            _unitGo = Instantiate(Resources.Load<GameObject>(prefab), transform);
            Animator = _unitGo.GetComponent<Animator>();
            _hpBarPoint = _unitGo.transform.Find("HpBarPoint");

            // Relay trigger events
            // var triggerable = _unitGo.GetComponent<Triggerable>();
            // triggerable.OnChildTriggerEnter = OnChildTriggerEnter;
            // triggerable.OnChildTriggerExit = OnChildTriggerExit;
            // triggerable.OnChildTriggerStay = OnChildTriggerStay;

            _animationNotify = _unitGo.GetComponent<UnitAnimationEvents>();

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

        /*public void ChangeAnimation(int anim, float fade, float delay = 0f)
        {
            if (delay > 0f) StartCoroutine(EndTransition());
            else Animator.CrossFade(anim, fade);
            return;

            IEnumerator EndTransition()
            {
                if (delay - fade > 0)
                    yield return new WaitForSeconds(delay - fade);
                Animator.CrossFade(anim, fade);
            }
        }*/

        // private void OnTargetDeath()
        // {
        //     if (_target is null) return;
        //     _target = null;
        //     // The Unsubscribe is implicit by the destruction of the unit's game object
        //     State.Value = (byte)UnitState.Walking;
        // }
    }
}