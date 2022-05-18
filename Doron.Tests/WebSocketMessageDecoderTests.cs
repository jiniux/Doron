using Doron.Connections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doron.Tests
{
    internal class WebSocketMessageDecoderTests
    {

        [Test]
        public async Task DecodeTextMessage()
        {
            using MemoryStream stream = new MemoryStream(new byte[] { 129, 131, 61, 84, 35, 6, 112, 16, 109 });
            Connection connection = new Connection(stream);

            WebSocketMessage message = await new WebSocketConnection(connection).ReceiveMessage();
        }
    }
}
