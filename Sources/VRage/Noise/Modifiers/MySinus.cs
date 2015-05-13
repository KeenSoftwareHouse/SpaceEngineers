using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Noise.Modifiers
{
    public class MySinus: IMyModule
    {
        private IMyModule module;

        public MySinus(IMyModule module)
        {
            this.module = module;
        }

        public double GetValue(double x)
        {
            return Math.Sin(module.GetValue(x)*Math.PI);
        }

        public double GetValue(double x, double y)
        {
            return Math.Sin(module.GetValue(x, y)*Math.PI);
        }

        public double GetValue(double x, double y, double z)
        {
            return Math.Sin(module.GetValue(x, y, z)*Math.PI);
        }
    }
}
