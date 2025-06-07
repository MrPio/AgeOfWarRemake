using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Model
{
    public class Unit
    {
        public const string PrefabPath = "Prefabs/Units/";

        public static Unit[] Units =
        {
            new Unit(health: 100, maxHealth: 100, damage: 1, cost: 10, spawnTime: 1, epoch: 1, level: 1,
                name: "caveman_1"),
            new Unit(health: 100, maxHealth: 100, damage: 1.5f, cost: 25, spawnTime: 2, epoch: 1, level: 2,
                name: "caveman_2", maxDistance: 5f),
            new Unit(health: 100, maxHealth: 100, damage: 5, cost: 100, spawnTime: 5, epoch: 1, level: 3,
                name: "caveman_3"),
        };

        public float Health;
        public float MaxHealth;
        public float Damage;
        public float Armor;
        public float MaxArmor;
        public float MoveSpeed;
        public float AttackRate;
        public int Cost;
        public float SpawnTime;
        public int Epoch; // The historic epoch 
        public int Level; // The index in the current epoch
        public string Name; // caveman
        public float? MaxDistance; // Whether it can attack from distance

        public Unit(float health, float maxHealth, float damage, int cost, float spawnTime, int epoch, int level,
            string name, float? maxDistance = null, float moveSpeed = 0.75f*4, float attackRate = 1f, float armor = 0f,
            float maxArmor = 0f)
        {
            this.Health = health;
            this.MaxHealth = maxHealth;
            this.Damage = damage;
            this.Armor = armor;
            this.MaxArmor = maxArmor;
            this.MoveSpeed = moveSpeed;
            this.AttackRate = attackRate;
            this.Cost = cost;
            this.SpawnTime = spawnTime;
            this.Epoch = epoch;
            this.Level = level;
            this.Name = name;
            this.MaxDistance = maxDistance;
        }

        public static Unit FromName(string name) =>
            Units.First(it => string.Equals(it.Name, name, StringComparison.CurrentCultureIgnoreCase));
        
        public GameObject Prefab => Resources.Load<GameObject>(PrefabPath + Name);
    }
}