using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sandbox.Game.WorldEnvironment;
using VRage.Utils;
using VRageMath;
using VRage.Voxels;

namespace Sandbox.Engine.Voxels
{
    [MyStorageDataProvider(10042)]
    public class MyPlanetStorageProvider : IMyStorageDataProvider
    {
        private static readonly int STORAGE_VERSION = 1;

        #region Saving/Loading & Data

        unsafe struct PlanetData
        {
            public long Version;
            public long Seed;
            public double Radius;
        }

        private PlanetData m_data;

        public MyPlanetGeneratorDefinition Generator
        {
            get;
            private set;
        }

        public int SerializedSize
        {
            get
            {
                unsafe
                {
                    int size = Encoding.UTF8.GetByteCount(Generator.Id.SubtypeName); // string length
                    size += (MathHelper.Log2Floor(size) + 6) / 7; // 7-bit encoded string size.

                    return sizeof(PlanetData) + size;
                }
            }
        }

        public void WriteTo(System.IO.Stream stream)
        {
            stream.WriteNoAlloc(m_data.Version);
            stream.WriteNoAlloc(m_data.Seed);
            stream.WriteNoAlloc(m_data.Radius);
            stream.WriteNoAlloc(Generator.Id.SubtypeName);
        }

        public void ReadFrom(ref MyOctreeStorage.ChunkHeader header, System.IO.Stream stream, ref bool isOldFormat)
        {
            m_data.Version = stream.ReadInt64();
            m_data.Seed = stream.ReadInt64();
            m_data.Radius = stream.ReadDouble();
            string generator = stream.ReadString();

            if (m_data.Version != STORAGE_VERSION) {
                isOldFormat = true;
            }

            var def = MyDefinitionManager.Static.GetDefinition<MyPlanetGeneratorDefinition>(MyStringHash.GetOrCompute(generator));

            if (def == null) throw new Exception(String.Format("Cannot load planet generator definition for subtype '{0}'.", generator));

            Generator = def;

            Init();
        }

        #endregion

        #region Fields & Initialization

        public bool Closed { get; private set; }

        public MyPlanetShapeProvider Shape
        {
            get;
            private set;
        }

        public MyPlanetMaterialProvider Material
        {
            get;
            private set;
        }

        public Vector3I StorageSize
        {
            get;
            private set;
        }

        public float Radius { get { return (float)m_data.Radius; } }

        public void Init(long seed, MyPlanetGeneratorDefinition generator, double radius)
        {
            Debug.Assert(radius > 0, "The planet radius must be a strictly positive number!");

            radius = Math.Max(radius, 1.0);

            Generator = generator;

            m_data = new PlanetData()
            {
                Radius = radius,
                Seed = seed,
                Version = STORAGE_VERSION
            };

            Init();

            Closed = false;
        }

        public void Init(long seed, string generator, double radius)
        {
            Debug.Assert(radius > 0, "The planet radius must be a strictly positive number!");

            radius = Math.Max(radius, 1.0);

            var def = MyDefinitionManager.Static.GetDefinition<MyPlanetGeneratorDefinition>(MyStringHash.GetOrCompute(generator));

            if (def == null) throw new Exception(String.Format("Cannot load planet generator definition for subtype '{0}'.", generator));

            Generator = def;

            m_data = new PlanetData()
            {
                Radius = radius,
                Seed = seed,
                Version = STORAGE_VERSION
            };

            Init();
        }

        protected void Init()
        {
            float rad = (float)m_data.Radius;

            float maxHeight = rad * Generator.HillParams.Max;

            float halfSize = rad + maxHeight;
            StorageSize = MyVoxelCoordSystems.FindBestOctreeSize(2 * halfSize);
            float halfStorageSize = StorageSize.X * 0.5f;

            Shape = new MyPlanetShapeProvider(new Vector3(halfStorageSize), rad, Generator);
            Material = new MyPlanetMaterialProvider(Generator, Shape);
        }

        public void Close()
        {
            Material.Close();
            Shape.Close();
            Closed = true;
        }

