using System;

namespace VRage.Profiler
{
    public class MyDrawArea
    {
        public readonly float x_start;
        public readonly float y_start;
        public readonly float x_scale;
        public readonly float y_scale;

        public float y_range { get; private set; }
        public float y_legend_ms_increment { get; private set; }
        public int y_legend_ms_count { get; private set; }
        public float y_legend_increment { get; private set; }

        /// <summary>
        /// Index 0, 1, 2, 3, 4, 5...
        /// Makes range 1, 1.5, 2, 3, 4, 6, 8, 12, 24, 32, 48, 64...
        /// Negative index is supported as well.
        /// </summary>
        private int m_index;

        /// <summary>
        /// Initializes draw area.
        /// </summary>
        /// <param name="yRange">Range of y axis, will be rounded to 2^n or 2^n * 1.5</param>
        public MyDrawArea(float xStart, float yStart, float xScale, float yScale, float yRange)
        {
            m_index = (int)Math.Round(Math.Log(yRange, 2) * 2);

            x_start = xStart;
            y_start = yStart;
            x_scale = xScale;
            y_scale = yScale;
            UpdateRange();
        }

        public void IncreaseYRange()
        {
            m_index++;
            UpdateRange();
        }

        public void DecreaseYRange()
        {
            m_index--;
            UpdateRange();
        }

        void UpdateRange()
        {
            y_range = (float)Math.Pow(2, m_index / 2) * (1 + (m_index % 2) * 0.5f);
            y_legend_ms_count = m_index % 2 == 0 ? 8 : 12;
            y_legend_ms_increment = y_range / y_legend_ms_count;
            y_legend_increment = y_scale / y_range * y_legend_ms_increment;
        }
    }
}
