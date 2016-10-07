using Sandbox.Game.Entities;
using System.Collections.Generic;
using VRage.Collections;
using VRage.Game.Components;
using VRageMath;

namespace Sandbox.Game.GameSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, 666)]
    public class MyGravityProviderSystem: MySessionComponentBase
    {
        public const float G = 9.81f;

        static List<IMyGravityProvider> m_gravityGenerators = new List<IMyGravityProvider>();
        static List<IMyGravityProvider> m_naturalGravityGenerators = new List<IMyGravityProvider>();
		public static ListReader<IMyGravityProvider> NaturalGravityProviders { get { return m_naturalGravityGenerators; } }

        [System.ThreadStatic]
        static public List<Vector3> GravityVectors;// = new List<Vector3>();
		private static float m_lastLargestNaturalGravityMultiplier = 0.0f;


        public static Vector3 CalculateTotalGravityInPoint(Vector3D worldPoint)
        {
            return CalculateTotalGravityInPoint(worldPoint, true);
        }

        public static Vector3 CalculateTotalGravityInPoint(Vector3D worldPoint, bool clearVectors)
        {
            if (GravityVectors == null)
                GravityVectors = new List<Vector3>();

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
            if(GravityVectors == null)
                GravityVectors = new List<Vector3>();

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
            if (GravityVectors == null)
                GravityVectors = new List<Vector3>();

            Vector3 resultGravity = Vector3.Zero;
			m_lastLargestNaturalGravityMultiplier = 0.0f;
			if(clearVectors)
				GravityVectors.Clear();

            foreach (IMyGravityProvider generator in m_naturalGravityGenerators)
            {
                if (generator.IsPositionInRange(worldPoint))
                {
                    Vector3 worldGravity = generator.GetWorldGravity(worldPoint);
					var gravityMultiplier = generator.GetGravityMultiplier(worldPoint);

					if (gravityMultiplier > m_lastLargestNaturalGravityMultiplier)
						m_lastLargestNaturalGravityMultiplier = gravityMultiplier;

                    resultGravity += worldGravity;
                    GravityVectors.Add(worldGravity);
                }
            }

            return resultGravity;
        }

		public static float CalculateHighestNaturalGravityMultiplierInPoint(Vector3D worldPoint)
		{
			var largestNaturalGravityMultiplier = 0.0f;

			foreach (IMyGravityProvider generator in m_naturalGravityGenerators)
			{
				if (generator.IsPositionInRange(worldPoint))
				{
					var gravityMultiplier = generator.GetGravityMultiplier(worldPoint);

					if (gravityMultiplier > largestNaturalGravityMultiplier)
						largestNaturalGravityMultiplier = gravityMultiplier;
				}
			}

			return largestNaturalGravityMultiplier;
		}

		public static float CalculateArtificialGravityStrengthMultiplier(float naturalGravityMultiplier)
		{
			return MathHelper.Clamp(1.0f - naturalGravityMultiplier * 2.0f, 0.0f, 1.0f);
		}

        /// <summary>
        /// Returns the planet that has the most influential gravity well in the given world point.
        /// The most influential gravity well is defined as the planet that has the highest gravity in the point and
        /// if no such planet is found, it returns the planet, whose gravity well is the closest to the given point.
        /// </summary>
        /// <param name="worldPosition">Position to test for the strongest gravity well</param>
        /// <returns>Planet that has the most influential gravity well in the given world point</returns>
        public static double GetStrongestNaturalGravityWell(Vector3D worldPosition, out IMyGravityProvider nearestProvider)
        {
            double maxMetricValue = double.MinValue;
            nearestProvider = null;

            foreach (IMyGravityProvider gravityProvider in m_naturalGravityGenerators)
            {
                var grav = gravityProvider.GetWorldGravity(worldPosition).Length();

                if (grav > maxMetricValue)
                {
                    maxMetricValue = grav;
                    nearestProvider = gravityProvider;
                }
            }

            return maxMetricValue;
        }

        /// <summary>
        /// This quickly checks if a given position is in any natural gravity.
        /// </summary>
        /// <param name="position">Position to check</param>
        /// <param name="sphereSize">Sphere size to test with.</param>
        /// <returns>True if there is natural gravity at this position, false otherwise.</returns>
        public static bool IsPositionInNaturalGravity(Vector3D position, double sphereSize = 0)
        {
            // Clamp sphere size to be at least 0.
            // TODO (DI/TT): Finish dis and add grav gens to a tree or smthing
            sphereSize = MathHelper.Max(sphereSize, 0);

            for (int i = 0; i < m_naturalGravityGenerators.Count; i++)
            {
                IMyGravityProvider provider = m_naturalGravityGenerators[i];
                if (provider == null)
                    continue;

                //we don't really care which planet's gravity we're in, so return as soon as we find one
                if (provider.IsPositionInRange(position))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified trajectory intersects any natural gravity wells.
        /// </summary>
        /// <param name="start">Starting point of the trajectory.</param>
        /// <param name="end">Destination of the trajectory.</param>
        /// <param name="raySize">Size of the ray to test with. (Cylinder test)</param>
        /// DI: Do you mean capsule?
        /// <returns></returns>
        public static bool DoesTrajectoryIntersectNaturalGravity(Vector3D start, Vector3D end, double raySize = 0)
        {
            // If the start and end point are identical, do a sphere test instead.
            Vector3D direction = start - end;
            if (Vector3D.IsZero(direction))
                return IsPositionInNaturalGravity(start, raySize);

            Ray trajectory = new Ray(start, Vector3.Normalize(direction));

            // Clamp ray size to be at least 0.
            raySize = MathHelper.Max(raySize, 0);

            for (int i = 0; i < m_naturalGravityGenerators.Count; i++)
            {
                IMyGravityProvider provider = m_naturalGravityGenerators[i];
                if (provider == null)
                    continue;

                // This should be done some other way, but works for nau
                MySphericalNaturalGravityComponent spherical = provider as MySphericalNaturalGravityComponent;
                if (spherical == null)
                    continue;

                //create a bounding sphere to represent the gravity sphere around the planet
                BoundingSphereD gravitySphere = new BoundingSphereD(spherical.Position, spherical.GravityLimit + raySize);

                //check for intersections
                float? intersect = trajectory.Intersects(gravitySphere);
                if (intersect.HasValue)
                    return true;
            }
            return false;
        }

        public static void AddGravityGenerator(IMyGravityProvider gravityGenerator)
        {
            m_gravityGenerators.Add(gravityGenerator);
        }

        public static void RemoveGravityGenerator(IMyGravityProvider gravityGenerator)
        {
            m_gravityGenerators.Remove(gravityGenerator);
        }

        public static void AddNaturalGravityProvider(IMyGravityProvider gravityGenerator)
        {
            m_naturalGravityGenerators.Add(gravityGenerator);
        }

        public static void RemoveNaturalGravityProvider(IMyGravityProvider gravityGenerator)
        {
            m_naturalGravityGenerators.Remove(gravityGenerator);
        }
    }
}
