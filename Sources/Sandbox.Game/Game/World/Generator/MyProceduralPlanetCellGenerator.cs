using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Library.Utils;
using VRage.Noise;
using VRageMath;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Profiler;

namespace Sandbox.Game.World.Generator
{
    public class MyProceduralPlanetCellGenerator : MyProceduralWorldModule
    {
        public const int MOON_SIZE_MIN_LIMIT = 4 * 1000;
        public const int MOON_SIZE_MAX_LIMIT = 30 * 1000;

        public const int PLANET_SIZE_MIN_LIMIT = 8 * 1000;
        public const int PLANET_SIZE_MAX_LIMIT = 120 * 1000;

        internal readonly float PLANET_SIZE_MIN;
        internal readonly float PLANET_SIZE_MAX;

        internal const int MOONS_MAX = 3;

        internal readonly float MOON_SIZE_MIN;
        internal readonly float MOON_SIZE_MAX;

        internal const int MOON_DISTANCE_MIN = 4 * 1000;
        internal const int MOON_DISTANCE_MAX = 32 * 1000;

        internal const double MOON_DENSITY = 0.0; // -1..+1

        internal const int FALLOFF = 16 * 1000;

        internal const double GRAVITY_SIZE_MULTIPLIER = 1.1;

        internal readonly double OBJECT_SEED_RADIUS;

        public MyProceduralPlanetCellGenerator(int seed, double density,
            float planetSizeMax, float planetSizeMin,
            float moonSizeMax, float moonSizeMin, MyProceduralWorldModule parent = null)
            : base(2048 * 1000, 250, seed, ((density + 1) / 2) - 1, parent)
        {
            if (planetSizeMax < planetSizeMin)
            {
                var tmp = planetSizeMax;
                planetSizeMax = planetSizeMin;
                planetSizeMin = tmp;
            }

            PLANET_SIZE_MAX = MathHelper.Clamp(planetSizeMax, PLANET_SIZE_MIN_LIMIT, PLANET_SIZE_MAX_LIMIT);
            PLANET_SIZE_MIN = MathHelper.Clamp(planetSizeMin, PLANET_SIZE_MIN_LIMIT, planetSizeMax);

            if (moonSizeMax < moonSizeMin)
            {
                var tmp = moonSizeMax;
                moonSizeMax = moonSizeMin;
                moonSizeMin = tmp;
            }

            MOON_SIZE_MAX = MathHelper.Clamp(moonSizeMax, MOON_SIZE_MIN_LIMIT, MOON_SIZE_MAX_LIMIT);
            MOON_SIZE_MIN = MathHelper.Clamp(moonSizeMin, MOON_SIZE_MIN_LIMIT, moonSizeMax);

            OBJECT_SEED_RADIUS = PLANET_SIZE_MAX / 2.0 * GRAVITY_SIZE_MULTIPLIER + 2 * (MOON_SIZE_MAX / 2.0 * GRAVITY_SIZE_MULTIPLIER + 2 * MOON_DISTANCE_MAX);
            Debug.Assert(OBJECT_SEED_RADIUS < CELL_SIZE / 2);
            AddDensityFunctionFilled(new MyInfiniteDensityFunction(MyRandom.Instance, 1e-3));
        }

