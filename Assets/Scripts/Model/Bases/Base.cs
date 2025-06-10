using Model.Utils;
using Unity.Netcode;

namespace Model.Bases
{
    public struct Base : INetworkSerializable
    {
        private const string PrefabPath = "Prefabs/Bases/";

        public float Hp, MaxHp;
        public int ExpRequired;
        public NetString Name;
        public NetString Prefab;
        public Turret[] Turrets;
        public bool HasValue => !Name.Message.IsEmpty;

        public Base(NetString name, float maxHp, int expRequired, Turret[] turrets)
        {
            Hp = maxHp;
            MaxHp = maxHp;
            ExpRequired = expRequired;
            Name = name;
            Turrets = turrets;
            Prefab = PrefabPath + name;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Hp);
            serializer.SerializeValue(ref MaxHp);
            serializer.SerializeValue(ref ExpRequired);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Prefab);
            serializer.SerializeValue(ref Turrets);
        }

        public override string ToString() =>
            $"{nameof(Hp)}: {Hp}, {nameof(MaxHp)}: {MaxHp}, {nameof(ExpRequired)}: {ExpRequired}, {nameof(Name)}: {Name}, {nameof(Prefab)}: {Prefab}, {nameof(HasValue)}: {HasValue}";
    }
}