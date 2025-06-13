using System;
using System.Collections.Generic;

namespace Model.Bases
{
    public static class BaseFactory
    {
        public static Base Cave() => new(name: "Cave", maxHp: 500, expRequired: 0, level: 1);

        public static List<Func<Base>> Bases = new() { Cave };
    }
}