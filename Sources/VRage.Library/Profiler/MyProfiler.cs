#if !XB1 // XB1_NOPROFILER
#region Using

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ParallelTasks;
using System.Threading;
using VRage.Library.Utils;
using System.IO;

#endregion

namespace VRage.Profiler
{
    /// <summary>
    /// Part of MyRenderProfiler, this is per-thread profiler
    /// </summary>
    public partial class MyProfiler
    {
        
        public struct HistoryLock : IDisposable
        {
            private MyProfiler m_profiler;
            private FastResourceLock m_lock;

            public HistoryLock(MyProfiler profiler, FastResourceLock historyLock)
            {
                m_profiler = profiler;
                m_lock = historyLock;
                m_lock.AcquireExclusive();
                m_profiler.OnHistorySafe();
            }

            public void Dispose()
            {
                m_profiler.OnHistorySafe();
                m_lock.ReleaseExclusive();
                m_lock = null;
            }
        }

        public static bool EnableAsserts = true;
        public static readonly int MAX_FRAMES = 1024;
        public static readonly int UPDATE_WINDOW = 16; // 16 frames is reserved for update, so we don't need locking

        private static readonly int INITIAL_PROFILER_BLOCK_COUNT = 2000;
        private static readonly int PROFILER_BLOCK_INCREMENT_STEP = 100;

        private const int ROOT_ID = 0;
        private int m_nextId = 1;
        private Dictionary<MyProfilerBlockKey, MyProfilerBlock> m_profilingBlocks = new Dictionary<MyProfilerBlockKey, MyProfilerBlock>(8192, new MyProfilerBlockKeyComparer());
        private List<MyProfilerBlock> m_rootBlocks = new List<MyProfilerBlock>(32);
        private Stack<MyProfilerBlock> m_currentProfilingStack = new Stack<MyProfilerBlock>(1024);
        private MyProfilerBlock m_selectedRoot = null;
        private int m_levelLimit = -1;
        private int m_levelSkipCount;
        private volatile int m_newLevelLimit = -1;
        private int m_remainingWindow = UPDATE_WINDOW;
        private FastResourceLock m_historyLock = new FastResourceLock();
        private string m_customName;
        private string m_axisName;

        private Dictionary<MyProfilerBlockKey, MyProfilerBlock> m_blocksToAdd = new Dictionary<MyProfilerBlockKey, MyProfilerBlock>(8192, new MyProfilerBlockKeyComparer());

        private volatile int m_lastFrameIndex;

        // Same rules as other history data
        public int[] TotalCalls = new int[MAX_FRAMES];

        /// <summary>
        /// Enable for background workers.
        /// It will automatically commit after top level profiling block is closed
        /// </summary>
        public bool AutoCommit = true;

        public readonly bool MemoryProfiling;
        public readonly Thread OwnerThread;
        public readonly Stopwatch Stopwatch = new Stopwatch();

        private StreamWriter m_logWriter;
        private static readonly int LOG_THRESHOLD_MS = 50;
        //very simple logging - it will create csv files with anything above ^^threshold into c:\keenswh dir
        private static readonly bool ENABLE_PROFILER_LOG = false;

        private List<MyProfilerBlock> blockPool = new List<MyProfilerBlock>(INITIAL_PROFILER_BLOCK_COUNT);

        public MyProfilerBlock SelectedRoot
        {
            get { return m_selectedRoot; }
            set { m_selectedRoot = value; }
        }

        public List<MyProfilerBlock> SelectedRootChildren
        {
            get { return m_selectedRoot != null ? m_selectedRoot.Children : m_rootBlocks; }
        }

        public List<MyProfilerBlock> RootBlocks
        {
            get { return m_rootBlocks; }
        }

        public int LastFrameIndexDebug
        {
            get { return m_lastFrameIndex; }
        }

        public string DisplayedName
        {
            get { return m_customName; }
        }

        public string AxisName
        {
            get { return m_axisName; }
        }

        public int LevelLimit
        {
            get { return m_levelLimit; }
        }

        string GetParentName()
        {
            if (m_currentProfilingStack.Count > 0)
                return m_currentProfilingStack.Peek().Key.Name;

            return "<root>";
        }

        int GetParentId()
        {
            if (m_currentProfilingStack.Count > 0)
                return m_currentProfilingStack.Peek().Id;

            return ROOT_ID;
        }

