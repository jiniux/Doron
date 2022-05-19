using Doron;
using Doron.Connections;
using Doron.Extensions;

Console.WriteLine("Doron example - echo server");
Server server = new Server(3000);

async Task HandleConnection(WebSocketConnection connection)
{
    using (connection)
    {
        while (true)
        {
            try
            {
                var actionResult = await connection.ReceiveMessageAsync().RunWithTimeout(10000);

                if (actionResult.Status != WebSocketConnection.WebSocketActionStatus.Ok)
                    break;

                if (actionResult.Result is WebSocketMessage.Text or WebSocketMessage.Binary)
                    await connection.SendMessageAsync(actionResult.Result!);
            }
            catch
            {
                return;
            }
        }
    }
}

async Task AcceptConnections()
{
    while (true)
    {
        WebSocketConnection connection = await server.AcceptConnectionAsync();
        _ = HandleConnection(connection);
    }
}

await Task.WhenAny(server.RunAsync(), AcceptConnections());
