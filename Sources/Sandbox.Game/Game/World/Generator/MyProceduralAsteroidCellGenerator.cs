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
    public class MyProceduralAsteroidCellGenerator : MyProceduralWorldModule
    {
        private const int OBJECT_SIZE_MIN = 64;
        private const int OBJECT_SIZE_MAX = 512;
        private const int SUBCELL_SIZE = 4 * 1024 + OBJECT_SIZE_MAX * 2;
        private const int SUBCELLS = 3;

        private const int OBJECT_MAX_IN_CLUSTER = 4;
        private const int OBJECT_MIN_DISTANCE_CLUSTER = 128;
        private const int OBJECT_MAX_DISTANCE_CLUSTER = 256;
        private const int OBJECT_SIZE_MIN_CLUSTER = 32;
        private const int OBJECT_SIZE_MAX_CLUSTER = 64;
        private const double OBJECT_DENSITY_CLUSTER = 0.50 * 2 - 1; // needs to be -1..1

        private double m_seedTypeProbabilitySum;
        private Dictionary<MyObjectSeedType, double> m_seedTypeProbability = new Dictionary<MyObjectSeedType, double>()
        {
            {MyObjectSeedType.Asteroid, 800},
            {MyObjectSeedType.AsteroidCluster, 300},
            {MyObjectSeedType.EncounterAlone, 1},
        };

        private double m_seedClusterTypeProbabilitySum;
        private Dictionary<MyObjectSeedType, double> m_seedClusterTypeProbability = new Dictionary<MyObjectSeedType, double>()
        {
            {MyObjectSeedType.Asteroid, 300},
            {MyObjectSeedType.EncounterSingle, 3},
            {MyObjectSeedType.EncounterMulti, 2},
        };

        public MyProceduralAsteroidCellGenerator(int seed, double density, MyProceduralWorldModule parent = null)
            : base(SUBCELLS * SUBCELL_SIZE, 1, seed, density, parent)
        {
            AddDensityFunctionFilled(new MyInfiniteDensityFunction(MyRandom.Instance, 0.003));

            m_seedTypeProbabilitySum = 0;
            foreach (var probability in m_seedTypeProbability.Values)
            {
                m_seedTypeProbabilitySum += probability;
            }
            Debug.Assert(m_seedTypeProbabilitySum != 0);

            m_seedClusterTypeProbabilitySum = 0;
            foreach (var probability in m_seedClusterTypeProbability.Values)
            {
                m_seedClusterTypeProbabilitySum += probability;
            }
            Debug.Assert(m_seedClusterTypeProbabilitySum != 0);
        }

        private long GetAsteroidEntityId(MyObjectSeed objectSeed)
        {
            var cellId = objectSeed.CellId;
            long hash = (long)(Math.Abs(cellId.X));
            hash = (hash * 397) ^ (long)(Math.Abs(cellId.Y));
            hash = (hash * 397) ^ (long)(Math.Abs(cellId.Z));
            hash = (hash * 397) ^ (long)(Math.Sign(cellId.X) + TWIN_PRIME_MIDDLE1);
            hash = (hash * 397) ^ (long)(Math.Sign(cellId.Y) + TWIN_PRIME_MIDDLE2);
            hash = (hash * 397) ^ (long)(Math.Sign(cellId.Z) + TWIN_PRIME_MIDDLE3);
            hash = (hash * 397) ^ (long)objectSeed.Index * BIG_PRIME1;

            return hash & 0x00FFFFFFFFFFFFFF | ((long)MyEntityIdentifier.ID_OBJECT_TYPE.ASTEROID << 56);
        }

        protected override MyProceduralCell GenerateProceduralCell(ref VRageMath.Vector3I cellId)
        {
            MyProceduralCell cell = new MyProceduralCell(cellId, this);
            ProfilerShort.Begin("GenerateObjectSeedsCell");

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
                int index = 0;
                Vector3I subCellId = Vector3I.Zero;
                Vector3I max = new Vector3I(SUBCELLS - 1);
                for (var iter = new Vector3I.RangeIterator(ref Vector3I.Zero, ref max); iter.IsValid(); iter.GetNext(out subCellId))
                {
                    // there is a bug in the position calculation which can very rarely cause overlaping objects but backwards compatibility so meh
                    Vector3D position = new Vector3D(random.NextDouble(), random.NextDouble(), random.NextDouble());
                    position += (Vector3D)subCellId / SUBCELL_SIZE;
                    position += cellId;
                    position *= CELL_SIZE;

                    if (!MyEntities.IsInsideWorld(position))
                    {
                        continue;
                    }

                    ProfilerShort.Begin("Density functions");
                    double valueRemoved = -1;
                    if (densityFunctionRemoved != null)
                    {
                        valueRemoved = densityFunctionRemoved.GetValue(position.X, position.Y, position.Z);

                        if (valueRemoved <= -1)
                        {
                            ProfilerShort.End();
                            continue;
                        }
                    }

                    var valueFilled = densityFunctionFilled.GetValue(position.X, position.Y, position.Z);

                    if (densityFunctionRemoved != null)
                    {
                        if (valueRemoved < valueFilled)
                        {
                            ProfilerShort.End();
                            continue;
                        }
                    }
                    ProfilerShort.End();

                    if (valueFilled < m_objectDensity) // -1..+1
                    {
                        var objectSeed = new MyObjectSeed(cell, position, GetObjectSize(random.NextDouble()));
                        objectSeed.Type = GetSeedType(random.NextDouble());
                        objectSeed.Seed = random.Next();
                        objectSeed.Index = index++;

                        GenerateObject(cell, objectSeed, ref index, random, densityFunctionFilled, densityFunctionRemoved);
                    }
                }
            }

            ProfilerShort.End();
            return cell;
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
                    switch (objectSeed.Type)
                    {
                        case MyObjectSeedType.Asteroid:
                            ProfilerShort.Begin("Asteroid");

                            var bbox = objectSeed.BoundingVolume;
                            MyGamePruningStructure.GetAllVoxelMapsInBox(ref bbox, m_tmpVoxelMapsList);

                            String storageName = string.Format("Asteroid_{0}_{1}_{2}_{3}_{4}", objectSeed.CellId.X, objectSeed.CellId.Y, objectSeed.CellId.Z, objectSeed.Index, objectSeed.Seed);

                            bool exists = false;
                            foreach (var voxelMap in m_tmpVoxelMapsList)
                            {
                                if (voxelMap.StorageName == storageName)
                                {
                                    exists = true;
                                    break;
                                }
                            }

                            if (!exists)
                            {
                                var provider = MyCompositeShapeProvider.CreateAsteroidShape(objectSeed.Seed, objectSeed.Size, MySession.Static.Settings.VoxelGeneratorVersion);
                                MyStorageBase storage = new MyOctreeStorage(provider, GetAsteroidVoxelSize(objectSeed.Size));
                                var voxelMap = MyWorldGenerator.AddVoxelMap(storageName, storage, objectSeed.BoundingVolume.Center - VRageMath.MathHelper.GetNearestBiggerPowerOfTwo(objectSeed.Size) / 2, GetAsteroidEntityId(objectSeed));

                                voxelMap.Save = false;
                                RangeChangedDelegate OnStorageRangeChanged = null;
                                OnStorageRangeChanged = delegate(Vector3I minVoxelChanged, Vector3I maxVoxelChanged, MyStorageDataTypeFlags changedData)
                                {
                                    voxelMap.Save = true;
                                    storage.RangeChanged -= OnStorageRangeChanged;
                                };
                                storage.RangeChanged += OnStorageRangeChanged;
                            }
                            m_tmpVoxelMapsList.Clear();
                            ProfilerShort.End();
                            break;
                        case MyObjectSeedType.EncounterAlone:
                        case MyObjectSeedType.EncounterSingle:
                        case MyObjectSeedType.EncounterMulti:
                            ProfilerShort.Begin("Encounter");
                            MyEncounterGenerator.PlaceEncounterToWorld(objectSeed.BoundingVolume, objectSeed.Seed, objectSeed.Type);
                            ProfilerShort.End();
                            break;
                        default:
                            throw new InvalidBranchException();
                            break;
                    }
                }
            }
            ProfilerShort.End();
        }

        List<BoundingBoxD> m_tmpClusterBoxes = new List<BoundingBoxD>(OBJECT_MAX_IN_CLUSTER + 1);

        private void GenerateObject(MyProceduralCell cell, MyObjectSeed objectSeed, ref int index, MyRandom random, IMyModule densityFunctionFilled, IMyModule densityFunctionRemoved)
        {
            cell.AddObject(objectSeed);
            switch (objectSeed.Type)
            {
                case MyObjectSeedType.Asteroid:
                    break;
                case MyObjectSeedType.AsteroidCluster:
                    objectSeed.Type = MyObjectSeedType.Asteroid;

                    m_tmpClusterBoxes.Add(objectSeed.BoundingVolume);

                    for (int i = 0; i < OBJECT_MAX_IN_CLUSTER; ++i)
                    {
                        var direction = GetRandomDirection(random);
                        var size = GetClusterObjectSize(random.NextDouble());
                        var distance = MathHelper.Lerp(OBJECT_MIN_DISTANCE_CLUSTER, OBJECT_MAX_DISTANCE_CLUSTER, random.NextDouble());
                        var clusterObjectPosition = objectSeed.BoundingVolume.Center + direction * (size + objectSeed.BoundingVolume.HalfExtents.Length() * 2 + distance);

                        ProfilerShort.Begin("Density functions");
                        double valueRemoved = -1;
                        if (densityFunctionRemoved != null)
                        {
                            valueRemoved = densityFunctionRemoved.GetValue(clusterObjectPosition.X, clusterObjectPosition.Y, clusterObjectPosition.Z);

                            if (valueRemoved <= -1)
                            {
                                ProfilerShort.End();
                                continue;
                            }
                        }

                        var valueFilled = densityFunctionFilled.GetValue(clusterObjectPosition.X, clusterObjectPosition.Y, clusterObjectPosition.Z);

                        if (densityFunctionRemoved != null)
                        {
                            if (valueRemoved < valueFilled)
                            {
                                ProfilerShort.End();
                                continue;
                            }
                        }
                        ProfilerShort.End();

                        if (valueFilled < OBJECT_DENSITY_CLUSTER) // -1..+1
                        {
                            var clusterObjectSeed = new MyObjectSeed(cell, clusterObjectPosition, size);
                            clusterObjectSeed.Seed = random.Next();
                            clusterObjectSeed.Index = index++;
                            clusterObjectSeed.Type = GetClusterSeedType(random.NextDouble());

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
                case MyObjectSeedType.EncounterAlone:
                case MyObjectSeedType.EncounterSingle:
                case MyObjectSeedType.EncounterMulti:
                    break;
                default:
                    throw new InvalidBranchException();
                    break;
            }
        }

        private MyObjectSeedType GetSeedType(double d)
        {
            d *= m_seedTypeProbabilitySum;
            foreach (var probability in m_seedTypeProbability)
            {
                if (probability.Value >= d)
                {
                    return probability.Key;
                }
                d -= probability.Value;
            }
            return MyObjectSeedType.Asteroid;
        }

        private MyObjectSeedType GetClusterSeedType(double d)
        {
            d *= m_seedClusterTypeProbabilitySum;
            foreach (var probability in m_seedClusterTypeProbability)
            {
                if (probability.Value >= d)
                {
                    return probability.Key;
                }
                d -= probability.Value;
            }
            return MyObjectSeedType.Asteroid;
        }

        private static double GetObjectSize(double noise)
        {
            return OBJECT_SIZE_MIN + noise * noise * (OBJECT_SIZE_MAX - OBJECT_SIZE_MIN); // x^2
        }

        private static double GetClusterObjectSize(double noise)
        {
            return OBJECT_SIZE_MIN_CLUSTER + noise * (OBJECT_SIZE_MAX_CLUSTER - OBJECT_SIZE_MIN_CLUSTER); // x
        }

        private static Vector3I GetAsteroidVoxelSize(double asteroidRadius)
        {
            int radius = Math.Max(64, (int)Math.Ceiling(asteroidRadius));
            return new Vector3I(radius);
        }

        protected override void CloseObjectSeed(MyObjectSeed objectSeed)
        {
            switch (objectSeed.Type)
            {
                case MyObjectSeedType.Asteroid:
                case MyObjectSeedType.AsteroidCluster:
                    var bbox = objectSeed.BoundingVolume;
                    MyGamePruningStructure.GetAllVoxelMapsInBox(ref bbox, m_tmpVoxelMapsList);

                    String storageName = string.Format("Asteroid_{0}_{1}_{2}_{3}_{4}", objectSeed.CellId.X, objectSeed.CellId.Y, objectSeed.CellId.Z, objectSeed.Index, objectSeed.Seed);

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
                    break;
                case MyObjectSeedType.EncounterAlone:
                case MyObjectSeedType.EncounterSingle:
                case MyObjectSeedType.EncounterMulti:
                    MyEncounterGenerator.RemoveEncounter(objectSeed.BoundingVolume, objectSeed.Seed);
                    break;
                default:
                    throw new InvalidBranchException();
                    break;
            }
        }
    }
}