        public bool ProvidesAmbient
        {
            get { return true; }
        }

        #endregion

        # region Storage access

        public void ReadRange(MyStorageData target, MyStorageDataTypeFlags dataType, ref Vector3I writeOffset, int lodIndex, ref Vector3I minInLod, ref Vector3I maxInLod)
        {
            if (Closed)
            {
                Debug.Fail("Storage closed!");
                return;
            }
            MyVoxelDataRequest request = new MyVoxelDataRequest()
            {
                Target = target,
                Offset = writeOffset,
                RequestedData = dataType,
                Lod = lodIndex,
                minInLod = minInLod,
                maxInLod = maxInLod
            };
            ReadRange(ref request);
        }

        public float GetDistanceToPoint(ref Vector3D localPos)
        {
            if (Closed)
            {
                Debug.Fail("Storage closed!");
                return float.PositiveInfinity;
            }

            Vector3 pos = localPos - Shape.Center();
            return Shape.SignedDistanceLocalCacheless(pos);
        }

        public MyVoxelMaterialDefinition GetMaterialAtPosition(ref Vector3D localPosition)
        {
            if (Closed)
            {
                Debug.Fail("Storage closed!");
                return null;
            }
            Vector3 pos = localPosition;
            return Material.GetMaterialForPosition(ref pos, 1);
        }
        public void ReadRange(ref MyVoxelDataRequest req)
        {
            if (Closed)
            {
                Debug.Fail("Storage closed!");
                return;
            }

            if (req.RequestedData.Requests(MyStorageDataTypeEnum.Content))
            {
                Shape.ReadContentRange(ref req);
                req.RequestFlags |= MyVoxelRequestFlags.ConsiderContent;
            }

            if (req.Flags.HasFlags(MyVoxelRequestFlags.EmptyContent))
            {
                if (req.RequestedData.Requests(MyStorageDataTypeEnum.Material))
                    req.Target.BlockFill(MyStorageDataTypeEnum.Material, req.minInLod, req.maxInLod, MyVoxelConstants.NULL_MATERIAL);
                if (req.RequestedData.Requests(MyStorageDataTypeEnum.Occlusion))
                    req.Target.BlockFill(MyStorageDataTypeEnum.Occlusion, req.minInLod, req.maxInLod, 0);
            }
            else
            {
                if (req.RequestedData.Requests(MyStorageDataTypeEnum.Material))
                {
                    Material.ReadMaterialRange(ref req);
                }
                // If only occlusion is requested
                else if (req.RequestedData.Requests(MyStorageDataTypeEnum.Occlusion))
                {
                    Material.ReadOcclusion(ref req);
                }
            }
        }

        public ContainmentType Intersect(BoundingBoxI box, bool lazy)
        {
            if (Closed)
            {
                Debug.Fail("Storage closed!");
                return ContainmentType.Disjoint;
            }
            BoundingBox bbox = new BoundingBox(box);
            bbox.Translate(-Shape.Center()); // Bring to center local
            return Shape.IntersectBoundingBox(ref bbox, 1.0f);
        }

        public bool Intersect(ref LineD line, out double startOffset, out double endOffset)
        {
            var ll = line;
            var center = Shape.Center();
            ll.To -= center;
            ll.From -= center;

            if (Shape.IntersectLine(ref ll, out startOffset, out endOffset))
            {
                ll.From += center;
                ll.To += center;
                line = ll;
                return true;
            }

            return false;
        }

        public MyVoxelRequestFlags SupportedFlags()
        {
            return MyVoxelRequestFlags.FullContent | MyVoxelRequestFlags.EmptyContent | MyVoxelRequestFlags.OneMaterial | MyVoxelRequestFlags.SurfaceMaterial;
        }

        #endregion

        public void DebugDraw(ref MatrixD worldMatrix)
        {
        }

        public void ReindexMaterials(Dictionary<byte, byte> oldToNewIndexMap)
        {
            // not relevant to us because we do not store material by ID.
        }

