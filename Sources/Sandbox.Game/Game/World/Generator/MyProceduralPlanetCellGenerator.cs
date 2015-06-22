using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage;
using VRage.Library.Utils;
using VRage.Noise;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Game.World.Generator
{
    public class MyProceduralPlanetCellGenerator : MyProceduralWorldModule
    {
        private const int PLANET_SIZE_MIN = 30 * 1000;
        private const int PLANET_SIZE_MAX = 50 * 1000;

        private const int MOONS_MAX = 2;

        private const int MOON_SIZE_MIN = 8 * 1000;
        private const int MOON_SIZE_MAX = 10 * 1000;

        private const int MOON_DISTANCE_MIN = 4 * 1000;
        private const int MOON_DISTANCE_MAX = 32 * 1000;

        private const double MOON_DENSITY = 0.0; // -1..+1

        private const int BORDER_PADDING_SIZE = 32 * 1000;

        private const double GRAVITY_SIZE_MULTIPLIER = 2.0;

        private const double OBJECT_SEED_RADIUS = PLANET_SIZE_MAX / 2.0 * GRAVITY_SIZE_MULTIPLIER + 2 * (MOON_SIZE_MAX / 2.0 * GRAVITY_SIZE_MULTIPLIER + 2 * MOON_DISTANCE_MAX);

        public MyProceduralPlanetCellGenerator(int seed, double density, MyProceduralWorldModule parent = null)
            : base(512 * 1000, 100, seed, ((density + 1) / 5) - 1, parent)
        {
            Debug.Assert(OBJECT_SEED_RADIUS < CELL_SIZE / 2);
            AddDensityFunctionFilled(new MyInfiniteDensityFunction(MyRandom.Instance, 1e-3));
        }

        protected override MyProceduralCell GenerateProceduralCell(ref VRageMath.Vector3I cellId)
        {
            MyProceduralCell cell = new MyProceduralCell(cellId, this);
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
                        objectSeed.Type = MyObjectSeedType.Planet;
                        objectSeed.Seed = random.Next();
                        objectSeed.Index = 0;
                        objectSeed.UserData = new MySphereDensityFunction(position, OBJECT_SEED_RADIUS, OBJECT_SEED_RADIUS);

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

            switch (objectSeed.Type)
            {
                case MyObjectSeedType.Moon:
                    break;
                case MyObjectSeedType.Planet:
                    m_tmpClusterBoxes.Add(objectSeed.BoundingVolume);

                    for (int i = 0; i < MOONS_MAX; ++i)
                    {
                        var direction = GetRandomDirection(random);
                        var size = MathHelper.Lerp(MOON_SIZE_MIN, MOON_SIZE_MAX, random.NextDouble());
                        var distance = MathHelper.Lerp(MOON_DISTANCE_MIN, MOON_DISTANCE_MAX, random.NextDouble());
                        var position = objectSeed.BoundingVolume.Center + direction * (size + objectSeed.BoundingVolume.HalfExtents.Length() * 2 + distance);

                        ProfilerShort.Begin("GetValue");
                        var value = densityFunctionFilled.GetValue(position.X, position.Y, position.Z);
                        ProfilerShort.End();

                        if (value < MOON_DENSITY) // -1..+1
                        {
                            var clusterObjectSeed = new MyObjectSeed(cell, position, size);
                            clusterObjectSeed.Seed = random.Next();
                            clusterObjectSeed.Type = MyObjectSeedType.Moon;
                            clusterObjectSeed.Index = index++;
                            clusterObjectSeed.UserData = new MySphereDensityFunction(position, OBJECT_SEED_RADIUS, OBJECT_SEED_RADIUS);

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

        public override void GenerateObjects(List<MyObjectSeed> objectsList)
        {
            ProfilerShort.Begin("GenerateObjects");
            foreach (var objectSeed in objectsList)
            {
                if (objectSeed.Generated)
                    continue;

                objectSeed.Generated = true;

                using (MyRandom.Instance.PushSeed(GetObjectIdSeed(objectSeed)))
                {
                    ProfilerShort.Begin("Planet");

                    var bbox = objectSeed.BoundingVolume;
                    MyGamePruningStructure.GetAllVoxelMapsInBox(ref bbox, m_tmpVoxelMapsList);

                    String storageName = string.Format("Planet_{0}_{1}_{2}_{3}_{4}", objectSeed.CellId.X, objectSeed.CellId.Y, objectSeed.CellId.Z, objectSeed.Index, objectSeed.Seed);

                    bool exists = false;
                    foreach (var voxelMap in m_tmpVoxelMapsList)
                    {
                        if (voxelMap.StorageName == storageName)
                        {
                            exists = true;
                            break;
                        }
                    }
                    m_tmpVoxelMapsList.Clear();

                    if (!exists)
                    {
                        var planet = MyWorldGenerator.AddPlanet(storageName, objectSeed.BoundingVolume.Center - VRageMath.MathHelper.GetNearestBiggerPowerOfTwo(objectSeed.Size) / 2, objectSeed.Seed, objectSeed.Size, GetPlanetEntityId(objectSeed));

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
                        planet.Storage.RangeChanged += OnStorageRangeChanged;
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
            hash = (hash * 397) ^ (long)objectSeed.Index * BIG_PRIME1;

            return hash & 0x00FFFFFFFFFFFFFF | ((long)MyEntityIdentifier.ID_OBJECT_TYPE.PLANET << 56); // TODO:SK Planet type?
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

            String storageName = string.Format("Planet_{0}_{1}_{2}_{3}_{4}", objectSeed.CellId.X, objectSeed.CellId.Y, objectSeed.CellId.Z, objectSeed.Index, objectSeed.Seed);

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
