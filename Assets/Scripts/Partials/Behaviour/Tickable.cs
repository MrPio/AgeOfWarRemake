using System;
using UnityEngine;

namespace Partials.Behaviour
{
    public class Tickable : MonoBehaviour
    {
        private float? _tickLength, _duration, _startDelay;
        private float _lastTick;
        private Action _onTick;


        private void FixedUpdate()
        {
            // Check duration limit
            if (_duration != null && Time.time - _lastTick >= _duration.Value)
                return;

            // Check tick
            if (_tickLength != null && Time.time - _lastTick >= _tickLength.Value)
            {
                _lastTick = Time.time;
                _onTick?.Invoke();
            }
        }

        public void Initialize(float? tickLength = null, float startDelay = 0, float? duration = null,
                               Action onTick = null)
        {
            _tickLength = tickLength;
            _duration = duration;
            _onTick = onTick;
            _lastTick = Time.time + startDelay;
        }
    }
}