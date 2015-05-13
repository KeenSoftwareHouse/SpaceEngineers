#region Using

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using VRage.Library.Utils;
using VRage.Profiler;
using VRageMath;

#endregion


namespace VRageRender.Profiler
{
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
        public const string Symbol = VRage.MyCompilationSymbols.RenderProfiling ? "WINDOWS" : "__RANDOM_UNDEFINED_PROFILING_SYMBOL__";

        protected class DrawArea
        {
            public float x_start { get; private set; }
            public float y_start { get; private set; }
            public float x_scale { get; private set; }
            public float y_scale { get; private set; }
            public float y_inv_range { get; private set; }
            public float y_range { get { return 1.0f / y_inv_range; } }
            public float y_legend_increment { get { return y_scale / y_range * m_legendMsIncrement; } }
            public float y_legend_ms_increment { get { return m_legendMsIncrement; } }
            public int y_legend_ms_count { get { return m_legendMsCount; } }

            private int m_legendMsCount;
            private float m_legendMsIncrement;

            private static readonly float[] m_increments = { 0.1f, 0.2f, 0.25f, 0.5f, 1, 2, 2.5f, 5, 10, 20, 25, 50, 100, 200, 250 };

            public DrawArea(float xStart, float yStart, float xScale, float yScale, float yRange)
            {
                x_start = xStart;
                y_start = yStart;
                x_scale = xScale;
                y_scale = yScale;
                y_inv_range = 1.0f / yRange;
                UpdateIncrements();
            }

            public void IncreaseYRange()
            {
                y_inv_range *= 0.75f;
                UpdateIncrements();
            }

            public void DecreaseYRange()
            {
                y_inv_range *= 1.333333f;
                UpdateIncrements();
            }

            private void UpdateIncrements()
            {
                m_legendMsCount = 15;

                m_legendMsIncrement = 5;
                for (int i = 0; i < m_increments.Length; ++i)
                {
                    float count = y_range / m_increments[i];
                    if (count >= 5.0f && count < 12.0f)
                    {
                        m_legendMsIncrement = m_increments[i];
                        m_legendMsCount = (int)Math.Floor(count);
                        break;
                    }
                }
            }
        }

        protected static DrawArea m_milisecondsGraphScale = new DrawArea(0.5f, 0, (2 - 0.51f) / 2, 0.9f, 25);
        protected static DrawArea m_memoryGraphScale = new DrawArea(0.5f, -0.7f, (2 - 0.51f) / 2, 0.6f, 0.001f);

        protected static Color[] m_colors = { Color.Aqua, Color.Orange, Color.BlueViolet * 1.5f, Color.BurlyWood, Color.Chartreuse,
                                  Color.CornflowerBlue, Color.Cyan, Color.ForestGreen, Color.Fuchsia,
                                  Color.Gold, Color.GreenYellow, Color.LightBlue, Color.LightGreen, Color.LimeGreen,
                                  Color.Magenta, Color.Navy, Color.Orchid, Color.PeachPuff, Color.Purple };

        protected static Color IndexToColor(int index)
        {
            return m_colors[index % m_colors.Length];
        }

        protected StringBuilder m_text = new StringBuilder(100);

        // Set to true to track memory in Render Profiler
        public const bool MemoryProfiling = false;

        protected static MyProfiler.MyProfilerBlock m_fpsBlock;
        protected static float m_fpsPctg;
        
        //{Color.Cyan, Color.Orange, new Color(208, 86, 255), Color.BurlyWood, Color.LightGray,
        //                          Color.CornflowerBlue,Color.LawnGreen,  Color.Fuchsia,
        //                          Color.Gold, Color.OrangeRed,Color.YellowGreen, Color.LightBlue, Color.LightCoral, Color.LimeGreen,
        //                          Color.Magenta, Color.Navy, Color.Orchid, Color.PeachPuff, Color.Purple };

        public static bool Paused = false;

        [ThreadStatic]
        static MyProfiler m_threadProfiler;
        static MyProfiler m_gpuProfiler;

