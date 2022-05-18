using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doron.Extensions
{
    internal static class StringBuilderExtensions
    {
        public static void AppendASCIISequence(this StringBuilder sb, ReadOnlySequence<byte> sequence)
        {
            foreach (var fragment in sequence)
            {
                ReadOnlySpan<byte> span = fragment.Span;

                for (int i = 0; i < fragment.Length; i++)
                    sb.Append((char)span[i]);
            }
        }

        public static void AppendUTF8Sequence(this StringBuilder sb, ReadOnlySequence<byte> sequence)
        {
            foreach (var fragment in sequence)
                sb.Append(Encoding.UTF8.GetString(fragment.Span));
        }
    }
}
