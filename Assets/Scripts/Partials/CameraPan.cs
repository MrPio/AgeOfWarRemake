using Managers;
using UnityEngine;

namespace Partials
{
    public class CameraEdgePan : MonoBehaviour
    {
        [SerializeField] private float edgeThreshold = 225f, edgeScrollSensitivity = 5f;
        [SerializeField] private float marginFromBase = 2.8f;
        [SerializeField] private float dragSensitivity = 0.01f;
        [SerializeField] private Camera cam;
        private float _currentSpeed;
        private Vector2 _boundX;
        private static SceneManager _sm;
        private bool isDragging;
        private float lastMouseX;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
        }

        private void Start()
        {
            _boundX = new Vector2(-_sm.fieldLenght / 2 + marginFromBase, _sm.fieldLenght / 2 - marginFromBase);
            var actualXBound = _boundX / (((float)Screen.width / Screen.height) / (16f / 9f));
            cam.transform.position = new Vector3(actualXBound.x, cam.transform.position.y, cam.transform.position.z);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMouseX = Input.mousePosition.x;
            }
            else if (Input.GetMouseButtonUp(0))
                isDragging = false;

            var actualXBound =
                _boundX / ((float)Screen.width / Screen.height / (16f / 9f)); // Window may resize runtime
            var camPos = cam.transform.position;
            float deltaX;

            // Drag behaviour
            if (isDragging)
            {
                deltaX = (lastMouseX - Input.mousePosition.x) * dragSensitivity;
                lastMouseX = Input.mousePosition.x;
            }
            // Edge scroll behaviour
            else
            {
                var speed = 0f;
                var mouseX = Input.mousePosition.x;
                var mouseY = Input.mousePosition.y;
                if (mouseX > Screen.width || mouseX < 0) return;
                if (mouseY > Screen.height || mouseY < 0) return;

                if (mouseX < edgeThreshold)
                {
                    var t = Mathf.Clamp(1 - mouseX / edgeThreshold, 0, 1);
                    speed = -edgeScrollSensitivity * Mathf.Pow(t, 3);
                }
                else if (mouseX > Screen.width - edgeThreshold)
                {
                    var t = Mathf.Clamp(1 - (Screen.width - mouseX) / edgeThreshold, 0, 1);
                    speed = edgeScrollSensitivity * Mathf.Pow(t, 3);
                }

                deltaX = speed * Time.deltaTime;
            }

            var x = Mathf.Clamp(camPos.x + deltaX, actualXBound.x, actualXBound.y);
            cam.transform.position = new Vector3(x, camPos.y, camPos.z);
        }
    }
}