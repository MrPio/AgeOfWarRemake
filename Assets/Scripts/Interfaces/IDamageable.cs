using Partials;

namespace Interfaces
{
    public interface IDamageable
    {
        public bool IsDamageable { get; }
        Observable Observable { get; }
        public void DamageRpc(float damage);
    }
}