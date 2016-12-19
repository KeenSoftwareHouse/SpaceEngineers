#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using VRage.Library.Utils;
using VRageMath;

#endregion


namespace VRage.Profiler
{
    #region Enums

    public enum RenderProfilerCommand
    {
        /// <summary>
        /// Only enables the profiler, used by the developer debug window
        /// </summary>
        Enable,

        /// <summary>
        /// Toggles profiler enabled/disabled state, doesn't reset profiler level
        /// </summary>
        ToggleEnabled,

        JumpToLevel,

        /// <summary>
        /// Jumps all the way to the root element
        /// </summary>
        JumpToRoot,

        Pause,

        NextFrame,
        PreviousFrame,

        /// <summary>
        /// Disables the current selection again
        /// </summary>
        DisableFrameSelection,

        NextThread,
        PreviousThread,
        IncreaseLevel,
        DecreaseLevel,
        IncreaseLocalArea,
        DecreaseLocalArea,
        IncreaseRange,
        DecreaseRange,
        Reset,
        SetLevel,

        /// <summary>
        /// Changes the profiler's sorting order, see ProfilerSortingOptions for the possible sorting options
        /// </summary>
        ChangeSortingOrder,

        /// <summary>
        /// Copies the current path to clipboard
        /// </summary>
        CopyPathToClipboard,

        /// <summary>
        /// Tries to navigate to the path in the clipboard
        /// </summary>
        TryGoToPathInClipboard,

        GetFomServer,
        GetFromClient,
        SaveToFile,
        LoadFromFile,
    }

    /// <summary>
    /// Profiler sorting order types
    /// </summary>
    public enum RenderProfilerSortingOrder
    {
        /// <summary>
        /// Order in which the elements are logged
        /// </summary>
        Id = 0,

        /// <summary>
        /// Milliseconds spent in the previous frame, sorted from slowest to fastest
        /// </summary>
        MillisecondsLastFrame,

        /// <summary>
        /// Milliseconds spent on average, sorted from slowest to fastest
        /// </summary>
        MillisecondsAverage,

        /// <summary>
        /// Total number of sorting types
        /// </summary>
        NumSortingTypes,
    }

    #endregion


    /// <summary>
    /// Provides profiling capability
    /// </summary>
    /// <remarks>
    /// Non-locking way of render profiler is used. Each thread has it's own profiler is ThreadStatic variable.
    /// Data for each profiling block are of two kinds: Immediate (current frame being profiled) and History (previous finished frames)
    /// Start/End locking is not necessary, because Start/Stop uses only immediate data and nothing else uses it at the moment.
    /// Commit is only other place which uses Immediate data, but it must be called from same thead, no racing condition.
    /// Draw and Commit both uses History data, and both can be called from different thread, so there's lock.
    /// This way everything runs with no waiting, unless Draw obtains lock in which case Commit wait for Draw to finish (Start/End is still exact).
    /// 
    /// For threads which does not call commit (background workers, parallel tasks), mechanism which calls commit automatically after each top level End should be added.
    /// This way each task will be one "frame" on display
    /// </remarks>
    public abstract class MyRenderProfiler
    {
        /// <summary>
        /// Sorting order will sort the listed elements in the profiler by the specified ProfilerSortingOrder
        /// </summary>
        protected static RenderProfilerSortingOrder m_sortingOrder = RenderProfilerSortingOrder.MillisecondsLastFrame;

        public const string PerformanceProfilingSymbol = VRage.MyCompilationSymbols.PerformanceProfiling ? "WINDOWS" : "__RANDOM_UNDEFINED_PROFILING_SYMBOL__";

        private static bool m_profilerProcessingEnabled = VRage.MyCompilationSymbols.PerformanceProfiling;

        public static bool ProfilerProcessingEnabled
        {
            get { return m_profilerProcessingEnabled; }
        }

        /// <summary>
        /// Returns true when profiler is visible.
        /// </summary>
        public static bool ProfilerVisible
        {
            get { return m_enabled; }
        }

        protected static MyDrawArea m_milisecondsGraphScale = new MyDrawArea(0.49f, 0, (2 - 0.51f) / 2, 0.9f, 25);
        protected static MyDrawArea m_memoryGraphScale = new MyDrawArea(0.49f, 0, (2 - 0.51f) / 2, 0.6f, 0.001f);

