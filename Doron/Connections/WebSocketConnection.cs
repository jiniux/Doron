using Doron.Memory;
using Doron.Extensions;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Doron.Types;

namespace Doron.Connections
{
    public class WebSocketConnection : IDisposable
    {
        Connection _connection;

        public int MaxMessageLength { get; set; } = int.MaxValue;
        
        public WebSocketConnection(Connection connection)
        {
            _connection = connection;
        }

        public Connection RawConnection => _connection;

        public ValueTask<WebSocketActionResult<EmptyRef>> SendMessageAsync(WebSocketMessage message) =>
            HandleAction(async () =>
            {
                await InternalSendMessageAsync(message);
                return new EmptyRef();
            });

        private async Task InternalSendMessageAsync(WebSocketMessage message)
        {
            int payloadLength = message.PayloadLength;
            using IMemoryOwner<byte> messageBuffer = MemoryPool<byte>.Shared.Rent(10 + payloadLength);
            
            int payloadOffset;

            // Sent messages have always FIN set to 1.
            messageBuffer.Memory.Span[0] = (byte)(0b10000000 | message.Opcode);

            // Sent messages have always MASK set to 0.
            if (payloadLength <= 125)
            {
                messageBuffer.Memory.Span[1] = (byte)payloadLength;
                payloadOffset = 2;
            }
            else if (payloadLength is >= 126 and <= ushort.MaxValue)
            {
                messageBuffer.Memory.Span[1] = 126;
                ExtendedBitConverter.CopyUShortToBEBytes((ushort)payloadLength, messageBuffer.Memory.Span[2..4]);
                payloadOffset = 4;
            }
            else
            {
                messageBuffer.Memory.Span[1] = 127;
                ExtendedBitConverter.CopyULongToBEBytes((ulong)payloadLength, messageBuffer.Memory.Span[2..10]);
                payloadOffset = 10;
            }

            message.CopyPayloadTo(messageBuffer.Memory[payloadOffset..]);

            await _connection.SendAsync(messageBuffer.Memory[..(payloadOffset + payloadLength)]);
        }

        private (bool End, bool IsMasked, byte Opcode, byte LengthFlag) DecodeHeader(Memory<byte> headerData)
        {
            bool end, isMasked;
            byte opcode, lengthFlag;

            // Decode first byte
            {
                byte fb = headerData.Span[0];
                end = (fb & 0b10000000) != 0;
                opcode = (byte)(fb & 0b00001111);
            }

            // Decode second byte
            {
                byte sb = headerData.Span[1];

                isMasked = (byte)(sb & 0b10000000) != 0;
                lengthFlag = (byte)(sb & 0b01111111);
            }

            return (end, isMasked, opcode, lengthFlag);
        }

        private string GetTextFromPayload(ReadOnlySequence<byte> payload)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendUTF8Sequence(payload);

            return stringBuilder.ToString();
        }

        private byte[] GetBytesFromPayload(ReadOnlySequence<byte> payload)
        {
            byte[] bytes = new byte[payload.Length];

            payload.CopyTo(bytes);
            return bytes;
        }

        private ushort GetCloseCodeFromPayload(ReadOnlySequence<byte> payload)
        {
            Span<byte> temp = stackalloc byte[2];
            payload.CopyTo(temp);

            return ExtendedBitConverter.UnsafeBEBytesToUShort(temp);
        }
        
        public enum WebSocketActionStatus : byte
        {
            Ok,
            Exception,
            ConnectionClosed
        }
        
        public readonly struct WebSocketActionResult<T> where T: class
        {
            internal WebSocketActionResult(WebSocketActionStatus status, Exception? exception = null, T? result = null)
            {
                Status = status;
                Exception = exception;
                Result = result;
            } 

            public WebSocketActionStatus Status { get; }
            public Exception? Exception { get; }
            public T? Result { get; }
        }

        private async ValueTask<WebSocketActionResult<TK>> HandleAction<TK>(Func<ValueTask<TK>> action) where TK: class
        {
            if (!_connection.Available)
                return new WebSocketActionResult<TK>(WebSocketActionStatus.ConnectionClosed);

            TK result;
            try
            {
                result = await action();
            }
            catch (Exception exception)
            {
                _connection.Dispose();
                
                if (exception is IOException or EndOfStreamException)
                    return new WebSocketActionResult<TK>(WebSocketActionStatus.ConnectionClosed);

                return new WebSocketActionResult<TK>(WebSocketActionStatus.Exception, exception: exception);
            }

            return new WebSocketActionResult<TK>(WebSocketActionStatus.Ok, result: result);
        }

