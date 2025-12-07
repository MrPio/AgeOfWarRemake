using System;
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
        public int EvolveExpRequired, UnlockedExpansions, Age, Money, Exp;
        public NetString Name;
        public NetString Prefab;
        public Turret[] Turrets;
        public SpecialAttack Special;
        public bool HasValue => !Name.Message.IsEmpty;

        public Base(NetString name, float maxHp, int evolveExpRequired, int age, SpecialAttack special,
                    Turret[] turrets = null, int unlockedExpansions = 1, int money = -1, int exp = 0)
        {
            Hp = maxHp;
            MaxHp = maxHp;
            EvolveExpRequired = evolveExpRequired;
            Name = name;
            Special = special;
            Turrets = turrets ?? new Turret[] { default, default, default, default };
            UnlockedExpansions = unlockedExpansions;
            Age = age;
            Prefab = PrefabPath + name;
            Money = money;
            Exp = exp;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Hp);
            serializer.SerializeValue(ref MaxHp);
            serializer.SerializeValue(ref EvolveExpRequired);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Prefab);
            serializer.SerializeValue(ref UnlockedExpansions);
            serializer.SerializeValue(ref Turrets);
            serializer.SerializeValue(ref Age);
            serializer.SerializeValue(ref Money);
            serializer.SerializeValue(ref Special);
            serializer.SerializeValue(ref Exp);
        }

        public override string ToString() =>
            $"{nameof(Hp)}: {Hp}, {nameof(MaxHp)}: {MaxHp}, {nameof(EvolveExpRequired)}: {EvolveExpRequired}, {nameof(Name)}: {Name}, {nameof(Prefab)}: {Prefab}, {nameof(HasValue)}: {HasValue}";
    }

    public enum SpecialType
    {
        Rain,
        Heal,
        Scan
    }

    public struct SpecialAttack : INetworkSerializable, INullable
    {
        private const string PrefabPath = "Prefabs/Specials/";
        private const string ExplosionPrefabPath = "Prefabs/Effects/";

        public float Damage, Duration, Rate, Cooldown, Range, MaxAngle, Speed;
        public int Age;

        public NetString Name, Prefab, ExplosionPrefab;
        private byte _type; // 0=Rain, 1=Heal, 2=Scan
        public SpecialType Type => (SpecialType)Enum.GetValues(typeof(SpecialType)).GetValue(_type);
        public bool HasValue => !Name.Message.IsEmpty;

        public SpecialAttack(int age,float damage, float rate, float range, NetString name, NetString prefab,
                             NetString explosionPrefab, SpecialType type, float duration = 10f, float cooldown = 140f,
                             float maxAngle = 0.5f, float speed = 25f
        )
        {
            Age = age;
            Damage = damage;
            Duration = duration;
            Rate = rate;
            Cooldown = cooldown;
            Range = range;
            Name = name;
            Prefab = PrefabPath + prefab;
            _type = (byte)type;
            ExplosionPrefab = ExplosionPrefabPath + explosionPrefab;
            MaxAngle = maxAngle;
            Speed = speed;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Age);
            serializer.SerializeValue(ref Damage);
            serializer.SerializeValue(ref Duration);
            serializer.SerializeValue(ref Rate);
            serializer.SerializeValue(ref Cooldown);
            serializer.SerializeValue(ref Range);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Prefab);
            serializer.SerializeValue(ref ExplosionPrefab);
            serializer.SerializeValue(ref _type);
            serializer.SerializeValue(ref MaxAngle);
            serializer.SerializeValue(ref Speed);
        }
    }
}