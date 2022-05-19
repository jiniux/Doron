using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;


namespace Doron.Connections
{
    using HeaderFields = IReadOnlyDictionary<string, string>;

    public class HttpConnection
    {
        public record HttpResponseHeader(string Version, int Code, string Message, HeaderFields HeaderFields)
        {
            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append($"{Version} {Code} {Message}\r\n");
                
                foreach (var field in HeaderFields)
                    sb.Append($"{field.Key}: {field.Value}\r\n");

                sb.Append("\r\n");

                return sb.ToString();
            }
        }

        public record HttpRequestHeader(string Method, string Path, string Version, HeaderFields HeaderFields);

        Connection _connection;

        public HttpConnection(Connection connection)
        {
            _connection = connection;
        }

        private static (string, string, string) ParseRequestLine(string line)
        {
            string[] components = line.Split(' ');

            if (components.Length != 3)
                throw new FormatException("Invalid request line");

            return (components[0], components[1], components[2]);
        }

        private static (string, string) ParseHeaderField(string headerFieldLine)
        {
            string[] components = headerFieldLine.Split(':', 2).ToArray();

            if (components.Length != 2)
                throw new FormatException("Invalid header field");

            return (components[0].Trim(), components[1].Trim());
        }

        public async Task<WebSocketConnection> Upgrade()
        {
            HttpRequestHeader requestHeader = await ReceiveRequestHeaderAsync();

            var swk = requestHeader.HeaderFields["Sec-WebSocket-Key"];

            string swka = swk + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            string hash = Crypto.ComputeSHA1Base64(Encoding.ASCII.GetBytes(swka));

            await _connection.SendAsync(new HttpResponseHeader
            (
                "HTTP/1.1",
                101,
                "Switching Protocols",
                new Dictionary<string, string>
                {
                    { "Connection", "Upgrade" },
                    { "Upgrade", "websocket" },
                    { "Sec-WebSocket-Accept", hash }
                }
            ).ToString());

            return new WebSocketConnection(_connection);
        }

        public Task SendResponseHeaderAsync(HttpResponseHeader responseHeader) =>
            _connection.SendAsync(responseHeader.ToString());

        public async Task<HttpRequestHeader> ReceiveRequestHeaderAsync()
        {
            const int headerMaxSize = 4096;

            int currentLimit = headerMaxSize;

            async Task<string> ReadLineAndDecrementLimit()
            {
                string line = (await _connection.ReadASCIILineAsync(currentLimit));
                currentLimit -= line.Length;

                return line.Trim();
            }

            var (method, version, path) = ParseRequestLine(await ReadLineAndDecrementLimit());

            Dictionary<string, string> headerFields = new Dictionary<string, string>();

            string headerFieldLine;
            while ((headerFieldLine = await ReadLineAndDecrementLimit()) != string.Empty)
            {
                var (key, value) = ParseHeaderField(headerFieldLine);
                headerFields.Add(key, value);
            }

            return new HttpRequestHeader(method, version, path, headerFields);
        }
    }
}
