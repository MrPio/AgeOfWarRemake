using System;
using Interfaces;
using Model.Utils;
using Partials.State;
using Partials.State.Unit;
using Unity.Netcode;

namespace Model
{
    public enum UnitStateType
    {
        Idling,
        Walking,
        Attacking,
        Dying,
    }

    public struct UnitState : INetworkSerializable, INullable
    {
        private byte _state;
        public bool IsShooting;
        public bool HasValue => _state != byte.MaxValue;

        // Abstracts the conversion, hiding the struct implementation.
        public IState State
        {
            get => (UnitStateType)_state switch
            {
                UnitStateType.Idling => new IdleState(shooting: IsShooting),
                UnitStateType.Attacking => new AttackState(),
                UnitStateType.Walking => new WalkState(shooting: IsShooting),
                UnitStateType.Dying => new DieState(),
                _ => throw new ArgumentOutOfRangeException()
            };
            private set
            {
                _state = value switch
                {
                    IdleState => (byte)UnitStateType.Idling,
                    AttackState => (byte)UnitStateType.Attacking,
                    WalkState => (byte)UnitStateType.Walking,
                    DieState => (byte)UnitStateType.Dying,
                    _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
                };
                IsShooting = value is IdleState { Shooting: true } or WalkState { Shooting: true };
            }
        }

        public UnitState(byte state = byte.MaxValue, bool isShooting = false)
        {
            _state = state;
            IsShooting = isShooting;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _state);
            serializer.SerializeValue(ref IsShooting);
        }

        public static IState ToIState(UnitState rValue) => rValue.State;

        public static UnitState FromIState(IState rValue) => new() { State = rValue };
    }
}