using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Doron.Memory
{
    internal class MemoryOwnerSegment<T> : ReadOnlySequenceSegment<T>, IDisposable
    {
        private IMemoryOwner<T> _owner;

        public MemoryOwnerSegment(IMemoryOwner<T> owner, int offset, int length)
        {
            _owner = owner;
            Memory = owner.Memory[offset..length];
        }

        public MemoryOwnerSegment<T> Append(IMemoryOwner<T> owner, int offset, int length)
        {
            var segment = new MemoryOwnerSegment<T>(owner, offset, length)
            {
                RunningIndex = RunningIndex + Memory.Length
            };

            Next = segment;

            return segment;
        }

        public void Dispose()
        {
            _owner.Dispose();
        }
    }
}
