using UnityEngine;

namespace Partials
{
    public class CameraZoom : MonoBehaviour
    {
        [SerializeField] private Vector3 minPos = new(-3.21f, 5.9f, -20f), maxPos = new(-5.42f, 5.6f, -11.2f);
        [SerializeField] private float minFov = 30f, maxFov = 42f;
        [SerializeField] private float scrollSpeed = 1.0f;
        [SerializeField] private float lerpSpeed = 5.0f;

        private UnityEngine.Camera _cam;
        private float _t, _targetT;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
        }

        private void Update()
        {
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            _targetT = Mathf.Clamp01(_targetT + scroll * scrollSpeed);
            if (Mathf.Abs(_t - _targetT) < 0.01f) return;

            // Smoothly interpolate toward the target position
            _t = Mathf.Lerp(_t, _targetT, Time.deltaTime * lerpSpeed);
            var newPos = Vector3.Lerp(minPos, maxPos, _t);
            newPos.x = transform.position.x;
            transform.position = newPos;
            _cam.fieldOfView = Mathf.Lerp(minFov, maxFov, _t);
        }
    }
}