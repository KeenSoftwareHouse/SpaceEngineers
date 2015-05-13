using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    class MyGridColorHelper
    {
        private Dictionary<MyCubeGrid, Color> m_colors = new Dictionary<MyCubeGrid, Color>();
        private int m_lastColorIndex = 0;

        public void Init(MyCubeGrid mainGrid = null)
        {
            m_lastColorIndex = 0;
            m_colors.Clear();
            if (mainGrid != null)
            {
                m_colors.Add(mainGrid, Color.White);
            }
        }

        public Color GetGridColor(MyCubeGrid grid)
        {
            Color result;
            if (!m_colors.TryGetValue(grid, out result))
            {
                do
                {
                    result = new Vector3((m_lastColorIndex++ % 20) / 20.0f, 0.75f, 1.0f).HSVtoColor();
                }
                while (result.HueDistance(Color.Red) < 0.04f || result.HueDistance(0.65f) < 0.07f);
                m_colors[grid] = result;
            }
            return result;
        }
    }
}
