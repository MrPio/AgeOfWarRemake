using System;
using System.Collections;
using Interfaces;
using Managers;
using Model.State.Unit;
using Model.Units;
using UnityEngine;
using UnityEngine.UI;
using IState = Model.State.IState;

namespace Prefabs
{
    public enum UnitState
    {
        Idling,
        Walking,
        Attacking
    }

    public class Unit : MonoBehaviour, IDamageable
    {
        private const float SpawnWalkDelay = 0.25f;

        public static readonly int IdleTrigger = Animator.StringToHash("idle");
        public static readonly int WalkTrigger = Animator.StringToHash("walk");
        public static readonly int AttackTrigger = Animator.StringToHash("attack");
        public static readonly int DieTrigger = Animator.StringToHash("die");

        public Model.Units.Unit Model;
        [NonSerialized] public Animator Animator;
        [NonSerialized] public bool IsEnemy = false; // TODO replace with ownership
        [SerializeField] private string modelName;
        [SerializeField] private Transform hpBarPoint;
        private SceneManager _sm;
        private IState _state;
        private IDamageable _target;
        private HpBar _hpBar;
        private bool _spawnBlocked = false;
        private float _spawnTime;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            Animator = GetComponent<Animator>();
        }

        public void Start()
        {
            Model = new Caveman1();
            if (IsEnemy)
                transform.localScale = new Vector3(-1, 1, 1);

            _spawnTime = Time.time;
            SetState(new IdleState());
            StartCoroutine(DelayedWalk());
            return;

            IEnumerator DelayedWalk()
            {
                yield return new WaitForSeconds(SpawnWalkDelay);
                if (!_spawnBlocked)
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

        private void OnTriggerStay(Collider other)
        {
            if (Time.time - _spawnTime > SpawnWalkDelay) return;
            if (other.CompareTag("Unit") && other.GetComponent<Unit>().IsEnemy == IsEnemy)
                _spawnBlocked = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Exit if already in attacking someone or if colliding with self.
            if (_target is not null || other.gameObject == gameObject) return;

            // If colliding with an ally or enemy unit, wait or attack respectively.
            if (other.gameObject.CompareTag("Unit") &&
                other.gameObject.TryGetComponent<Unit>(out var otherUnit))
            {
                var dx = IsEnemy
                    ? transform.position.x - otherUnit.transform.position.x
                    : otherUnit.transform.position.x - transform.position.x;
                if (dx < 0) return;
                SetState(otherUnit.IsEnemy == IsEnemy ? new IdleState() : new AttackingState());
                _target = otherUnit;
            }
            else if (other.gameObject.CompareTag("Base") &&
                     other.gameObject.TryGetComponent<Base>(out var otherBase) && otherBase.isEnemy != IsEnemy)
            {
                SetState(new AttackingState());
                _target = otherBase;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (_target is not null && other.gameObject.CompareTag("Unit") && other.gameObject != gameObject &&
                other.gameObject.TryGetComponent<Unit>(out var otherUnit))
            {
                SetState(new WalkingState());
                _target = null;
            }
        }

        public void Damage(float damage)
        {
            if (damage <= 0) return;
            Model.hp = Mathf.Clamp(Model.hp - damage, 0, Model.maxHp);
            if (_hpBar is null)
            {
                var go = Instantiate(_sm.hpBarHorizontal, _sm.canvas.transform);
                _hpBar = go.GetComponent<HpBar>();
                _hpBar.Target = hpBarPoint;
            }

            _hpBar.SetValue(Model.hp / Model.maxHp, alsoText: false);
        }
    }
}