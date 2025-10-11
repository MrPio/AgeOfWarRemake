using System;
using System.Collections.Generic;

namespace Model.Bases
{
    public static class BaseFactory
    {
        public static Base Cave() => new(name: "Cave", maxHp: 500, evolveExpRequired: 4_000, level: 1, money: 175);
        public static Base Castle() => new(name: "Castle", maxHp: 1_200, evolveExpRequired: 14_000, level: 2);
        public static Base Church() => new(name: "Church", maxHp: 2_000, evolveExpRequired: 45_000, level: 3);
        public static Base Camp() => new(name: "Camp", maxHp: 3_200, evolveExpRequired: 200_000, level: 4);
        public static Base Ship() => new(name: "Ship", maxHp: 5_000, evolveExpRequired: 0, level: 5);

        public static List<Func<Base>> Bases = new() { Cave, Castle, Church, Camp, Ship };
        public static List<int> ExpansionCosts = new() { 1_000, 3_000, 7_500 };
    }
}