        public MyProfiler(bool memoryProfiling, string name, string axisName)
        {
            OwnerThread = Thread.CurrentThread;
            MemoryProfiling = memoryProfiling;
            m_lastFrameIndex = MAX_FRAMES - 1;
            m_customName = name ?? OwnerThread.Name;
            m_axisName = axisName;
            if (ENABLE_PROFILER_LOG)
                m_logWriter = new StreamWriter(@"c:\keenswh\profiler" + Thread.CurrentThread.ManagedThreadId + "_" + m_customName + ".csv");

            for (int i = 0; i < 2000; i++)
            {
                blockPool.Add(new MyProfilerBlock());
            }
        }

        /// <summary>
        /// End operation on history data
        /// </summary>
        void OnHistorySafe()
        {
            // History lock is locked, safe to reset update window
            Interlocked.Exchange(ref m_remainingWindow, UPDATE_WINDOW);
        }

        public static MyProfilerBlock CreateExternalBlock(string name, int blockId)
        {
            MyProfilerBlockKey key = new MyProfilerBlockKey(String.Empty, String.Empty, name, 0, ROOT_ID);
            return new MyProfilerBlock(ref key, String.Empty, blockId);
        }

        public void SetNewLevelLimit(int newLevelLimit)
        {
            m_newLevelLimit = newLevelLimit;
        }

        public HistoryLock LockHistory(out int lastValidFrame)
        {
            var result = new HistoryLock(this, m_historyLock);
            lastValidFrame = m_lastFrameIndex;
            return result;
        }

        /// <summary>
        /// Adds current frame to history and clear it
        /// Returns number of calls this frame
        /// </summary>
        public void CommitFrame()
        {
            Debug.Assert(!AutoCommit, "AutoCommit is enabled, Commit should not be called!");
            CommitInternal();
        }

        private void CommitInternal()
        {
            Debug.Assert(!EnableAsserts || OwnerThread == Thread.CurrentThread);
            Debug.Assert(m_currentProfilingStack.Count == 0, "CommitFrame cannot be called when there are some opened blocks, it must be outside blocks!");
            m_currentProfilingStack.Clear();

            if (m_blocksToAdd.Count > 0)
            {
                using (m_historyLock.AcquireExclusiveUsing())
                {
                    foreach (var block in m_blocksToAdd)
                    {
                        if (block.Value.Parent != null)
                        {
                            block.Value.Parent.Children.AddOrInsert(block.Value, block.Value.ForceOrder);
                        }
                        else
                        {
                            m_rootBlocks.AddOrInsert(block.Value, block.Value.ForceOrder);
                        }

                        m_profilingBlocks.Add(block.Key, block.Value);
                    }
                    m_blocksToAdd.Clear();
                    Interlocked.Exchange(ref m_remainingWindow, UPDATE_WINDOW - 1); // We have lock, no one is in draw, reset window
                }
            }
            else if (m_historyLock.TryAcquireExclusive())
            {
                Interlocked.Exchange(ref m_remainingWindow, UPDATE_WINDOW - 1); // We have lock, no one is in draw, reset window
                m_historyLock.ReleaseExclusive();
            }
            else if (Interlocked.Decrement(ref m_remainingWindow) < 0)
            {
                // Window is empty, wait for lock and reset it
                using (m_historyLock.AcquireExclusiveUsing())
                {
                    Interlocked.Exchange(ref m_remainingWindow, UPDATE_WINDOW - 1); // We have lock, no one is in draw, reset window
                }
            }

            int callCount = 0;
            m_levelLimit = m_newLevelLimit;

            int writeFrame = (m_lastFrameIndex + 1) % MyProfiler.MAX_FRAMES;
            foreach (MyProfilerBlock profilerBlock in m_profilingBlocks.Values)
            {
                callCount += profilerBlock.NumCalls;

                profilerBlock.ManagedMemoryBytes[writeFrame] = profilerBlock.DeltaManagedB;
                if (MemoryProfiling)
                {
                    profilerBlock.ProcessMemory[writeFrame] = profilerBlock.ProcessDeltaMB;
                }
                profilerBlock.NumCallsArray[writeFrame] = profilerBlock.NumCalls;
                profilerBlock.CustomValues[writeFrame] = profilerBlock.CustomValue;
                profilerBlock.Miliseconds[writeFrame] = (float)profilerBlock.Elapsed.Milliseconds;

                // Unused
                profilerBlock.averageMiliseconds = 0.9f * profilerBlock.averageMiliseconds + 0.1f * (float)profilerBlock.Elapsed.Milliseconds;
                //profilerBlock.NumChildCalls = profilerBlock.GetNumChildCalls();

                if (ENABLE_PROFILER_LOG)
                    if (profilerBlock.Elapsed.Milliseconds > LOG_THRESHOLD_MS)
                    {
                        m_logWriter.Write(DateTime.Now.ToString());
                        m_logWriter.Write("; ");
                        m_logWriter.Write(((int)profilerBlock.Elapsed.Milliseconds).ToString());
                        m_logWriter.Write("; ");
                        m_logWriter.Write(profilerBlock.Name);
                        MyProfilerBlock tempBlock = profilerBlock;
                        while (tempBlock.Parent != null)
                        {
                            tempBlock = tempBlock.Parent;
                            m_logWriter.Write(" <- " + tempBlock.Name);
                        }
                        m_logWriter.WriteLine("");
                    }

                profilerBlock.Clear();
            }

            TotalCalls[writeFrame] = callCount;
            m_lastFrameIndex = writeFrame;
        }

