using Atomic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    public class ProcessingSequenceBarrier : ISequenceBarrier
    {
        private readonly IWaitStrategy waitStrategy;
        private readonly Sequence cursorSequence;
        private readonly Sequence[] dependentSequences;
        private volatile bool alerted;



        public ProcessingSequenceBarrier(IWaitStrategy waitStrategy,
                                  Sequence cursorSequence,
                                  Sequence[] dependentSequences)
        {
            this.waitStrategy = waitStrategy;
            this.cursorSequence = cursorSequence;
            this.dependentSequences = dependentSequences;
        }

        public long WaitFor(long sequence)
        {
            CheckAlert();

            return waitStrategy.WaitFor(sequence, cursorSequence, dependentSequences, this);
        }

        public long WaitFor(long sequence, TimeSpan timeout)
        {
            CheckAlert();

            return waitStrategy.WaitFor(sequence, cursorSequence, dependentSequences, this, timeout);
        }

        public long Cursor
        {
            get { return cursorSequence.Value; }
        }

        public bool IsAlerted
        {
            get { return alerted; }
        }

        public void Alert()
        {
            alerted = true;
            waitStrategy.SignalAllWhenBlocking();
        }

        public void ClearAlert()
        {
            alerted = false;
        }

        public void CheckAlert()
        {
            if (alerted)
            {
                throw AlertException.Instance();
            }
        }
    }
}
