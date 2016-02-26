using ParallelTasks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Stats;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    /// <summary>
    /// Draws statistics
    /// </summary>
    public static class MyRenderStatsDraw
    {
        static float m_rightGapSizeRatio = 0.1f;
        static float m_rightColumnWidth;
        static MyTimeSpan m_rightColumnChangeTime = MyRender11.CurrentDrawTime;

        static StringBuilder m_tmpDrawText = new StringBuilder(4096);

        public static void Draw(Dictionary<VRageRender.MyRenderStats.ColumnEnum, List<MyStats>> m_stats, float scale, Color color)
        {
            foreach (var pair in m_stats)
            {
                try
                {
                    foreach (var s in pair.Value)
                    {
                        s.WriteTo(m_tmpDrawText);
                        m_tmpDrawText.AppendLine(); // Newline between each group
                    }

                    Vector2 pos = new Vector2(0, 0);

                    if (pair.Key == VRageRender.MyRenderStats.ColumnEnum.Right)
                    {
                        Vector2 size = MyRender11.GetDebugFont().MeasureString(m_tmpDrawText, scale);
                        if (m_rightColumnWidth < size.X)
                        {
                            m_rightColumnWidth = size.X * m_rightGapSizeRatio; // Add some gap
                            m_rightColumnChangeTime = MyRender11.CurrentDrawTime;
                        }
                        else if (m_rightColumnWidth > size.X * m_rightGapSizeRatio && (MyRender11.CurrentDrawTime - m_rightColumnChangeTime).Seconds > 3)
                        {
                            m_rightColumnWidth = size.X * m_rightGapSizeRatio;
                            m_rightColumnChangeTime = MyRender11.CurrentDrawTime;
                        }
                        pos = new Vector2(MyRender11.ViewportResolution.X - m_rightColumnWidth, 0);
                    }

                    /* Add info about render buffers */
                    m_tmpDrawText.Append("Hardware Buffers (count/bytes):\n");

                    MyBufferStats stats, total = new MyBufferStats();

                    MyHwBuffers.GetConstantBufferStats(out stats);
                    m_tmpDrawText.AppendFormat("   Constant: {0:N0}/{1:N0}\n", stats.TotalBuffers, stats.TotalBytes);
                    total.TotalBytes += stats.TotalBytes; total.TotalBuffers += stats.TotalBuffers;

                    MyHwBuffers.GetVertexBufferStats(out stats);
                    m_tmpDrawText.AppendFormat("   Vertex: {0:N0}/{1:N0}\n", stats.TotalBuffers, stats.TotalBytes);
                    total.TotalBytes += stats.TotalBytes; total.TotalBuffers += stats.TotalBuffers;

                    MyHwBuffers.GetIndexBufferStats(out stats);
                    m_tmpDrawText.AppendFormat("   Index: {0:N0}/{1:N0}\n", stats.TotalBuffers, stats.TotalBytes);
                    total.TotalBytes += stats.TotalBytes; total.TotalBuffers += stats.TotalBuffers;

                    MyHwBuffers.GetStructuredBufferStats(out stats);
                    m_tmpDrawText.AppendFormat("   Structured: {0:N0}/{1:N0}\n", stats.TotalBuffers, stats.TotalBytes);
                    total.TotalBytes += stats.TotalBytes; total.TotalBuffers += stats.TotalBuffers;

                    m_tmpDrawText.AppendFormat("   Total: {0:N0}/{1:N0}\n", total.TotalBuffers, total.TotalBytes);


                    m_tmpDrawText.Append("Textures:\n");
                    MyTextureUsageReport report = MyTextures.GetReport();

                    m_tmpDrawText.AppendFormat("   Total: {0}\n", report.TexturesTotal);
                    m_tmpDrawText.AppendFormat("   Loaded: {0}\n", report.TexturesLoaded);
                    m_tmpDrawText.AppendFormat("   Memory: {0:N0}\n", report.TotalTextureMemory);

                    MySpritesRenderer.DrawText(pos, m_tmpDrawText, color, scale);
                }
                finally
                {
                    m_tmpDrawText.Clear();
                }
            }
        }
    }
}
