using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Network;

namespace VRage
{
    /// <summary>
    /// A simple performance profiler intended to show players information about which area of the game is slowing it down
    /// </summary>
    public class MySimpleProfiler
    {
        public class MySimpleProfilingBlock
        {
            public enum ProfilingBlockType { GRAPHICS, BLOCK, OTHER }
            public ProfilingBlockType type = ProfilingBlockType.BLOCK;
            public bool GPU;
            public MyTimeSpan Time;
            public MyTimeSpan TotalTime;
            public MyTimeSpan TimeStamp;
            public long Frames;
            // Threshold checked for every frame. Negative values mean a warning is displayed when BELOW threshold
            public int ThresholdFrameMilliseconds = 100;
            // Threshold checked for average value over one second. Negative values mean a warning is displayed when BELOW threshold
            public int ThresholdSecondMilliseconds = 10;
            public string Name;
            public MyStringId LocalizedName;
            public MyStringId Description;
            public string DisplayName 
            { 
                get 
                {
                    if (LocalizedName != MyStringId.NullOrEmpty)
                    {
                        return MyTexts.Get(LocalizedName).ToString();
                    }
                    else
                    {
                        return Name;
                    }
                } 
            }

            public double Average
            {
                get { return TotalTime.Milliseconds / Frames; }
            }

            public void Log(MyTimeSpan currentTime)
            {
                Time += currentTime - TimeStamp;
                TotalTime += currentTime - TimeStamp;
            }
        }

        public class PerformanceWarning
        {
            public int Time;
            public MySimpleProfilingBlock Block;

            public PerformanceWarning(MySimpleProfilingBlock block)
            {
                Block = block;
                Time = 0;
            }
        }

        private static readonly Dictionary<string, MySimpleProfilingBlock> m_profilingBlocks = new Dictionary<string, MySimpleProfilingBlock>();
        private static readonly Dictionary<string, PerformanceWarning> m_currentWarnings = new Dictionary<string, PerformanceWarning>();
        private static bool m_initialized;
        private static string m_GPUBlock;
        private static FastResourceLock m_blockLock = new FastResourceLock();
        
        public static Action<MySimpleProfilingBlock> ShowPerformanceWarning;

        public static Dictionary<string, PerformanceWarning> CurrentWarnings
        {
            get { return m_currentWarnings; }
        }

        /// <summary>
        /// Special settings for profiling blocks should be set here
        /// </summary>
        public static void Init()
        {
            SetBlockSettings("Grid", type: MySimpleProfilingBlock.ProfilingBlockType.BLOCK);
            SetBlockSettings("Oxygen", type: MySimpleProfilingBlock.ProfilingBlockType.BLOCK);
            SetBlockSettings("Gyro", type: MySimpleProfilingBlock.ProfilingBlockType.BLOCK);
            SetBlockSettings("Conveyor", type: MySimpleProfilingBlock.ProfilingBlockType.BLOCK);
            SetBlockSettings("Blocks", type: MySimpleProfilingBlock.ProfilingBlockType.BLOCK);
            SetBlockSettings("Physics", type: MySimpleProfilingBlock.ProfilingBlockType.OTHER);
            SetBlockSettings("AI", type: MySimpleProfilingBlock.ProfilingBlockType.OTHER);
            SetBlockSettings("Scripts", type: MySimpleProfilingBlock.ProfilingBlockType.BLOCK);
            SetBlockSettings("Render", type: MySimpleProfilingBlock.ProfilingBlockType.GRAPHICS);
            SetBlockSettings("Textures", type: MySimpleProfilingBlock.ProfilingBlockType.OTHER);

            SetBlockSettings("ClearAndGeometryRender", gpu: true, type: MySimpleProfilingBlock.ProfilingBlockType.GRAPHICS);
            SetBlockSettings("RenderFoliage", gpu: true, type: MySimpleProfilingBlock.ProfilingBlockType.GRAPHICS);
            SetBlockSettings("Shadows", gpu: true, type: MySimpleProfilingBlock.ProfilingBlockType.GRAPHICS);
            SetBlockSettings("SSAO", gpu: true, type: MySimpleProfilingBlock.ProfilingBlockType.GRAPHICS);
            SetBlockSettings("Lights", gpu: true, type: MySimpleProfilingBlock.ProfilingBlockType.GRAPHICS);
            SetBlockSettings("TransparentPass", gpu: true, type: MySimpleProfilingBlock.ProfilingBlockType.GRAPHICS);
            SetBlockSettings("PostProcess", gpu: true, type: MySimpleProfilingBlock.ProfilingBlockType.GRAPHICS);

            ShowPerformanceWarning += AddWarningToCurrent;
        }
                
