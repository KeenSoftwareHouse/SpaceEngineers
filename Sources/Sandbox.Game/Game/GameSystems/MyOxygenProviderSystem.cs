using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.GameSystems
{
    public static class MyOxygenProviderSystem
    {
        static List<IMyOxygenProvider> m_oxygenGenerators = new List<IMyOxygenProvider>();

        public static float  GetOxygenInPoint(Vector3D worldPoint)
        {
            float resultOxygen = 0.0f;

            foreach (IMyOxygenProvider generator in m_oxygenGenerators)
            {
                if (generator.IsPositionInRange(worldPoint))
                {
                    resultOxygen += generator.GetOxygenForPosition(worldPoint);
                }
            }

            return MathHelper.Saturate(resultOxygen);
        }
     
        public static void AddOxygenGenerator(IMyOxygenProvider gravityGenerator)
        {
            m_oxygenGenerators.Add(gravityGenerator);
        }

        public static void RemoveOxygenGenerator(IMyOxygenProvider gravityGenerator)
        {
            m_oxygenGenerators.Remove(gravityGenerator);
        }
    }
}
