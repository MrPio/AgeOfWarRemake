using System;
using System.Collections.Generic;
using Model.Bases;

namespace Model.Turrets
{
    public static class TurretFactory
    {
        public static readonly float[]
            ExpansionsRangeMultiplier = { 1.15f, 1.1f, 1.05f, 1.0f }; // Penalize higher places

        private static Turret Rock() => new(damage: 2, range: 4, bulletSpeed: 6f, cost: 75, sellPrice: 35,
            name: "Rock", "turret_1_1");

        private static Turret Chicken() => new(damage: 1, range: 3, bulletSpeed: 4f, cost: 150, sellPrice: 75,
            name: "Chicken", "turret_1_2");

        private static Turret Catapult() => new(damage: 5, range: 5, bulletSpeed: 8f, cost: 500, sellPrice: 250,
            name: "Catapult", "turret_1_3");

        public static readonly List<List<Func<Turret>>> Turrets = new()
        {
            new List<Func<Turret>> { Rock, Chicken, Catapult }
        };
    }
}