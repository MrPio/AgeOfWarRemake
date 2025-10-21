using System;
using System.Collections.Generic;

namespace Model.Bases
{
    public static class BaseFactory
    {
        public static Base Cave() => new(name: "Cave", maxHp: 500, evolveExpRequired: 4_000, level: 1, money: 99175, exp:999999,
            special: new SpecialAttack(damage: 70, rate: 3, range: 0.5f, name: "Volcano eruption", prefab: "special_1",
                explosionPrefab: "explosion", type: 0, maxAngle: 20, speed: 200));

        public static Base Castle() => new(name: "Castle", maxHp: 1_200, evolveExpRequired: 14_000, level: 2,
            special: new SpecialAttack(damage: 70, rate: 12, range: 0f, name: "Archers' support", prefab: "special_2",
                explosionPrefab: "ground_damage_small", type: 0, maxAngle: 10, speed: 300));

        public static Base Church() => new(name: "Church", maxHp: 2_000, evolveExpRequired: 45_000, level: 3,
            special: new SpecialAttack(damage: 70, rate: 5, range: 0.5f, name: "Blessing", prefab: "special_3",
                explosionPrefab: "ground_damage", type: 1));

        public static Base Camp() => new(name: "Camp", maxHp: 3_200, evolveExpRequired: 200_000, level: 4,
            special: new SpecialAttack(damage: 70, rate: 5, range: 0.5f, name: "Aerial support", prefab: "special_4",
                explosionPrefab: "ground_damage", type: 2));

        public static Base Ship() => new(name: "Ship", maxHp: 5_000, evolveExpRequired: 0, level: 5,
            special: new SpecialAttack(damage: 70, rate: 5, range: 0.5f, name: "Satellite", prefab: "special_5",
                explosionPrefab: "ground_damage", type: 2));

        public static readonly List<Func<Base>> Bases = new() { Cave, Castle, Church, Camp, Ship };
        public static readonly List<int> ExpansionCosts = new() { 1_000, 3_000, 7_500 };
    }
}