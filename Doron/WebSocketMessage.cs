using Doron.Memory;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doron
{
    public abstract record WebSocketMessage
    {
        public abstract int PayloadLength { get; }
        public abstract byte Opcode { get; }
        public abstract void CopyPayloadTo(Memory<byte> target);

        public record Binary(ReadOnlyMemory<byte> Data) : WebSocketMessage
        {
            public override int PayloadLength => Data.Length;

            public override byte Opcode => 0x2;

            public override void CopyPayloadTo(Memory<byte> target) =>
                Data.CopyTo(target);
        }

        public record Text(string Data) : WebSocketMessage
        {
            public override int PayloadLength => Encoding.UTF8.GetByteCount(Data);

            public override byte Opcode => 0x1;

            public override void CopyPayloadTo(Memory<byte> target) =>
                Encoding.UTF8.GetBytes(Data).CopyTo(target);
        }

        public record Close(ushort Code) : WebSocketMessage
        {
            public override int PayloadLength => sizeof(ushort);

            public override byte Opcode => 0x8;

            public override void CopyPayloadTo(Memory<byte> target) =>
                ExtendedBitConverter.CopyUShortToBEBytes(Code, target.Span);
        }

        public record Ping : WebSocketMessage
        {
            public override int PayloadLength => 0;

            public override byte Opcode => 0x9;

            public override void CopyPayloadTo(Memory<byte> target) {}
        }

        public record Pong : WebSocketMessage
        {
            public override int PayloadLength => 0;

            public override byte Opcode => 0xA;

            public override void CopyPayloadTo(Memory<byte> target) { }
        }
    }
}
