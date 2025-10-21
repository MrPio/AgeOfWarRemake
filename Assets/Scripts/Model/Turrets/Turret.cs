using System;
using Interfaces;
using Model.Utils;
using Unity.Netcode;

namespace Model.Turrets
{
    public struct Turret : INetworkSerializable, IEquatable<Turret>, INullable
    {
        private const string PrefabPath = "Prefabs/Turrets/";
        private const float DefaultSellRatio = 0.5f;

        // The ROF is given by the animation speed
        public float Damage, ClusterDamage, Range, BulletSpeed;
        public int Cost, SellPrice, Level, Age;
        public NetString DisplayName;
        public NetString Prefab;
        public bool HasValue => !DisplayName.Message.IsEmpty;

        public Turret(float damage, float range, int cost, NetString displayName,
                      NetString prefabName, int level,int age,float bulletSpeed = 0f, float clusterDamage=-1f)
        {
            Damage = damage;
            Range = range;
            BulletSpeed = bulletSpeed;
            Cost = cost;
            SellPrice = (int)(cost * DefaultSellRatio);
            DisplayName = displayName;
            Level = level;
            Age = age;
            Prefab = PrefabPath + prefabName;
            ClusterDamage=clusterDamage;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref Range);
            serializer.SerializeValue(ref BulletSpeed);
            serializer.SerializeValue(ref Cost);
            serializer.SerializeValue(ref SellPrice);
            serializer.SerializeValue(ref DisplayName);
            serializer.SerializeValue(ref Level);
            serializer.SerializeValue(ref Age);
            serializer.SerializeValue(ref Prefab);
        }

        // Equality ================================================
        public static bool operator ==(Turret a, Turret b)
        {
            return a.DisplayName.Message == b.DisplayName.Message;
        }

        public static bool operator !=(Turret a, Turret b)
        {
            return !(a == b);
        }

        public override string ToString() =>
            $"{nameof(Damage)}: {Damage}, {nameof(Range)}: {Range}, {nameof(BulletSpeed)}: {BulletSpeed}, {nameof(Cost)}: {Cost}, {nameof(SellPrice)}: {SellPrice}, {nameof(DisplayName)}: {DisplayName}";

        public bool Equals(Turret other)
        {
            return DisplayName.Message.Equals(other.DisplayName);
        }

        public override bool Equals(object obj)
        {
            return obj is Turret other && Equals(other);
        }

        public override int GetHashCode()
        {
            return DisplayName.Message.GetHashCode();
        }
    }
}