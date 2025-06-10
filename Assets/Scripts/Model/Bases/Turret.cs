using Model.Utils;
using Unity.Netcode;

namespace Model.Bases
{
    public struct Turret : INetworkSerializable
    {
        // private const string PrefabPath = "Prefabs/Turrets/"; Not needed. The base prefab holds the references.

        // The ROF is given by the animation speed
        public float Damage, Range, BulletSpeed;
        public int Cost, SellPrice;
        public NetString Name;
        // public NetString Prefab;

        public Turret(float damage, float range, float bulletSpeed, int cost, int sellPrice, NetString name)
        {
            Damage = damage;
            Range = range;
            BulletSpeed = bulletSpeed;
            Cost = cost;
            SellPrice = sellPrice;
            Name = name;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref Range);
            serializer.SerializeValue(ref BulletSpeed);
            serializer.SerializeValue(ref Cost);
            serializer.SerializeValue(ref SellPrice);
            serializer.SerializeValue(ref Name);
        }

        public override string ToString() =>
            $"{nameof(Damage)}: {Damage}, {nameof(Range)}: {Range}, {nameof(BulletSpeed)}: {BulletSpeed}, {nameof(Cost)}: {Cost}, {nameof(SellPrice)}: {SellPrice}, {nameof(Name)}: {Name}";
    }
}