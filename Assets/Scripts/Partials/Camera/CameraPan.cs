using Managers;
using Unity.Mathematics;
using UnityEngine;

namespace Partials.Camera
{
    public class CameraEdgePan : MonoBehaviour
    {
        [SerializeField] private float edgeThreshold = 225f, edgeScrollSensitivity = 5f;
        [SerializeField] public float marginFromBase = 2.8f;
        [SerializeField] private float dragSensitivity = 0.01f, skyRotationFactor = 0.25f;
        [SerializeField] private float allowedY = 0.75f;
        [SerializeField] private UnityEngine.Camera cam;
        [SerializeField] private Texture2D cursorArrow, cursorHand;
        private static SceneManager _sm;
        private float _currentSpeed, _lastMouseX;
        private Vector2 _boundX;
        private bool _isDragging;
        private RotateSkybox _rotateSkybox;
        public bool BlockPan = false;

        private void Awake()
        {
            _sm = GameObject.FindWithTag("SceneManager").GetComponent<SceneManager>();
            _rotateSkybox = GetComponent<RotateSkybox>();
        }

        private void Start()
        {
            _boundX = new Vector2(-_sm.fieldLenght / 2 + marginFromBase, _sm.fieldLenght / 2 - marginFromBase);
            var actualXBound = _boundX / (((float)Screen.width / Screen.height) / (16f / 9f));
            cam.transform.position = new Vector3(actualXBound.x, cam.transform.position.y, cam.transform.position.z);
            Cursor.SetCursor(cursorArrow, Vector2.zero, CursorMode.Auto);
        }

        private void LateUpdate()
        {
            if (Input.GetMouseButtonDown(0) && IsPanAllowed())
            {
                _isDragging = true;
                _lastMouseX = Input.mousePosition.x;
                Cursor.SetCursor(cursorHand, Vector2.zero, CursorMode.Auto);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
                Cursor.SetCursor(cursorArrow, Vector2.zero, CursorMode.Auto);
            }

            _boundX = new Vector2(-_sm.fieldLenght / 2 + marginFromBase, _sm.fieldLenght / 2 - marginFromBase);
            var actualXBound =
                _boundX / ((float)Screen.width / Screen.height / (16f / 9f)); // Window may resize runtime
            var camPos = cam.transform.position;
            float deltaX = 0;

            // Drag behaviour
            if (_isDragging)
            {
                deltaX = (_lastMouseX - Input.mousePosition.x) * dragSensitivity;
                _lastMouseX = Input.mousePosition.x;
            }
            // Edge scroll behaviour
            else if (IsPanAllowed())
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
            _rotateSkybox.RotationAcc = x * skyRotationFactor;
            cam.transform.position = new Vector3(x, camPos.y, camPos.z);
        }

        private bool IsPanAllowed()
        {
            var threshold = Screen.height * allowedY;
            return !BlockPan && Input.mousePosition.y < threshold;
        }
    }
}