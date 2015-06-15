using Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public interface ISequencer
    {

        void SetGatingSequences(params Sequence[] sequences);

        ISequenceBarrier NewBarrier(params Sequence[] sequencesToTrack);

        int Size { get; }

        long Cursor { get; }

        long Next();

        long TryNext(int availableCapacity);

        long Claim(long sequence);

        void Publish(long sequence);

        void ForcePublish(long sequence);

        long RemainingCapacity();

        bool HasCapacityAvailable(int availableCapacity);
        
        bool HasCapacityAvailable(Sequence[] gatingSequences, int requiredCapacity, long cursorValue);
    }
}