        protected static Color[] m_colors = { new Color(0,192,192), Color.Orange, Color.BlueViolet * 1.5f, Color.BurlyWood, Color.Chartreuse,
                                  Color.CornflowerBlue, Color.Cyan, Color.ForestGreen, Color.Fuchsia,
                                  Color.Gold, Color.GreenYellow, Color.LightBlue, Color.LightGreen, Color.LimeGreen,
                                  Color.Magenta, Color.MintCream, Color.Orchid, Color.PeachPuff, Color.Purple };

        protected static Color IndexToColor(int index)
        {
            return m_colors[index % m_colors.Length];
        }

        protected StringBuilder m_text = new StringBuilder(100);

        // Set to true to track memory in Render Profiler
        public const bool MemoryProfiling = VRage.MyCompilationSymbols.ProfileWorkingSetMemory;

        protected static MyProfilerBlock m_fpsBlock;
        protected static float m_fpsPctg;

        //{Color.Cyan, Color.Orange, new Color(208, 86, 255), Color.BurlyWood, Color.LightGray,
        //                          Color.CornflowerBlue,Color.LawnGreen,  Color.Fuchsia,
        //                          Color.Gold, Color.OrangeRed,Color.YellowGreen, Color.LightBlue, Color.LightCoral, Color.LimeGreen,
        //                          Color.Magenta, Color.Navy, Color.Orchid, Color.PeachPuff, Color.Purple };

        public static bool Paused = false;

        public static Action<int> GetProfilerFromServer;
        public static Action<int> SaveProfilerToFile;
        public static Action<int> LoadProfilerFromFile;

        [ThreadStatic]
        static MyProfiler m_threadProfiler;
        static MyProfiler m_gpuProfiler;

        static MyProfiler GpuProfiler
        {
            get
            {
                if (m_gpuProfiler == null)
                {
                    m_gpuProfiler = CreateProfiler("GPU", null, MemoryProfiling);
                }
                return m_gpuProfiler;
            }
        }

        static MyProfiler ThreadProfiler
        {
            get
            {
                if (m_threadProfiler == null)
                {
                    m_threadProfiler = CreateProfiler(null, null, MemoryProfiling);
                }
                return m_threadProfiler;
            }
        }

        protected static List<MyProfiler> m_threadProfilers = new List<MyProfiler>(16);

        protected static MyProfiler m_selectedProfiler;

        public static MyProfiler SelectedProfiler
        {
            get { return m_selectedProfiler; }
            set { m_selectedProfiler = value; }
        }

        public static int SelectedIndex
        {
            get { return m_threadProfilers.IndexOf(m_selectedProfiler); }
        }

        protected static bool m_enabled = false;
        protected static int m_selectedFrame = 0;   // Index of selected frame. It will be showed in text legend.
        protected static int m_levelLimit = -1;
        protected static bool m_useCustomFrame = false;
        protected static int m_frameLocalArea = MyProfiler.MAX_FRAMES;
        private int m_currentDumpNumber = 0;

        public static int ServerSelectedIndex;
        public static bool IsProfilerFromServer;

        static MyRenderProfiler()
        {
            m_levelLimit = VRage.MyCompilationSymbols.ProfileFromStart ? -1 : 0;
            // Create block, some unique id
            m_fpsBlock = MyProfiler.CreateExternalBlock("FPS", -2);
        }

        /// <summary>
        /// Creates new profiler which can be used to profile anything (e.g. network stats).
        /// </summary>
        public static MyProfiler CreateProfiler(string name, string axisName = null, bool memoryProfiling = false)
        {
            lock (m_threadProfilers)
            {
                var profiler = new MyProfiler(memoryProfiling, name, axisName ?? "[ms]");
                m_threadProfilers.Add(profiler);
                profiler.SetNewLevelLimit(m_profilerProcessingEnabled ? m_levelLimit : 0);
                if (m_selectedProfiler == null)
                    m_selectedProfiler = profiler;
                return profiler;
            }
        }