        /// <summary>
        /// Clears current frame.
        /// </summary>
        public void ClearFrame()
        {
            Debug.Assert(!EnableAsserts || OwnerThread == Thread.CurrentThread);
            Debug.Assert(m_currentProfilingStack.Count == 0, "ClearFrame cannot be called when there are some opened blocks, it must be outside blocks!");

            m_currentProfilingStack.Clear();

            if (m_blocksToAdd.Count > 0)
                m_blocksToAdd.Clear();

            m_levelLimit = m_newLevelLimit;

            foreach (MyProfilerBlock profilerBlock in m_profilingBlocks.Values)
            {
                profilerBlock.Clear();
            }
        }

        public void Reset()
        {
            using(new HistoryLock(this, m_historyLock))
            {
                foreach(var block in m_profilingBlocks)
                {
                    for (int i = 0; i < MAX_FRAMES; i++)
                    {
                        block.Value.ProcessMemory[i] = 0;
                        block.Value.ManagedMemoryBytes[i] = 0;
                        block.Value.Miliseconds[i] = 0;
                        block.Value.CustomValues[i] = 0;
                        block.Value.NumCallsArray[i] = 0;
                    }
                }

                m_lastFrameIndex = MAX_FRAMES - 1;
            }
        }

        // TODO: OP! Don't know what's this, try remove
        public void InitMemoryHack(string name)
        {
            StartBlock(name, "InitMemoryHack", 0, String.Empty);
            var profilingBlock = m_currentProfilingStack.Peek();
            EndBlock("InitMemoryHack", 0, String.Empty);

            profilingBlock.StartManagedMB = 0;
            profilingBlock.EndManagedMB = System.GC.GetTotalMemory(true);

            if (MemoryProfiling)
            {
                profilingBlock.StartProcessMB = 0;
                profilingBlock.EndProcessMB = System.Environment.WorkingSet;
            }
        }

        public void StartBlock(string name, string memberName, int line, string file, int forceOrder = int.MaxValue)
        {
            Debug.Assert(!EnableAsserts || OwnerThread == Thread.CurrentThread);

            if (m_levelLimit != -1 && m_currentProfilingStack.Count >= m_levelLimit)
            {
                m_levelSkipCount++;
                return;
            }

            MyProfilerBlock profilingBlock = null;
            MyProfilerBlockKey key = new MyProfilerBlockKey(file, memberName, name, line, GetParentId());

            if (!m_profilingBlocks.TryGetValue(key, out profilingBlock) && !m_blocksToAdd.TryGetValue(key, out profilingBlock))
            {
                if (blockPool.Count == 0)
                {
                    for (int i = 0; i < PROFILER_BLOCK_INCREMENT_STEP; i++)
                        blockPool.Add(new MyProfilerBlock());
                }

                profilingBlock = blockPool[0];
                blockPool.RemoveAt(0);

                profilingBlock.SetBlockData(ref key, m_nextId++, forceOrder);

                if (m_currentProfilingStack.Count > 0)
                {
                    profilingBlock.Parent = m_currentProfilingStack.Peek();
                    Debug.Assert(!profilingBlock.Parent.Children.Contains(profilingBlock), "Why is already between children?");
                }

                m_blocksToAdd.Add(key, profilingBlock);
            }

            profilingBlock.Start(MemoryProfiling);

            m_currentProfilingStack.Push(profilingBlock);
        }