        public ValueTask<WebSocketActionResult<WebSocketMessage>> ReceiveMessageAsync() => 
            HandleAction(async () => await InternalReceiveMessageAsync());

        private async Task<WebSocketMessage> InternalReceiveMessageAsync()
        {
            using IMemoryOwner<byte> headerBuffer = MemoryPool<byte>.Shared.Rent(14);
            MemoryOwnerSegment<byte>? firstPayloadSegment = null, lastPayloadSegment = null;

            int maxLength = MaxMessageLength;
            
            int? initialOpcode = null;

            try
            {
                while (true)
                {
                    (bool End, bool IsMasked, ushort Opcode, ushort LengthFlag) header;
                    int length;

                    Memory<byte> maskData = headerBuffer.Memory[10..14]; 

                    // Read header
                    {
                        await _connection.ReadExact(headerBuffer.Memory[..2]);
                        header = DecodeHeader(headerBuffer.Memory[..2]);

                        if (initialOpcode == null)
                            initialOpcode = header.Opcode;
                        else if (initialOpcode != null && header.Opcode != 0x0)
                            throw new InvalidDataException("Only continuation frames allowed before the END.");

                        if (!header.IsMasked)
                            throw new InvalidDataException("Client messages must be masked.");

                        if (header.LengthFlag != 0 && (header.Opcode is 0x9 or 0xA))
                            throw new InvalidDataException("Ping or pong packets must have 0 length.");

                        if (header.LengthFlag == 126)
                        {
                            Memory<byte> shortLengthData = headerBuffer.Memory[2..4];

                            await _connection.ReadExact(shortLengthData);
                            length = ExtendedBitConverter.UnsafeBEBytesToUShort(shortLengthData.Span);
                        }
                        else if (header.LengthFlag == 127)
                        {
                            Memory<byte> longLengthData = headerBuffer.Memory[2..10];

                            await _connection.ReadExact(longLengthData);
                            ulong longLength = ExtendedBitConverter.UnsafeBEBytesToULong(longLengthData.Span);

                            if (longLength > int.MaxValue)
                                throw new InvalidDataException("Message too long.");
                            else
                                length = (int)longLength;
                        }
                        else
                            length = header.LengthFlag;
                        
                        if (length > maxLength)
                            throw new InvalidDataException("Message too long.");

                        maxLength -= length;
                        
                        await _connection.ReadExact(maskData);
                    }

                    // Read payload
                    {
                        IMemoryOwner<byte> payloadBuffer = MemoryPool<byte>.Shared.Rent(length);
                        await _connection.ReadExact(payloadBuffer.Memory[0..length]);

                        for (int i = 0; i < length; ++i)
                        {
                            byte b = payloadBuffer.Memory.Span[i];
                            payloadBuffer.Memory.Span[i] = (byte)(b ^ maskData.Span[i % 4]);
                        }

                        if (lastPayloadSegment == null)
                            firstPayloadSegment = lastPayloadSegment = new MemoryOwnerSegment<byte>(payloadBuffer, 0, length);
                        else
                            lastPayloadSegment = lastPayloadSegment.Append(payloadBuffer, 0, length);

                        if (header.End)
                            break;
                    }
                }

                ReadOnlySequence<byte> payload = new ReadOnlySequence<byte>(firstPayloadSegment!, 0, lastPayloadSegment, lastPayloadSegment.Memory.Length);

                return initialOpcode!.Value switch
                {
                    0x1 => new WebSocketMessage.Text(GetTextFromPayload(payload)),
                    0x2 => new WebSocketMessage.Binary(GetBytesFromPayload(payload)),
                    0x8 => new WebSocketMessage.Close(GetCloseCodeFromPayload(payload)),

                    _ => throw new NotImplementedException(),
                };
            } 
            finally
            {
                MemoryOwnerSegment<byte>? segment = firstPayloadSegment;

                while (segment != null)
                {
                    MemoryOwnerSegment<byte>? next = (MemoryOwnerSegment<byte>?)segment.Next;
                    segment.Dispose();

                    segment = next;   
                }
            }
        }
         
        public void Dispose()
        {
            _connection.Dispose();
        }               
    }
}