        public static List<MyProfilerBlock> GetSortedChildren(int frameToSortBy)
        {
            List<MyProfilerBlock> sortedChildren = new List<MyProfilerBlock>(m_selectedProfiler.SelectedRootChildren);

            switch (m_sortingOrder)
            {
                case RenderProfilerSortingOrder.Id:
                    sortedChildren.Sort(delegate(MyProfilerBlock a, MyProfilerBlock b)
                    {
                        return a.Id.CompareTo(b.Id);
                    });
                    break;
                case RenderProfilerSortingOrder.MillisecondsLastFrame:
                    sortedChildren.Sort(delegate(MyProfilerBlock a, MyProfilerBlock b)
                    {
                        // Sorts by milliseconds, in case of equal performance, sorts by ID instead
                        int comparisonResult = b.Miliseconds[frameToSortBy].CompareTo(a.Miliseconds[frameToSortBy]);
                        if (comparisonResult != 0) return comparisonResult;
                        return a.Id.CompareTo(b.Id);
                    });
                    break;
                case RenderProfilerSortingOrder.MillisecondsAverage:
                    sortedChildren.Sort(delegate(MyProfilerBlock a, MyProfilerBlock b)
                    {
                        // Sorts by average milliseconds, in case of equal performance, sorts by ID instead
                        int comparisonResult = b.averageMiliseconds.CompareTo(a.averageMiliseconds);
                        if (comparisonResult != 0) return comparisonResult;
                        return a.Id.CompareTo(b.Id);
                    });
                    break;
            }

            return sortedChildren;
        }

        public static MyProfilerBlock FindBlockByIndex(int index)
        {
            var children = GetSortedChildren(m_selectedFrame);
            if (index >= 0 && index < children.Count)
                return children[index];

            if (index == -1 && m_selectedProfiler.SelectedRoot != null)
                return m_selectedProfiler.SelectedRoot.Parent;

            return null;
        }

        protected static bool IsValidIndex(int frameIndex, int lastValidFrame)
        {
            int wrappedIndex = frameIndex > lastValidFrame ? frameIndex : frameIndex + MyProfiler.MAX_FRAMES;
            return wrappedIndex > (lastValidFrame + MyProfiler.UPDATE_WINDOW); // Outside update window
        }


        public static MyProfilerBlock FindBlockByMax(int frameIndex, int lastValidFrame)
        {
            if (!IsValidIndex(frameIndex, lastValidFrame))
                return null;

            float max = float.MinValue;
            MyProfilerBlock block = null;

            var children = m_selectedProfiler.SelectedRootChildren;
            for (int i = 0; i < children.Count; i++)
            {
                var profilerBlock = children[i];
                float val = profilerBlock.Miliseconds[frameIndex];
                if (val > max)
                {
                    max = val;
                    block = profilerBlock;
                }
            }
            return block;
        }

