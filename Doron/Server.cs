using System.Net;
using System.Net.Sockets;
using System.Threading.Channels;
using Doron.Connections;
using Doron.Extensions;

namespace Doron
{
    public class Server
    {
        private readonly TcpListener _listener;

        public int HandshakeTimeout { get; init; } = 10000;

        public Server(IPAddress address, int port) 
        {
            _listener = new TcpListener(address, port);
            _running = false;

            _inboundConnections = Channel.CreateUnbounded<WebSocketConnection>();
        }

        public Server(int port) : this(IPAddress.Any, port) { }

        private volatile bool _running;

        private async Task<WebSocketConnection> BeginHandshake(Socket socket) =>
            await new HttpConnection(new Connection(socket)).Upgrade();

        Channel<WebSocketConnection> _inboundConnections;

        public ValueTask<WebSocketConnection> AcceptConnectionAsync() => 
            _inboundConnections.Reader.ReadAsync();

        private async Task AcceptSocketAsync(Socket socket)
        {
            try
            {
                WebSocketConnection webSocketConnection = await BeginHandshake(socket).RunWithTimeout(HandshakeTimeout);
                await _inboundConnections.Writer.WriteAsync(webSocketConnection);
            }
            catch
            {
                socket.Close();
            }
        }
        
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            if (_running)
                throw new InvalidOperationException("The server is already running.");

            _running = true;
            _listener.Start();
            cancellationToken.Register(() => _listener.Stop());

            try
            {
                for (; ; )
                {
                    Socket socket = await _listener.AcceptSocketAsync(cancellationToken);
                    _ = AcceptSocketAsync(socket);
                }
            }
            finally
            {
                _listener.Stop();
                _running = false;
            }
        }
    }
}