using System;
using UnityEngine;

namespace Partials
{
    public class UnitAnimationEvents : MonoBehaviour
    {
        public Action OnAttack, OnDie;
        private void Attack() => OnAttack?.Invoke();
        private void Die() => OnDie?.Invoke();
    }
}