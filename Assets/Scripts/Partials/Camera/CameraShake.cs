using System.Collections;
using UnityEngine;

namespace Partials.Camera
{
    public class CameraShake : MonoBehaviour
    {
        private Vector3 _originalPos;
        private Coroutine _shakeCoroutine;

        public void Shake(float duration, float strength = 0.05f)
        {
            if (_shakeCoroutine != null)
                return;

            _originalPos = transform.position;
            var curve = AnimationCurve.Linear(0, strength, duration, 0);
            _shakeCoroutine = StartCoroutine(ShakeRoutine(duration, curve));
        }

        private IEnumerator ShakeRoutine(float duration, AnimationCurve curve)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                var randomOffset = Random.insideUnitSphere * curve.Evaluate(elapsed);
                transform.position = new Vector3(
                    x: transform.position.x + randomOffset.x,
                    y: _originalPos.y + randomOffset.y,
                    z: _originalPos.z
                );
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = new Vector3(
                x: transform.position.x,
                y: _originalPos.y,
                z: _originalPos.z
            );
            _shakeCoroutine = null;
        }
    }
}