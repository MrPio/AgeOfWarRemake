using Interfaces;
using Model.Turrets;
using Model.Utils;
using Unity.Netcode;

namespace Model.Bases
{
    public struct Base : INetworkSerializable, INullable
    {
        private const string PrefabPath = "Prefabs/Bases/";

        public float Hp, MaxHp;
        public int ExpRequired, UnlockedExpansions, Level;
        public NetString Name;
        public NetString Prefab;
        public Turret[] Turrets;
        public bool HasValue => !Name.Message.IsEmpty;

        public Base(NetString name, float maxHp, int expRequired, int level, Turret[] turrets = null,
                    int unlockedExpansions = 1)
        {
            Hp = maxHp;
            MaxHp = maxHp;
            ExpRequired = expRequired;
            Name = name;
            Turrets = turrets ?? new Turret[] { default, default, default, default };
            UnlockedExpansions = unlockedExpansions;
            Level = level;
            Prefab = PrefabPath + name;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Hp);
            serializer.SerializeValue(ref MaxHp);
            serializer.SerializeValue(ref ExpRequired);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Prefab);
            serializer.SerializeValue(ref UnlockedExpansions);
            serializer.SerializeValue(ref Turrets);
            serializer.SerializeValue(ref Level);
        }

        public override string ToString() =>
            $"{nameof(Hp)}: {Hp}, {nameof(MaxHp)}: {MaxHp}, {nameof(ExpRequired)}: {ExpRequired}, {nameof(Name)}: {Name}, {nameof(Prefab)}: {Prefab}, {nameof(HasValue)}: {HasValue}";
    }
}