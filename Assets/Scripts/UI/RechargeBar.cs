using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RechargeBar : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        private Coroutine _slideCoroutine;

        public void Recharge(float duration, float from, float to)
        {
            if (_slideCoroutine != null)
                StopCoroutine(_slideCoroutine);

            _slideCoroutine = StartCoroutine(SlideRoutine(duration, from, to));
        }

        private IEnumerator SlideRoutine(float duration, float from, float to)
        {
            var elapsed = 0f;
            slider.value = from;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                slider.value = Mathf.Lerp(from, to, t);
                yield return null;
            }

            slider.value = to;
            _slideCoroutine = null;
        }
    }
}