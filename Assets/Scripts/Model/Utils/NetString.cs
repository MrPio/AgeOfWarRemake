using System;
using Unity.Collections;
using Unity.Netcode;

namespace Model.Utils
{
    /// <summary>
    /// A string message of 64 bytes in size.
    /// </summary>
    public struct NetString : INetworkSerializable
    {
        public FixedString64Bytes Message;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Message);
        }

        public static implicit operator string(NetString rValue) => rValue.Message.Value;

        public static implicit operator NetString(string rValue) => new() { Message = rValue };
    }
}