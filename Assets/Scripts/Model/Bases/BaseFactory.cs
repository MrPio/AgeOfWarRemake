using System;
using System.Collections.Generic;

namespace Model.Bases
{
    public static class BaseFactory
    {
        public static Base Cave() => new Base(name: "Cave", maxHp: 500, expRequired: 0);

        public static List<Func<Base>> Bases = new() { Cave };
    }
}