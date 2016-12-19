using VRage.ObjectBuilders;
using VRage.Serialization;
using System.Collections.Generic;
using VRage.Profiler;
using System.IO;
using VRage.FileSystem;

namespace VRage.Game
{
    public class MyObjectBuilder_Profiler : MyObjectBuilder_Base
    {
        public List<MyObjectBuilder_ProfilerBlock> ProfilingBlocks;

        public List<MyProfilerBlockKey> RootBlocks;

        public int[] TotalCalls;

        public string CustomName = "";

        public string AxisName = "";

        public static MyObjectBuilder_Profiler GetObjectBuilder(MyProfiler profiler)
        {
            MyProfiler.MyProfilerObjectBuilderInfo data = profiler.GetObjectBuilderInfo();
            MyObjectBuilder_Profiler objectBuilder = new MyObjectBuilder_Profiler();

            objectBuilder.ProfilingBlocks = new List<MyObjectBuilder_ProfilerBlock>();
            foreach (var block in data.ProfilingBlocks)
            {
                objectBuilder.ProfilingBlocks.Add(MyObjectBuilder_ProfilerBlock.GetObjectBuilder(block.Value));
            }

            objectBuilder.RootBlocks = new List<MyProfilerBlockKey>();
            foreach (MyProfilerBlock block in data.RootBlocks)
            {
                objectBuilder.RootBlocks.Add(block.Key);
            }

            objectBuilder.TotalCalls = data.TotalCalls;
            objectBuilder.CustomName = data.CustomName;
            objectBuilder.AxisName = data.AxisName;

            return objectBuilder;
        }

        public static MyProfiler Init(MyObjectBuilder_Profiler objectBuilder)
        {
            MyProfiler.MyProfilerObjectBuilderInfo data = new MyProfiler.MyProfilerObjectBuilderInfo();

            data.ProfilingBlocks = new Dictionary<MyProfilerBlockKey, MyProfilerBlock>();
            foreach (var blockOB in objectBuilder.ProfilingBlocks)
            {
                data.ProfilingBlocks.Add(blockOB.Key, new MyProfilerBlock());
            }

            foreach (var blockOB in objectBuilder.ProfilingBlocks)
            {
                MyObjectBuilder_ProfilerBlock.Init(blockOB, data);
            }

            data.RootBlocks = new List<MyProfilerBlock>();
            foreach (var blockKey in objectBuilder.RootBlocks)
            {
                data.RootBlocks.Add(data.ProfilingBlocks[blockKey]);
            }

            data.TotalCalls = objectBuilder.TotalCalls;
            data.CustomName = objectBuilder.CustomName;
            data.AxisName = objectBuilder.AxisName;

            MyProfiler profiler = new MyProfiler(false, data.CustomName, data.AxisName);
            profiler.Init(data);

            return profiler;
        }

        public static void SaveToFile(int index)
        {
            try
            {
                MyObjectBuilder_Profiler profilerBuilder = MyObjectBuilder_Profiler.GetObjectBuilder(VRage.Profiler.MyRenderProfiler.SelectedProfiler);
                MyObjectBuilderSerializer.SerializeXML(Path.Combine(MyFileSystem.UserDataPath, "Profiler-" + index), false, profilerBuilder);
            }
            catch
            {
                System.Diagnostics.Debug.Fail("Cannot save profiler.");
            }
        }

        public static void LoadFromFile(int index)
        {
            try
            {
                MyObjectBuilder_Profiler profilerBuilder;
                MyObjectBuilderSerializer.DeserializeXML(Path.Combine(MyFileSystem.UserDataPath, "Profiler-" + index), out profilerBuilder);
                VRage.Profiler.MyRenderProfiler.SelectedProfiler = MyObjectBuilder_Profiler.Init(profilerBuilder);
            }
            catch
            {
                System.Diagnostics.Debug.Fail("Cannot load profiler. File may not exist.");
            }
        }

        public static void SetDelegates()
        {
            VRage.Profiler.MyRenderProfiler.SaveProfilerToFile = SaveToFile;
            VRage.Profiler.MyRenderProfiler.LoadProfilerFromFile = LoadFromFile;
        }
    }
}
