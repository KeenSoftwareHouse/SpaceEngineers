using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using VRage.Library.Utils;

namespace VRage.Stats
{
    public enum MyStatTypeEnum : byte
    {
        Unset = 0,
        CurrentValue = 1,
        Min = 2,
        Max = 3,
        Avg = 4,
        MinMax = 5,
        MinMaxAvg = 6,
        Sum = 7,
        Counter = 8,
        CounterSum = 9,

        DontDisappearFlag = 16,
        KeepInactiveLongerFlag = 32,
        LongFlag = 64,
        FormatFlag = 128,
        AllFlags = DontDisappearFlag | KeepInactiveLongerFlag | LongFlag | FormatFlag,
    }

    // Class allows updating values with read-only access
    internal class MyStat
    {
        [StructLayout(LayoutKind.Explicit)]
        internal struct Value
        {
            [FieldOffset(0)]
            public float AsFloat;
            [FieldOffset(0)]
            public long AsLong;
        }

        public String DrawText;

        private MyStatTypeEnum Type;
        private int RefreshRate;
        private int ClearRate;
        private int NumDecimals;
        private MyTimeSpan LastRefresh;
        private MyTimeSpan LastClear;

        private Value Sum;
        private int Count; // Negative values are used to store inactivity frame count
        private Value Min;
        private Value Max;
        private Value Last;

        private Value DrawSum;
        private int DrawCount;
        private Value DrawMin;
        private Value DrawMax;
        private Value DrawLast;

        private SpinLock Lock = new SpinLock();

        public MyStat()
        {
        }

        private int DeltaTimeToInt(MyTimeSpan delta)
        {
            return (int)(1000.0f * delta.Milliseconds + 0.5f);
        }

        private MyTimeSpan IntToDeltaTime(int v)
        {
            return MyTimeSpan.FromMilliseconds(v / 1000.0f);
        }

        public void ReadAndClear(MyTimeSpan currentTime, out Value sum, out int count, out Value min, out Value max, out Value last, out MyStatTypeEnum type, out int decimals, out MyTimeSpan inactivityMs)
        {
            Lock.Enter();
            try
            {
                inactivityMs = MyTimeSpan.Zero;

                if (Count <= 0) // Nothing was written
                {
                    // Load delta, increment it and save it
                    var delta = IntToDeltaTime(-Count);
                    delta += Count < 0 ? currentTime - LastClear : MyTimeSpan.FromMilliseconds(1);
                    Count = -DeltaTimeToInt(delta);

                    inactivityMs = delta;
                    LastClear = currentTime; // Nothing was written, postpone clear 
                }
                else
                {
                    if (currentTime >= (LastRefresh + MyTimeSpan.FromMilliseconds(RefreshRate)))
                    {
                        DrawSum = Sum;
                        DrawCount = Count;
                        DrawMin = Min;
                        DrawMax = Max;
                        DrawLast = Last;
                        LastRefresh = currentTime;

                        if (ClearRate == -1) // Clear with refresh
                        {
                            Count = 0;
                            ClearUnsafe();
                        }
                    }

                    if (ClearRate != -1 && currentTime >= (LastClear + MyTimeSpan.FromMilliseconds(ClearRate)))
                    {
                        Count = 0;
                        ClearUnsafe();
                        LastClear = currentTime;
                    }
                }

                type = Type;
                decimals = NumDecimals;
            }
            finally
            {
                Lock.Exit();
            }

            // No need lock, not accessed anywhere else outside read
            sum = DrawSum;
            count = DrawCount;
            min = DrawMin;
            max = DrawMax;
            last = DrawLast;
        }

        public void Clear()
        {
            Lock.Enter();
            try
            {
                ClearUnsafe();
                if (Count > 0)
                    Count = 0;
                LastRefresh = MyTimeSpan.Zero;
            }
            finally
            {
                Lock.Exit();
            }
        }

        public void ChangeSettings(MyStatTypeEnum type, int refreshRate, int numDecimals, int clearRate)
        {
            Lock.Enter();
            try
            {
                ChangeSettingsUnsafe(type, refreshRate, numDecimals, clearRate);
            }
            finally
            {
                Lock.Exit();
            }
        }

        public void Write(long value, MyStatTypeEnum type, int refreshRate, int numDecimals, int clearRate)
        {
            Lock.Enter();
            try
            {
                ChangeSettingsUnsafe(type | MyStatTypeEnum.LongFlag, refreshRate, numDecimals, clearRate);
                WriteUnsafe(value);
            }
            finally
            {
                Lock.Exit();
            }
        }

        public void Write(float value, MyStatTypeEnum type, int refreshRate, int numDecimals, int clearRate)
        {
            Lock.Enter();
            try
            {
                ChangeSettingsUnsafe(type & ~MyStatTypeEnum.LongFlag, refreshRate, numDecimals, clearRate);
                WriteUnsafe(value);
            }
            finally
            {
                Lock.Exit();
            }
        }

        public void Write(float value)
        {
            Lock.Enter();
            try
            {
                WriteUnsafe(value);
            }
            finally
            {
                Lock.Exit();
            }
        }

        private void ChangeSettingsUnsafe(MyStatTypeEnum type, int refreshRate, int numDecimals, int clearRate)
        {
            Debug.Assert(Type == MyStatTypeEnum.Unset || type == Type, "Changing stat type");
            Type = type;
            RefreshRate = refreshRate;
            ClearRate = clearRate;
            NumDecimals = numDecimals;
        }

        private void WriteUnsafe(float value)
        {
            Last.AsFloat = value;
            Count = Math.Max(1, Count + 1);
            Sum.AsFloat += value;
            Min.AsFloat = Math.Min(Min.AsFloat, value);
            Max.AsFloat = Math.Max(Max.AsFloat, value);
        }

        private void WriteUnsafe(long value)
        {
            Last.AsLong = value;
            Count = Math.Max(1, Count + 1);
            Sum.AsLong += value;
            Min.AsLong = Math.Min(Min.AsLong, value);
            Max.AsLong = Math.Max(Max.AsLong, value);
        }

        private void ClearUnsafe()
        {
            if ((Type & MyStatTypeEnum.LongFlag) == MyStatTypeEnum.LongFlag)
            {
                Sum.AsLong = 0;
                Min.AsLong = long.MaxValue;
                Max.AsLong = long.MinValue;
                Last.AsLong = 0;
            }
            else
            {
                Sum.AsFloat = 0;
                Min.AsFloat = float.MaxValue;
                Max.AsFloat = float.MinValue;
                Last.AsFloat = 0;
            }
        }
    }
}