        /// <summary>
        /// Begin new profiling block
        /// </summary>
        public static void Begin(string key)
        {
            if (String.IsNullOrEmpty(key))
                return;
            MySimpleProfilingBlock block;
            if (!m_profilingBlocks.TryGetValue(key, out block)) 
            {
                block = new MySimpleProfilingBlock();
                block.Name = key;
                using (m_blockLock.AcquireExclusiveUsing())
                {
                    m_profilingBlocks.Add(key, block);
                }
            }
            block.TimeStamp = new MyTimeSpan(Stopwatch.GetTimestamp());
        }

        /// <summary>
        /// End profiling block
        /// </summary>
        public static void End(string key)
        {
            if (String.IsNullOrEmpty(key))
                return;
            MySimpleProfilingBlock block;
            if (m_profilingBlocks.TryGetValue(key, out block))
            {
                block.Log(new MyTimeSpan(Stopwatch.GetTimestamp()));
            }
            else
            {
                block = new MySimpleProfilingBlock();
                block.Name = key;
                using (m_blockLock.AcquireExclusiveUsing())
                {
                    m_profilingBlocks.Add(key, block);
                }
            }
        }

        /// <summary>
        /// Set which GPU profiling block is going to receive timing next
        /// </summary>
        public static void BeginGPUBlock(string key)
        {
            if (!m_profilingBlocks.ContainsKey(key))
            {
                using (m_blockLock.AcquireExclusiveUsing())
                {
                    m_profilingBlocks.Add(key, new MySimpleProfilingBlock() { Name = key, GPU = true });
                }
            }
            m_GPUBlock = key;
        }

        /// <summary>
        /// Log timing of currently set GPU block
        /// </summary>
        public static void EndGPUBlock(MyTimeSpan time)
        {
            MySimpleProfilingBlock block;
            if (m_GPUBlock != null && m_profilingBlocks.TryGetValue(m_GPUBlock, out block))
            {
                block.Time += time;
                block.TotalTime += time;
                block.Frames++;
                m_GPUBlock = null;
            }
        }

        /// <summary>
        /// Check performance and reset time
        /// </summary>
        public static void Commit()
        {
            if (!m_initialized)
            {
                Init();
                m_initialized = true;
            }

            // Grid is special case - we already profile oxygen, gyros and conveyors on their own, so we need to exclude them from grid
            m_profilingBlocks["Grid"].Time -= m_profilingBlocks["Oxygen"].Time + m_profilingBlocks["Gyro"].Time + m_profilingBlocks["Conveyor"].Time;
            m_profilingBlocks["Grid"].TotalTime -= m_profilingBlocks["Oxygen"].TotalTime + m_profilingBlocks["Gyro"].TotalTime + m_profilingBlocks["Conveyor"].TotalTime;

            CheckPerformance();

            using (m_blockLock.AcquireExclusiveUsing())
            {
                foreach (MySimpleProfilingBlock block in m_profilingBlocks.Values)
                {
                    if (!block.GPU)
                        block.Frames++;
                    block.Time = MyTimeSpan.Zero;
                    if (block.Frames >= 60)
                    {
                        block.Frames = 0;
                        block.TotalTime = MyTimeSpan.Zero;
                    }
                }
            }

            foreach (var time in m_currentWarnings)
            {
                time.Value.Time++;
            }
        }

