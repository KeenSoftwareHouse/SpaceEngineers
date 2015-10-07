#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using VRage.Library.Utils;

#endregion

namespace VRage.Profiler
{
    public partial class MyProfiler
    {
        public class MyProfilerBlock
        {
            public readonly int Id;
            public readonly MyProfilerBlockKey Key;
            public readonly int ForceOrder;

            public string Name { get { return Key.Name; } }

            // Immediate data not accessed by Draw
            public MyTimeSpan MeasureStart;
            public MyTimeSpan Elapsed;
            public long StartManagedMB = 0;
            public long EndManagedMB = 0;
            public float DeltaManagedB = 0; //?
            public float TotalManagedMB = 0; //?
            public long StartProcessMB = 0;
            public long EndProcessMB = 0;
            public float DeltaProcessB = 0;       //?
            public float TotalProcessMB = 0;      //?

            public bool Invalid = false;

            public int NumCalls = 0;
            public float CustomValue = 0;

            public string TimeFormat;
            public string ValueFormat;
            public string CallFormat;

            public float ManagedDeltaMB
            {
                //conversion to MB
                get { return (DeltaManagedB) * 0.000000953674f; }
            }

            public float ProcessDeltaMB
            {
                //conversion to MB
                get { return (DeltaProcessB) * 0.000000953674f; }
            }

            // History data not accessed by Start/End
            public float[] ProcessMemory = new float[MAX_FRAMES];
            public float[] ManagedMemory = new float[MAX_FRAMES];
            public float[] Miliseconds = new float[MAX_FRAMES];
            public float[] CustomValues = new float[MAX_FRAMES];
            public int[] NumCallsArray = new int[MAX_FRAMES];

            // Unused
            public float averageMiliseconds;

            public List<MyProfilerBlock> Children = new List<MyProfilerBlock>();
            public MyProfilerBlock Parent = null;
            
            public MyProfilerBlock(ref MyProfilerBlockKey key, string memberName, int blockId, int forceOrder = int.MaxValue)
            {
                Id = blockId;
                Key = key;
                ForceOrder = forceOrder;
            }

            public void Reset()
            {
                MeasureStart = new MyTimeSpan(Stopwatch.GetTimestamp());
                Elapsed = MyTimeSpan.Zero;
            }

            /// <summary>
            /// Clears immediate data
            /// </summary>
            public void Clear()
            {
                Reset();
                NumCalls = 0;

                StartManagedMB = 0;
                EndManagedMB = 0;
                DeltaManagedB = 0;

                StartProcessMB = 0;
                EndProcessMB = 0;
                DeltaProcessB = 0;

                CustomValue = 0;
            }

            public void Start(bool memoryProfiling)
            {
                NumCalls++;

                if (memoryProfiling)
                {
                    StartManagedMB = GC.GetTotalMemory(false);   // About 1ms per 2000 calls

                    // TODO: OP! Use better non-alloc call
                    StartProcessMB = Environment.WorkingSet;   // WorkingSet is allocating memory in each call, also its expensive (about 7ms per 2000 calls).
                }

                MeasureStart = new MyTimeSpan(Stopwatch.GetTimestamp());
            }

            public void End(bool memoryProfiling, MyTimeSpan? customTime = null)
            {
                MyTimeSpan delta = customTime ?? (new MyTimeSpan(Stopwatch.GetTimestamp()) - MeasureStart);
                Elapsed += delta;

                if (memoryProfiling)
                {
                    EndManagedMB = System.GC.GetTotalMemory(false);
                    EndProcessMB = System.Environment.WorkingSet;
                }

                DeltaManagedB += EndManagedMB - StartManagedMB;
                if (memoryProfiling)
                {
                    DeltaProcessB += EndProcessMB - StartProcessMB;
                }
            }

            public override string ToString()
            {
                return Key.Name + " (" + NumCalls.ToString() + " calls)";
            }
        }
    }
}
