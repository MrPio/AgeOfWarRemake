using UnityEngine;

namespace Debug
{
    public class UnitDebug : MonoBehaviour
    {
        private static readonly int IdleTrigger = Animator.StringToHash("idle");
        private static readonly int WalkTrigger = Animator.StringToHash("walk");
        private static readonly int AttackTrigger = Animator.StringToHash("attack");
        private static readonly int DieTrigger = Animator.StringToHash("die");
        private static readonly int Shoot = Animator.StringToHash("shoot");

        [SerializeField] private Animator animator;
        [SerializeField] private bool destroyOnSpace;

        private void DebugAnimator()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                animator.SetTrigger(IdleTrigger);

            if (Input.GetKeyDown(KeyCode.Alpha2))
                animator.SetTrigger(WalkTrigger);

            if (Input.GetKeyDown(KeyCode.Alpha3))
                animator.SetTrigger(AttackTrigger);

            if (Input.GetKeyDown(KeyCode.Alpha4))
                animator.SetTrigger(DieTrigger);
            if (Input.GetKeyDown(KeyCode.Alpha0))
                animator.SetTrigger(Shoot);

            // if (Input.GetKeyDown(KeyCode.Alpha1))
            //     animator.CrossFade(IdleTrigger,0.2f);
            // if (Input.GetKeyDown(KeyCode.Alpha2))
            //     animator.CrossFade(WalkTrigger,0.2f);
            // if (Input.GetKeyDown(KeyCode.Alpha3))
            //     animator.CrossFade(AttackTrigger,0.2f);
            // if (Input.GetKeyDown(KeyCode.Alpha4))
            //     animator.CrossFade(DieTrigger,0.2f);
        }

        private void Update()
        {
            DebugAnimator();
            if (destroyOnSpace && Input.GetKeyDown(KeyCode.Space))
                Destroy(gameObject);
        }

        private void OnTriggerStay(Collider other)
        {
            UnityEngine.Debug.Log($"{gameObject.name} STAY {other.gameObject.name}");
        }

        private void OnTriggerEnter(Collider other)
        {
            UnityEngine.Debug.Log($"{gameObject.name} ENTER {other.gameObject.name}");
        }

        private void OnTriggerExit(Collider other)
        {
            UnityEngine.Debug.Log($"{gameObject.name} EXIT {other.gameObject.name}");
        }
    }
}