        static MyProfiler GpuProfiler
        {
            get
            {
                if (m_gpuProfiler == null)
                { 
                    lock (m_threadProfilers)
                    {
                        m_gpuProfiler = new MyProfiler(m_threadProfilers.Count, MemoryProfiling);
                        m_gpuProfiler.m_customName = "GPU";
                        //m_gpuProfiler.AutoCommit = false;
                        m_threadProfilers.Add(m_gpuProfiler);
                    }
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
                    lock (m_threadProfilers)
                    {
                        m_threadProfiler = new MyProfiler(m_threadProfilers.Count, MemoryProfiling);
                        m_threadProfilers.Add(m_threadProfiler);
                    }
                }
                return m_threadProfiler;
            }
        }

        protected static List<MyProfiler> m_threadProfilers = new List<MyProfiler>(16);

        protected static MyProfiler m_selectedProfiler;

        protected static bool m_enabled = false;
        protected static int m_selectedFrame = 0;   // Index of selected frame. It will be showed in text legend.
        protected static int m_levelLimit = -1;
        protected static bool m_useCustomFrame = false;
        protected static int m_frameLocalArea = MyProfiler.MAX_FRAMES;

        static MyRenderProfiler()
        {
            // Create block, some unique id
            m_fpsBlock = MyProfiler.CreateExternalBlock("FPS", -2);
        }

        public static MyProfiler.MyProfilerBlock FindBlockByIndex(int index)
        {
            var children = m_selectedProfiler.SelectedRootChildren;
            if (index >= 0 && index < children.Count)
                return children[index];
            return null;
        }

        protected static bool IsValidIndex(int frameIndex, int lastValidFrame)
        {
            int wrappedIndex = frameIndex > lastValidFrame ? frameIndex : frameIndex + MyProfiler.MAX_FRAMES;
            return wrappedIndex > (lastValidFrame + MyProfiler.UPDATE_WINDOW); // Outside update window
        }

        public static MyProfiler.MyProfilerBlock FindBlockByMax(int frameIndex, int lastValidFrame)
        {
            if (!IsValidIndex(frameIndex, lastValidFrame))
                return null;

            float max = float.MinValue;
            MyProfiler.MyProfilerBlock block = null;

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
                        // Enable or Disable profiler drawing
                        if (m_enabled && m_selectedProfiler.SelectedRoot == null)
                        {
                            m_enabled = false;
                            m_useCustomFrame = false;
                        }
                        else if (!m_enabled)
                        {
                            m_enabled = true;
                        }
                        else
                        {
                            // Go to parent node
                            if (m_selectedProfiler.SelectedRoot != null)
                            {
                                m_selectedProfiler.SelectedRoot = m_selectedProfiler.SelectedRoot.Parent;
                            }
                        }
                        break;
                    }

                case RenderProfilerCommand.JumpToLevel:
                    {
                        m_selectedProfiler.SelectedRoot = FindBlockByIndex(index - 1); // On screen it's indexed from 1 (zero is level up)
                        break;
                    }

                case RenderProfilerCommand.FindMaxChild:
                    {
                        MyProfiler.MyProfilerBlock block;
                        int lastFrameIndex;
                        using (m_selectedProfiler.LockHistory(out lastFrameIndex))
                        {
                            block = FindBlockByMax(m_selectedFrame, lastFrameIndex);
                        }
                        if (block != null)
                        {
                            m_selectedProfiler.SelectedRoot = block;
                        }
                        break;
                    }

                case RenderProfilerCommand.Pause:
                    {
                        Paused = !Paused;
                        m_useCustomFrame = false; // Turn-off custom frame after ALT + ENTER

                        break;
                    }

                case RenderProfilerCommand.NextThread:
                    {
                        lock (m_threadProfilers)
                        {
                            int profilerIndex = (m_threadProfilers.IndexOf(m_selectedProfiler) + 1) % m_threadProfilers.Count;
                            m_selectedProfiler = m_threadProfilers[profilerIndex];
                        }
                        break;
                    }

                case RenderProfilerCommand.PreviousThread:
                    {
                        lock (m_threadProfilers)
                        {
                            int profilerIndex = (m_threadProfilers.IndexOf(m_selectedProfiler) - 1 + m_threadProfilers.Count) % m_threadProfilers.Count;
                            m_selectedProfiler = m_threadProfilers[profilerIndex];
                        }
                        break;
                    }

                case RenderProfilerCommand.NextFrame:
                    {
                        MyRenderProfiler.NextFrame();
                        break;
                    }