        /// <summary>
        /// Set special settings for a profiling block
        /// </summary>
        public static void SetBlockSettings(string key, int thresholdFrame = 100, int thresholdSecond = 10, bool gpu = false, MySimpleProfilingBlock.ProfilingBlockType type = MySimpleProfilingBlock.ProfilingBlockType.BLOCK)
        {
            MySimpleProfilingBlock block;
            if (!m_profilingBlocks.TryGetValue(key, out block))
            {
                block = new MySimpleProfilingBlock();
                block.Name = key;
                m_profilingBlocks.Add(key, block);
            }

            block.LocalizedName = MyStringId.GetOrCompute("PerformanceWarningArea" + key);
            block.Description = MyStringId.GetOrCompute("PerformanceWarningArea" + key + "Description");
            block.ThresholdFrameMilliseconds = thresholdFrame;
            block.ThresholdSecondMilliseconds = thresholdSecond;
            block.GPU = gpu;
            block.type = type;
        }

        /// <summary>
        /// Checks performance of each profiling block and sends notifications if above threshold
        /// </summary>
        private static void CheckPerformance()
        {
            bool performanceLow;
            using (m_blockLock.AcquireExclusiveUsing())
            {
                foreach (MySimpleProfilingBlock block in m_profilingBlocks.Values)
                {
                    performanceLow = false;
                    if (block.ThresholdFrameMilliseconds > 0)
                    {
                        performanceLow |= block.Time.Milliseconds > block.ThresholdFrameMilliseconds;
                    }
                    else if (block.ThresholdFrameMilliseconds < 0)
                    {
                        performanceLow |= block.Time.Milliseconds < -block.ThresholdFrameMilliseconds;
                    }
                    // Only check the average if you have data from the entire second. Otherwise it can be inaccurate
                    if (block.Frames == 59)
                    {
                        if (block.ThresholdSecondMilliseconds > 0)
                        {
                            performanceLow |= block.Average > block.ThresholdSecondMilliseconds;
                        }
                        else if (block.ThresholdSecondMilliseconds < 0)
                        {
                            performanceLow |= block.Average < -block.ThresholdSecondMilliseconds;
                        }
                    }
                    if (performanceLow && ShowPerformanceWarning != null)
                    {
                        ShowPerformanceWarning(block);
                    }
                }
            }
        }

        /// <summary>
        /// Show performance warning received from server
        /// </summary>
        public static void ShowServerPerformanceWarning(string key)
        {
            MySimpleProfilingBlock block;
            if (!m_profilingBlocks.TryGetValue(key, out block))
            {
                block = new MySimpleProfilingBlock();
                block.Name = key;
            }
            if (ShowPerformanceWarning != null)
            {
                ShowPerformanceWarning(block);
            }
        }

        private static void AddWarningToCurrent(MySimpleProfilingBlock block)
        {
            if (m_currentWarnings.ContainsKey(block.Name))
                m_currentWarnings[block.Name].Time = 0;
            else
                m_currentWarnings.Add(block.Name, new PerformanceWarning(block));
        }

        /// <summary>
        /// Unused, but returns the area which is the most above threshold, null if none is
        /// </summary>
        private static MySimpleProfilingBlock FindWorstPerformanceBlock()
        {
            MySimpleProfilingBlock worstPerformanceBlock = null;
            double worstPerformance = 0;
            double performance;

            foreach (MySimpleProfilingBlock block in m_profilingBlocks.Values)
            {
                performance = 0;

                if (block.ThresholdFrameMilliseconds > 0)
                {
                    performance = block.Time.Milliseconds / block.ThresholdFrameMilliseconds;
                }
                else if (block.ThresholdFrameMilliseconds < 0)
                {
                    performance = -block.ThresholdFrameMilliseconds / block.Time.Milliseconds;
                }
                if (performance > worstPerformance)
                {
                    worstPerformance = performance;
                    worstPerformanceBlock = block;
                }

                if (block.Frames == 59)
                {
                    if (block.ThresholdSecondMilliseconds > 0)
                    {
                        performance = block.Average / block.ThresholdFrameMilliseconds;
                    }
                    else if (block.ThresholdSecondMilliseconds < 0)
                    {
                        performance = -block.ThresholdFrameMilliseconds / block.Average;
                    }
                }
                if (performance > worstPerformance)
                {
                    worstPerformance = performance;
                    worstPerformanceBlock = block;
                }
            }
            if (worstPerformance > 1)
                return worstPerformanceBlock;
            else 
                return null;
        }
    }
}
