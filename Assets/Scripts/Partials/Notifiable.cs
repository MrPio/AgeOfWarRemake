using System;
using UnityEngine;

namespace Partials
{
    public class Notifiable : MonoBehaviour
    {
        public Action OnNotify;
        private void Notify() => OnNotify?.Invoke();
    }
}