                case RenderProfilerCommand.PreviousFrame:
                    {
                        MyRenderProfiler.PreviousFrame();
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
                    p.SetNewLevelLimit(m_levelLimit);
                }
            }
        }

        static void PreviousFrame()
        {
            m_useCustomFrame = true;

            m_selectedFrame--;
            if (m_selectedFrame < 0)
                m_selectedFrame = MyProfiler.MAX_FRAMES - 1;
        }

        static void NextFrame()
        {
            m_useCustomFrame = true;

            m_selectedFrame++;
            if (m_selectedFrame >= MyProfiler.MAX_FRAMES)
                m_selectedFrame = 0;
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

        [Conditional(MyRenderProfiler.Symbol)]
        public void GetAutocommit(ref bool val)
        {
            val = ThreadProfiler.AutoCommit;
        }

        [Conditional(MyRenderProfiler.Symbol)]
        public void SetAutocommit(bool val)
        {
            ThreadProfiler.AutoCommit = val;
        }

        [Conditional(MyRenderProfiler.Symbol)]
        public void Commit([CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            var profiler = ThreadProfiler;
            profiler.Stopwatch.Restart();
            if (!Paused)
            {
                profiler.CommitFrame();
            }
            else
            {
                profiler.ClearFrame();
            }
            profiler.ProfileCustomValue("Profiler.Commit", member, line, file, 0, MyTimeSpan.FromMiliseconds(profiler.Stopwatch.ElapsedMilliseconds), null, null);
        }

        [Conditional(MyRenderProfiler.Symbol)]
        public void Draw([CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            if (!m_enabled)
                return;

            var profiler = ThreadProfiler;
            profiler.Stopwatch.Restart();

            var drawProfiler = m_selectedProfiler;
            int lastFrameIndex;
            using (drawProfiler.LockHistory(out lastFrameIndex))
            {
                int frameToDraw = m_useCustomFrame ? m_selectedFrame : lastFrameIndex;
                Draw(drawProfiler, lastFrameIndex, frameToDraw);
            }

            profiler.ProfileCustomValue("Profiler.Draw", member, line, file, 0, MyTimeSpan.FromMiliseconds(profiler.Stopwatch.ElapsedMilliseconds), null, null);
        }

        protected abstract void Draw(MyProfiler drawProfiler, int lastFrameIndex, int frameToDraw);

        [Conditional(MyRenderProfiler.Symbol)]
        public void StartProfilingBlock(string blockName = null, float customValue = 0, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            ThreadProfiler.StartBlock(blockName, member, line, file);

            if (m_selectedProfiler == null)
            {
                m_selectedProfiler = ThreadProfiler;
            }
        }

        [Conditional(MyRenderProfiler.Symbol)]
        public void EndProfilingBlock(float customValue = 0, MyTimeSpan? customTime = null, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            ThreadProfiler.EndBlock(member, line, file, customTime, customValue, timeFormat, valueFormat);
        }

        [Conditional(MyRenderProfiler.Symbol)]
        public void GPU_StartProfilingBlock(string blockName = null, float customValue = 0, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            GpuProfiler.StartBlock(blockName, member, line, file);
        }

        [Conditional(MyRenderProfiler.Symbol)]
        public void GPU_EndProfilingBlock(float customValue = 0, MyTimeSpan? customTime = null, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            GpuProfiler.EndBlock(member, line, file, customTime, customValue, timeFormat, valueFormat);
        }

        // same as EndProfilingBlock(); StartProfilingBlock(string name);
        [Conditional(MyRenderProfiler.Symbol)]
        public void StartNextBlock(string name, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            EndProfilingBlock(0, null, null, null, member, line, file);
            StartProfilingBlock(name, 0, member, line, file);
        }

        [Conditional(MyRenderProfiler.Symbol)]
        public void InitMemoryHack(string name)
        {
            ThreadProfiler.InitMemoryHack(name);
        }

        [Conditional(MyRenderProfiler.Symbol)]
        public void ProfileCustomValue(string name, float value, MyTimeSpan? customTime = null, string timeFormat = null, string valueFormat = null, [CallerMemberName] string member = "", [CallerLineNumber] int line = 0, [CallerFilePath] string file = "")
        {
            if (m_levelLimit != -1)
            {
                return;
            }

            ThreadProfiler.ProfileCustomValue(name, member, line, file, value, customTime, timeFormat, valueFormat);
        }
    }
}
