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
    public class HttpConnectionTests
    {
        [Test]
        public async Task ParseRequestHeader()
        {
            byte[] data = Encoding.ASCII.GetBytes(
                "GET / HTTP/1.0\r\n" +
                "host: abcd\r\n" +
                "user-agent: efgh\r\n" +
                "\r\n");

            using MemoryStream stream = new MemoryStream(data);
            Connection connection = new Connection(stream);

            HttpConnection httpConnection = new HttpConnection(connection);
            HttpConnection.HttpRequestHeader request = await httpConnection.ReceiveRequestHeaderAsync();
        }

        [Test]
        public void ThrowIfHeaderTooLong()
        {
            byte[] data = Encoding.ASCII.GetBytes(
                "GET / HTTP/1.0\r\n" +
                "host: abcd\r\n" + new string('+', 10000) + "\r\n" +
                "user-agent: efgh\r\n" +
                "\r\n");
             
            using MemoryStream stream = new MemoryStream(data);
            Connection connection = new Connection(stream);

            HttpConnection httpConnection = new HttpConnection(connection);
            Assert.ThrowsAsync<FormatException>(async () => await httpConnection.ReceiveRequestHeaderAsync());
        }

        [Test]
        public async Task SendHeader()
        {
            using MemoryStream stream = new MemoryStream();
            Connection connection = new Connection(stream);

            HttpConnection httpConnection = new HttpConnection(connection);
            await httpConnection.SendResponseHeaderAsync(
                new HttpConnection.HttpResponseHeader("HTTP/1.1", 101, "Switching Protocols", new Dictionary<string, string>()
                {
                    { "AAaaa", "AAaaa" }
                }));

            CollectionAssert.AreEqual(stream.ToArray(), Encoding.ASCII.GetBytes(
                "HTTP/1.1 101 Switching Protocols\r\nAAaaa: AAaaa\r\n\r\n"
            ));
        }

    }
}
