using System;
using System.Collections.Generic;

namespace Model.Bases
{
    public static class BaseFactory
    {
        public static Base Cave() => new(name: "Cave", maxHp: 500, expRequired: 0, turrets: new[]
        {
            new Turret(damage: 2, range: 4, bulletSpeed: 1.5f, cost: 75, sellPrice: 35, name: "Rock"),
            new Turret(damage: 1, range: 3, bulletSpeed: 1, cost: 150, sellPrice: 75, name: "Chicken"),
            new Turret(damage: 5, range: 5, bulletSpeed: 2, cost: 500, sellPrice: 250, name: "Catapult"),
        });

        public static List<Func<Base>> Bases = new() { Cave };
    }
}