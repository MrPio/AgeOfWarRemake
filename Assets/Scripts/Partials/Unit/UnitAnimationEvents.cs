using System;
using UnityEngine;

namespace Partials.Unit
{
    public class UnitAnimationEvents : MonoBehaviour
    {
        public Action OnAttack, OnShoot, OnDie;
        private void Attack() => OnAttack?.Invoke();
        private void Shoot() => OnShoot?.Invoke();
        private void Die() => OnDie?.Invoke();
    }
}