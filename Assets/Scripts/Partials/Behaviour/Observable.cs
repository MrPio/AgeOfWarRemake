using System;
using System.Collections.Generic;
using UnityEngine;

namespace Partials.Behaviour
{
    public class Observable : MonoBehaviour
    {
        private readonly Dictionary<string, Action> topicSubscribers = new();

        public void Subscribe(string topic, Action handler)
        {
            topicSubscribers.TryAdd(topic, null);
            topicSubscribers[topic] += handler;
        }

        public void Unsubscribe(string topic, Action handler)
        {
            if (topicSubscribers.ContainsKey(topic))
                topicSubscribers[topic] -= handler;
        }

        // Notify all listeners of a specific topic
        public void Notify(string topic)
        {
            if (topicSubscribers.TryGetValue(topic, out var handlers))
                handlers?.Invoke();
        }
    }
}