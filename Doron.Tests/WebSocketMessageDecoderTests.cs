using Doron.Connections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            HttpConnection.HttpRequestHeader mockRequestHeader = new HttpConnection.HttpRequestHeader(
                "GET", "/", "", new Dictionary<string, string> { { "", ""} }
            );
            using MemoryStream stream = new MemoryStream(new byte[] { 129, 131, 61, 84, 35, 6, 112, 16, 109 });
            Connection connection = new Connection(stream);

            WebSocketConnection.WebSocketActionResult<WebSocketMessage> actionResult = await new WebSocketConnection(connection, mockRequestHeader).ReceiveMessageAsync();
            Assert.AreEqual(actionResult.Status, WebSocketConnection.WebSocketActionStatus.Ok);
            Assert.IsTrue(actionResult.Result is WebSocketMessage.Text);

            WebSocketMessage.Text message = (WebSocketMessage.Text) actionResult.Result!;
            
            Assert.AreEqual(message.Opcode, 1);
            Assert.AreEqual(message.PayloadLength, 3);
            Assert.AreEqual(message.Data, "MDN");
        }
    }
}
