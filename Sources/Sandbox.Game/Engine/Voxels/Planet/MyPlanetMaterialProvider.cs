using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.GameSystems;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using VRage.Profiler;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public partial class MyPlanetMaterialProvider
    {
        #region Material Data Structures
        unsafe struct MapBlendCache
        {
            public Vector2I Cell;
            public fixed ushort Data[4];

            public byte Face;
            public int HashCode;
        }

        public struct PlanetOre
        {
            public byte Value;
            public float Depth;
            public float Start;
            public MyVoxelMaterialDefinition Material;

            public override string ToString()
            {
                if (Material == null)
                    return "";
                else
                    return String.Format("{0}({1}:{2}; {3})", Material.Id.SubtypeName, Start, Depth, Value);
            }
        }
        #endregion

        int m_mapResolutionMinusOne;
        MyPlanetShapeProvider m_planetShape;

        Dictionary<byte, PlanetMaterial> m_materials = null;
        Dictionary<byte, PlanetBiome> m_biomes = null;
        Dictionary<byte, PlanetOre> m_ores = null;

        PlanetMaterial m_defaultMaterial = null;
        PlanetMaterial m_subsurfaceMaterial = null;

        // Materials
        MyCubemap m_materialMap;

        // Biomes
        MyCubemap m_biomeMap;

        // Ore Deposits
        MyCubemap m_oreMap;

        // Occlusion
        MyCubemap m_occlusionMap;

        MyTileTexture<byte> m_blendingTileset;

        MyPlanetGeneratorDefinition m_generator;

        float m_invHeightRange;

        float m_heightmapScale;

        private float m_biomePixelSize;

        private bool m_hasRules;

        private int m_hashCode;

        // List of range reduced rules for biomes.
        [ThreadStatic]
        private static List<PlanetMaterialRule>[] m_rangeBiomes = null;

        [ThreadStatic]
        private static bool m_rangeClean = false;

        [ThreadStatic]
        private static MyPlanetMaterialProvider m_providerForRules = null;

        public bool Closed { get; private set; }

        public MyPlanetMaterialProvider(MyPlanetGeneratorDefinition generatorDef, MyPlanetShapeProvider planetShape)
        {
            m_materials = new Dictionary<byte, PlanetMaterial>(generatorDef.SurfaceMaterialTable.Length);

            for (int i = 0; i < generatorDef.SurfaceMaterialTable.Length; ++i)
            {
                byte materialValue = (byte)generatorDef.SurfaceMaterialTable[i].Value;

                m_materials[materialValue] = new PlanetMaterial(generatorDef.SurfaceMaterialTable[i]);
            }

            m_defaultMaterial = new PlanetMaterial(generatorDef.DefaultSurfaceMaterial);

            if (generatorDef.DefaultSubSurfaceMaterial != null)
            {
                m_subsurfaceMaterial = new PlanetMaterial(generatorDef.DefaultSubSurfaceMaterial);
            }
            else
            {
                m_subsurfaceMaterial = m_defaultMaterial;
            }

            m_planetShape = planetShape;

            MyCubemap[] maps;
            MyHeightMapLoadingSystem.Static.GetPlanetMaps(generatorDef.FolderName, generatorDef.Context, generatorDef.PlanetMaps, out maps);


            m_materialMap = maps[0];
            m_biomeMap = maps[1];
            m_oreMap = maps[2];
            m_occlusionMap = maps[3];

            if (m_biomeMap != null)
                m_mapResolutionMinusOne = m_biomeMap.Resolution - 1;

            m_generator = generatorDef;

            m_invHeightRange = 1 / (m_planetShape.MaxHillHeight - m_planetShape.MinHillHeight);

            m_biomePixelSize = (float)((planetShape.MaxHillHeight + planetShape.Radius) * Math.PI) / ((float)(m_mapResolutionMinusOne + 1) * 2);

            m_hashCode = generatorDef.FolderName.GetHashCode();

            // Material groups

            if (m_generator.MaterialGroups != null && m_generator.MaterialGroups.Length > 0)
            {
                m_biomes = new Dictionary<byte, PlanetBiome>();

                foreach (var group in m_generator.MaterialGroups)
                {
                    m_biomes.Add(group.Value, new PlanetBiome(group));
                }

            }

            m_blendingTileset = MyHeightMapLoadingSystem.Static.GetTerrainBlendTexture(m_generator.MaterialBlending);

            m_ores = new Dictionary<byte, PlanetOre>();

            foreach (var mapping in m_generator.OreMappings)
            {
                var mat = GetMaterial(mapping.Type);
                if (mat != null)
                {
                    if (m_ores.ContainsKey(mapping.Value))
                    {
                        string message = String.Format("Value {0} is already mapped to another ore.", mapping.Value);
                        Debug.Fail(message);
                        MyLog.Default.WriteLine(message);
                    }
                    else
                    {
                        m_ores[mapping.Value] = new PlanetOre()
                        {
                            Depth = mapping.Depth,
                            Start = mapping.Start,
                            Value = mapping.Value,
                            Material = mat
                        };
                    }
                }
            }

            Closed = false;
        }

        public void Close()
        {
            if (m_providerForRules == this)
                m_providerForRules = null;

            // Clear to speed up collection

            m_blendingTileset = null;
            m_subsurfaceMaterial = null;
            m_generator = null;
            m_biomeMap = null;
            m_biomes = null;
            m_materials = null;
            m_planetShape = null;
            m_ores = null;

            m_materialMap = null;
            m_oreMap = null;
            m_biomeMap = null;
            m_occlusionMap = null;

            Closed = true;
        }

        private unsafe void GetRuleBounds(ref BoundingBox request, out BoundingBox ruleBounds)
        {
            Vector3* vertices = stackalloc Vector3[8];

            ruleBounds.Min = new Vector3(float.PositiveInfinity);
            ruleBounds.Max = new Vector3(float.NegativeInfinity);

            request.GetCornersUnsafe(vertices);

            float latitude, longitude;

            if (Vector3.Zero.IsInsideInclusive(ref request.Min, ref request.Max))
            {
                ruleBounds.Min.X = 0;
            }
            else
            {
                var clamp = Vector3.Clamp(Vector3.Zero, request.Min, request.Max);
                ruleBounds.Min.X = m_planetShape.DistanceToRatio(clamp.Length());
            }

            { // Calculate furthest point in BB
                Vector3 end;
                Vector3 c = request.Center;

                if (c.X < 0)
                    end.X = request.Min.X;
                else
                    end.X = request.Max.X;

                if (c.Y < 0)
                    end.Y = request.Min.Y;
                else
                    end.Y = request.Max.Y;

                if (c.Z < 0)
                    end.Z = request.Min.Z;
                else
                    end.Z = request.Max.Z;

                ruleBounds.Max.X = m_planetShape.DistanceToRatio(end.Length());
            }

            // If box intercepts Y axis (north south axis).
            if (request.Min.X < 0 && request.Min.Z < 0 && request.Max.X > 0 && request.Max.Z > 0)
            {
                ruleBounds.Min.Z = -1;
                ruleBounds.Max.Z = 3;

                for (int i = 0; i < 8; i++)
                {
                    float len = vertices[i].Length();
                    latitude = vertices[i].Y / len;
                    if (ruleBounds.Min.Y > latitude) ruleBounds.Min.Y = latitude;
                    if (ruleBounds.Max.Y < latitude) ruleBounds.Max.Y = latitude;
                }
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    float len = vertices[i].Length();

                    vertices[i] /= len;
                    latitude = vertices[i].Y;

                    Vector2 lon = new Vector2(-vertices[i].X, -vertices[i].Z);
                    lon.Normalize();

                    longitude = lon.Y;
                    if (lon.X > 0)
                    {
                        longitude = 2 - longitude;
                    }

                    if (ruleBounds.Min.Y > latitude) ruleBounds.Min.Y = latitude;
                    if (ruleBounds.Max.Y < latitude) ruleBounds.Max.Y = latitude;

                    if (ruleBounds.Min.Z > longitude) ruleBounds.Min.Z = longitude;
                    if (ruleBounds.Max.Z < longitude) ruleBounds.Max.Z = longitude;
                }
            }
        }

        public void PrepareRulesForBox(ref BoundingBox request)
        {
            if (m_biomes != null)
            {
                if (request.Extents.Sum > 50)
                    PrepareRulesForBoxInternal(ref request);
                else CleanRules();
            }
        }

        private void PrepareRulesForBoxInternal(ref BoundingBox request)
        {
            if (m_rangeBiomes == null) m_rangeBiomes = new List<PlanetMaterialRule>[256];

            BoundingBox box;
            request.Translate(-m_planetShape.Center());

            // Inflate so we don't miss any rules.
            request.Inflate(request.Extents.Length() * .1f);

            GetRuleBounds(ref request, out box);

            foreach (var bio in m_biomes.Values)
            {
                if (ReferenceEquals(m_rangeBiomes[bio.Value], bio.Rules) || m_rangeBiomes[bio.Value] == null || m_providerForRules != this)
                    m_rangeBiomes[bio.Value] = new List<PlanetMaterialRule>();

                bio.MateriaTree.OverlapAllBoundingBox(ref box, m_rangeBiomes[bio.Value], clear: true);
            }
            m_rangeClean = false;
            m_providerForRules = this;
        }

        private void CleanRules()
        {
            if (m_rangeBiomes == null) m_rangeBiomes = new List<PlanetMaterialRule>[256];

            foreach (var bio in m_biomes.Values)
            {
                m_rangeBiomes[bio.Value] = bio.Rules;
            }
            m_rangeClean = true;
            m_providerForRules = this;
        }

        public void ReadMaterialRange(ref MyVoxelDataRequest req)
        {
            byte biome;

            ProfilerShort.Begin("MaterialComputation");

            req.Flags = req.RequestFlags & (MyVoxelRequestFlags.SurfaceMaterial | MyVoxelRequestFlags.ConsiderContent);

            Vector3I minInLod = req.minInLod;
            Vector3I maxInLod = req.maxInLod;
            var target = req.Target;

            float lodVoxelSize = 1 << req.Lod;

            MyVoxelRequestFlags usedFlags = 0;

            bool computeOcclusion = req.RequestedData.Requests(MyStorageDataTypeEnum.Occlusion);

            // We don't bother determining where the surface is if we don't have the normal.
            bool assignToSurface = req.RequestFlags.HasFlags(MyVoxelRequestFlags.SurfaceMaterial);

            bool useContent = req.RequestFlags.HasFlags(MyVoxelRequestFlags.ConsiderContent);

            // Prepare coefficient cache
            m_planetShape.PrepareCache();

            // Here we will compute which rules match the requested range and apply those.
            if (m_biomes != null)
            {
                if (req.SizeLinear > 125)
                {
                    BoundingBox rbox = new BoundingBox((Vector3)minInLod * lodVoxelSize, (Vector3)maxInLod * lodVoxelSize);

                    PrepareRulesForBoxInternal(ref rbox);
                }
                else if (!m_rangeClean || m_providerForRules != this)
                {
                    CleanRules();
                }
            }

            Vector3I combinedOffset = -minInLod + req.Offset;
            Vector3I v = new Vector3I();
            for (v.Z = minInLod.Z; v.Z <= maxInLod.Z; ++v.Z)
            {
                for (v.Y = minInLod.Y; v.Y <= maxInLod.Y; ++v.Y)
                {
                    for (v.X = minInLod.X; v.X <= maxInLod.X; ++v.X)
                    {
                        Vector3I coords = v;

                        var write = v + combinedOffset;
                        var writeLinear = target.ComputeLinear(ref write);

                        if ((assignToSurface && target.Material(writeLinear) != 0)
                            || (useContent && target.Content(writeLinear) == 0))
                        {
                            if (computeOcclusion)
                            {
                                // Prevent empty voxels from affecting occlusion.
                                target.Content(writeLinear, 0);
                            }
                            target.Material(writeLinear, MyVoxelConstants.NULL_MATERIAL);
                            continue;
                        }

                        MyVoxelMaterialDefinition mat = null;
                        byte occlusion = (byte)(computeOcclusion ? 255 : 0); // flag that we want occlusion.

                        Vector3 localPos = coords * lodVoxelSize;
                        mat = GetMaterialForPosition(ref localPos, lodVoxelSize, out biome, ref occlusion);

                        if (mat == null) mat = MyDefinitionManager.Static.GetVoxelMaterialDefinition(0);

                        target.Material(writeLinear, mat.Index);
                        if (computeOcclusion)
                        {
                            target[MyStorageDataTypeEnum.Occlusion][writeLinear] = occlusion;
                        }
                    }
                }
            }

            ProfilerShort.End();
        }

        public void ReadOcclusion(ref MyVoxelDataRequest req)
        {
            ProfilerShort.Begin("Occlusion Computation");

            req.Flags = req.RequestFlags & (MyVoxelRequestFlags.SurfaceMaterial | MyVoxelRequestFlags.ConsiderContent);

            Vector3I minInLod = req.minInLod;
            Vector3I maxInLod = req.maxInLod;
            var target = req.Target;

            float lodVoxelSize = 1 << req.Lod;

            // We don't bother determining where the surface is if we don't have the normal.
            bool assignToSurface = req.RequestFlags.HasFlags(MyVoxelRequestFlags.SurfaceMaterial) && req.RequestedData.Requests(MyStorageDataTypeEnum.Material);

            bool useContent = req.RequestFlags.HasFlags(MyVoxelRequestFlags.ConsiderContent);

            // Prepare coefficient cache
            m_planetShape.PrepareCache();

            Vector3I combinedOffset = -minInLod + req.Offset;
            Vector3I v = new Vector3I();
            for (v.Z = minInLod.Z; v.Z <= maxInLod.Z; ++v.Z)
            {
                for (v.Y = minInLod.Y; v.Y <= maxInLod.Y; ++v.Y)
                {
                    for (v.X = minInLod.X; v.X <= maxInLod.X; ++v.X)
                    {
                        Vector3I coords = v;

                        var write = v + combinedOffset;
                        var writeLinear = target.ComputeLinear(ref write);

                        if ((assignToSurface && target.Material(writeLinear) != 0)
                            || (useContent && target.Content(writeLinear) == 0))
                        {
                            target.Content(writeLinear, 0);
                            continue;
                        }

                        Vector3 localPos = coords * lodVoxelSize;
                        var occlusion = GetOcclusionForPosition(ref localPos, lodVoxelSize);
                        target[MyStorageDataTypeEnum.Occlusion][writeLinear] = occlusion;
                    }
                }
            }

            ProfilerShort.End();
        }

        private static MyVoxelMaterialDefinition GetMaterial(string name)
        {
            MyVoxelMaterialDefinition def = MyDefinitionManager.Static.GetVoxelMaterialDefinition(name);
            if (def == null)
            {
                //Debug.Fail("Could not load voxel material " + name);
                MyLog.Default.WriteLine("Could not load voxel material " + name);
            }
            return def;
        }

        private byte GetOcclusionForPosition(ref Vector3 localPos, float lodVoxelSize)
        {
            int face;
            Vector2 texcoord;

            Vector3 localPosition = localPos - m_planetShape.Center();
            var distanceToCenter = localPosition.Length();

            MyCubemapHelpers.CalculateSampleTexcoord(ref localPosition, out face, out texcoord);

            Vector3 normal;
            float sampledHeight = m_planetShape.GetValueForPositionWithCache(face, ref texcoord, out normal);

            var surfaceDepth = m_planetShape.SignedDistanceWithSample(lodVoxelSize, distanceToCenter, sampledHeight) * normal.Z;

            if (m_occlusionMap != null && surfaceDepth > -(lodVoxelSize * 1.5f))
            {
                if (m_biomePixelSize < lodVoxelSize)
                {
                    return m_occlusionMap[face].GetValue(texcoord.X, texcoord.Y);
                }
                else
                {
                    return ComputeMapBlend(texcoord, face, ref m_occlusionBC,
                        m_occlusionMap[face]);
                }
            }

            return 0;
        }

        public MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos, float lodSize)
        {
            byte bb, oo = 0;
            return GetMaterialForPosition(ref pos, lodSize, out bb, ref oo);
        }

        public MyVoxelMaterialDefinition GetMaterialForPosition(ref Vector3 pos, float lodSize, out byte biomeValue, ref byte occlusion)
        {
            biomeValue = 0;

            MaterialSampleParams ps;
            GetPositionParams(ref pos, lodSize, out ps);

            MyVoxelMaterialDefinition def = null;

            float oreDepth = ps.SurfaceDepth / Math.Max(lodSize * .5f, 1f) + .5f; // Hack to preserve position for ore detector.
            // Ore depositis from map come first.
            if (m_oreMap != null)
            {
                byte ore = m_oreMap.Faces[ps.Face].GetValue(ps.Texcoord.X, ps.Texcoord.Y);
                PlanetOre om;
                if (m_ores.TryGetValue(ore, out om))
                {
                    if (om.Start <= -oreDepth && om.Start + om.Depth >= -oreDepth)
                    {
                        occlusion = 0;
                        return om.Material;
                    }
                }
            }

            PlanetMaterial voxelMaterial = GetLayeredMaterialForPosition(ref ps, out biomeValue, ref occlusion);


            // Extend depth to compensate for lod rounding
            float voxelDepth = ps.SurfaceDepth / lodSize;

            // Check layers
            if (voxelMaterial.HasLayers)
            {
                var layers = voxelMaterial.Layers;

                for (int i = 0; i < layers.Length; i++)
                {
                    if (voxelDepth >= -layers[i].Depth)
                    {
                        def = voxelMaterial.Layers[i].Material;
                        break;
                    }
                }
            }
            // Check single layered
            else
            {
                if (voxelDepth >= -voxelMaterial.Depth)
                {
                    def = voxelMaterial.Material;
                }
            }

            if (def == null)
            {
                def = m_subsurfaceMaterial.FirstOrDefault;
            }

            return def;
        }

        [ThreadStatic]
        private static MapBlendCache m_materialBC;

        [ThreadStatic]
        private static MapBlendCache m_occlusionBC;

        private unsafe byte ComputeMapBlend(Vector2 coords, int face, ref MapBlendCache cache, MyCubemapData<byte> map)
        {
            coords = coords * map.Resolution - .5f;
            Vector2I isample = new Vector2I(coords);

            if (cache.HashCode != m_hashCode || cache.Face != face || cache.Cell != isample)
            {
                byte tl, tr, bl, br;

                cache.HashCode = m_hashCode;
                cache.Cell = isample;
                cache.Face = (byte)face;

                // Biome
                if (m_materialMap != null)
                {
                    map.GetValue(isample.X, isample.Y, out tl);
                    map.GetValue(isample.X + 1, isample.Y, out tr);
                    map.GetValue(isample.X, isample.Y + 1, out bl);
                    map.GetValue(isample.X + 1, isample.Y + 1, out br);

                    byte* ss = stackalloc byte[4];

                    ss[0] = tl;
                    ss[1] = tr;
                    ss[2] = bl;
                    ss[3] = br;

                    if (tl == tr && bl == br && bl == tl)
                    {
                        fixed (ushort* smpls = cache.Data)
                        {
                            smpls[0] = (ushort)((tl << 8) | 0xF);
                            smpls[1] = 0;
                            smpls[2] = 0;
                            smpls[3] = 0;
                        }
                    }
                    else
                    {
                        fixed (ushort* smpls = cache.Data)
                        {
                            Sort4(ss);
                            ComputeTilePattern(tl, tr, bl, br, ss, smpls);
                        }
                    }
                }
            }

            byte value;
            fixed (ushort* smpls = cache.Data)
            {
                coords -= Vector2.Floor(coords);
                if (coords.X == 1) coords.X = .99999f;
                if (coords.Y == 1) coords.Y = .99999f;

                SampleTile(smpls, ref coords, out value);
            }

            return value;
        }

        private static unsafe void Sort4(byte* v)
        {
            byte tmp;
            // 4 way sorting network
            if (v[0] > v[1]) { tmp = v[1]; v[1] = v[0]; v[0] = tmp; }
            if (v[2] > v[3]) { tmp = v[2]; v[2] = v[3]; v[3] = tmp; }
            if (v[0] > v[3]) { tmp = v[3]; v[3] = v[0]; v[3] = tmp; }
            if (v[1] > v[2]) { tmp = v[1]; v[1] = v[2]; v[2] = tmp; }
            if (v[0] > v[1]) { tmp = v[1]; v[1] = v[0]; v[0] = tmp; }
            if (v[2] > v[3]) { tmp = v[2]; v[2] = v[3]; v[3] = tmp; }
        }

        private static unsafe void ComputeTilePattern(byte tl, byte tr, byte bl, byte br, byte* ss, ushort* values)
        {
            int cnt = 0;
            for (int i = 0; i < 4; ++i)
            {
                if (i > 0 && ss[i] == ss[i - 1]) continue;

                values[cnt++] = (ushort)((ss[i] << 8) | (ss[i] == tl ? 1 << 3 : 0) | (ss[i] == tr ? 1 << 2 : 0) | (ss[i] == bl ? 1 << 1 : 0) | (ss[i] == br ? 1 << 0 : 0));
            }

            for (; cnt < 4; ++cnt)
            {
                values[cnt] = 0;
            }
        }

        private unsafe void SampleTile(ushort* values, ref Vector2 coords, out byte computed)
        {
            byte last = 0;
            for (int i = 0; i < 4; ++i)
            {
                byte mat = (byte)(values[i] >> 8);
                if (values[i] != 0)
                {
                    int tileId = values[i] & 0xF;
                    byte value;
                    m_blendingTileset.GetValue(tileId, coords, out value);

                    last = mat;
                    if (value == 0)
                    {
                        break;
                    }
                }
                else
                {
                    break;
                }
            }
            computed = last;
        }

        public struct MaterialSampleParams
        {
            public Vector3 Gravity;
            public Vector3 Normal;
            public float DistanceToCenter;
            public float SampledHeight;
            public int Face;
            public Vector2 Texcoord;
            public float SurfaceDepth;
            public float LodSize;
            public float Latitude;
            public float Longitude;
        }

        public void GetPositionParams(ref Vector3 pos, float lodSize, out MaterialSampleParams ps, bool skipCache = false)
        {
            Vector3 localPosition = pos - m_planetShape.Center();
            ps.DistanceToCenter = localPosition.Length();

            ps.LodSize = lodSize;

            if (ps.DistanceToCenter < 0.01f)
            {
                ps.SurfaceDepth = 0;
                ps.Gravity = Vector3.Down;
                ps.Latitude = 0;
                ps.Longitude = 0;
                ps.Texcoord = Vector2.One / 2;
                ps.Face = 0;
                ps.Normal = Vector3.Backward;
                ps.SampledHeight = 0;
                return;
            }

            ps.Gravity = localPosition / ps.DistanceToCenter;

            MyCubemapHelpers.CalculateSampleTexcoord(ref localPosition, out ps.Face, out ps.Texcoord);

            // this guarantess texcoord in [0,1)
            if (skipCache)
                ps.SampledHeight = m_planetShape.GetValueForPositionCacheless(ps.Face, ref ps.Texcoord, out ps.Normal);
            else
                ps.SampledHeight = m_planetShape.GetValueForPositionWithCache(ps.Face, ref ps.Texcoord, out ps.Normal);

            ps.SurfaceDepth = m_planetShape.SignedDistanceWithSample(lodSize, ps.DistanceToCenter, ps.SampledHeight) * ps.Normal.Z;
            ps.Latitude = ps.Gravity.Y;

            Vector2 lon = new Vector2(-ps.Gravity.X, -ps.Gravity.Z);
            lon.Normalize();

            ps.Longitude = lon.Y;
            if (-ps.Gravity.X > 0)
            {
                ps.Longitude = 2 - ps.Longitude;
            }
        }

        public PlanetMaterial GetLayeredMaterialForPosition(ref MaterialSampleParams ps, out byte spawnsItems, ref byte occlusion)
        {
            if (ps.DistanceToCenter < 0.01)
            {
                spawnsItems = 255;
                occlusion = 0;
                return m_defaultMaterial;
            }

            Byte roundedMaterial = 0;
            PlanetMaterial voxelMaterial = null;
            byte spawns = 0;

            bool computeOcclusion = m_occlusionMap != null && ps.SurfaceDepth > -(ps.LodSize * 2) && occlusion != 0;

            if (m_biomeMap != null)
                spawns = m_biomeMap.Faces[ps.Face].GetValue(ps.Texcoord.X, ps.Texcoord.Y);

            if (m_biomePixelSize < ps.LodSize)
            {
                if (m_materialMap != null)
                    roundedMaterial = m_materialMap.Faces[ps.Face].GetValue(ps.Texcoord.X, ps.Texcoord.Y);

                if (computeOcclusion)
                    occlusion = m_occlusionMap.Faces[ps.Face].GetValue(ps.Texcoord.X, ps.Texcoord.Y);
                else
                    occlusion = 0;
            }
            else
            {
                if (m_biomeMap != null)
                    roundedMaterial = ComputeMapBlend(ps.Texcoord, ps.Face, ref m_materialBC,
                        m_materialMap.Faces[ps.Face]);

                if (computeOcclusion)
                    occlusion = ComputeMapBlend(ps.Texcoord, ps.Face, ref m_occlusionBC,
                        m_occlusionMap.Faces[ps.Face]);
                else occlusion = 0;
            }
            m_materials.TryGetValue(roundedMaterial, out voxelMaterial);

            if (MyFakes.ENABLE_DEFINITION_ENVIRONMENTS && voxelMaterial == null && m_biomes != null)
            {
                var rules = m_rangeBiomes[roundedMaterial];

                if (rules != null && rules.Count != 0)
                {
                    float height = (ps.SampledHeight - m_planetShape.MinHillHeight) * m_invHeightRange;

                    foreach (var rule in rules)
                    {
                        if (rule.Check(height, ps.Latitude, ps.Longitude, ps.Normal.Z))
                        {
                            voxelMaterial = rule;
                            break;
                        }
                    }
                }
            }

            if (voxelMaterial == null)
            {
                voxelMaterial = m_defaultMaterial;
            }

            spawnsItems = spawns;

            return voxelMaterial;
        }
    }
}
