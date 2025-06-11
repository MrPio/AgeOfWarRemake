using System;
using Model.Turrets;
using Model.Utils;
using Unity.Netcode;

namespace Model.Bases
{
    public struct Base : INetworkSerializable
    {
        private const string PrefabPath = "Prefabs/Bases/";

        public float Hp, MaxHp;
        public int ExpRequired, UnlockedExpansions;
        public NetString Name;
        public NetString Prefab;
        public Turrets.Turret[] Turrets;
        public bool HasValue => !Name.Message.IsEmpty;

        public Base(NetString name, float maxHp, int expRequired, Turrets.Turret[] turrets = null,
            int unlockedExpansions = 0)
        {
            Hp = maxHp;
            MaxHp = maxHp;
            ExpRequired = expRequired;
            Name = name;
            Turrets = turrets ?? new Turrets.Turret[] { default, default, default, default };
            UnlockedExpansions = unlockedExpansions;
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

            // To avoid deep copy each time
            // if (serializer.IsReader)
            //     Turrets = new Turret[4];
            // for (var i = 0; i < 4; i++)
            //     serializer.SerializeValue(ref Turrets[i]);
        }

        public override string ToString() =>
            $"{nameof(Hp)}: {Hp}, {nameof(MaxHp)}: {MaxHp}, {nameof(ExpRequired)}: {ExpRequired}, {nameof(Name)}: {Name}, {nameof(Prefab)}: {Prefab}, {nameof(HasValue)}: {HasValue}";
    }
}