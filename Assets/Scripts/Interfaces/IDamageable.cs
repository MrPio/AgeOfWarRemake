using Partials;
using UnityEngine;

namespace Interfaces
{
    public interface IDamageable
    {
        public Transform Transform { get; }
        public bool IsDamageable { get; }
        public void DamageRpc(float damage);
    }
}