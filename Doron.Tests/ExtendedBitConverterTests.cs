using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doron.Tests
{
    internal class ExtendedBitConverterTests
    {
        [Test]
        public void UnsafeBytesBEToUShort()
        {
            Span<byte> bytes = stackalloc byte[] { 0x85, 0x4C };
            Assert.AreEqual(34124, ExtendedBitConverter.UnsafeBEBytesToUShort(bytes));
        }

        [Test]
        public void UnsafeBytesBEToULong()
        {
            Span<byte> bytes = stackalloc byte[] { 0x21, 0x34, 0x85, 0x20, 0x00, 0x00, 0x00, 0x00 };
            Assert.AreEqual(2392683674526023680, ExtendedBitConverter.UnsafeBEBytesToULong(bytes));
        }
    }
}
