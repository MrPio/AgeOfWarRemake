using System;
using System.Collections.Generic;
using Model.Bases;

namespace Model.Turrets
{
    public static class TurretFactory
    {
        public static readonly float[]
            ExpansionsRangeMultiplier = { 1.0f, 1.05f, 1.1f, 1.15f }; // Penalize lower places

        // === Primitive age =================================================================
        private static Turret Rock() => new(damage: 10, range: 5.75f, cost: 100,
            displayName: "Rock Slingshot", "turret_1_1", bulletSpeed: 6f, level: 1, age: 1);

        private static Turret Chicken() => new(damage: 2.25f, range: 5.75f, cost: 200,
            displayName: "Egg Automatic", "turret_1_2", bulletSpeed: 4f, level: 2, age: 1);

        private static Turret Catapult() => new(damage: 30, range: 7f, cost: 500,
            displayName: "Primitive Catapult", "turret_1_3", bulletSpeed: 12f, level: 3, age: 1);

        // === Medieval age =================================================================
        private static Turret StoneCatapult() => new(damage: 32, range: 6.75f, cost: 500,
            displayName: "Stone Catapult", "turret_2_1", bulletSpeed: 7f, level: 1, age: 2);

        private static Turret FireCatapult() => new(damage: 45, range: 6.5f, cost: 750,
            displayName: "Fire Catapult", "turret_2_2", bulletSpeed: 5.5f, clusterDamage: 15, level: 2, age: 2);

        private static Turret Oil() => new(damage: 60, range: 2.75f, cost: 1000,
            displayName: "Oil", "turret_2_3", isFluid: true, level: 3, age: 2);

        // === Renaissance age =================================================================
        private static Turret SmallCannon() => new(damage: 50, range: 6.5f, cost: 1_500,
            displayName: "Small Cannon", "turret_3_1", bulletSpeed: 8f, level: 1, age: 3);

        private static Turret LargeCannon() => new(damage: 63, range: 6.75f, cost: 3_000,
            displayName: "Large Cannon", "turret_3_2", bulletSpeed: 8f, level: 2, age: 3);

        private static Turret ExplosiveCannon() => new(damage: 72, range: 7.15f, cost: 6_000,
            displayName: "Explosive Cannon", "turret_3_3", clusterDamage: 15, bulletSpeed: 8f, level: 3, age: 3);

        // === Modern age =================================================================
        private static Turret SingleTurret() => new(damage: 50, range: 6.75f, cost: 7_000,
            displayName: "Single Turret", "turret_4_1", bulletSpeed: 8f, level: 1, age: 4);

        private static Turret RocketLauncher() => new(damage: 100, range: 8f, cost: 9_000,
            displayName: "Rocket Launcher", "turret_4_2", bulletSpeed: 2.75f, level: 2, age: 4, isFollow: true);

        private static Turret DoubleTurret() => new(damage: 40, range: 6.5f, cost: 14_000,
            displayName: "Double Turret", "turret_4_3", bulletSpeed: 8f, level: 3, age: 4); // @

        // === Future age =================================================================
        private static Turret TitaniumShooter() => new(damage: 100, range: 8.5f, cost: 24_000,
            displayName: "Titanium Shooter", "turret_5_1", bulletSpeed: 4f, level: 1, age: 5);

        private static Turret LaserCannon() => new(damage: 65, range: 10f, cost: 40_000,
            displayName: "Laser Cannon", "turret_5_2", bulletSpeed: 5f, level: 2, age: 5);

        private static Turret IonCannon() => new(damage: 85, range: 14f, cost: 100_000,
            displayName: "Ion Cannon", "turret_5_3", bulletSpeed: 6f, level: 3, age: 5);

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