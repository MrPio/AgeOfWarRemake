using Model.Utils;
using Unity.Netcode;

namespace Model.Units
{
    public struct Unit : INetworkSerializable
    {
        private const string PrefabPath = "Prefabs/Units/";

        public float Hp;
        public float MaxHp;
        public float Damage;
        public float Armor;
        public float MaxArmor;
        public float MoveSpeed;
        public float AttackRate;
        public int Cost;
        public float SpawnTime;
        public int Level;
        public NetString Prefab;
        public NetString DisplayName;
        public float MaxDistance;
        public bool HasValue => !DisplayName.Message.IsEmpty;

        public Unit(
            NetString displayName, NetString prefabName, float maxHp, float damage, int cost, float spawnTime,
            int level,
            float maxDistance = 0, float moveSpeed = 0.75f * 1.25f, float attackRate = 1f, float armor = 0f,
            float maxArmor = 0f)
        {
            DisplayName = displayName;
            Prefab = PrefabPath + prefabName;
            Hp = maxHp;
            MaxHp = maxHp;
            Damage = damage;
            Armor = armor;
            MaxArmor = maxArmor;
            MoveSpeed = moveSpeed;
            AttackRate = attackRate;
            Cost = cost;
            SpawnTime = spawnTime;
            Level = level;
            MaxDistance = maxDistance;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Hp);
            serializer.SerializeValue(ref MaxHp);
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref Armor);
            serializer.SerializeValue(ref MaxArmor);
            serializer.SerializeValue(ref MoveSpeed);
            serializer.SerializeValue(ref AttackRate);
            serializer.SerializeValue(ref Cost);
            serializer.SerializeValue(ref SpawnTime);
            serializer.SerializeValue(ref Level);
            serializer.SerializeValue(ref MaxDistance);
            serializer.SerializeValue(ref Prefab);
            serializer.SerializeValue(ref DisplayName);
        }
    }
}