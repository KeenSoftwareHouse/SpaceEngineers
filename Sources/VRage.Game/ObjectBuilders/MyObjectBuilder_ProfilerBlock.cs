using System.Collections.Generic;
using VRage.Profiler;
using VRage.Serialization;

namespace VRage.Game
{
    public class MyObjectBuilder_ProfilerBlock
    {
        public int Id;
        public MyProfilerBlockKey Key;

        public bool Invalid = false;

        public string TimeFormat;
        public string ValueFormat;
        public string CallFormat;

        public float[] ProcessMemory;
        public long[] ManagedMemoryBytes;
        public float[] Miliseconds;
        public float[] CustomValues;
        public int[] NumCallsArray;
        public List<MyProfilerBlockKey> Children;
        public MyProfilerBlockKey Parent;

        public static MyObjectBuilder_ProfilerBlock GetObjectBuilder(MyProfilerBlock profilerBlock)
        {
            MyProfilerBlock.MyProfilerBlockObjectBuilderInfo data = profilerBlock.GetObjectBuilderInfo();
            MyObjectBuilder_ProfilerBlock objectBuilder = new MyObjectBuilder_ProfilerBlock();

            objectBuilder.Id = data.Id;
            objectBuilder.Key = data.Key;

            objectBuilder.Invalid = data.Invalid;
            objectBuilder.TimeFormat = data.TimeFormat;
            objectBuilder.ValueFormat = data.ValueFormat;
            objectBuilder.CallFormat = data.CallFormat;
            objectBuilder.ProcessMemory = data.ProcessMemory;
            objectBuilder.ManagedMemoryBytes = data.ManagedMemoryBytes;
            objectBuilder.Miliseconds = data.Miliseconds;
            objectBuilder.CustomValues = data.CustomValues;
            objectBuilder.NumCallsArray = data.NumCallsArray;

            objectBuilder.Children = new List<MyProfilerBlockKey>();
            foreach (var child in data.Children)
            {
                objectBuilder.Children.Add(child.Key);
            }
            if (data.Parent != null)
                objectBuilder.Parent = data.Parent.Key;

            return objectBuilder;
        }

        public static MyProfilerBlock Init(MyObjectBuilder_ProfilerBlock objectBuilder, MyProfiler.MyProfilerObjectBuilderInfo profiler)
        {
            MyProfilerBlock.MyProfilerBlockObjectBuilderInfo data = new MyProfilerBlock.MyProfilerBlockObjectBuilderInfo();

            data.Id = objectBuilder.Id;
            data.Key = objectBuilder.Key;

            data.Invalid = objectBuilder.Invalid;
            data.TimeFormat = objectBuilder.TimeFormat;
            data.ValueFormat = objectBuilder.ValueFormat;
            data.CallFormat = objectBuilder.CallFormat;
            data.ProcessMemory = objectBuilder.ProcessMemory;
            data.ManagedMemoryBytes = objectBuilder.ManagedMemoryBytes;
            data.Miliseconds = objectBuilder.Miliseconds;
            data.CustomValues = objectBuilder.CustomValues;
            data.NumCallsArray = objectBuilder.NumCallsArray;

            data.Children = new List<MyProfilerBlock>();
            foreach (MyProfilerBlockKey child in objectBuilder.Children)
            {
                data.Children.Add(profiler.ProfilingBlocks[child]);
            }
            if (objectBuilder.Parent.File != null)
                data.Parent = profiler.ProfilingBlocks[objectBuilder.Parent];

            MyProfilerBlock profilerBlock = profiler.ProfilingBlocks[data.Key];
            profilerBlock.Init(data);

            return profilerBlock;
        }
    }
}
