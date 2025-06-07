using System;
using UnityEngine;

namespace Model.Units
{
    [Serializable]
    public abstract class Unit
    {
        public const string PrefabPath = "Prefabs/Units/";

        public float hp;
        public float maxHp;
        public float damage;
        public float armor;
        public float maxArmor;
        public float moveSpeed;
        public float attackRate;
        public int cost;
        public float spawnTime;
        public int level;
        public string prefabName, displayName;
        public float maxDistance;

        protected Unit(
            string displayName, string prefabName, float maxHp, float damage, int cost, float spawnTime, int level,
            float maxDistance = 0, float moveSpeed = 0.75f * 4, float attackRate = 1f, float armor = 0f,
            float maxArmor = 0f)
        {
            this.displayName = displayName;
            this.prefabName = prefabName;
            this.hp = maxHp;
            this.maxHp = maxHp;
            this.damage = damage;
            this.armor = armor;
            this.maxArmor = maxArmor;
            this.moveSpeed = moveSpeed;
            this.attackRate = attackRate;
            this.cost = cost;
            this.spawnTime = spawnTime;
            this.level = level;
            this.maxDistance = maxDistance;
        }

        public GameObject Prefab => Resources.Load<GameObject>(PrefabPath + prefabName);
    }
}