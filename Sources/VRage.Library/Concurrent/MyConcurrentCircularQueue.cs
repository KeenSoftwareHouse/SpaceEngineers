using Atomic;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Concurrent
{
    public class MyConcurrentCircularQueue<T> : ISequencer where T : class
    {
        private readonly Sequence CursorSequence = new Sequence(-1);
        private readonly int buffersize;
        private readonly int indexMask;
        private readonly T[] buffer;
        private readonly ThreadLocal<MutableLong> minGatingSequence;
        private Sequence claimSequence = new Sequence(-1);
        private Sequence[] gatingSequences;
        private IWaitStrategy waitStrategy;



        public MyConcurrentCircularQueue()
            : this(128, new SleepingWaitStrategy())
        {
        }

        public MyConcurrentCircularQueue(Func<T> tasks)
            : this(tasks, 128, new SleepingWaitStrategy())
        {
        }

        public MyConcurrentCircularQueue(int buffersize)
            : this(buffersize, new SleepingWaitStrategy())
        {
        }

        public MyConcurrentCircularQueue(IWaitStrategy waitStrategy)
            : this(128, waitStrategy)
        {
        }

        public MyConcurrentCircularQueue(Func<T> tasks, IWaitStrategy waitStrategy)
            : this(tasks, 128, waitStrategy)
        {
        }

        public MyConcurrentCircularQueue(Func<T> tasks, int buffersize)
            : this(tasks, buffersize, new SleepingWaitStrategy())
        {
        }

        public MyConcurrentCircularQueue(int buffersize, IWaitStrategy waitStrtegy)
        {
            if (Util.CeilingNextPowerOfTwo(buffersize) != buffersize)
            {
                throw new ArgumentException("bufferSize must be a power of 2");
            }

            this.waitStrategy = waitStrtegy;
            this.buffersize = buffersize;
            indexMask = buffersize - 1;

            minGatingSequence = new ThreadLocal<MutableLong>(() => new MutableLong(-1));
            buffer = new T[buffersize];
        }

        public MyConcurrentCircularQueue(Func<T> tasks, int buffersize, IWaitStrategy waitStrtegy)
        {
            if (Util.CeilingNextPowerOfTwo(buffersize) != buffersize)
            {
                throw new ArgumentException("bufferSize must be a power of 2");
            }

            this.waitStrategy = waitStrtegy;
            this.buffersize = buffersize;
            indexMask = buffersize - 1;

            minGatingSequence = new ThreadLocal<MutableLong>(() => new MutableLong(-1));
            buffer = new T[buffersize];

            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = tasks();
            }
        }

        public long Cursor { get { return CursorSequence.Value; } }

        public ISequenceBarrier NewBarrier(params Sequence[] sequencesToTrack)
        {
            return new ProcessingSequenceBarrier(waitStrategy, CursorSequence, sequencesToTrack);
        }

        public void SetGatingSequences(params Sequence[] sequences)
        {
            gatingSequences = sequences;
        }

        public T this[long sequence]
        {
            get { return buffer[(int)sequence & indexMask]; }
        }



        public void Publish(long sequence)
        {
            Publish(sequence, 1);
        }

        private void Publish(long lo, int hi)
        {
            long expectedSequence = lo - hi;
            while (expectedSequence != CursorSequence.Value)
            {
                // busy spin
            }

            CursorSequence.LazySet(lo);

            waitStrategy.SignalAllWhenBlocking();
        }

        public void ForcePublish(long sequence)
        {
            CursorSequence.LazySet(sequence);
            waitStrategy.SignalAllWhenBlocking();
        }
        public long Next()
        {
            if (gatingSequences == null)
            {
                throw new NullReferenceException("gatingSequences must be set before claiming sequences");
            }

            return IncrementAndGet(gatingSequences);
        }

        public long TryNext(int availableCapacity)
        {
            if (gatingSequences == null)
            {
                throw new NullReferenceException("gatingSequences must be set before claiming sequences");
            }

            if (availableCapacity < 1)
            {
                throw new ArgumentOutOfRangeException("availableCapacity", "Available capacity must be greater than 0");
            }

            return CheckAndIncrement(availableCapacity, 1, gatingSequences);
        }

        public long Claim(long sequence)
        {
            if (gatingSequences == null)
            {
                throw new NullReferenceException("gatingSequences must be set before claiming sequences");
            }

            SetSequence(sequence, gatingSequences);

            return sequence;
        }

        public long Sequence
        {
            get { return claimSequence.Value; }
        }

        public long CheckAndIncrement(int requiredCapacity, int delta, Sequence[] gatingSequences)
        {
            for (; ; )
            {
                long sequence = CursorSequence.Value;
                if (HasCapacityAvailable(gatingSequences, requiredCapacity, sequence))
                {
                    long nextSequence = sequence + delta;
                    if (CursorSequence.CompareAndSet(sequence, nextSequence))
                    {
                        return nextSequence;
                    }
                }
                else
                {
                    throw InsufficientCapacityException.Instance();
                }
            }
        }

        public long IncrementAndGet(Sequence[] dependentSequences)
        {
            MutableLong minGatingSequence = this.minGatingSequence.Value;
            WaitForCapacity(dependentSequences, minGatingSequence);

            long nextSequence = claimSequence.IncrementAndGet();


            WaitForFreeSlotAt(nextSequence, dependentSequences, minGatingSequence);

            return nextSequence;
        }

        public void SetSequence(long sequence, Sequence[] dependentSequences)
        {
            CursorSequence.Value = sequence;
            WaitForFreeSlotAt(sequence, dependentSequences, minGatingSequence.Value);
        }
        public void PublishBatch(long sequence, AtomicPaddedLong cursor, long batchSize)
        {
            long expectedSequence = sequence - batchSize;

            while (expectedSequence != cursor.ReadUnfenced()) { }

            cursor.WriteUnfenced(sequence);
        }



        private void GetNextSequence(long sequence, Sequence cursor, long batchSize)
        {
            long expectedSequence = sequence - batchSize;

            while (expectedSequence != cursor.Value) { }

            cursor.LazySet(sequence);
        }

        private void WaitForFreeSlotAt(long sequence, Sequence[] dependentSequences, MutableLong minGatingSequence)
        {
            long wrapPoint = sequence - buffersize;
            if (wrapPoint > minGatingSequence.Value)
            {
                var spinWait = default(SpinWait);
                long minSequence;
                while (wrapPoint > (minSequence = Util.GetMinimumSequence(dependentSequences)))
                {
                    spinWait.SpinOnce();
                }

                minGatingSequence.Value = minSequence;
            }
        }

        private void WaitForCapacity(Sequence[] dependentSequences, MutableLong minGatingSequence)
        {
            long wrapPoint = (CursorSequence.Value + 1L) - buffersize;
            if (wrapPoint > minGatingSequence.Value)
            {
                var spinWait = default(SpinWait);
                long minSequence;
                while (wrapPoint > (minSequence = Util.GetMinimumSequence(dependentSequences)))
                {
                    spinWait.SpinOnce();
                }

                minGatingSequence.Value = minSequence;
            }
        }

        public bool HasCapacityAvailable(int requiredCapacity)
        {
            return HasCapacityAvailable(this.gatingSequences, requiredCapacity, CursorSequence.Value);
        }

        public bool HasCapacityAvailable(Sequence[] gatingSequences, int requiredCapacity, long cursorValue)
        {
            long wrapPoint = (cursorValue + requiredCapacity) - buffersize;
            MutableLong minGatingSequence = this.minGatingSequence.Value;
            if (wrapPoint > minGatingSequence.Value)
            {
                long minSequence = Util.GetMinimumSequence(gatingSequences);
                minGatingSequence.Value = minSequence;

                if (wrapPoint > minSequence)
                {
                    return false;
                }
            }

            return true;
        }

        public int Size
        {
            get { return buffersize; }
        }

        public long RemainingCapacity()
        {
            long consumed = Util.GetMinimumSequence(gatingSequences);
            long produced = CursorSequence.Value;
            return Size - (produced - consumed);
        }
    }
}
