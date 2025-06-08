namespace Interfaces
{
    public interface IDamageable
    {
        public bool IsActive { get; }
        public void DamageRpc(float damage);
    }
}