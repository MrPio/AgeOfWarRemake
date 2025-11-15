using System;
using System.Collections;
using UnityEngine;

namespace Managers.Utils
{
    public sealed class Debouncer
    {
        private readonly MonoBehaviour _owner;
        private readonly float _delay;

        private float _nextTime;
        private Coroutine _routine;

        public Debouncer(MonoBehaviour owner, float delay = 1f)
        {
            _owner = owner;
            _delay = delay;
        }

        public void Debounce(Action action)
        {
            _nextTime = Time.realtimeSinceStartup + _delay;
            _routine ??= _owner.StartCoroutine(ScheduleRun(action));
        }

        private IEnumerator ScheduleRun(Action action)
        {
            while (true)
            {
                var wait = _nextTime - Time.realtimeSinceStartup;

                if (wait > 0f)
                {
                    yield return new WaitForSecondsRealtime(wait);
                    continue;
                }

                break;
            }

            action();
            _routine = null;
        }
    }
}