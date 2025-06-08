using System;
using System.Collections.Generic;

namespace Model.Units
{
    public static class UnitFactory
    {
        public static Unit Caveman1() => new Unit(displayName: "Caveman", prefabName: "caveman_1", maxHp: 10,
            damage: 700, cost: 10, spawnTime: 1, level: 1);

        public static Unit Caveman2() => new Unit(displayName: "Slingshot-man", prefabName: "caveman_2", maxHp: 10,
            damage: 2.5f, cost: 25, spawnTime: 2, level: 2, maxDistance: 5f);

        public static Unit Caveman3() => new Unit(displayName: "Dino", prefabName: "caveman_3", maxHp: 50, damage: 10,
            cost: 100, spawnTime: 5, level: 3);

        public static List<List<Func<Unit>>> Units = new() { new List<Func<Unit>> { Caveman1, Caveman2, Caveman3 } };
    }
}