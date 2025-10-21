using System;
using System.Linq;
using JetBrains.Annotations;
using Managers;
using Unity.Netcode;
using UnityEngine;

namespace Partials.Behaviour
{
    [Serializable]
    public class FollowableTarget
    {
        public Transform transform;
        public bool followMouse, followLastEnemy, isLeft;

        public FollowableTarget(Transform transform = null, bool followMouse = false, bool followLastEnemy = false,
                                bool isLeft = true)
        {
            this.transform = transform;
            this.followMouse = followMouse;
            this.followLastEnemy = followLastEnemy;
            this.isLeft = isLeft;
            if (transform != null && followMouse)
                throw new Exception("Cannot both have a target and follow the mouse. Choose either.");
        }
    }

    /// <summary>
    /// Adds the ability to follow a world GO, from the Canvas world.
    /// </summary>
    public class Followable : MonoBehaviour
    {
        private SceneManager _sm;
        private FollowableTarget _target;
        private float _smoothing;
        private bool _hasRectTransform, _updateAngle;

        private RectTransform _rectTransform;
        private Rigidbody _rb;

        public void Initialize(FollowableTarget target, float smoothing = 0f, bool updateAngle = false)
        {
            _target = target;
            _smoothing = smoothing;
            _updateAngle = updateAngle;
        }

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            _hasRectTransform = TryGetComponent(out _rectTransform);
            _rb = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (_target == null) return;
            var target = _target.transform;
            if (_target.followLastEnemy)
            {
                var enemies = _target.isLeft ? _sm.GameManager.UnitsEnemy : _sm.GameManager.UnitsAlly;
                target = enemies[0].transform;
            }

            // Canvas mode
            Vector3 a, b;
            if (_hasRectTransform)
            {
                a = _rectTransform.position;
                b = _target.followMouse
                    ? Input.mousePosition
                    : _sm.cam.WorldToScreenPoint(target.position);
            }
            // World mode
            else
            {
                a = transform.position;
                b = target.position + Vector3.up * 0.65f;
            }

            if (Vector3.Distance(a, b) < 0.1f) return;
            var pos = Vector3.Lerp(a, b, 1 - _smoothing);
            if (_hasRectTransform)
                _rectTransform.position = pos;
            else
                _rb.linearVelocity = (pos - a).normalized * _rb.linearVelocity.magnitude;

            // Angle
            if (_updateAngle)
            {
                var dir = pos - a;
                var angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                var currentEuler = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(currentEuler.x, currentEuler.y,
                    Mathf.LerpAngle(currentEuler.z, angle, 1 - _smoothing));
            }
        }
    }
}