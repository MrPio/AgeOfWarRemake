using System;
using Managers;
using UnityEngine;

namespace Partials.Behaviour
{
    [Serializable]
    public class FollowableTarget
    {
        public Transform transform;
        public bool followMouse;

        public FollowableTarget(Transform transform = null, bool followMouse = false)
        {
            this.transform = transform;
            this.followMouse = followMouse;
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
        [SerializeField] public FollowableTarget target;

        private RectTransform _rectTransform;

        private void Start()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (target == null) return;
            var screenPos = target.followMouse
                ? Input.mousePosition
                : _sm.cam.WorldToScreenPoint(target.transform.position);
            _rectTransform.position = screenPos;
        }
    }
}