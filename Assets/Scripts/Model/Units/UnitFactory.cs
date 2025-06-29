using System;
using System.Collections.Generic;

namespace Model.Units
{
    public static class UnitFactory
    {
        // === Primitive age =================================================================
        private static Unit Caveman1() => new(displayName: "Caveman", prefabName: "caveman_1", maxHp: 55f,
            damage: 16, attackRate: 1f, cost: 15, revenue: 20, spawnTime: 1, level: 1);

        private static Unit Caveman2() => new(displayName: "Slingshot man", prefabName: "caveman_2", maxHp: 42f,
            damage: 10, shootDamage: 8, attackRate: 1.15f, shootRate: 1.5f, cost: 25, revenue: 33, spawnTime: 1,
            level: 2, maxShootingDistance: 5f);

        private static Unit Caveman3() => new(displayName: "Dino", prefabName: "caveman_3", maxHp: 160f,
            damage: 40, attackRate: 1f, cost: 100, revenue: 130, spawnTime: 3f, level: 3);

        // === Medieval age =================================================================
        private static Unit Knight1() => new(displayName: "Swordman", prefabName: "knight_1", maxHp: 100,
            damage: 32, attackRate: 1f, cost: 50, revenue: 65, spawnTime: 2, level: 1);

        private static Unit Knight2() => new(displayName: "Archer", prefabName: "knight_2", maxHp: 80,
            damage: 20, shootDamage: 9f, attackRate: 1.15f, shootRate: 1.25f, cost: 75, revenue: 98, spawnTime: 1,
            level: 2, maxShootingDistance: 6f);

        private static Unit Knight3() => new(displayName: "Knight", prefabName: "knight_3", maxHp: 300,
            damage: 60, attackRate: 1f, cost: 500, revenue: 650, spawnTime: 5f, level: 3);

        // === Renaissance age =================================================================
        private static Unit Swordman1() => new(displayName: "Dueler", prefabName: "swordman_1", maxHp: 200,
            damage: 79, attackRate: 1f, cost: 200, revenue: 260, spawnTime: 3, level: 1);

        private static Unit Swordman2() => new(displayName: "Mousquettere", prefabName: "swordman_2", maxHp: 160,
            damage: 40, shootDamage: 20f, attackRate: 1.15f, shootRate: 0.75f, cost: 400, revenue: 520, spawnTime: 3,
            level: 2, maxShootingDistance: 6f);

        private static Unit Swordman3() => new(displayName: "Canoneer", prefabName: "swordman_3", maxHp: 600,
            damage: 120, attackRate: 1f, cost: 1_000, revenue: 1_300, spawnTime: 5, level: 3);

        // === Modern age =================================================================
        private static Unit Soldier1() => new(displayName: "Melee Infantry", prefabName: "soldier_1", maxHp: 350,
            damage: 100, attackRate: 1f, cost: 1_500, revenue: 1_950, spawnTime: 3, level: 1);

        private static Unit Soldier2() => new(displayName: "Infantry", prefabName: "soldier_2", maxHp: 300,
            damage: 60, shootDamage: 30f, attackRate: 1.15f, shootRate: 1f, cost: 2_000, revenue: 2_600, spawnTime: 3,
            level: 2, maxShootingDistance: 6f);

        private static Unit Soldier3() => new(displayName: "Tank", prefabName: "soldier_3", maxHp: 1_200,
            damage: 300, attackRate: 1f, cost: 7_000, revenue: 9_100, spawnTime: 8, level: 3);

        // === Future age =================================================================
        private static Unit Trooper1() => new(displayName: "God's Blade", prefabName: "trooper_1", maxHp: 1_000,
            damage: 250, attackRate: 1f, cost: 5_000, revenue: 6_500, spawnTime: 3, level: 1);

        private static Unit Trooper2() => new(displayName: "Blaster", prefabName: "trooper_2", maxHp: 800,
            damage: 130, shootDamage: 80f, attackRate: 1.15f, shootRate: 1f, cost: 6_000, revenue: 7_800, spawnTime: 3,
            level: 2, maxShootingDistance: 6f);

        private static Unit Trooper3() => new(displayName: "War Machine", prefabName: "trooper_3", maxHp: 3_000,
            damage: 600, attackRate: 1f, cost: 20_000, revenue: 26_000, spawnTime: 8, level: 3);

        public static readonly List<List<Func<Unit>>> Units = new()
        {
            new List<Func<Unit>> { Caveman1, Caveman2, Caveman3 },
            new List<Func<Unit>> { Knight1, Knight2, Knight3 },
            new List<Func<Unit>> { Swordman1, Swordman2, Swordman3 },
            new List<Func<Unit>> { Soldier1, Soldier2, Soldier3 },
            new List<Func<Unit>> { Trooper1, Trooper2, Trooper3 },
        };
    }
}