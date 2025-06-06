using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public enum UnitState
{
    Idling,
    Walking,
    Attacking
}

public class Unit : MonoBehaviour
{
    private static readonly int IdleTrigger = Animator.StringToHash("idle");
    private static readonly int WalkTrigger = Animator.StringToHash("walk");
    private static readonly int AttackTrigger = Animator.StringToHash("attack");

    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float spawnDelay = 0.5f;
    [SerializeField] private UnitState state = UnitState.Idling;
    [SerializeField] private bool isEnemy = false;

    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        StartCoroutine(Walk());
        return;

        IEnumerator Walk()
        {
            yield return new WaitForSeconds(spawnDelay);
            SetState(UnitState.Walking);
        }
    }

    public void SetState(UnitState newState)
    {
        state = newState;
        if (state == UnitState.Idling)
            _animator.SetTrigger(IdleTrigger);
        if (state == UnitState.Walking)
            _animator.SetTrigger(WalkTrigger);
        if (state == UnitState.Attacking)
            _animator.SetTrigger(AttackTrigger);
    }

    private void Update()
    {
        if (state == UnitState.Walking)
        {
            transform.Translate((isEnemy ? Vector3.left : Vector3.right) * (moveSpeed * Time.deltaTime));
        }
    }
}