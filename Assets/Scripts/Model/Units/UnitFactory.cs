using System;
using System.Collections.Generic;

namespace Model.Units
{
    public static class UnitFactory
    {
        private static Unit Caveman1() => new(displayName: "Caveman", prefabName: "caveman_1", maxHp: 40,
            damage: 20, attackRate: 1f, cost: 10, spawnTime: 1, level: 1);

        private static Unit Caveman2() => new(displayName: "Slingshot man", prefabName: "caveman_2", maxHp: 50,
            damage: 15f, shootDamage: 10f, attackRate: 1.15f, shootRate: 1.5f, cost: 25, spawnTime: 2, level: 2,
            maxShootingDistance: 5f);

        private static Unit Caveman3() => new(displayName: "Dino", prefabName: "caveman_3", maxHp: 750, damage: 50,
            attackRate: 1f, cost: 100, spawnTime: 5, level: 3);

        public static readonly List<List<Func<Unit>>> Units = new() { new List<Func<Unit>> { Caveman1, Caveman2, Caveman3 } };
    }
}