        /**
         * Combined surface properties for the planet.
         * 
         * This object stores very detailed information about a point in the planet's surface.
         * 
         */
        public struct SurfacePropertiesExtended
        {
            public Vector3 Position;
            public Vector3 Gravity;
            public MyVoxelMaterialDefinition Material;
            public float Slope;
            public float HeightRatio;
            public float Depth;
            public float Latitude;
            public float Longitude;
            public float Altitude;

            public int Face;
            public Vector2 Texcoord;

            public byte BiomeValue;
            public byte MaterialValue;
            public byte OcclusionValue;
            public byte OreValue;

            public MyPlanetMaterialProvider.PlanetMaterial EffectiveRule;
            public MyPlanetMaterialProvider.PlanetBiome Biome;
            public MyPlanetMaterialProvider.PlanetOre Ore;

            public enum MaterialOrigin
            {
                Rule,
                Ore,
                Map,
                Default
            }

            public MaterialOrigin Origin;
        }

        /**
         * All mighty method for computing material, surface position and surface parameters in a single pass.
         * 
         * If material and surface position are required this is the best way to obtain that information.
         * 
         * When using the coefficient cache be sure to have it properly set up. (MyPlanetShapeProvider.PrepareCache())
         */
        public void ComputeCombinedMaterialAndSurface(Vector3 position, bool useCache, out MySurfaceParams props)
        {
            if (Closed)
            {
                Debug.Fail("Storage closed!");
                props = new MySurfaceParams();
                return;
            }

            byte occl = 0;

            MyPlanetMaterialProvider.MaterialSampleParams pars;

            position -= Shape.Center();

            float distance = position.Length();

            pars.Gravity = position / distance;

            // Latitude
            props.Latitude = pars.Gravity.Y;

            Vector2 lon = new Vector2(-pars.Gravity.X, -pars.Gravity.Z);
            lon.Normalize();

            props.Longitude = lon.Y;
            if (-pars.Gravity.X > 0)
            {
                props.Longitude = 2 - props.Longitude;
            }

            // Height and slope
            int face;
            Vector2 pos;
            MyCubemapHelpers.CalculateSampleTexcoord(ref position, out face, out pos);

            float value;
            if(!useCache)
                value = Shape.GetValueForPositionCacheless(face, ref pos, out props.Normal);
            else
                value = Shape.GetValueForPositionWithCache(face, ref pos, out props.Normal);

            pars.SampledHeight = value;
            pars.SurfaceDepth = 0;
            pars.Texcoord = pos;
            pars.LodSize = 1.0f;
            pars.Latitude = props.Latitude;
            pars.Longitude = props.Longitude;
            pars.Face = face;
            pars.Normal = props.Normal;

            props.Position = pars.Gravity * (Radius + value) + Shape.Center();

            props.Gravity = pars.Gravity = -pars.Gravity;

            pars.DistanceToCenter = props.Position.Length();

            var rule = Material.GetLayeredMaterialForPosition(ref pars, out props.Biome, ref occl);

            if (rule.FirstOrDefault == null) props.Material = 0;
            else props.Material = rule.FirstOrDefault.Index;

            //props.Altitude = value;

            props.Normal = pars.Normal;

            props.HeightRatio = Shape.AltitudeToRatio(value);
        }

        /**
         * All mighty method for computing material, surface position and surface parameters in a single pass.
         * 
         * If material and surface position are required this is the best way to obtain that information.
         * 
         * When using the coefficient cache be sure to have it properly set up. (MyPlanetShapeProvider.PrepareCache())
         * 
         * This versio provides extremelly detailed data about a surface position and should be used for debugging only.
         */
        public void ComputeCombinedMaterialAndSurfaceExtended(Vector3 position, out SurfacePropertiesExtended props)
        {
            if (Closed)
            {
                Debug.Fail("Storage closed!");
                props = new SurfacePropertiesExtended();
                return;
            }
            Material.GetMaterialForPositionDebug(ref position, out props);
        }
    }
}
