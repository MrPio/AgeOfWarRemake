using System;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class UnitLoadingMenu : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private List<Image> slots;
        [SerializeField] private Color slotOnColor, slotOffColor;
        private readonly Queue<float> _durationQueue = new();
        private readonly Queue<Action> _callbackQueue = new();
        private float? _currentSpawnTime;
        private Action? _currentCallback;
        private float _acc;

        private void Start()
        {
            slider.value = 0;
        }

        private void FixedUpdate()
        {
            if (!_currentSpawnTime.HasValue)
            {
                if (_durationQueue.Count == 0) return;
                _currentSpawnTime = _durationQueue.Peek();
                _currentCallback = _callbackQueue.Peek();
                _acc = 0;
                // slider.gameObject.SetActive(true);
            }

            _acc += Time.fixedDeltaTime;
            if (_acc >= _currentSpawnTime.Value)
            {
                // slider.gameObject.SetActive(false);
                _currentCallback?.Invoke();
                _currentSpawnTime = null;
                _currentCallback = null;
                _acc = 0;
                slider.value = 0;
                _durationQueue.Dequeue();
                _callbackQueue.Dequeue();
                SetSlots(_durationQueue.Count);
            }
            else
                slider.value = _acc / _currentSpawnTime.Value;
        }

        private void SetSlots(int slotCount)
        {
            for (var i = 0; i < slots.Count; i++)
                slots[i].color = i < slotCount ? slotOnColor : slotOffColor;
        }

        public void Enqueue(float duration, Action callback)
        {
            if (_durationQueue.Count < slots.Count)
            {
                _durationQueue.Enqueue(duration);
                _callbackQueue.Enqueue(callback);
                SetSlots(_durationQueue.Count);
            }
        }
    }
}