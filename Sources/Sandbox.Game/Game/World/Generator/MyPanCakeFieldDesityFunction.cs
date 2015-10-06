using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Noise;
using VRageMath;

namespace Sandbox.Game.World.Generator
{
    class MyPanCakeFieldDesityFunction : IMyAsteroidFieldDensityFunction
    {
        private IMyModule noise;

        public MyPanCakeFieldDesityFunction(MyRandom random, double frequency)
        {
            noise = new MySimplexFast(random.Next(), frequency);
        }

        public bool ExistsInCell(ref BoundingBoxD bbox)
        {
            return true;
        }

        public double GetValue(double x)
        {
            return noise.GetValue(x);
        }

        public double GetValue(double x, double y)
        {
            return noise.GetValue(x, y);
        }

        public double GetValue(double x, double y, double z)
        {
            double zInKm = z / 1000.0;
            return noise.GetValue(x, y, z) * Math.Exp(-zInKm * zInKm);
        }
    }
}
