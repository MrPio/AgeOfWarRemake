using System;
using UnityEngine;

namespace Partials
{
    public class Triggerable : MonoBehaviour
    {
        public Action<Collider> OnChildTriggerEnter, OnChildTriggerStay, OnChildTriggerExit;

        private void OnTriggerEnter(Collider other)
        {
            OnChildTriggerEnter?.Invoke(other);
        }

        private void OnTriggerStay(Collider other) => OnChildTriggerStay?.Invoke(other);
        private void OnTriggerExit(Collider other) => OnChildTriggerExit?.Invoke(other);
    }
}