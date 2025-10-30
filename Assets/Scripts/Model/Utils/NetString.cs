using System;
using Unity.Collections;
using Unity.Netcode;

namespace Model.Utils
{
    /// <summary>
    /// A string message of 64 bytes in size.
    /// </summary>
    public struct NetString : INetworkSerializable, IEquatable<NetString>
    {
        public FixedString64Bytes Message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Message);
        }

        public static implicit operator string(NetString rValue) => rValue.Message.Value;

        public static implicit operator NetString(string rValue) => new() { Message = rValue };

        public bool Equals(NetString other)
        {
            return Message.Equals(other.Message);
        }

        public override bool Equals(object obj)
        {
            return obj is NetString other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Message.GetHashCode();
        }
    }
}