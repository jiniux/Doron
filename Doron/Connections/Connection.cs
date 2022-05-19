using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
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

        public bool Available => _inputStream.CanRead && _outputStream.CanWrite;

        public Connection(Socket socket) : this(new NetworkStream(socket, FileAccess.ReadWrite, ownsSocket: true)) {}

        public Connection(Stream stream) : this(stream, stream) {}

        public async ValueTask SendAsync(ReadOnlyMemory<byte> memory) =>
            await _outputStream.WriteAsync(memory);

        public ValueTask SendAsync(string data) =>
            this.SendAsync(data, Encoding.ASCII);

        public ValueTask SendAsync(string data, Encoding encoding) =>
            _outputStream.WriteAsync(encoding.GetBytes(data));

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
                ReadResult result = await _reader.ReadAsync();
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
            ReadResult result = await _reader.ReadAtLeastAsync(outBuffer.Length);
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