        [Conditional("DEBUG")]
        void CheckEndBlock(MyProfilerBlock profilingBlock, string member, string file, int parentId)
        {
            if (EnableAsserts && !profilingBlock.Key.Member.Equals(member) || profilingBlock.Key.ParentId != parentId || profilingBlock.Key.File != file)
            {
                var trace = new StackTrace(2, true);
                for (int i = 0; i < trace.FrameCount; i++)
                {
                    var frame = trace.GetFrame(i);
                    if (frame.GetFileName() == profilingBlock.Key.File && frame.GetMethod().Name == member)
                    {
                        Debug.Fail(String.Format("Premature call to EndProfilingBlock in {0}({1}){2}", file, frame.GetFileLineNumber(), Environment.NewLine));
                        return;
                    }
                }

                Debug.Fail(String.Format("Profiling end block missing for '{0}'{1}File: {2}({3}){1}", profilingBlock.Key.Name, Environment.NewLine, profilingBlock.Key.File, profilingBlock.Key.Line));
            }
        }

        public void EndBlock(string member, int line, string file, MyTimeSpan? customTime = null, float customValue = 0, string timeFormat = null, string valueFormat = null, string callFormat = null)
        {
            Debug.Assert(!EnableAsserts || OwnerThread == Thread.CurrentThread);

            if (m_levelSkipCount > 0)
            {
                m_levelSkipCount--;
                return;
            }

            if (m_currentProfilingStack.Count > 0)
            {
                MyProfilerBlock profilingBlock = m_currentProfilingStack.Pop();
                CheckEndBlock(profilingBlock, member, file, GetParentId());
                profilingBlock.CustomValue = customValue;
                profilingBlock.TimeFormat = timeFormat;
                profilingBlock.ValueFormat = valueFormat;
                profilingBlock.CallFormat = callFormat;
                profilingBlock.End(MemoryProfiling, customTime);
            }
            else
            {
                Debug.Fail(String.Format("Unpaired profiling end block encountered for '{0}'{1}File: {2}({3}){1}", member, Environment.NewLine, file, line));
            }

            if (AutoCommit && m_currentProfilingStack.Count == 0)
                CommitInternal();
        }

        public void ProfileCustomValue(string name, string member, int line, string file, float value, MyTimeSpan? customTime, string timeFormat, string valueFormat, string callFormat = null)
        {
            StartBlock(name, member, line, file);
            EndBlock(member, line, file, customTime, value, timeFormat, valueFormat, callFormat);
        }

        public StringBuilder Dump()
        {
            var sb = new StringBuilder();

            foreach (var block in m_rootBlocks)
            {
                block.Dump(sb, m_lastFrameIndex);
            }

            return sb;
        }

        public class MyProfilerObjectBuilderInfo
        {
            public Dictionary<MyProfilerBlockKey, MyProfilerBlock> ProfilingBlocks;
            public List<MyProfilerBlock> RootBlocks;
            public string CustomName;
            public string AxisName;
            public int[] TotalCalls;
        }

        public MyProfilerObjectBuilderInfo GetObjectBuilderInfo()
        {
            MyProfilerObjectBuilderInfo objectBuilder = new MyProfilerObjectBuilderInfo();
            objectBuilder.ProfilingBlocks = m_profilingBlocks;
            objectBuilder.RootBlocks = m_rootBlocks;
            objectBuilder.CustomName = m_customName;
            objectBuilder.AxisName = m_axisName;
            objectBuilder.TotalCalls = TotalCalls;
            return objectBuilder;
        }

        public void Init(MyProfilerObjectBuilderInfo data)
        {
            m_profilingBlocks = data.ProfilingBlocks;
            m_rootBlocks = data.RootBlocks;
            m_customName = data.CustomName;
            m_axisName = data.AxisName;
            TotalCalls = data.TotalCalls;
        }
    }
}
#endif // !XB1