        protected override MyProceduralCell GenerateProceduralCell(ref VRageMath.Vector3I cellId)
        {
            MyProceduralCell cell = new MyProceduralCell(cellId, this.CELL_SIZE);
            ProfilerShort.Begin("GenerateProceduralCell");

            IMyModule densityFunctionFilled = GetCellDensityFunctionFilled(cell.BoundingVolume);
            if (densityFunctionFilled == null)
            {
                ProfilerShort.End();
                return null;
            }
            IMyModule densityFunctionRemoved = GetCellDensityFunctionRemoved(cell.BoundingVolume);

            int cellSeed = GetCellSeed(ref cellId);
            var random = MyRandom.Instance;
            using (random.PushSeed(cellSeed))
            {
                Vector3D position = new Vector3D(random.NextDouble(), random.NextDouble(), random.NextDouble());
                position *= (CELL_SIZE - 2 * OBJECT_SEED_RADIUS) / CELL_SIZE;
                position += OBJECT_SEED_RADIUS / CELL_SIZE;
                position += (Vector3D)cellId;
                position *= CELL_SIZE;

                if (MyEntities.IsInsideWorld(position))
                {
                    ProfilerShort.Begin("GetValue");
                    var value = densityFunctionFilled.GetValue(position.X, position.Y, position.Z);
                    ProfilerShort.End();

                    if (value < m_objectDensity) // -1..+1
                    {
                        var size = MathHelper.Lerp(PLANET_SIZE_MIN, PLANET_SIZE_MAX, random.NextDouble());
                        var objectSeed = new MyObjectSeed(cell, position, size);
                        objectSeed.Params.Type = MyObjectSeedType.Planet;
                        objectSeed.Params.Seed = random.Next();
                        objectSeed.Params.Index = 0;
                        objectSeed.UserData = new MySphereDensityFunction(position, PLANET_SIZE_MAX / 2.0 * GRAVITY_SIZE_MULTIPLIER + FALLOFF, FALLOFF);

                        int index = 1;
                        GenerateObject(cell, objectSeed, ref index, random, densityFunctionFilled, densityFunctionRemoved);
                    }
                }
            }

            ProfilerShort.End();
            return cell;
        }

        List<BoundingBoxD> m_tmpClusterBoxes = new List<BoundingBoxD>(MOONS_MAX + 1);

        private void GenerateObject(MyProceduralCell cell, MyObjectSeed objectSeed, ref int index, MyRandom random, IMyModule densityFunctionFilled, IMyModule densityFunctionRemoved)
        {
            cell.AddObject(objectSeed);

            IMyAsteroidFieldDensityFunction func = objectSeed.UserData as IMyAsteroidFieldDensityFunction;
            if (func != null)
            {
                ChildrenAddDensityFunctionRemoved(func);
            }

            switch (objectSeed.Params.Type)
            {
                case MyObjectSeedType.Moon:
                    break;
                case MyObjectSeedType.Planet:
                    m_tmpClusterBoxes.Add(objectSeed.BoundingVolume);

                    for (int i = 0; i < MOONS_MAX; ++i)
                    {
                        var direction = MyProceduralWorldGenerator.GetRandomDirection(random);
                        var size = MathHelper.Lerp(MOON_SIZE_MIN, MOON_SIZE_MAX, random.NextDouble());
                        var distance = MathHelper.Lerp(MOON_DISTANCE_MIN, MOON_DISTANCE_MAX, random.NextDouble());
                        var position = objectSeed.BoundingVolume.Center + direction * (size + objectSeed.BoundingVolume.HalfExtents.Length() * 2 + distance);

                        ProfilerShort.Begin("GetValue");
                        var value = densityFunctionFilled.GetValue(position.X, position.Y, position.Z);
                        ProfilerShort.End();

                        if (value < MOON_DENSITY) // -1..+1
                        {
                            var clusterObjectSeed = new MyObjectSeed(cell, position, size);
                            clusterObjectSeed.Params.Seed = random.Next();
                            clusterObjectSeed.Params.Type = MyObjectSeedType.Moon;
                            clusterObjectSeed.Params.Index = index++;
                            clusterObjectSeed.UserData = new MySphereDensityFunction(position, MOON_SIZE_MAX / 2.0 * GRAVITY_SIZE_MULTIPLIER + FALLOFF, FALLOFF);

                            bool overlaps = false;
                            foreach (var box in m_tmpClusterBoxes)
                            {
                                if (overlaps |= clusterObjectSeed.BoundingVolume.Intersects(box))
                                {
                                    break;
                                }
                            }

                            if (!overlaps)
                            {
                                m_tmpClusterBoxes.Add(clusterObjectSeed.BoundingVolume);
                                GenerateObject(cell, clusterObjectSeed, ref index, random, densityFunctionFilled, densityFunctionRemoved);
                            }
                        }
                    }
                    m_tmpClusterBoxes.Clear();
                    break;
                case MyObjectSeedType.Empty:
                    break;
                default:
                    throw new InvalidBranchException();
                    break;
            }
        }

        private List<MyVoxelBase> m_tmpVoxelMapsList = new List<MyVoxelBase>();

