using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace Sandbox.Game.GameSystems
{
    public static class MyGravityProviderSystem
    {
        public const float G = 9.81f;
        static List<IMyGravityProvider> m_gravityGenerators = new List<IMyGravityProvider>();
        static List<IMyGravityProvider> m_planetGenerators = new List<IMyGravityProvider>();
        static public List<Vector3> GravityVectors = new List<Vector3>();

        public static Vector3 CalculateGravityInPoint(Vector3D worldPoint)
        {
            Vector3 resultGravity = Vector3.Zero;
            GravityVectors.Clear();

            foreach (IMyGravityProvider generator in m_gravityGenerators)
            {
                if (generator.IsWorking && generator.IsPositionInRange(worldPoint))
                {
                    Vector3 worldGravity = generator.GetWorldGravity(worldPoint);
                    resultGravity += worldGravity;
                    GravityVectors.Add(worldGravity);
                }
            }

            return resultGravity;
        }
        public static Vector3 CalculateGravityInPointForGrid(Vector3D worldPoint)
        {
            Vector3 resultGravity = Vector3.Zero;
            GravityVectors.Clear();

            foreach (IMyGravityProvider generator in m_planetGenerators)
            {
                if (generator.IsPositionInRangeGrid(worldPoint))
                {
                    Vector3 worldGravity = generator.GetWorldGravityGrid(worldPoint);
                    resultGravity += worldGravity;
                    GravityVectors.Add(worldGravity);
                }
            }

            return resultGravity;
        }

        public static void AddGravityGenerator(IMyGravityProvider gravityGenerator)
        {
            m_gravityGenerators.Add(gravityGenerator);
        }

        public static void RemoveGravityGenerator(IMyGravityProvider gravityGenerator)
        {
            m_gravityGenerators.Remove(gravityGenerator);
        }

        public static void AddPlanet(IMyGravityProvider gravityGenerator)
        {
            m_planetGenerators.Add(gravityGenerator);
            m_gravityGenerators.Add(gravityGenerator);
        }

        public static void RemovePlanet(IMyGravityProvider gravityGenerator)
        {
            m_planetGenerators.Remove(gravityGenerator);
            m_gravityGenerators.Remove(gravityGenerator);
        }


    }
}
