using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Noise.Modifiers
{
    public class MyBendFilter: IMyModule
    {
        public IMyModule Module {get; set;}

        private double m_rangeSizeInverted;

        private double m_clampingMin;
        private double m_clampingMax;

        public double OutOfRangeMin { get; set; }
        public double OutOfRangeMax { get; set; }

        public double ClampingMin { get { return m_clampingMin; } set { m_clampingMin = value; m_rangeSizeInverted = 1.0/(m_clampingMax - m_clampingMin); } }
        public double ClampingMax { get { return m_clampingMax; } set { m_clampingMax = value; m_rangeSizeInverted = 1.0/(m_clampingMax - m_clampingMin); } }
        
        public MyBendFilter(IMyModule module, double clampRangeMin, double clampRangeMax, double outOfRangeMin, double outOfRangeMax)
        {
            this.Module = module;
            this.m_rangeSizeInverted = 1.0/(clampRangeMax - clampRangeMin);
            this.m_clampingMin = clampRangeMin;
            this.m_clampingMax = clampRangeMax;
            this.OutOfRangeMin = outOfRangeMin;
            this.OutOfRangeMax = outOfRangeMax;
        }

        public double GetValue(double x)
        {
            double value = Module.GetValue(x);

            return expandRange(value);
        }

        public double GetValue(double x, double y)
        {
            double value = Module.GetValue(x, y);

            return expandRange(value);
        }

        public double GetValue(double x, double y, double z)
        {
            double value = Module.GetValue(x, y, z);

            return expandRange(value);
        }

        private double expandRange(double value)
        {
            if (value < m_clampingMin)
                return OutOfRangeMin;
            else if (value > m_clampingMax)
                return OutOfRangeMax;
            else
                return (value - m_clampingMin) * m_rangeSizeInverted;
        }
    }
}
