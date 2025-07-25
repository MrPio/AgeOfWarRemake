using System;
using UnityEngine;

namespace Partials.Unit
{
    /// <summary>
    /// Units' animation triggers.
    /// Uses HOP for genericity.
    /// </summary>
    public class UnitAnimationEvents : MonoBehaviour
    {
        public Action OnAttack, OnShoot;
        private void Attack() => OnAttack?.Invoke();
        private void Shoot() => OnShoot?.Invoke();
    }
}