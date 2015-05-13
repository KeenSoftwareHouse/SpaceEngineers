using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage;
using VRage.Library.Utils;
using VRage.Noise;
using VRage.Noise.Modifiers;

namespace Sandbox.Game.World.Generator
{
    class MyAsteroidInfiniteDensityFunction : IMyAsteroidFieldDensityFunction
    {
        private IMyModule noise;

        public MyAsteroidInfiniteDensityFunction(MyRandom random, double frequency)
        {
            noise = new MySimplexFast(random.Next(), frequency);
        }

        public bool ExistsInCell(ref VRageMath.Vector3I cellId)
        {
            return true;
        }

        public double GetValue(ref VRageMath.Vector3D position)
        {
            return noise.GetValue(position.X, position.Y, position.Z);
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
            return noise.GetValue(x, y, z);
        }
    }
}
