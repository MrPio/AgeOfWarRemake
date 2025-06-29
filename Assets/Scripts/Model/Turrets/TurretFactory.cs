using System;
using System.Collections.Generic;
using Model.Bases;

namespace Model.Turrets
{
    public static class TurretFactory
    {
        public static readonly float[]
            ExpansionsRangeMultiplier = { 1.15f, 1.1f, 1.05f, 1.0f }; // Penalize higher places

        // === Primitive age =================================================================
        private static Turret Rock() => new(damage: 10, range: 4, cost: 100,
            name: "Rock Slingshot", "turret_1_1", bulletSpeed: 6f);

        private static Turret Chicken() => new(damage: 2.5f, range: 3, cost: 200,
            name: "Egg Automatic", "turret_1_2", bulletSpeed: 4f);

        private static Turret Catapult() => new(damage: 30, range: 5, cost: 500,
            name: "Primitive Catapult", "turret_1_3", bulletSpeed: 8f);

        // === Medieval age =================================================================
        private static Turret StoneCatapult() => new(damage: 32, range: 4f, cost: 500,
            name: "Stone Catapult", "turret_2_1", bulletSpeed: 6f);

        private static Turret FireCatapult() => new(damage: 45, range: 4.35f, cost: 750,
            name: "Fire Catapult", "turret_2_2", bulletSpeed: 5f);

        private static Turret Oil() => new(damage: 60, range: 1.25f, cost: 1000,
            name: "Oil", "turret_2_3");

        // === Renaissance age =================================================================
        private static Turret SmallCannon() => new(damage: 50, range: 3.5f, cost: 1_500,
            name: "Small Cannon", "turret_3_1", bulletSpeed: 8f);

        private static Turret LargeCannon() => new(damage: 65, range: 3.75f, cost: 3_000,
            name: "Large Cannon", "turret_3_2", bulletSpeed: 8f);

        private static Turret ExplosiveCannon() => new(damage: 80, range: 4f, cost: 6_000,
            name: "Explosive Cannon", "turret_3_3", bulletSpeed: 8f);

        // === Modern age =================================================================
        private static Turret SingleTurret() => new(damage: 50, range: 4.5f, cost: 7_000,
            name: "Single Turret", "turret_4_1", bulletSpeed: 2f);

        private static Turret RocketLauncher() => new(damage: 100, range: 3.75f, cost: 9_000,
            name: "Rocket Launcher", "turret_4_2", bulletSpeed: 7f);

        private static Turret DoubleTurret() => new(damage: 50, range: 3.75f, cost: 14_000,
            name: "Double Turret", "turret_4_3", bulletSpeed: 7f); // @

        // === Future age =================================================================
        private static Turret TitaniumShooter() => new(damage: 100, range: 5.5f, cost: 24_000,
            name: "Titanium Shooter", "turret_5_1", bulletSpeed: 2f);

        private static Turret LaserCannon() => new(damage: 60, range: 6.5f, cost: 40_000,
            name: "Laser Cannon", "turret_5_2", bulletSpeed: 2f);

        private static Turret IonCannon() => new(damage: 75, range: 7.5f, cost: 100_000,
            name: "Ion Cannon", "turret_5_3", bulletSpeed: 2f);
        
        public static readonly List<List<Func<Turret>>> Turrets = new()
        {
            new List<Func<Turret>> { Rock, Chicken, Catapult },
            new List<Func<Turret>> { StoneCatapult, FireCatapult, Oil },
            new List<Func<Turret>> { SmallCannon, LargeCannon, ExplosiveCannon },
            new List<Func<Turret>> { SingleTurret, RocketLauncher, DoubleTurret },
            new List<Func<Turret>> { TitaniumShooter, LaserCannon, IonCannon },
        };
    }
}