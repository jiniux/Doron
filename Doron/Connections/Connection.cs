﻿using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Doron.Extensions;

namespace Doron.Connections
{
    public class Connection : IDisposable
    {
        private PipeReader _reader;
        
        private Stream _inputStream;
        private Stream _outputStream;

        public IPAddress? RemoteIPAddress { get; }
        public int? RemotePort { get; }
        
        public Guid Guid { get; }

        public int ReceiveTimeout { get; set; } = 35000;
        public int SendTimeout { get; set; } = 10000;

        public bool Available => _inputStream.CanRead && _outputStream.CanWrite;

        public Connection(Socket socket) : this(new NetworkStream(socket, FileAccess.ReadWrite, ownsSocket: true))
        {
            Guid = Guid.NewGuid();
            
            if (socket.RemoteEndPoint is IPEndPoint endPoint)
            {
                RemoteIPAddress = endPoint.Address;
                RemotePort = endPoint.Port;
            }
        }

        public Connection(Stream stream) : this(stream, stream) {}

        public Task SendAsync(ReadOnlyMemory<byte> memory) =>
             _outputStream.WriteAsync(memory).RunWithTimeout(SendTimeout);

        public Task SendAsync(string data) =>
            this.SendAsync(data, Encoding.ASCII);

        public Task SendAsync(string data, Encoding encoding) =>
            _outputStream.WriteAsync(encoding.GetBytes(data)).RunWithTimeout(SendTimeout);

        public Connection(Stream inputStream, Stream outputStream)
        {
            _inputStream = inputStream;
            _outputStream = outputStream;
            
            _reader = PipeReader.Create(inputStream);
        }

        public async Task<string> ReadASCIILineAsync(long limit = long.MaxValue)
        {
            StringBuilder sb = new StringBuilder();

            while (true)
            {
                ReadResult result = await _reader.ReadAsync().RunWithTimeout(ReceiveTimeout);
                ReadOnlySequence<byte> buffer = result.Buffer;

                SequencePosition? endLinePosition = buffer.PositionOf((byte)'\n');
                SequencePosition endPosition = endLinePosition != null ? buffer.GetPosition(1, endLinePosition.Value) : buffer.End;

                ReadOnlySequence<byte> sequence = buffer.Slice(0, endPosition);

                if (sequence.Length > limit)
                    throw new FormatException("Line is too long");

                sb.AppendASCIISequence(sequence);
                _reader.AdvanceTo(endPosition);

                limit -= sequence.Length;

                if (endLinePosition != null)
                    break;

                if (result.IsCompleted)
                    throw new EndOfStreamException();
            }

            return sb.ToString();
        }

        public async ValueTask ReadExact(Memory<byte> outBuffer)
        {
            ReadResult result = await _reader.ReadAtLeastAsync(outBuffer.Length).RunWithTimeout(ReceiveTimeout);
            ReadOnlySequence<byte> buffer = result.Buffer;

            if (buffer.Length == 0)
                throw new EndOfStreamException();

            buffer.Slice(0, outBuffer.Length).CopyTo(outBuffer.Span);

            _reader.AdvanceTo(buffer.GetPosition(outBuffer.Length));
        }

        public void Dispose()
        {
            _inputStream.Dispose();
            _outputStream.Dispose();
        }
    }
}
