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

                    MySpritesRenderer.DrawText(pos, m_tmpDrawText, color, scale, MyRender11.DebugFont);
                }
                finally
                {
                    m_tmpDrawText.Clear();
                }
            }
        }
    }
}