        public static void HandleInput(RenderProfilerCommand command, int index)
        {
            switch (command)
            {
                case RenderProfilerCommand.Enable:
                    {
                        if (!m_enabled)
                        {
                            m_enabled = true;
                            m_profilerProcessingEnabled = true; // Enable when disabled and keep enabled
                            SetLevel();
                        }
                        break;
                    }

                case RenderProfilerCommand.ToggleEnabled:
                    {
                        // Enable or Disable profiler drawing
                        if (m_enabled)
                        {
                            m_enabled = false;
                            m_useCustomFrame = false;
                        }
                        else
                        {
                            m_enabled = true;
                            m_profilerProcessingEnabled = true; // Enable when disabled and keep enabled
                        }
                        break;
                    }

                case RenderProfilerCommand.JumpToRoot:
                    m_selectedProfiler.SelectedRoot = null;
                    break;

                case RenderProfilerCommand.JumpToLevel:
                    {
                        // Enable when disabled, added this for programmers who are too used to using the numpad 0 to open the profiler.
                        if (index == 0 && !m_enabled)
                        {
                            m_enabled = true;
                            m_profilerProcessingEnabled = true; // Enable when disabled and keep enabled
                        }
                        else
                            m_selectedProfiler.SelectedRoot = FindBlockByIndex(index - 1); // On screen it's indexed from 1 (zero is level up)
                        break;
                    }
                case RenderProfilerCommand.Pause:
                    {
                        Paused = !Paused;
                        GpuProfiler.AutoCommit = !Paused;

                        if (IsProfilerFromServer)
                        {
                            m_selectedProfiler = m_threadProfilers[0];
                            IsProfilerFromServer = false;
                        }
                        break;
                    }

                case RenderProfilerCommand.NextThread:
                    {
                        if (IsProfilerFromServer && GetProfilerFromServer != null)
                        {
                            ServerSelectedIndex++;
                            GetProfilerFromServer(ServerSelectedIndex);
                        }
                        else
                        {
                            lock (m_threadProfilers)
                            {
                                int profilerIndex = (m_threadProfilers.IndexOf(m_selectedProfiler) + 1) % m_threadProfilers.Count;
                                m_selectedProfiler = m_threadProfilers[profilerIndex];
                            }
                        }
                        break;
                    }

                case RenderProfilerCommand.PreviousThread:
                    {
                        if (IsProfilerFromServer && GetProfilerFromServer != null)
                        {
                            ServerSelectedIndex--;
                            GetProfilerFromServer(ServerSelectedIndex);
                        }
                        else
                        {
                            lock (m_threadProfilers)
                            {
                                int profilerIndex = (m_threadProfilers.IndexOf(m_selectedProfiler) - 1 + m_threadProfilers.Count) % m_threadProfilers.Count;
                                m_selectedProfiler = m_threadProfilers[profilerIndex];
                            }
                        }
                        break;
                    }

                case RenderProfilerCommand.Reset:
                    {
                        lock (m_threadProfilers)
                        {
                            foreach (var profiler in m_threadProfilers)
                            {
                                profiler.Reset();
                            }
                            m_selectedFrame = 0;
                        }
                        break;
                    }

                case RenderProfilerCommand.NextFrame:
                    {
                        MyRenderProfiler.NextFrame(index);
                        break;
                    }

                case RenderProfilerCommand.PreviousFrame:
                    {
                        MyRenderProfiler.PreviousFrame(index);
                        break;
                    }

                case RenderProfilerCommand.DisableFrameSelection:
                    {
                        m_useCustomFrame = false;
                        break;
                    }

                case RenderProfilerCommand.IncreaseLevel:
                    {
                        m_levelLimit++;
                        SetLevel();
                        break;
                    }

                case RenderProfilerCommand.DecreaseLevel:
                    {
                        m_levelLimit--;
                        if (m_levelLimit < -1)
                            m_levelLimit = -1;
                        SetLevel();
                        break;
                    }

                case RenderProfilerCommand.CopyPathToClipboard:
                    {
                        StringBuilder pathBuilder = new StringBuilder(200);
                        MyProfilerBlock currentBlock = m_selectedProfiler.SelectedRoot;

                        while (currentBlock != null)
                        {
                            if (pathBuilder.Length > 0)
                                pathBuilder.Insert(0, " > ");
                            pathBuilder.Insert(0, currentBlock.Name);
                            currentBlock = currentBlock.Parent;
                        }

                        if (pathBuilder.Length > 0)
                        {
                            // Clipboard can only be accessed from a thread on the STA apartment
                            System.Threading.Thread thread = new System.Threading.Thread(() => System.Windows.Forms.Clipboard.SetText(pathBuilder.ToString()));
                            thread.SetApartmentState(System.Threading.ApartmentState.STA);
                            thread.Start();
                            thread.Join();
                        }
                        break;
                    }

                case RenderProfilerCommand.TryGoToPathInClipboard:
                    {
                        string fullPath = string.Empty;

                        Exception threadEx = null;
                        System.Threading.Thread staThread = new System.Threading.Thread(
                            delegate()
                            {
                                try
                                {
                                    fullPath = System.Windows.Forms.Clipboard.GetText();
                                }

                                catch (Exception ex)
                                {
                                    threadEx = ex;
                                }
                            });
                        staThread.SetApartmentState(System.Threading.ApartmentState.STA);
                        staThread.Start();
                        staThread.Join();

                        if (!string.IsNullOrEmpty(fullPath))
                        {
                            string[] split = fullPath.Split(new string[] { " > " }, StringSplitOptions.None);

                            MyProfilerBlock pathBlock = null;
                            List<MyProfilerBlock> blockSet = m_selectedProfiler.RootBlocks;
                            for (int i = 0; i < split.Length; i++)
                            {
                                string blockName = split[i];
                                MyProfilerBlock oldPath = pathBlock;

                                for (int j = 0; j < blockSet.Count; j++)
                                {
                                    MyProfilerBlock block = blockSet[j];
                                    if (block.Name == blockName)
                                    {
                                        pathBlock = block;
                                        blockSet = pathBlock.Children;
                                        break;
                                    }
                                }

                                // If the path did not change, we cannot go any deeper, break out of this loop
                                if (oldPath == pathBlock)
                                    break;
                            }

                            if (pathBlock != null)
                                m_selectedProfiler.SelectedRoot = pathBlock;
                        }
                        break;
                    }

                case RenderProfilerCommand.SetLevel:
                    {
                        m_levelLimit = index;
                        if (m_levelLimit < -1)
                            m_levelLimit = -1;
                        SetLevel();
                        break;
                    }

                case RenderProfilerCommand.DecreaseLocalArea:
                    m_frameLocalArea = Math.Max(2, m_frameLocalArea / 2);
                    break;

                case RenderProfilerCommand.IncreaseLocalArea:
                    m_frameLocalArea = Math.Min(MyProfiler.MAX_FRAMES, m_frameLocalArea * 2);
                    break;

                case RenderProfilerCommand.IncreaseRange:
                    m_milisecondsGraphScale.IncreaseYRange();
                    break;

                case RenderProfilerCommand.DecreaseRange:
                    m_milisecondsGraphScale.DecreaseYRange();
                    break;

                case RenderProfilerCommand.ChangeSortingOrder:
                    m_sortingOrder += 1;
                    if (m_sortingOrder >= RenderProfilerSortingOrder.NumSortingTypes)
                        m_sortingOrder = RenderProfilerSortingOrder.Id;
                    break;

                case RenderProfilerCommand.GetFomServer:
                    if (m_enabled && GetProfilerFromServer != null)
                    {
                        GetProfilerFromServer(ServerSelectedIndex);
                        Paused = true;
                    }
                    break;

                case RenderProfilerCommand.GetFromClient:
                    m_selectedProfiler = m_threadProfilers[0];
                    IsProfilerFromServer = false;
                    break;

                case RenderProfilerCommand.SaveToFile:
                    SaveProfilerToFile(index);
                    break;

                case RenderProfilerCommand.LoadFromFile:
                    LoadProfilerFromFile(index);
                    Paused = true;
                    break;

                default:
                    System.Diagnostics.Debug.Assert(false, "Unknown command");
                    break;
            }
        }


