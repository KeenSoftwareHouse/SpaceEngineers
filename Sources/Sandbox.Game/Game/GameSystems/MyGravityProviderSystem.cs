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
        static List<IMyGravityProvider> m_planetGenerators = new List<IMyGravityProvider>();
		public static ListReader<IMyGravityProvider> NaturalGravityProviders { get { return m_planetGenerators; } }

        static public List<Vector3> GravityVectors = new List<Vector3>();
		private static float m_lastLargestNaturalGravityMultiplier = 0.0f;

        public static Vector3 CalculateTotalGravityInPoint(Vector3D worldPoint)
        {
            return CalculateTotalGravityInPoint(worldPoint, true);
        }

        public static Vector3 CalculateTotalGravityInPoint(Vector3D worldPoint, bool clearVectors)
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
            Vector3 resultGravity = Vector3.Zero;
			m_lastLargestNaturalGravityMultiplier = 0.0f;
			if(clearVectors)
				GravityVectors.Clear();

            foreach (IMyGravityProvider generator in m_planetGenerators)
            {
                if (generator.IsPositionInRangeGrid(worldPoint))
                {
                    Vector3 worldGravity = generator.GetWorldGravityGrid(worldPoint);
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

			foreach (IMyGravityProvider generator in m_planetGenerators)
			{
				if (generator.IsPositionInRangeGrid(worldPoint))
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

        public static MyPlanet GetNearestPlanet(Vector3D worldPosition)
        {
            MyPlanet nearestPlanet = null;
            double nearestPlanetDistanceSq = double.MaxValue;
            foreach (IMyGravityProvider gravityProvider in m_planetGenerators)
            {
                MyPlanet planet = gravityProvider as MyPlanet;
                if (planet == null)
                    continue;

                var planetDistanceSq = (planet.PositionComp.GetPosition() - worldPosition).LengthSquared();
                if (planetDistanceSq < nearestPlanetDistanceSq)
                {
                    nearestPlanet = planet;
                    nearestPlanetDistanceSq = planetDistanceSq;
                }
            }

            return nearestPlanet;
        }

        /// <summary>
        /// Returns the planet that has the most influential gravity well in the given world point.
        /// The most influential gravity well is defined as the planet that has the highest gravity in the point and
        /// if no such planet is found, it returns the planet, whose gravity well is the closest to the given point.
        /// </summary>
        /// <param name="worldPosition">Position to test for the strongest gravity well</param>
        /// <returns>Planet that has the most influential gravity well in the given world point</returns>
        public static MyPlanet GetStrongestGravityWell(Vector3D worldPosition)
        {
            MyPlanet nearestPlanet = null;
            double maxMetricValue = double.MinValue;
            foreach (IMyGravityProvider gravityProvider in m_planetGenerators)
            {
                MyPlanet planet = gravityProvider as MyPlanet;
                if (planet == null)
                    continue;

                double planetDistance = (planet.PositionComp.GetPosition() - worldPosition).Length();
                double metricValue = double.MinValue;
                if (planetDistance <= planet.GravityLimit)
                {
                    metricValue = (double)planet.GetGravityMultiplier(worldPosition);
                }
                else
                {
                    // Outside of the gravity well, just invert the distance, so that we can use only one value
                    // for comparison of planets which affect us and which don't
                    metricValue = planet.GravityLimit - planetDistance;
                }

                if (metricValue > maxMetricValue)
                {
                    nearestPlanet = planet;
                    maxMetricValue = metricValue;
                }
            }

            return nearestPlanet;
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
            sphereSize = MathHelper.Max(sphereSize, 0);

            for (int i = 0; i < m_planetGenerators.Count; i++)
            {
                IMyGravityProvider provider = m_planetGenerators[i];
                if (provider == null)
                    continue;

                MyPlanet planet = provider as MyPlanet;
                if (planet == null) 
                    continue;

                //we don't really care which planet's gravity we're in, so return as soon as we find one
                if (planet.IsPositionInGravityWell(position))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified trajectory intersects any natural gravity wells.
        /// </summary>
        /// <param name="start">Starting point of the trajectory.</param>
        /// <param name="end">Destination of the trajectory.</param>
        /// <param name="raySize">Size of the ray to test with. (Cilinder test)</param>
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

            for (int i = 0; i < m_planetGenerators.Count; i++)
            {
                IMyGravityProvider provider = m_planetGenerators[i];
                if (provider == null)
                    continue;

                MyPlanet planet = provider as MyPlanet;
                if (planet == null)
                    continue;

                //create a bounding sphere to represent the gravity sphere around the planet
                BoundingSphereD gravitySphere = new BoundingSphereD(planet.PositionComp.GetPosition(), planet.GravityLimit + raySize);

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

        public static void AddPlanet(IMyGravityProvider gravityGenerator)
        {
            m_planetGenerators.Add(gravityGenerator);
        }

        public static void RemovePlanet(IMyGravityProvider gravityGenerator)
        {
            m_planetGenerators.Remove(gravityGenerator);
        }
    }
}
