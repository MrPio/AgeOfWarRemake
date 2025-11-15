using UnityEngine;

namespace Partials.Behaviour
{
    // Source - https://stackoverflow.com/a/32325207
    // Posted by JeanLuc, modified by community. See post 'Timeline' for change history
    // Retrieved 2025-11-15, License - CC BY-SA 3.0

    public class PausableRigidbody : MonoBehaviour
    {
        private Rigidbody _rb;
        private Vector3 _pausedVelocity, _pausedAngularVelocity;

        private void Awake() => _rb = GetComponent<Rigidbody>();


        public void Pause()
        {
            _pausedVelocity = _rb.linearVelocity;
            _pausedAngularVelocity = _rb.angularVelocity;
            _rb.isKinematic = true;
        }

        public void Resume()
        {
            _rb.isKinematic = false;
            _rb.linearVelocity = _pausedVelocity;
            _rb.angularVelocity = _pausedAngularVelocity;
        }
    }
}