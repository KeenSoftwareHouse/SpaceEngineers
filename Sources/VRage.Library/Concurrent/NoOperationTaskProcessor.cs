using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Concurrent
{
    /// <summary>
    /// No operation version of a <see cref="ITaskProcessor"/> that simply tracks a <see cref="Sequencer"/>.
    /// This is useful in tests or for pre-filling a <see cref="MyConcurrentCircularQueue{T}"/> from a producer.
    /// </summary>
    public class NoOperationTaskProcessor : ITaskProcessor
    {

        private readonly SequencerFollowingSequence sequence;

        public NoOperationTaskProcessor(ISequencer sequencer)
        {
            sequence = new SequencerFollowingSequence(sequencer);
        }

        public Sequence Sequence
        {
            get { return sequence; }
        }

        public void Halt()
        {
        }

        public void DoWork()
        {
        }


        private sealed class SequencerFollowingSequence : Sequence
        {
            private readonly ISequencer sequencer;

            public SequencerFollowingSequence(ISequencer sequencer)
                : base(-1)
            {
                this.sequencer = sequencer;
            }

            public override long Value
            {
                get { return sequencer.Cursor; }
            }
        }
    }
}
