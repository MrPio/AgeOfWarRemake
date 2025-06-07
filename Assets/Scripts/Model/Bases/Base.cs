using System;

namespace Model.Bases
{
    [Serializable]
    public abstract class Base
    {
        public const string PrefabPath = "Prefabs/Caves/";

        public float hp, maxHp;
        public int expRequired;
        public string name;

        protected Base(string name, float maxHp, int expRequired)
        {
            this.hp = maxHp;
            this.maxHp = maxHp;
            this.expRequired = expRequired;
            this.name = name;
        }
    }
}