using Partials.Camera;
using UnityEngine;

namespace Partials
{
    public class CameraZoom : MonoBehaviour
    {
        [SerializeField] private Vector3 maxPos = new(-5.42f, 5.6f, -11.2f);
        [SerializeField] private float maxFov = 42f;
        [SerializeField] private float maxBaseMargin = 6.06f;
        [SerializeField] private float lerpSpeed = 4.0f;
        private Vector3 _minPos;
        private float _minFov;
        private float _minBaseMargin;
        private CameraEdgePan _cameraEdgePan;
        private float _lastScroll;

        private UnityEngine.Camera _cam;
        private float _t, _targetT;

        private void Awake()
        {
            _cam = GetComponent<UnityEngine.Camera>();
            _cameraEdgePan = GetComponent<CameraEdgePan>();
            _minPos = transform.position;
            _minFov = _cam.fieldOfView;
            _minBaseMargin = _cameraEdgePan.marginFromBase;
        }

        private void Update()
        {
            if (Time.time - _lastScroll < 0.1f) return;
            var scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01)
            {
                _targetT = 1;
                _lastScroll = Time.time;
            }
            else if (scroll < -0.01)
            {
                _targetT = 0;
                _lastScroll = Time.time;
            }
        }

        public void Initialize()
        {
            _targetT = 1;
            _lastScroll = Time.time;
        }

        private void FixedUpdate()
        {
            if (Mathf.Abs(_t - _targetT) < 0.05f) return;

            // Smoothly interpolate camera zoom
            _t = Mathf.Lerp(_t, _targetT, Time.fixedDeltaTime * lerpSpeed);
            var newPos = Vector3.Lerp(_minPos, maxPos, _t);
            newPos.x = transform.position.x;
            transform.position = newPos;
            _cam.fieldOfView = Mathf.Lerp(_minFov, maxFov, _t);
            _cameraEdgePan.marginFromBase = Mathf.Lerp(_minBaseMargin, maxBaseMargin, _t);
        }
    }
}