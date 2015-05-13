#region Using

using System;
using System.Text;
using System.Collections.Generic;
using VRageMath;
using System.Diagnostics;
using System.Threading;
using VRage.Profiler;
using System.Runtime.CompilerServices;
using VRage;

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
    public class MyRenderProfilerDX9 : MyRenderProfiler
    {
        static MyLineBatch m_lineBatch;
        
        private static void DrawBlockLine(float[] data, int start, int end, DrawArea area, Color color)
        {
            Vector3 v0 = Vector3.Zero;
            Vector3 v1 = Vector3.Zero;

            for (int i = start + 1; i <= end; i++)
            {
                v0.X = -1.0f + area.x_start + area.x_scale * (i - 1) / 512.0f;
                v0.Y = area.y_start + data[i - 1] * area.y_scale * area.y_inv_range;
                v0.Z = 0;

                v1.X = -1.0f + area.x_start + area.x_scale * i / 512.0f;
                v1.Y = area.y_start + data[i] * area.y_scale * area.y_inv_range;
                v1.Z = 0;

                if (v0.Y - area.y_start > 1e-3f || v1.Y - area.y_start > 1e-3f)
                    m_lineBatch.DrawOnScreenLine(v0, v1, color);
            }
        }

        private static void DrawBlockLineSeparated(float[] data, int lastFrameIndex, int windowEnd, DrawArea scale, Color color)
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

        public void DrawEvent(float textPosY, MyProfiler.MyProfilerBlock profilerBlock, int blockIndex, int frameIndex, int lastValidFrame)
        {
            Color color = IndexToColor(blockIndex);
            float miliseconds = 0;
            float managedMemory = 0;
            float processMemory = 0;
            int numCalls = -1; // To show update window in profiler
            float customValue = 0;

            if (IsValidIndex(frameIndex, lastValidFrame))
            {
                miliseconds = profilerBlock.Miliseconds[frameIndex];
                managedMemory = profilerBlock.ManagedMemory[frameIndex];
                processMemory = profilerBlock.ProcessMemory[frameIndex];
                numCalls = profilerBlock.NumCallsArray[frameIndex];
                customValue = profilerBlock.CustomValues[frameIndex];
            }

            float Y_TEXT_POSITION = MyRender.GraphicsDevice.Viewport.Height / 2;

            float textScale = 0.7f;

            m_text.Clear().Append(blockIndex + 1).Append(" ").Append(profilerBlock.Name);
            MyRender.DrawTextShadow(new Vector2(20, textPosY), m_text, color, textScale);

            float length = 500;

            m_text.Clear();
            m_text.Append("(").Append(profilerBlock.Children.Count).Append(") ");
            MyRender.DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);
            length += 50 * textScale;

            m_text.Clear();
            //text.Append(((index != -1 ? profilerBlock.TimePercentage[index] : profilerBlock.averagePctg)).ToString("#,#0.0%"));
            //MyDebugDraw.DrawTextShadow(new Vector2(20 + length, textPosY), text, color, textScale);
            length += 155 * textScale;

            m_text.Clear();
            m_text.ConcatFormat(profilerBlock.TimeFormat ?? "{0:.00}ms", miliseconds);
            MyRender.DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);
            length += 155 * textScale;

            m_text.Clear();
            m_text.Concat(managedMemory, 3).Append(" GC");
            MyRender.DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);
            length += 40 + 158 * textScale;

            m_text.Clear();
            if (MemoryProfiling)
            {
                m_text.Concat(processMemory, 3).Append(" MB");
                MyRender.DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);
                length += 158 * textScale;

                m_text.Clear();
            }

            length += 40 + 40 * textScale;
            m_text.Append(numCalls);
            m_text.Append(" calls");
            MyRender.DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);

            length += 150 * textScale;
            m_text.Clear();
            m_text.ConcatFormat(profilerBlock.ValueFormat ?? "Custom: {0:.00}", customValue);
            MyRender.DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);

            int maxIndex;
            length += 250 * textScale;
            float max = FindMaxWrap(profilerBlock.Miliseconds, frameIndex - m_frameLocalArea / 2, frameIndex + m_frameLocalArea / 2, lastValidFrame, out maxIndex);
            m_text.Clear();
            m_text.ConcatFormat(profilerBlock.TimeFormat ?? "{0:.00}ms", max);
            MyRender.DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);

            length += MyRender.DrawTextShadow(new Vector2(20 + length, textPosY), m_text, color, textScale);
        }

        protected override void Draw(MyProfiler drawProfiler, int lastFrameIndex, int frameToDraw)
        {
            Debug.Assert(frameToDraw >= 0 && frameToDraw < MyProfiler.MAX_FRAMES, "Invalid selected frame");

            // Init linebatch
            if (m_lineBatch == null)
            {
                SharpDX.Direct3D9.Device device = MyRender.GraphicsDevice;
                m_lineBatch = new MyLineBatch(Matrix.Identity, Matrix.CreateOrthographicOffCenter(0, device.Viewport.Width, device.Viewport.Height, 0, 0, -1), 50000);

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

            float managedDeltaMB = m_fpsBlock.ManagedDeltaMB;
            m_fpsBlock.ManagedMemory[lastFrameIndex] = managedDeltaMB;
            m_fpsBlock.CustomValues[lastFrameIndex] = m_fpsBlock.CustomValue;

            m_fpsBlock.Reset();

            m_fpsBlock.Start(false);

            if (m_enabled)
            {
                // Draw events as text 
                float eventLineSize = 20;
                float largeTextLineSize = 28;
                float textOffsetY = MyRender.GraphicsDevice.Viewport.Height / 2 - 8 * largeTextLineSize;

                // Draw thread name and level limit
                m_text.Clear();
                m_text.ConcatFormat("\"{2}\" ({0}/{1})", m_selectedProfiler.GlobalProfilerIndex + 1, m_threadProfilers.Count, m_selectedProfiler.OwnerThread.Name).AppendLine();
                m_text.Append("Level limit: ").AppendInt32(m_levelLimit).AppendLine();
                MyRender.DrawText(new Vector2(20, textOffsetY), m_text, Color.LightGray, 1);
                textOffsetY += largeTextLineSize * 2 + 10;

                // Draw frame number and local area
                m_text.Clear();
                m_text.Append("Frame: ").AppendInt32(frameToDraw).AppendLine();
                m_text.Append("Local area: ").AppendInt32(m_frameLocalArea);
                MyRender.DrawText(new Vector2(20, textOffsetY), m_text, Color.Yellow, 1);
                textOffsetY += largeTextLineSize * 2 + 10;

                // Draw fps and total calls
                m_text.Clear();
                m_text.Append(m_fpsBlock.Name).Append(" ");
                if (!m_useCustomFrame) // Show FPS only when not using custom frame
                    m_text.AppendDecimal(m_fpsPctg, 3);
                m_text.AppendLine();
                m_text.Append("Total calls: ").AppendInt32(IsValidIndex(frameToDraw, lastFrameIndex) ? m_selectedProfiler.TotalCalls[frameToDraw] : -1);
                MyRender.DrawText(new Vector2(20, textOffsetY), m_text, Color.Red, 1);
                textOffsetY += largeTextLineSize;

                textOffsetY = MyRender.GraphicsDevice.Viewport.Height / 2;
                var children = m_selectedProfiler.SelectedRootChildren;
                for (int i = 0; i < children.Count; i++)
                {
                    MyProfiler.MyProfilerBlock profilerBlock = children[i];

                    DrawEvent(textOffsetY, profilerBlock, i, frameToDraw, lastFrameIndex);
                    textOffsetY += eventLineSize;
                }

                // Draw graphs
                m_lineBatch.Begin();
                DrawPerfEvents(lastFrameIndex);

                VRageRender.Graphics.BlendState.Opaque.Apply();
                m_lineBatch.End();
            }

            // Update horizontal offset
            if (!Paused && !m_useCustomFrame)
            {
                m_selectedFrame = lastFrameIndex;
            }
        }

        public void DrawPerfEvents(int lastFrameIndex)
        {
            // Next valid index
            int windowEnd = (lastFrameIndex + 1 + MyProfiler.UPDATE_WINDOW) % MyProfiler.MAX_FRAMES;

            // Draw graphs for selected events
            var children = m_selectedProfiler.SelectedRootChildren;
            for (int i = 0; i < children.Count; i++)
            {
                var block = children[i];
                DrawBlockLineSeparated(block.Miliseconds, lastFrameIndex, windowEnd, m_milisecondsGraphScale, IndexToColor(i));

                // Broken/Unused
                //DrawBlockLineSeparated(MemoryProfiling ? block.ProcessMemory : block.ManagedMemory, lastFrameIndex, windowEnd, m_memoryGraphScale, IndexToColor(i));
            }

            // Draw legend
            float x_legend_start = m_milisecondsGraphScale.x_start - 0.02f;
            float x_legend_ms_size = 0.01f;
            m_lineBatch.DrawOnScreenLine(new Vector3(-1.0f + x_legend_start, 0, 0), new Vector3(-1.0f + x_legend_start, m_milisecondsGraphScale.y_scale, 0), Color.Silver);

            m_text.Clear();

            float viewportWidth = MyRender.GraphicsDevice.Viewport.Width;
            float viewportHeight = MyRender.GraphicsDevice.Viewport.Height;

            // Miliseconds legend
            for (int i = 0; i <= m_milisecondsGraphScale.y_legend_ms_count; i++)
            {
                m_lineBatch.DrawOnScreenLine(new Vector3(-1.0f + x_legend_start, m_milisecondsGraphScale.y_legend_increment * i, 0),
                                            new Vector3(-1.0f + x_legend_start + x_legend_ms_size, m_milisecondsGraphScale.y_legend_increment * i, 0), Color.Silver);

                m_text.Clear();
                m_text.Append((i * m_milisecondsGraphScale.y_legend_ms_increment).ToString());
                MyRender.DrawText(new Vector2(0.5f * viewportWidth * x_legend_start - 25f + 3 * x_legend_ms_size, -10 + 0.5f * viewportHeight - m_milisecondsGraphScale.y_legend_increment * i * 0.5f * viewportHeight), m_text, Color.Silver, 0.7f);
            }

            m_text.Clear();
            m_text.Append("[ms]");
            MyRender.DrawText(new Vector2(0.5f * viewportWidth * x_legend_start - 25f + 3 * x_legend_ms_size, -10 + 0.5f * viewportHeight - m_milisecondsGraphScale.y_scale * 0.5f * viewportHeight * 1.05f), m_text, Color.Silver, 0.7f);

            // Broken/Unused
            // Memory legend
            //x_legend_start = m_milisecondsGraphScale.x_start + 0.48f;
            //x_legend_ms_size = -0.01f;

            //for (int i = 0; i <= legendMsCount; i++)
            //{
            //    m_text.Clear();
            //    m_text.AppendDecimal((i * m_memoryGraphScale.y_range / legendMsCount), 4);
            //    MyRender.DrawText(new Vector2(0.5f * viewportWidth * x_legend_start - 25f + 3 * x_legend_ms_size,
            //        -10 + 0.85f * viewportHeight - m_memoryGraphScale.y_scale * 0.5f * viewportHeight * ((float)i / legendMsCount)),
            //        m_text, Color.Yellow, 0.7f);

            //    if (i == 0)
            //    {
            //        m_text.Clear();
            //        m_text.Append("[MB]");
            //        MyRender.DrawText(new Vector2(0.5f * viewportWidth * x_legend_start - 25f + 3 * x_legend_ms_size,
            //            -30 + 0.85f * viewportHeight - m_memoryGraphScale.y_scale * 0.5f * viewportHeight * ((float)i / legendMsCount)),
            //            m_text, Color.Yellow, 0.7f);
            //    }
            //}

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
                    v1.Y = 0.9f;
                    v1.Z = 0;

                    m_lineBatch.DrawOnScreenLine(v0, v1, Color.Yellow);
                }
            }
        }
    }
}