using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using LogType = UI.LogType;

namespace Partials.Unit
{
    public class UnitMovement : NetworkBehaviour
    {
        /// <summary>
        /// The greater, the buffer size, the more robust to lag the movement, and the greater the delay.
        /// </summary>
        /// <example> If the tick rate is 20 Hz,
        /// a buffer size of 5 indicates a delay of 0.2 seconds in the client units movement.
        /// That's affordable, right? </example>
        [Tooltip("The greater the buffer size, the more robust to lag the movement is, but the greater the delay.")]
        [SerializeField, Range(2, 20)]
        private int bufferSize = 5;

        /// <summary>
        /// The Z position of the units. The movement is restricted to the X axis.
        /// </summary>
        [SerializeField] public float zPos = 0.085f;

        /// <summary>
        /// A list of tuples containing the normalized x-axis progression and the client-time of that update.
        /// Note that the timestamp is not important per se,
        /// but the delta time between two later values is what's important.
        /// </summary>
        private readonly List<Tuple<float, float>> _buffer = new();

        private Prefabs.Unit _unit;
        private float _xEnd, _xStart;

        # region NetVars

        /// <summary>
        /// Represent the normalized x-axis progression from 0 (spawn point) to 1 (unit's enemy base).
        /// </summary>
        public readonly NetworkVariable<float> X = new();

        private void OnXChanged(float _, float newValue)
        {
            _buffer.Add(new Tuple<float, float>(newValue, Time.time));
            _unit.Sm.logger.Log($"{newValue}-{Time.time}", LogType.ReadingStatus);
        }

        # endregion

        #region Events

        private void Awake()
        {
            _unit = GetComponent<Prefabs.Unit>();
        }

        private void Start()
        {
            _xStart = _unit.AllyBase.BasePrefab.unitSpawnPointX.position.x;
            _xEnd = _unit.EnemyBase.BasePrefab.unitSpawnPointX.position.x;
            transform.position = new Vector3(x: _xStart, y: 0, z: zPos);
        }

        // Client only
        public override void OnNetworkSpawn()
        {
            if (IsServer) return;
            X.OnValueChanged += OnXChanged;
        }

        // Client only
        public override void OnNetworkDespawn()
        {
            if (IsServer) return;
            X.OnValueChanged -= OnXChanged;
        }

        // Client only
        private void Update()
        {
            if (IsServer) return; // The server handles the movement directly
            if (_buffer.Count < bufferSize) return; // Not enough data

            // Replay the server movements using linear interpolation
            var duration = _buffer[1].Item2 - _buffer[0].Item2;
            var t = (Time.time - _buffer[0].Item2) / duration;

            // Get the real new X value.
            // You could use Lerp to avoid extrapolation,
            // but that would make the movement jittery.
            var xPosNorm =
                _unit.IsWalking.Value
                    ? Mathf.LerpUnclamped(_buffer[0].Item1, _buffer[1].Item1, t)
                    : Mathf.Lerp(_buffer[0].Item1, _buffer[1].Item1, t);
            var xPos = Mathf.Lerp(_xStart, _xEnd, xPosNorm);

            transform.position = new Vector3(x: xPos, y: 0, z: zPos);

            // Remove the oldest value if there's a newer value in the buffer.
            // If that's not the case, predict the future position (Using LerpUnclamped)
            if (t >= 1 && _buffer.Count > bufferSize)
                _buffer.RemoveAt(0);
        }

        #endregion
    }
}