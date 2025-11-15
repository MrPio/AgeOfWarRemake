using System;
using System.Collections.Generic;

namespace Model.Bases
{
    public static class BaseFactory
    {
        public static Base Cave() => new(name: "Cave", maxHp: 500, evolveExpRequired: 4_000, age: 1, money: 175,
            exp: 0,
            special: new SpecialAttack(age: 1, damage: 70, rate: 2.25f, range: 0.5f, name: "Volcano eruption",
                prefab: "rock", explosionPrefab: "explosion", type: SpecialType.Rain, maxAngle: 20, speed: 7f));

        public static Base Castle() => new(name: "Castle", maxHp: 1_200, evolveExpRequired: 14_000, age: 2,
            special: new SpecialAttack(age: 2, damage: 70, rate: 9, range: 0f, name: "Archers' support",
                prefab: "arrow", explosionPrefab: "ground_damage_small", type: SpecialType.Rain, maxAngle: 10,
                speed: 10f));

        public static Base Church() => new(name: "Church", maxHp: 2_000, evolveExpRequired: 45_000, age: 3,
            special: new SpecialAttack(age: 3, damage: -26, rate: 4, range: 0f, name: "Blessing", prefab: "halo",
                explosionPrefab: "ground_damage", type: SpecialType.Heal));

        public static Base Camp() => new(name: "Camp", maxHp: 3_200, evolveExpRequired: 200_000, age: 4,
            special: new SpecialAttack(age: 4, damage: 400, rate: 3.75f, range: 1f, name: "Aerial support",
                prefab: "plane", explosionPrefab: "ground_damage", type: SpecialType.Scan));

        public static Base Ship() => new(name: "Ship", maxHp: 5_000, evolveExpRequired: 0, age: 5,
            special: new SpecialAttack(age: 5, damage: 1_500, rate: 3f, range: 1.25f, speed: 25f, duration: 7f,
                name: "Satellite", prefab: "beam", explosionPrefab: "ground_damage", type: SpecialType.Scan));

        public static readonly List<Func<Base>> Bases = new() { Cave, Castle, Church, Camp, Ship };
        public static readonly List<int> ExpansionCosts = new() { 1_000, 3_000, 7_500 };
        public static readonly List<int> MoneyPerSecond = new() { 2, 5, 20, 150, 500 }; // Only multiplayer
    }
}