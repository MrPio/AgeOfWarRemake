using UnityEngine;

namespace Interfaces
{
    public interface IDamageable
    {
        /// <summary>
        /// This is used to determine where a bullet should be fired in order to hit the target. 
        /// </summary>
        public Transform PrefabTransform { get; }

        /// <summary>
        /// The name of the model instance.
        /// Used to determine whether the attacked entity is of the same species as the attacker.
        /// </summary>
        public string Name { get; }

        ulong Owner { get; }

        public void Damage(float damage);
    }
}