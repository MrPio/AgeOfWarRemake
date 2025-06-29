using System;
using System.Collections.Generic;

namespace Model.Bases
{
    public static class BaseFactory
    {
        public static Base Cave() => new(name: "Cave", maxHp: 500, expRequired: 0, level: 1);
        public static Base Castle() => new(name: "Castle", maxHp: 1_200, expRequired: 4_000, level: 2);
        public static Base Church() => new(name: "Church", maxHp: 2_000, expRequired: 45_000, level: 3);
        public static Base Camp() => new(name: "Camp", maxHp: 3_200, expRequired: 200_000, level: 4);
        public static Base Ship() => new(name: "Ship", maxHp: 5_000, expRequired: -1, level: 5);

        public static List<Func<Base>> Bases = new() { Cave, Castle, Church, Camp, Ship };
    }
}○