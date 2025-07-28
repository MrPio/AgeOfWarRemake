using Interfaces;
using Model.Utils;
using Unity.Netcode;

namespace Model.Units
{
    public struct Unit : INetworkSerializable, INullable
    {
        private const string PrefabPath = "Prefabs/Units/";

        public float Hp;
        public float MaxHp;
        public float Damage;
        public float ShootDamage;
        public float Armor;
        public float MaxArmor;
        public float MoveSpeed;
        public float AttackDuration;
        public float ShootRate;
        public int Cost, Revenue;
        public float SpawnTime;
        public int Level;
        public NetString Prefab;
        public NetString DisplayName;
        public float MaxShootingDistance;
        public bool HasValue => !DisplayName.Message.IsEmpty;

        public Unit(
            NetString displayName, NetString prefabName, float maxHp, float damage, int cost, int revenue, float spawnTime,
            int level, float maxShootingDistance = 0, float moveSpeed = 0.625f /** 1.25f*/, float attackDuration = 1f,
            float armor = 0f, float maxArmor = 0f, float shootDamage = 0f, float shootRate = 1f)
        {
            DisplayName = displayName;
            Prefab = PrefabPath + prefabName;
            Hp = maxHp;
            MaxHp = maxHp;
            Damage = damage;
            Armor = armor;
            MaxArmor = maxArmor;
            MoveSpeed = moveSpeed;
            AttackDuration = attackDuration;
            Cost = cost;
            Revenue = revenue;
            SpawnTime = spawnTime;
            Level = level;
            MaxShootingDistance = maxShootingDistance;
            ShootDamage = shootDamage;
            ShootRate = shootRate;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Hp);
            serializer.SerializeValue(ref MaxHp);
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref ShootDamage);
            serializer.SerializeValue(ref Armor);
            serializer.SerializeValue(ref MaxArmor);
            serializer.SerializeValue(ref MoveSpeed);
            serializer.SerializeValue(ref AttackDuration);
            serializer.SerializeValue(ref ShootRate);
            serializer.SerializeValue(ref Cost);
            serializer.SerializeValue(ref Revenue);
            serializer.SerializeValue(ref SpawnTime);
            serializer.SerializeValue(ref Level);
            serializer.SerializeValue(ref MaxShootingDistance);
            serializer.SerializeValue(ref Prefab);
            serializer.SerializeValue(ref DisplayName);
        }
    }
}