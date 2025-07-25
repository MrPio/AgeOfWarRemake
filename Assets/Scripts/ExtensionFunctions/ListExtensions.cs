using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExtensionFunctions
{
    public static class ListExtensions
    {
        private static readonly System.Random Random = new();

        public static T RandomItem<T>(this List<T> list) =>
            list[Random.Next(0, list.Count)];

        public static List<T> Shuffle<T>(this List<T> list)
        {
            var n = list.Count;
            while (n > 1)
            {
                var k = Random.Next(n--);
                (list[n], list[k]) = (list[k], list[n]);
            }

            return list;
        }

        public static void ForEach<T>(this List<T> list, Action<T, int> action)
        {
            for (var i = 0; i < list.Count; i++)
                action(list[i], i);
        }

        public static List<T> RandomSublist<T>(this List<T> list, int length)
            => list.ToList().Shuffle().Take(length).ToList();

        public static void Print<T>(this List<T> list)
        {
            UnityEngine.Debug.Log(string.Join(", ", list));
            // foreach (var item in list)
            // Debug.Log(item.ToString());
        }

        public static T RandomWeighted<T>(this List<T> items, List<float> weights)
        {
            var totalWeight = weights.Sum();
            var randomValue = Random.NextDouble() * totalWeight;
            var cumulative = 0f;

            for (var i = 0; i < items.Count; i++)
            {
                cumulative += weights[i];
                if (randomValue < cumulative)
                    return items[i];
            }

            return items[^1];
        }
    }
}