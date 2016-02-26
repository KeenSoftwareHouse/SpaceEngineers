using System;
using System.Diagnostics;
using System.Threading;

namespace VRage
{
    public sealed class FastResourceLock : IDisposable, IResourceLock
    {
        private const int LockOwned = 0x1;

        private const int LockExclusiveWaking = 0x2;

        private const int LockSharedOwnersShift = 2;
        private const int LockSharedOwnersMask = 0x3ff;
        private const int LockSharedOwnersIncrement = 0x4;

        private const int LockSharedWaitersShift = 12;
        private const int LockSharedWaitersMask = 0x3ff;
        private const int LockSharedWaitersIncrement = 0x1000;

        private const int LockExclusiveWaitersShift = 22;
        private const int LockExclusiveWaitersMask = 0x3ff;
        private const int LockExclusiveWaitersIncrement = 0x400000;

        private const int ExclusiveMask = LockExclusiveWaking | (LockExclusiveWaitersMask << LockExclusiveWaitersShift);

        public struct Statistics
        {
            public int AcqExcl;
            public int AcqShrd;
            public int AcqExclCont;
            public int AcqShrdCont;
            public int AcqExclSlp;
            public int AcqShrdSlp;
            public int PeakExclWtrsCount;
            public int PeakShrdWtrsCount;
        }

        private static readonly int SpinCount = NativeMethods.SpinCount;

        private int _value;
        private IntPtr _sharedWakeEvent;
        private IntPtr _exclusiveWakeEvent;

        public FastResourceLock()
        {
            _value = 0;
        }

