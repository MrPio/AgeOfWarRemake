using System;
using Managers;
using UnityEngine;

namespace Partials
{
    public class CameraEdgePan : MonoBehaviour
    {
        [Header("Movement Settings")] [SerializeField]
        private float edgeThreshold = 200f, moveSpeed = 5f;

        [SerializeField] private readonly float marginFromBase = 3.4f;
        [SerializeField] private Camera cam;
        private float _currentSpeed;
        private Vector2 _boundX;
        private static SceneManager _sm;
        private void Awake()
        {
            _sm=GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        private void Start()
        {
            _boundX = new Vector2(-_sm.fieldLenght / 2 + marginFromBase, _sm.fieldLenght / 2 - marginFromBase);
            var actualXBound = _boundX / (((float)Screen.width / Screen.height) / (16f / 9f));
            cam.transform.position = new Vector3(actualXBound.x, cam.transform.position.y, cam.transform.position.z);
        }

        private void Update()
        {
            var actualXBound =
                _boundX / (((float)Screen.width / Screen.height) / (16f / 9f)); // Window may resize runtime
            var speed = 0f;
            var mouseX = Input.mousePosition.x;
            var mouseY = Input.mousePosition.y;
            if (mouseX > Screen.width || mouseX < 0) return;
            if (mouseY > Screen.height || mouseY < 0) return;

            var camPos = cam.transform.position;

            if (mouseX < edgeThreshold)
            {
                var t = Mathf.Clamp(1 - mouseX / edgeThreshold, 0, 1);
                speed = -moveSpeed * Mathf.Pow(t, 3);
            }
            else if (mouseX > Screen.width - edgeThreshold)
            {
                var t = Mathf.Clamp(1 - (Screen.width - mouseX) / edgeThreshold, 0, 1);
                speed = moveSpeed * Mathf.Pow(t, 3);
            }

            var x = Mathf.Clamp(camPos.x + speed * Time.deltaTime, actualXBound.x, actualXBound.y);
            cam.transform.position = new Vector3(x, camPos.y, camPos.z);
        }
    }
}