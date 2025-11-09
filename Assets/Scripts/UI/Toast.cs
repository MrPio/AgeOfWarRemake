using System.Collections;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(Animator))]
    public sealed class Toast : MonoBehaviour
    {
        private static readonly int ShowTrigger = Animator.StringToHash("show");
        private static readonly int HideTrigger = Animator.StringToHash("hide");

        [SerializeField] private Image panel;
        [SerializeField] private TextMeshProUGUI message;
        [SerializeField] private float duration = 3f;

        private Animator _animator;
        private Coroutine _hideRoutine;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void Initialize(string text, Color color)
        {
            panel.color = color;
            message.text = text;
            _animator.SetTrigger(ShowTrigger);

            if (_hideRoutine != null)
                StopCoroutine(_hideRoutine);

            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(duration);
            _animator.SetTrigger(HideTrigger);
        }

        // Call from animation event
        public void OnHideComplete() => Destroy(gameObject);
    }
}