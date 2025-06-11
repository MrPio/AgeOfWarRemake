using System;
using System.Collections.Generic;

namespace Model.Bases
{
    public static class TurretFactory
    {
        public static float[] ExpansionsRangeMultiplier = { 1.15f, 1.1f, 1.05f, 1.0f }; // Penalize higher places

        public static Turrets.Turret Rock() => new(damage: 2, range: 4, bulletSpeed: 6f, cost: 75, sellPrice: 35,
            name: "Rock", "turret_1_1");

        public static Turrets.Turret Chicken() => new(damage: 1, range: 3, bulletSpeed: 4f, cost: 150, sellPrice: 75,
            name: "Chicken", "turret_1_2");

        public static Turrets.Turret Catapult() => new(damage: 5, range: 5, bulletSpeed:8f, cost: 500, sellPrice: 250,
            name: "Catapult", "turret_1_3");
    }
}