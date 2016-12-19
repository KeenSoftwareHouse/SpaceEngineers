using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using VRage.Profiler;
using VRageMath;

// We're using if(constant) here and want to keep using it
#pragma warning disable 0162

namespace VRageRender.Profiler
{
    public abstract class MyRenderProfilerRendering : MyRenderProfiler
    {
        /// <summary>
        /// Returns viewport size in pixels
        /// </summary>
        public abstract Vector2 ViewportSize { get; }
        public abstract Vector2 MeasureText(StringBuilder text, float scale);
        public abstract float DrawText(Vector2 screenCoord, StringBuilder text, Color color, float scale);
        public abstract float DrawTextShadow(Vector2 screenCoord, StringBuilder text, Color color, float scale);
        public abstract void DrawOnScreenLine(Vector3 v0, Vector3 v1, Color color);
        public abstract void Init();
        public abstract void BeginLineBatch();
        public abstract void EndLineBatch();

        bool m_initialized = false;
        System.Globalization.NumberFormatInfo m_numberInfo = new System.Globalization.NumberFormatInfo();

        void DrawBlockLine(float[] data, int start, int end, MyDrawArea area, Color color)
        {
            Vector3 v0 = Vector3.Zero;
            Vector3 v1 = Vector3.Zero;

            for (int i = start + 1; i <= end; i++)
            {
                v0.X = -1.0f + area.x_start + area.x_scale * (i - 1) / 512.0f;
                v0.Y = area.y_start + data[i - 1] * area.y_scale / area.y_range;
                v0.Z = 0;

                v1.X = -1.0f + area.x_start + area.x_scale * i / 512.0f;
                v1.Y = area.y_start + data[i] * area.y_scale / area.y_range;
                v1.Z = 0;

                if (v0.Y - area.y_start > 1e-3f || v1.Y - area.y_start > 1e-3f)
                    DrawOnScreenLine(v0, v1, color);
            }
        }

        void DrawBlockLineSeparated(float[] data, int lastFrameIndex, int windowEnd, MyDrawArea scale, Color color)
        {
            if (lastFrameIndex > windowEnd)
            {
                DrawBlockLine(data, windowEnd, lastFrameIndex, scale, color);
            }
            else
            {
                DrawBlockLine(data, 0, lastFrameIndex, scale, color);
                DrawBlockLine(data, windowEnd, MyProfiler.MAX_FRAMES - 1, scale, color);
            }
        }

        void DrawEvent(float textPosY, MyProfilerBlock profilerBlock, int blockIndex, int frameIndex, int lastValidFrame, ref Color color)
        {
            float miliseconds = 0;
            long managedMemory = 0;
            float processMemory = 0;
            int numCalls = -1; // To show update window in profiler
            float customValue = 0;

            if (IsValidIndex(frameIndex, lastValidFrame))
            {
                miliseconds = profilerBlock.Miliseconds[frameIndex];
                managedMemory = profilerBlock.ManagedMemoryBytes[frameIndex];
                processMemory = profilerBlock.ProcessMemory[frameIndex];
                numCalls = profilerBlock.NumCallsArray[frameIndex];
                customValue = profilerBlock.CustomValues[frameIndex];
            }

            float Y_TEXT_POSITION = ViewportSize.Y / 2;

            float textScale = 0.7f;

            if (blockIndex >= 0)
                m_text.Clear().Append(blockIndex + 1).Append(" ").Append(profilerBlock.Name);
            else
                m_text.Clear().Append("- ").Append(profilerBlock.Name);
            DrawTextShadow(new Vector2(20, textPosY), m_text, color, textScale);

            float length = 500;

            m_text.Clear();
            m_text.Append("(").Append(profilerBlock.Children.Count).Append(") ");
            DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);
            length += 50 * textScale;

            m_text.Clear();
            //text.Append(((index != -1 ? profilerBlock.TimePercentage[index] : profilerBlock.averagePctg)).ToString("#,#0.0%"));
            //MyDebugDraw.DrawTextShadow(new Vector2(20 + length, textPosY), text, color, textScale);
            length += 155 * textScale;

            m_text.Clear();
            m_text.ConcatFormat(profilerBlock.TimeFormat ?? "{0:.00}ms", miliseconds);
            DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);
            length += 155 * textScale;

