using NUnit.Framework;
using Doron;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Doron.Connections;

namespace Doron.Tests
{
    public class ConnectionTests
    {
        [Test]
        public async Task ReadExact()
        {
            byte[] originalBuffer = { 255, 255, 2, 3, 4 };
            using MemoryStream stream = new MemoryStream(originalBuffer);
            Connection connection = new Connection(stream);

            byte[] buffer = new byte[2];
            await connection.ReadExact(buffer);

            byte[] buffer2 = new byte[3];
            await connection.ReadExact(buffer2);

            CollectionAssert.AreEqual(originalBuffer[..2], buffer);
            CollectionAssert.AreEqual(originalBuffer[2..], buffer2);
        }

        [Test]
        public async Task ReadLineASCII()
        {
            string str = "Test1\nTest2\nTest3\n";

            using MemoryStream stream = new MemoryStream(Encoding.ASCII.GetBytes(str));
            Connection connection = new Connection(stream);

            Assert.AreEqual(await connection.ReadASCIILineAsync(), "Test1\n");
            Assert.AreEqual(await connection.ReadASCIILineAsync(), "Test2\n");
            Assert.AreEqual(await connection.ReadASCIILineAsync(), "Test3\n");
        }
    }
}