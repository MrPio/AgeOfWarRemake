using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Partials.Unit
{
    /// <summary>
    /// Client-only utility to smoothly move the unit.
    /// </summary>
    public class UnitMovement : NetworkBehaviour
    {
        /// <summary>
        /// How far behind real time we render (in seconds). 
        /// </summary>
        [SerializeField] private float interpolationBackTime = 0.2f;

        /// <summary>
        /// The Z position of the units. The movement is restricted to the X axis.
        /// </summary>
        [SerializeField] public float zPos = 0.085f;

        private struct Snapshot
        {
            public float Time; // The client-time
            public float X;
        }

        /// <summary>
        /// A list of tuples containing the normalized x-axis progression and the client-time of that update.
        /// Note that the timestamp is not important per se,
        /// but the delta time between two later values is what's important.
        /// </summary>
        private readonly List<Snapshot> _buffer = new();

        private Prefabs.Unit _unit;
        private float _xEnd, _xStart;

        # region NetVars

        /// <summary>
        /// Represent the normalized x-axis progression from 0 (spawn point) to 1 (unit's enemy base).
        /// </summary>
        public readonly NetworkVariable<float> X = new();

        private void OnXChanged(float _, float newValue) =>
            _buffer.Add(new Snapshot
            {
                Time = Time.realtimeSinceStartup,
                X = newValue
            });

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
            if (_buffer.Count == 0) return; // Not enough data

            var renderTime = Time.realtimeSinceStartup - interpolationBackTime;

            // Drop any states that are too old
            while (_buffer.Count >= 2 && _buffer[1].Time <= renderTime)
                _buffer.RemoveAt(0);

            if (_buffer.Count >= 2)
            {
                // We have at least two states surrounding renderTime → interpolate
                var t = Mathf.InverseLerp(_buffer[0].Time, _buffer[1].Time, renderTime);
                var xPosNorm = Mathf.Lerp(_buffer[0].X, _buffer[1].X, t);
                var xPos = Mathf.Lerp(_xStart, _xEnd, xPosNorm);
                transform.position = new Vector3(x: xPos, y: 0, z: zPos);

                // Currently no extrapolation is used.
                // If you want to add it, do like follows:
                // var xPosNorm =
                //     _unit.IsWalking.Value
                //         ? Mathf.LerpUnclamped(_buffer[0].Item1, _buffer[1].Item1, t)
                //         : Mathf.Lerp(_buffer[0].Item1, _buffer[1].Item1, t);
            }
            else
            {
                // Only one state available: just snap to it
                var xPos = Mathf.Lerp(_xStart, _xEnd, _buffer[0].X);
                transform.position = new Vector3(x: xPos, y: 0, z: zPos);
            }
        }

        #endregion
    }
}