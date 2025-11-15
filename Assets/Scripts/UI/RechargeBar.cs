using System;
using System.Collections;
using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class RechargeBar : MonoBehaviour
    {
        private static SceneManager _sm;
        [SerializeField] private Slider slider;
        private Coroutine _slideCoroutine;
        private float _remaining;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        public void Recharge(float from, float to, float duration)
        {
            if (_remaining > duration) return;
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
                yield return new WaitUntil(() => !_sm.GameManager.IsGamePaused);
                elapsed += Time.deltaTime;
                _remaining = duration - elapsed;
                var t = Mathf.Clamp01(elapsed / duration);
                slider.value = Mathf.Lerp(from, to, t);
                yield return null;
            }

            slider.value = to;
            _slideCoroutine = null;
        }
    }
}