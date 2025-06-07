using UnityEngine;

namespace Model.Partials
{
    public class CameraEdgePan : MonoBehaviour
    {
        [Header("Movement Settings")] [SerializeField]
        private float edgeThreshold = 200f, moveSpeed = 5f;

        [SerializeField] private Vector2 xBounds = new(-10f, 10f);
        [SerializeField] private Camera cam;
        private float _currentSpeed;

        private void Update()
        {
            var speed = 0f;
            var mouseX = Input.mousePosition.x;
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

            var x = Mathf.Clamp(camPos.x + speed * Time.deltaTime, xBounds.x, xBounds.y);
            cam.transform.position = new Vector3(x, camPos.y, camPos.z);
        }
    }
}