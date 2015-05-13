using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRage.Noise.Patterns
{
    public class MyConstant: IMyModule
    {
        public double Constant { get; set; }

        public MyConstant(double constant)
        {
            Constant = constant;
        }

        public double GetValue(double x)
        {
            return Constant;
        }

        public double GetValue(double x, double y)
        {
            return Constant;
        }

        public double GetValue(double x, double y, double z)
        {
            return Constant;
        }
    }
}
