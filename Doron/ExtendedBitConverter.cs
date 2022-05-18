using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doron
{
    public static class ExtendedBitConverter
    {
        public static unsafe ulong UnsafeBEBytesToULong(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != sizeof(ulong))
                throw new ArgumentException("Span is not the size of ulong.");

            Span<byte> tempBuffer = stackalloc byte[bytes.Length];
            bytes.CopyTo(tempBuffer);

            if (BitConverter.IsLittleEndian)
                tempBuffer.Reverse();

            fixed (byte* ptr = tempBuffer)
                return *(ulong*)ptr;
        }

        public static unsafe ushort UnsafeBEBytesToUShort(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != sizeof(ushort))
                throw new ArgumentException("Span is not the size of ushort.");

            Span<byte> tempBuffer = stackalloc byte[bytes.Length];
            bytes.CopyTo(tempBuffer);

            if (BitConverter.IsLittleEndian)
                tempBuffer.Reverse();

            fixed (byte* ptr = tempBuffer)
                return *(ushort*)ptr;
        }

        public static unsafe void CopyUShortToBEBytes(ushort value, Span<byte> target)
        {
            Span<byte> temp = stackalloc byte[2];

            temp[0] = (byte)value;
            temp[1] = (byte)(value >> 8);

            if (BitConverter.IsLittleEndian)
                temp.Reverse();

            temp.CopyTo(target);
        }

        public static unsafe void CopyULongToBEBytes(ulong value, Span<byte> target)
        {
            Span<byte> temp = stackalloc byte[8];

            temp[0] = (byte)value;
            temp[1] = (byte)(value >> 8);
            temp[2] = (byte)(value >> 16);
            temp[3] = (byte)(value >> 24);
            temp[4] = (byte)(value >> 32);
            temp[5] = (byte)(value >> 40);
            temp[6] = (byte)(value >> 48);
            temp[7] = (byte)(value >> 56);

            if (BitConverter.IsLittleEndian)
                temp.Reverse();

            temp.CopyTo(target);
        }
    }
}
