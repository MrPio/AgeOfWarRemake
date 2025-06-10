using System;
using Model.Utils;
using Unity.Netcode;

namespace Model.Turrets
{
    public struct Turret : INetworkSerializable, IEquatable<Turret>
    {
        private const string PrefabPath = "Prefabs/Turrets/";

        // The ROF is given by the animation speed
        public float Damage, Range, BulletSpeed;
        public int Cost, SellPrice;
        public NetString Name;
        public NetString Prefab;
        public bool HasValue => !Name.Message.IsEmpty;

        public Turret(float damage, float range, float bulletSpeed, int cost, int sellPrice, NetString name,
            NetString prefabName)
        {
            Damage = damage;
            Range = range;
            BulletSpeed = bulletSpeed;
            Cost = cost;
            SellPrice = sellPrice;
            Name = name;
            Prefab = PrefabPath + prefabName;
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

        // Equality ================================================
        public static bool operator ==(Turret a, Turret b)
        {
            return a.Name.Message == b.Name.Message;
        }

        public static bool operator !=(Turret a, Turret b)
        {
            return !(a == b);
        }

        public override string ToString() =>
            $"{nameof(Damage)}: {Damage}, {nameof(Range)}: {Range}, {nameof(BulletSpeed)}: {BulletSpeed}, {nameof(Cost)}: {Cost}, {nameof(SellPrice)}: {SellPrice}, {nameof(Name)}: {Name}";

        public bool Equals(Turret other)
        {
            return Name.Message.Equals(other.Name);
        }

        public override bool Equals(object obj)
        {
            return obj is Turret other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Name.Message.GetHashCode();
        }
    }
}