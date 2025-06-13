using UnityEngine;

namespace Partials.Camera
{
    public class RotateSkybox : MonoBehaviour
    {
        private static readonly int Rotation = Shader.PropertyToID("_Rotation");
        [SerializeField] private float rotationSpeed = 0.4f;

        private void Update()
        {
            RenderSettings.skybox.SetFloat(Rotation, Time.time * rotationSpeed);
        }
    }
}