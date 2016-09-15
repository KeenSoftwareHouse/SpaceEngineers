using System.Collections.Generic;
using VRage.Stats;

namespace VRageRender.Utils
{
    /// <summary>
    /// Draws statistics
    /// </summary>
    public static class MyRenderStats
    {
        public enum ColumnEnum
        {
            Left,
            Right
        }

        // HACK: moving stats to render, there should be interface instead
        public static Dictionary<ColumnEnum, List<MyStats>> m_stats;

        //static float m_rightGapSizeRatio = 0.1f;
        //static float m_rightColumnWidth;
        //static MyTimeSpan m_rightColumnChangeTime = MyRenderProxy.CurrentDrawTime;

        //static StringBuilder m_tmpDrawText = new StringBuilder(4096);

        public static readonly MyStats Generic = new MyStats();

        static MyRenderStats()
        {
            Generic = new MyStats();
            m_stats = new Dictionary<ColumnEnum, List<MyStats>>(EqualityComparer<ColumnEnum>.Default)
            {
                { ColumnEnum.Left, new List<MyStats>() { Generic } },
                { ColumnEnum.Right, new List<MyStats>() { } },
            };
        }

        public static void SetColumn(ColumnEnum column, params MyStats[] stats)
        {
            List<MyStats> statList;
            if (!m_stats.TryGetValue(column, out statList))
            {
                statList = new List<MyStats>();
                m_stats[column] = statList;
            }

            statList.Clear();
            statList.AddArray(stats);
        }

        //public static void Draw(float scale, Color color)
        //{
        //    // TODO: implement!
        //    /*
        //    foreach (var pair in m_stats)
        //    {
        //        try
        //        {
        //            foreach (var s in pair.Value)
        //            {
        //                s.WriteTo(m_tmpDrawText);
        //                m_tmpDrawText.AppendLine(); // Newline between each group
        //            }

        //            Vector2 pos = new Vector2(0, 0);

        //            if (pair.Key == ColumnEnum.Right)
        //            {
        //                Vector2 size = MyRender.MeasureText(m_tmpDrawText, scale);
        //                if (m_rightColumnWidth < size.X)
        //                {
        //                    m_rightColumnWidth = size.X * m_rightGapSizeRatio; // Add some gap
        //                    m_rightColumnChangeTime = MyRenderProxy.CurrentDrawTime;
        //                }
        //                else if (m_rightColumnWidth > size.X * m_rightGapSizeRatio && (MyRenderProxy.CurrentDrawTime - m_rightColumnChangeTime).Seconds > 3)
        //                {
        //                    m_rightColumnWidth = size.X * m_rightGapSizeRatio;
        //                    m_rightColumnChangeTime = MyRenderProxy.CurrentDrawTime;
        //                }
        //                pos = new Vector2(MyRenderProxy.MainViewport.Width - m_rightColumnWidth, 0);
        //            }

        //            MyRender.DrawText(pos, m_tmpDrawText, color, scale);
        //        }
        //        finally
        //        {
        //            m_tmpDrawText.Clear();
        //        }
        //    }
        //    */
        //}

        //static Vector2 GetPosition(ColumnEnum col)
        //{
        //    switch (col)
        //    {
        //        case ColumnEnum.Left:
        //            return new Vector2(0, 0);

        //        case ColumnEnum.Right:
        //            return new Vector2(MyRenderProxy.MainViewport.Width - m_rightColumnWidth, 0);

        //        default:
        //            throw new InvalidBranchException();
        //    }
        //}
    }
}