        static void SetLevel()
        {
            lock (m_threadProfilers)
            {
                foreach (var p in m_threadProfilers)
                {
                    p.SetNewLevelLimit(m_profilerProcessingEnabled ? m_levelLimit : 0);
                }
            }
        }

        static void PreviousFrame(int step)
        {
            m_useCustomFrame = true;

            m_selectedFrame -= step;
            while (m_selectedFrame < 0)
                m_selectedFrame += MyProfiler.MAX_FRAMES - 1;
        }

        static void NextFrame(int step)
        {
            m_useCustomFrame = true;

            m_selectedFrame += step;
            while (m_selectedFrame >= MyProfiler.MAX_FRAMES)
                m_selectedFrame -= MyProfiler.MAX_FRAMES;
        }

        static void FindMax(float[] data, int start, int end, ref float max, ref int maxIndex)
        {
            for (int i = start; i <= end; i++)
            {
                if (data[i] > max)
                {
                    max = data[i];
                    maxIndex = i;
                }
            }
        }

        static void FindMax(float[] data, int lower, int upper, int lastValidFrame, ref float max, ref int maxIndex)
        {
            Debug.Assert(lower <= upper, "Lower must be smaller or equal to Upper");
            int windowEnd = (lastValidFrame + 1 + MyProfiler.UPDATE_WINDOW) % MyProfiler.MAX_FRAMES;

            if (lastValidFrame > windowEnd)
            {
                FindMax(data, Math.Max(lower, windowEnd), Math.Min(lastValidFrame, upper), ref max, ref maxIndex);
            }
            else
            {
                FindMax(data, lower, Math.Min(lastValidFrame, upper), ref max, ref maxIndex);
                FindMax(data, Math.Max(lower, windowEnd), upper, ref max, ref maxIndex);
            }
        }

        // Wraps lower and upper
        protected static float FindMaxWrap(float[] data, int lower, int upper, int lastValidFrame, out int maxIndex)
        {
            lower = (lower + MyProfiler.MAX_FRAMES) % MyProfiler.MAX_FRAMES;
            upper = (upper + MyProfiler.MAX_FRAMES) % MyProfiler.MAX_FRAMES;

            float max = 0;
            maxIndex = -1;
            if (upper > lower)
            {
                FindMax(data, lower, upper, lastValidFrame, ref max, ref maxIndex);
            }
            else
            {

                FindMax(data, 0, upper, lastValidFrame, ref max, ref maxIndex);
                FindMax(data, lower, MyProfiler.MAX_FRAMES - 1, lastValidFrame, ref max, ref maxIndex);
            }
            return max;
        }

        public void GetAutocommit(ref bool val)
        {
            val = ThreadProfiler.AutoCommit;
        }