            m_text.Clear();
            if (managedMemory < 1024 && managedMemory > -1024) // Still in bytes?
            {
                m_text.Append(managedMemory.ToString()).Append(" B");
            }
            else if (managedMemory < 1048576 && managedMemory > -1048576) // Still in kilobytes?
            {
                float managedMemoryKB = managedMemory / 1024f;
                m_text.Concat(managedMemoryKB, 3).Append(" KB");
            }
            else // Else display in megabytes
            {
                float managedMemoryKB = managedMemory / 1048576;
                m_text.Concat(managedMemoryKB, 3).Append(" MB");
            }
            DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);
            length += 40 + 158 * textScale;

            m_text.Clear();
            if (MemoryProfiling)
            {
                m_text.Concat(processMemory, 3).Append(" MB");
                DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);
                length += 158 * textScale;

                m_text.Clear();
            }

            length += 40 + 40 * textScale;
            m_text.ConcatFormat(profilerBlock.CallFormat ?? "{0} calls", numCalls);
            DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);

            length += 150 * textScale;
            m_text.Clear();
            m_text.ConcatFormat(profilerBlock.ValueFormat ?? "Custom: {0:.00}", customValue);
            DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);

            int maxIndex;
            length += 250 * textScale;
            float max = FindMaxWrap(profilerBlock.Miliseconds, frameIndex - m_frameLocalArea / 2, frameIndex + m_frameLocalArea / 2, lastValidFrame, out maxIndex);
            m_text.Clear();
            m_text.ConcatFormat(profilerBlock.TimeFormat ?? "{0:.00}ms", max);
            DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);

            length += DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);
        }

        protected sealed override void Draw(MyProfiler drawProfiler, int lastFrameIndex, int frameToDraw)
        {
            Debug.Assert(frameToDraw >= 0 && frameToDraw < MyProfiler.MAX_FRAMES, "Invalid selected frame");

            if (!m_initialized)
            {
                // Init linebatch
                Init();
                m_initialized = true;
                m_fpsBlock.Start(false);
            }

            // Handle FPS timer
            m_fpsBlock.End(false);

            float elapsedTime = (float)m_fpsBlock.Elapsed.Seconds;
            float invElapsedTime = elapsedTime > 0 ? 1 / elapsedTime : 0;
            m_fpsPctg = 0.9f * m_fpsPctg + 0.1f * invElapsedTime;

            if (MemoryProfiling)
            {
                // Handle memory usage for frame
                float processDeltaMB = m_fpsBlock.ProcessDeltaMB;
                m_fpsBlock.ProcessMemory[lastFrameIndex] = processDeltaMB;
            }

            long managedDeltaMB = m_fpsBlock.ManagedDeltaMB;
            m_fpsBlock.ManagedMemoryBytes[lastFrameIndex] = managedDeltaMB;
            m_fpsBlock.CustomValues[lastFrameIndex] = m_fpsBlock.CustomValue;

            m_fpsBlock.Reset();

            m_fpsBlock.Start(false);

            if (m_enabled)
            {
                // Draw events as text 
                float eventLineSize = 20;
                float largeTextLineSize = 28;
                float textOffsetY = ViewportSize.Y / 2 - 8 * largeTextLineSize;

                // Draw thread name and level limit
                m_text.Clear();
                m_text.ConcatFormat("\"{2}\" ({0}/{1})", m_threadProfilers.IndexOf(m_selectedProfiler) + 1, m_threadProfilers.Count, m_selectedProfiler.DisplayedName).AppendLine();
                m_text.Append("Level limit: ").AppendInt32(m_selectedProfiler.LevelLimit).AppendLine();
                DrawText(new Vector2(20, textOffsetY), m_text, Color.LightGray, 1);
                textOffsetY += largeTextLineSize * 2 + 10;

                // Draw frame number and local area
                m_text.Clear();
                m_text.Append("Frame: ").AppendInt32(frameToDraw).AppendLine();
                m_text.Append("Local area: ").AppendInt32(m_frameLocalArea);
                DrawText(new Vector2(20, textOffsetY), m_text, Color.Yellow, 1);
                textOffsetY += largeTextLineSize * 2 + 10;

                // Draw fps and total calls
                m_text.Clear();
                m_text.Append(m_fpsBlock.Name).Append(" ");
                if (!m_useCustomFrame) // Show FPS only when not using custom frame
                    m_text.AppendDecimal(m_fpsPctg, 3);
                m_text.AppendLine();
                m_text.Append("Total calls: ").AppendInt32(IsValidIndex(frameToDraw, lastFrameIndex) ? m_selectedProfiler.TotalCalls[frameToDraw] : -1);
                DrawText(new Vector2(20, textOffsetY), m_text, Color.Red, 1);
                textOffsetY += largeTextLineSize;

                m_text.Clear();
                if (!VRage.MyCompilationSymbols.PerformanceProfiling)
                {
                    m_text.Append("MyCompilationSymbols.PerformanceProfiling NOT ENABLED!").AppendLine();
                }
                if (!ProfilerProcessingEnabled)
                {
                    m_text.Append("Profiler processing disabled, F12 -> Profiler").AppendLine();
                }
                DrawText(new Vector2(0, 0), m_text, Color.Yellow, 0.6f);

                textOffsetY = ViewportSize.Y / 2;
                List<MyProfilerBlock> children = m_selectedProfiler.SelectedRootChildren;
                List<MyProfilerBlock> sortedChildren = GetSortedChildren(frameToDraw);

                // Draw the 'stack trace'
                m_text.Clear();
                MyProfilerBlock currentBlock = m_selectedProfiler.SelectedRoot;

                while (currentBlock != null)
                {
                    // Stop inserting new elements if the path becomes too long
                    if (currentBlock.Name.Length + 3 + m_text.Length > 170)
                    {
                        m_text.Insert(0, "... > ");
                        break;
                    }

                    if (m_text.Length > 0)
                        m_text.Insert(0, " > ");
                    m_text.Insert(0, currentBlock.Name);
                    currentBlock = currentBlock.Parent;
                }

                DrawTextShadow(new Vector2(20, textOffsetY), m_text, Color.White, 0.7f);
                textOffsetY += eventLineSize;

                if (m_selectedProfiler.SelectedRoot != null)
                {
                    Color whiteColor = Color.White;
                    DrawEvent(textOffsetY, m_selectedProfiler.SelectedRoot, -1, frameToDraw, lastFrameIndex, ref whiteColor);
                    textOffsetY += eventLineSize;
                }

                if (sortedChildren.Count > 0)
                {
                    // Draw the sorting order indicator
                    m_text.Clear().Append("\\/");
                    switch (m_sortingOrder)
                    {
                        case RenderProfilerSortingOrder.Id:
                            m_text.Append(" ASC");
                            DrawTextShadow(new Vector2(20, textOffsetY), m_text, Color.White, 0.7f);
                            break;
                        case RenderProfilerSortingOrder.MillisecondsLastFrame:
                            m_text.Append(" DESC");
                            DrawTextShadow(new Vector2(660, textOffsetY), m_text, Color.White, 0.7f);
                            break;
                        case RenderProfilerSortingOrder.MillisecondsAverage:
                            m_text.Append(" DESC");
                            DrawTextShadow(new Vector2(1270, textOffsetY), m_text, Color.White, 0.7f);
                            break;
                    }
                    textOffsetY += eventLineSize;

                    // Draw the profiler blocks
                    for (int i = 0; i < sortedChildren.Count; i++)
                    {
                        MyProfilerBlock profilerBlock = sortedChildren[i];

                        Color lineColor = IndexToColor(children.IndexOf(profilerBlock));

                        DrawEvent(textOffsetY, profilerBlock, i, frameToDraw, lastFrameIndex, ref lineColor);
                        textOffsetY += eventLineSize;
                    }
                }
                else
                {
                    m_text.Clear().Append("No more blocks at this point!");
                    textOffsetY += eventLineSize;
                    DrawTextShadow(new Vector2(20, textOffsetY), m_text, Color.White, 0.7f);
                    textOffsetY += eventLineSize;
                }

                // Draw graphs
                BeginLineBatch();
                DrawPerfEvents(lastFrameIndex);
                EndLineBatch();
            }

            // Update horizontal offset
            if (!Paused)
            {
                m_selectedFrame = lastFrameIndex;
            }
        }

        void DrawCustomFrameLine()
        {
            // Draw selected frame
            if (m_useCustomFrame)
            {
                Vector3 v0, v1;
                if (m_selectedFrame >= 0 && m_selectedFrame < MyProfiler.MAX_FRAMES)
                {
                    v0.X = -1.0f + m_memoryGraphScale.x_start + m_memoryGraphScale.x_scale * (m_selectedFrame) / 512.0f;
                    v0.Y = m_memoryGraphScale.y_start;
                    v0.Z = 0;

                    v1.X = v0.X;
                    v1.Y = 1;
                    v1.Z = 0;

                    DrawOnScreenLine(v0, v1, Color.Yellow);
                }
            }
        }

        void DrawGraphs(int lastFrameIndex)
        {
            // Next valid index
            int windowEnd = (lastFrameIndex + 1 + MyProfiler.UPDATE_WINDOW) % MyProfiler.MAX_FRAMES;

            // Draw graph for selected event
            if (m_selectedProfiler.SelectedRoot != null)
            {
                DrawBlockLineSeparated(m_selectedProfiler.SelectedRoot.Miliseconds, lastFrameIndex, windowEnd, m_milisecondsGraphScale, Color.White);
            }

            // Draw graphs for selected events
            var children = m_selectedProfiler.SelectedRootChildren;
            for (int i = 0; i < children.Count; i++)
            {
                var block = children[i];
                DrawBlockLineSeparated(block.Miliseconds, lastFrameIndex, windowEnd, m_milisecondsGraphScale, IndexToColor(i));

                // Broken/Unused
                //DrawBlockLineSeparated(MemoryProfiling ? block.ProcessMemory : block.ManagedMemory, lastFrameIndex, windowEnd, m_memoryGraphScale, IndexToColor(i));
            }
        }

        private void DrawLegend()
        {
            // Draw legend
            float x_legend_start = m_milisecondsGraphScale.x_start;
            float x_legend_ms_size = 0.01f;
            DrawOnScreenLine(new Vector3(-1.0f + x_legend_start, 0, 0), new Vector3(-1.0f + x_legend_start, m_milisecondsGraphScale.y_scale, 0), new Color(40, 40, 40));

            float viewportWidth = ViewportSize.X;
            float viewportHeight = ViewportSize.Y;
            float max = m_milisecondsGraphScale.y_legend_ms_increment * m_milisecondsGraphScale.y_legend_ms_count;

            int numDecimals = 0;
            float x = m_milisecondsGraphScale.y_legend_ms_increment;
            while (x != (int)x && numDecimals < 5)
            {
                x *= 10;
                numDecimals++;
            }
            m_numberInfo.NumberDecimalDigits = numDecimals;

            // Miliseconds legend
            for (int i = 0; i <= m_milisecondsGraphScale.y_legend_ms_count; i++)
            {
                m_text.Clear();
                m_text.ConcatFormat("{0}", i * m_milisecondsGraphScale.y_legend_ms_increment, m_numberInfo);
                var textSize = MeasureText(m_text, 0.7f);
                DrawText(new Vector2(0.5f * viewportWidth * x_legend_start - textSize.X - 6 + 3 * x_legend_ms_size, -10 + 0.5f * viewportHeight - m_milisecondsGraphScale.y_legend_increment * i * 0.5f * viewportHeight), m_text, Color.Silver, 0.7f);

                Vector3 v0 = new Vector3(-1.0f + x_legend_start, i * m_milisecondsGraphScale.y_legend_increment, 0);
                Vector3 v1 = new Vector3(v0.X + m_milisecondsGraphScale.x_scale * 2, i * m_milisecondsGraphScale.y_legend_increment, 0);
                DrawOnScreenLine(v0, v1, new Color(40, 40, 40));
            }

            m_text.Clear().Append(m_selectedProfiler.AxisName);
            DrawText(new Vector2(0.5f * viewportWidth * x_legend_start - 25f + 3 * x_legend_ms_size, -10 + 0.5f * viewportHeight - m_milisecondsGraphScale.y_scale * 0.5f * viewportHeight * 1.05f), m_text, Color.Silver, 0.7f);
        }

        void DrawPerfEvents(int lastFrameIndex)
        {
            DrawLegend();
            DrawGraphs(lastFrameIndex);
            DrawCustomFrameLine();

            // Broken/Unused
            // Memory legend
            //x_legend_start = m_milisecondsGraphScale.x_start + 0.48f;
            //x_legend_ms_size = -0.01f;

            //for (int i = 0; i <= legendMsCount; i++)
            //{
            //    m_text.Clear();
            //    m_text.AppendDecimal((i * m_memoryGraphScale.y_range / legendMsCount), 4);
            //    DrawText(new Vector2(0.5f * viewportWidth * x_legend_start - 25f + 3 * x_legend_ms_size,
            //        -10 + 0.85f * viewportHeight - m_memoryGraphScale.y_scale * 0.5f * viewportHeight * ((float)i / legendMsCount)),
            //        m_text, Color.Yellow, 0.7f);

            //    if (i == 0)
            //    {
            //        m_text.Clear();
            //        m_text.Append("[MB]");
            //        DrawText(new Vector2(0.5f * viewportWidth * x_legend_start - 25f + 3 * x_legend_ms_size,
            //            -30 + 0.85f * viewportHeight - m_memoryGraphScale.y_scale * 0.5f * viewportHeight * ((float)i / legendMsCount)),
            //            m_text, Color.Yellow, 0.7f);
            //    }
            //}
        }
    }
}
