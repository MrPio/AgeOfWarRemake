using System;
using System.Collections.Generic;

namespace Model.Bases
{
    public static class BaseFactory
    {
        public static readonly Dictionary<Func<Base>, List<Func<Turrets.Turret>>> BaseTurrets = new()
        {
            { Cave, new List<Func<Turrets.Turret>> { TurretFactory.Rock, TurretFactory.Chicken, TurretFactory.Catapult } }
        };

        public static Base Cave() => new(name: "Cave", maxHp: 500, expRequired: 0);

        public static List<Func<Base>> Bases = new() { Cave };
    }
}