        ~FastResourceLock()
        {
            this.Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (_sharedWakeEvent != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(_sharedWakeEvent);
                _sharedWakeEvent = IntPtr.Zero;
            }

            if (_exclusiveWakeEvent != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(_exclusiveWakeEvent);
                _exclusiveWakeEvent = IntPtr.Zero;
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public int ExclusiveWaiters
        {
            get { return (_value >> LockExclusiveWaitersShift) & LockExclusiveWaitersMask; }
        }

        public bool Owned
        {
            get { return (_value & LockOwned) != 0; }
        }

        public int SharedOwners
        {
            get { return (_value >> LockSharedOwnersShift) & LockSharedOwnersMask; }
        }

        public int SharedWaiters
        {
            get { return (_value >> LockSharedWaitersShift) & LockSharedWaitersMask; }
        }

        [DebuggerStepThrough]
        public void AcquireExclusive()
        {
            int value;
            int i = 0;

            while (true)
            {
                value = _value;

                if ((value & (LockOwned | LockExclusiveWaking)) == 0)
                {
                    if (Interlocked.CompareExchange(
                        ref _value,
                        value + LockOwned,
                        value
                        ) == value)
                        break;
                }
                else if (i >= SpinCount)
                {
                    this.EnsureEventCreated(ref _exclusiveWakeEvent);

                    if (Interlocked.CompareExchange(
                        ref _value,
                        value + LockExclusiveWaitersIncrement,
                        value
                        ) == value)
                    {
                        if (NativeMethods.WaitForSingleObject(
                            _exclusiveWakeEvent,
                            Timeout.Infinite
                            ) != NativeMethods.WaitObject0)
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }

                        do
                        {
                            value = _value;
                        } while (Interlocked.CompareExchange(
                            ref _value,
                            value + LockOwned - LockExclusiveWaking,
                            value
                            ) != value);

                        break;
                    }
                }
                i++;
            }
        }

        [DebuggerStepThrough]
        public void AcquireShared()
        {
            int value;
            int i = 0;

            while (true)
            {
                value = _value;

                if ((value & (
                    LockOwned |
                    (LockSharedOwnersMask << LockSharedOwnersShift) |
                    ExclusiveMask
                    )) == 0)
                {
                    if (Interlocked.CompareExchange(
                        ref _value,
                        value + LockOwned + LockSharedOwnersIncrement,
                        value
                        ) == value)
                        break;
                }
                else if (
                    (value & LockOwned) != 0 &&
                    ((value >> LockSharedOwnersShift) & LockSharedOwnersMask) != 0 &&
                    (value & ExclusiveMask) == 0
                    )
                {
                    if (Interlocked.CompareExchange(
                        ref _value,
                        value + LockSharedOwnersIncrement,
                        value
                        ) == value)
                        break;
                }
                else if (i >= SpinCount)
                {
                    this.EnsureEventCreated(ref _sharedWakeEvent);

                    if (Interlocked.CompareExchange(
                        ref _value,
                        value + LockSharedWaitersIncrement,
                        value
                        ) == value)
                    {
                        if (NativeMethods.WaitForSingleObject(
                            _sharedWakeEvent,
                            Timeout.Infinite
                            ) != NativeMethods.WaitObject0)
                        {
                            System.Diagnostics.Debug.Assert(false);
                        }

                        continue;
                    }
                }
                i++;
            }
        }

        public void ConvertExclusiveToShared()
        {
            int value;
            int sharedWaiters;

            while (true)
            {
                value = _value;
                sharedWaiters = (value >> LockSharedWaitersShift) & LockSharedWaitersMask;

                if (Interlocked.CompareExchange(
                    ref _value,
                    (value + LockSharedOwnersIncrement) & ~(LockSharedWaitersMask << LockSharedWaitersShift),
                    value
                    ) == value)
                {
                    if (sharedWaiters != 0)
                        NativeMethods.ReleaseSemaphore(_sharedWakeEvent, sharedWaiters, IntPtr.Zero);

                    break;
                }
            }
        }

        private void EnsureEventCreated(ref IntPtr handle)
        {
            if (Thread.VolatileRead(ref handle) != IntPtr.Zero)
                return;

            IntPtr eventHandle = NativeMethods.CreateSemaphore(IntPtr.Zero, 0, int.MaxValue, null);

            if (Interlocked.CompareExchange(ref handle, eventHandle, IntPtr.Zero) != IntPtr.Zero)
                NativeMethods.CloseHandle(eventHandle);
        }

        public Statistics GetStatistics()
        {
            return new Statistics();
        }

        public void ReleaseExclusive()
        {
            int value;

            while (true)
            {
                value = _value;

                if (((value >> LockExclusiveWaitersShift) & LockExclusiveWaitersMask) != 0)
                {
                    if (Interlocked.CompareExchange(
                        ref _value,
                        value - LockOwned + LockExclusiveWaking - LockExclusiveWaitersIncrement,
                        value
                        ) == value)
                    {
                        NativeMethods.ReleaseSemaphore(_exclusiveWakeEvent, 1, IntPtr.Zero);

                        break;
                    }
                }
                else
                {
                    int sharedWaiters = (value >> LockSharedWaitersShift) & LockSharedWaitersMask;

                    if (Interlocked.CompareExchange(
                        ref _value,
                        value & ~(LockOwned | (LockSharedWaitersMask << LockSharedWaitersShift)),
                        value
                        ) == value)
                    {
                        if (sharedWaiters != 0)
                            NativeMethods.ReleaseSemaphore(_sharedWakeEvent, sharedWaiters, IntPtr.Zero);

                        break;
                    }
                }
            }
        }

        public void ReleaseShared()
        {
            int value;
            int sharedOwners;

            while (true)
            {
                value = _value;
                sharedOwners = (value >> LockSharedOwnersShift) & LockSharedOwnersMask;

                if (sharedOwners > 1)
                {
                    if (Interlocked.CompareExchange(
                        ref _value,
                        value - LockSharedOwnersIncrement,
                        value
                        ) == value)
                        break;
                }
                else if (((value >> LockExclusiveWaitersShift) & LockExclusiveWaitersMask) != 0)
                {
                    if (Interlocked.CompareExchange(
                        ref _value,
                        value - LockOwned + LockExclusiveWaking - LockSharedOwnersIncrement - LockExclusiveWaitersIncrement,
                        value
                        ) == value)
                    {
                        NativeMethods.ReleaseSemaphore(_exclusiveWakeEvent, 1, IntPtr.Zero);

                        break;
                    }
                }
                else
                {
                    if (Interlocked.CompareExchange(
                        ref _value,
                        value - LockOwned - LockSharedOwnersIncrement,
                        value
                        ) == value)
                        break;
                }
            }
        }

        [DebuggerStepThrough]
        public void SpinAcquireExclusive()
        {
            int value;

            while (true)
            {
                value = _value;

                if ((value & (LockOwned | LockExclusiveWaking)) == 0)
                {
                    if (Interlocked.CompareExchange(
                        ref _value,
                        value + LockOwned,
                        value
                        ) == value)
                        break;
                }

                if (NativeMethods.SpinEnabled)
                    Thread.SpinWait(8);
                else
                    Thread.Sleep(0);
            }
        }

        [DebuggerStepThrough]
        public void SpinAcquireShared()
        {
            int value;

            while (true)
            {
                value = _value;

                if ((value & ExclusiveMask) == 0)
                {
                    if ((value & LockOwned) == 0)
                    {
                        if (Interlocked.CompareExchange(
                            ref _value,
                            value + LockOwned + LockSharedOwnersIncrement,
                            value
                            ) == value)
                            break;
                    }
                    else if (((value >> LockSharedOwnersShift) & LockSharedOwnersMask) != 0)
                    {
                        if (Interlocked.CompareExchange(
                            ref _value,
                            value + LockSharedOwnersIncrement,
                            value
                            ) == value)
                            break;
                    }
                }

                if (NativeMethods.SpinEnabled)
                    Thread.SpinWait(8);
                else
                    Thread.Sleep(0);
            }
        }

        [DebuggerStepThrough]
        /**
         * Two threads calling this at the same time can apparently deadlock waiting for shared readers to reach 1.
         * 
         * So this is basically useless because you can only use it if only if you can guarantee only one thread is calling it.
         */
        public void SpinConvertSharedToExclusive()
        {
            int value;

            while (true)
            {
                value = _value;

                if (((value >> LockSharedOwnersShift) & LockSharedOwnersMask) == 1)
                {
                    if (Interlocked.CompareExchange(
                        ref _value,
                        value - LockSharedOwnersIncrement,
                        value
                        ) == value)
                        break;
                }

                if (NativeMethods.SpinEnabled)
                    Thread.SpinWait(8);
                else
                    Thread.Sleep(0);
            }
        }

        public bool TryAcquireExclusive()
        {
            int value;

            value = _value;

            if ((value & (LockOwned | LockExclusiveWaking)) != 0)
                return false;

            return Interlocked.CompareExchange(
                ref _value,
                value + LockOwned,
                value
                ) == value;
        }

        public bool TryAcquireShared()
        {
            int value;

            value = _value;

            if ((value & ExclusiveMask) != 0)
                return false;

            if ((value & LockOwned) == 0)
            {
                return Interlocked.CompareExchange(
                    ref _value,
                    value + LockOwned + LockSharedOwnersIncrement,
                    value
                    ) == value;
            }

            if (((value >> LockSharedOwnersShift) & LockSharedOwnersMask) != 0)
            {
                return Interlocked.CompareExchange(
                    ref this._value,
                    value + LockSharedOwnersIncrement,
                    value
                    ) == value;
            }

            return false;
        }

        public bool TryConvertSharedToExclusive()
        {
            int value;

            while (true)
            {
                value = _value;

                if (((value >> LockSharedOwnersShift) & LockSharedOwnersMask) != 1)
                    return false;

                if (Interlocked.CompareExchange(
                    ref _value,
                    value - LockSharedOwnersIncrement,
                    value
                    ) == value)
                    return true;
            }
        }
    }
}
