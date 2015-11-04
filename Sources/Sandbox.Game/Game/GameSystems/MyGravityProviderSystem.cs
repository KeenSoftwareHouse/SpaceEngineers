using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Collections;
using VRageMath;

namespace Sandbox.Game.GameSystems
{
    public static class MyGravityProviderSystem
    {
        public const float G = 9.81f;

        static List<IMyGravityProvider> m_gravityGenerators = new List<IMyGravityProvider>();

        static public List<Vector3> GravityVectors = new List<Vector3>();
		private static float m_lastLargestNaturalGravityMultiplier = 0.0f;

        public static Vector3 CalculateTotalGravityInPoint(Vector3D worldPoint, bool clearVectors = true)
        {
            Vector3 resultGravity = Vector3.Zero;

			if(clearVectors)
				GravityVectors.Clear();

			var naturalGravity = CalculateNaturalGravityInPoint(worldPoint, false);
			var artificialGravity = CalculateArtificialGravityInPoint(worldPoint, false, CalculateArtificialGravityStrengthMultiplier(m_lastLargestNaturalGravityMultiplier));
			resultGravity = naturalGravity + artificialGravity;

            return resultGravity;
        }

		public static Vector3 CalculateArtificialGravityInPoint(Vector3D worldPoint, bool clearVectors = true, float gravityMultiplier = 1.0f)
		{
			Vector3 resultGravity = Vector3.Zero;
			if(clearVectors)
				GravityVectors.Clear();

			foreach (IMyGravityProvider generator in m_gravityGenerators)
			{
				if (generator.IsWorking && generator.IsPositionInRange(worldPoint))
				{
					Vector3 worldGravity = generator.GetWorldGravity(worldPoint)*gravityMultiplier;
					resultGravity += worldGravity;
					GravityVectors.Add(worldGravity);
				}
			}

			return resultGravity;
		}

        public static Vector3 CalculateNaturalGravityInPoint(Vector3D worldPoint, bool clearVectors = true)
        {
            return Vector3.Zero;
        }

		public static float CalculateHighestNaturalGravityMultiplierInPoint(Vector3D worldPoint)
		{
			return 0;
		}

		public static float CalculateArtificialGravityStrengthMultiplier(float naturalGravityMultiplier)
		{
			return MathHelper.Clamp(1.0f - naturalGravityMultiplier * 2.0f, 0.0f, 1.0f);
		}

        public static void AddGravityGenerator(IMyGravityProvider gravityGenerator)
        {
            m_gravityGenerators.Add(gravityGenerator);
        }

        public static void RemoveGravityGenerator(IMyGravityProvider gravityGenerator)
        {
            m_gravityGenerators.Remove(gravityGenerator);
        }
    }
}
