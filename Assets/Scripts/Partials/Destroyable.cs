using UnityEngine;

namespace Partials
{
    public class Destroyable : MonoBehaviour
    {
        [SerializeField] private bool onStart = true;

        private void Start()
        {
            if (onStart)
                Destroy(gameObject);
        }
    }
}