        public void SetAutocommit(bool val)
        {
            ThreadProfiler.AutoCommit = val;
        }

        public void Commit([CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            var profiler = ThreadProfiler;
            profiler.Stopwatch.Restart();
            if (!Paused)
            {
                profiler.CommitFrame();
                m_useCustomFrame = true;
            }
            else
            {
                profiler.ClearFrame();
            }
            profiler.ProfileCustomValue("Profiler.Commit", member, line, file, 0, MyTimeSpan.FromMilliseconds(profiler.Stopwatch.ElapsedMilliseconds), null, null);
        }

        public void Draw([CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            if (!m_enabled)
                return;

            var profiler = ThreadProfiler;
            profiler.Stopwatch.Restart();

            var drawProfiler = m_selectedProfiler;
            if (drawProfiler != null)
            {
                int lastFrameIndex;
                using (drawProfiler.LockHistory(out lastFrameIndex))
                {
                    int frameToDraw = m_useCustomFrame ? m_selectedFrame : lastFrameIndex;
                    Draw(drawProfiler, lastFrameIndex, frameToDraw);
                }

                profiler.ProfileCustomValue("Profiler.Draw", member, line, file, 0, MyTimeSpan.FromMilliseconds(profiler.Stopwatch.Elapsed.TotalMilliseconds), null, null);
            }
        }

        protected abstract void Draw(MyProfiler drawProfiler, int lastFrameIndex, int frameToDraw);

        [Conditional(PerformanceProfilingSymbol)]
        public void StartProfilingBlock(string blockName = null, float customValue = 0, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            ThreadProfiler.StartBlock(blockName, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public void EndProfilingBlock(float customValue = 0, MyTimeSpan? customTime = null, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            ThreadProfiler.EndBlock(member, line, file, customTime, customValue, timeFormat, valueFormat);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public void GPU_StartProfilingBlock(string blockName = null, float customValue = 0, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            GpuProfiler.StartBlock(blockName, member, line, file);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public void GPU_EndProfilingBlock(float customValue = 0, MyTimeSpan? customTime = null, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            GpuProfiler.EndBlock(member, line, file, customTime, customValue, timeFormat, valueFormat);
        }

        // same as EndProfilingBlock(); StartProfilingBlock(string name);
        [Conditional(PerformanceProfilingSymbol)]
        public void StartNextBlock(string name, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            EndProfilingBlock(0, null, null, null, member, line, file);
            StartProfilingBlock(name, 0, member, line, file);
        }

        public void InitMemoryHack(string name)
        {
            ThreadProfiler.InitMemoryHack(name);
        }

        [Conditional(PerformanceProfilingSymbol)]
        public void ProfileCustomValue(string name, float value, MyTimeSpan? customTime = null, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            if (m_levelLimit != -1)
            {
                return;
            }

            ThreadProfiler.ProfileCustomValue(name, member, line, file, value, customTime, timeFormat, valueFormat);
        }

        internal void DestroyThread()
        {
            m_threadProfilers.Remove(m_threadProfiler);
            if (m_selectedProfiler == m_threadProfiler)
                m_selectedProfiler = m_threadProfilers.Count > 0 ? m_threadProfilers[0] : null;

            m_threadProfiler = null;
        }

        public void SetLevel(int index)
        {
            m_levelLimit = index;
            if (m_levelLimit < -1)
                m_levelLimit = -1;
            SetLevel();
        }

        public void Dump()
        {
            try
            {
                StringBuilder dump;
                string path = null;
                for (; m_currentDumpNumber < 100; m_currentDumpNumber++)
                {
                    path = string.Concat(VRage.FileSystem.MyFileSystem.UserDataPath, string.Format("\\dump{0}.xml", m_currentDumpNumber));
                    if (!VRage.FileSystem.MyFileSystem.FileExists(path))
                        break;
                }
                if (path == null)
                    return;
                var stream = VRage.FileSystem.MyFileSystem.OpenWrite(path);
                if (stream == null)
                    return;
                var wr = new System.IO.StreamWriter(stream);
                dump = ThreadProfiler.Dump();
                wr.Write(dump);
                wr.Close();
                stream.Close();
            }
            catch { }
        }

        public static MyProfiler GetProfilerAtIndex(int index)
        {
            index = MyMath.Mod(index, m_threadProfilers.Count);
            if (m_threadProfilers[index] != null)
                return m_threadProfilers[index];
            else
                return m_threadProfilers[0];
        }
    }
}