        public override void GenerateObjects(List<MyObjectSeed> objectsList, HashSet<MyObjectSeedParams> existingObjectsSeeds)
        {
            ProfilerShort.Begin("GenerateObjects");
            foreach (var objectSeed in objectsList)
            {
                if (objectSeed.Params.Generated)
                    continue;

                objectSeed.Params.Generated = true;

                using (MyRandom.Instance.PushSeed(GetObjectIdSeed(objectSeed)))
                {
                    ProfilerShort.Begin(objectSeed.Params.Type.ToString());

                    var bbox = objectSeed.BoundingVolume;
                    MyGamePruningStructure.GetAllVoxelMapsInBox(ref bbox, m_tmpVoxelMapsList);

                    String storageName = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", objectSeed.Params.Type, objectSeed.CellId.X, objectSeed.CellId.Y, objectSeed.CellId.Z, objectSeed.Params.Index, objectSeed.Params.Seed);

                    bool exists = false;
                    foreach (var voxelMap in m_tmpVoxelMapsList)
                    {
                        if (voxelMap.StorageName == storageName)
                        {
                            existingObjectsSeeds.Add(objectSeed.Params);
                            exists = true;
                            break;
                        }
                    }
                    m_tmpVoxelMapsList.Clear();

                    if (!exists)
                    {
                      /*  var planet = MyWorldGenerator.AddPlanet(storageName, objectSeed.BoundingVolume.Center - VRageMath.MathHelper.GetNearestBiggerPowerOfTwo(objectSeed.Size) / 2, objectSeed.Seed, objectSeed.Size, GetPlanetEntityId(objectSeed), objectSeed.Type == MyObjectSeedType.Moon);

                        if (planet == null)
                        {
                            continue;
                        }
                        planet.Save = false;
                        RangeChangedDelegate OnStorageRangeChanged = null;
                        OnStorageRangeChanged = delegate(Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData)
                        {
                            planet.Save = true;
                            planet.Storage.RangeChanged -= OnStorageRangeChanged;
                        };
                        planet.Storage.RangeChanged += OnStorageRangeChanged;*/
                    }
                    ProfilerShort.End();
                }
            }
            ProfilerShort.End();
        }

        private long GetPlanetEntityId(MyObjectSeed objectSeed)
        {
            var cellId = objectSeed.CellId;
            long hash = (long)(Math.Abs(cellId.X));
            hash = (hash * 397) ^ (long)(Math.Abs(cellId.Y));
            hash = (hash * 397) ^ (long)(Math.Abs(cellId.Z));
            hash = (hash * 397) ^ (long)(Math.Sign(cellId.X) + TWIN_PRIME_MIDDLE1);
            hash = (hash * 397) ^ (long)(Math.Sign(cellId.Y) + TWIN_PRIME_MIDDLE2);
            hash = (hash * 397) ^ (long)(Math.Sign(cellId.Z) + TWIN_PRIME_MIDDLE3);
            hash = (hash * 397) ^ (long)objectSeed.Params.Index * BIG_PRIME1;

            return hash & 0x00FFFFFFFFFFFFFF | ((long)MyEntityIdentifier.ID_OBJECT_TYPE.PLANET << 56);
        }

        private static Vector3I GetPlanetVoxelSize(double size)
        {
            return new Vector3I(Math.Max(64, (int)Math.Ceiling(size)));
        }

        protected override void CloseObjectSeed(MyObjectSeed objectSeed)
        {
            IMyAsteroidFieldDensityFunction func = objectSeed.UserData as IMyAsteroidFieldDensityFunction;
            if (func != null)
            {
                ChildrenRemoveDensityFunctionRemoved(func);
            }

            var bbox = objectSeed.BoundingVolume;
            MyGamePruningStructure.GetAllVoxelMapsInBox(ref bbox, m_tmpVoxelMapsList);

            String storageName = string.Format("{0}_{1}_{2}_{3}_{4}_{5}", objectSeed.Params.Type, objectSeed.CellId.X, objectSeed.CellId.Y, objectSeed.CellId.Z, objectSeed.Params.Index, objectSeed.Params.Seed);

            foreach (var voxelBase in m_tmpVoxelMapsList)
            {
                if (voxelBase.StorageName == storageName)
                {
                    if (!voxelBase.Save)
                    {
                        voxelBase.Close();
                    }
                    break;
                }
            }
            m_tmpVoxelMapsList.Clear();
        }
    }
}
