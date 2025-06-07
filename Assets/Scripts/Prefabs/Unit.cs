using System;
using System.Collections;
using Model.State.Unit;
using Unity.VisualScripting;
using UnityEngine;
using IState = Model.State.IState;

namespace Prefabs
{
    public enum UnitState
    {
        Idling,
        Walking,
        Attacking
    }

    public class Unit : MonoBehaviour
    {
        private const float SpawnWalkDelay = 0.25f;

        public static readonly int IdleTrigger = Animator.StringToHash("idle");
        public static readonly int WalkTrigger = Animator.StringToHash("walk");
        public static readonly int AttackTrigger = Animator.StringToHash("attack");
        public static readonly int DieTrigger = Animator.StringToHash("die");

        public Model.Unit Model;
        [NonSerialized] public Animator animator;
        [NonSerialized] public bool IsEnemy = false; // TODO replace with ownership
        [SerializeField] private string modelName;
        private IState _state;
        private Unit _collider;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void Start()
        {
            Model = global::Model.Unit.FromName(modelName);
            if (IsEnemy)
                transform.localScale = new Vector3(-1, 1, 1);

            SetState(new IdleState());
            StartCoroutine(DelayedWalk());
            return;

            IEnumerator DelayedWalk()
            {
                yield return new WaitForSeconds(SpawnWalkDelay);
                SetState(new WalkingState());
            }
        }

        private void Update()
        {
            _state?.Update(this);
        }

        public void SetState(IState newState)
        {
            _state?.Exit(this);
            _state = newState;
            _state?.Enter(this);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_collider is not null || other.gameObject == gameObject) return;
            if (other.gameObject.CompareTag("Unit") &&
                other.gameObject.TryGetComponent<Unit>(out var otherUnit))
            {
                SetState(otherUnit.IsEnemy == IsEnemy ? new IdleState() : new AttackingState());
                _collider = otherUnit;
            }
            else if (other.gameObject.CompareTag("Base") &&
                     other.gameObject.TryGetComponent<Base>(out var otherBase) && otherBase.isEnemy != IsEnemy)
            {
                SetState(new AttackingState());
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_collider is not null && other.gameObject.CompareTag("Unit") && other.gameObject != gameObject &&
                other.gameObject.TryGetComponent<Unit>(out var otherUnit))
            {
                SetState(new WalkingState());
                _collider = null;
            }
        }
    }
}