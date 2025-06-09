using Partials;

namespace Interfaces
{
    public interface IDamageable
    {
        public bool IsDamageable { get; }
        public void DamageRpc(float damage);
    }
}