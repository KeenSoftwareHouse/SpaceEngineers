using Sandbox.Engine.Utils;
using System;
using VRageMath;

namespace Sandbox.Engine.Voxels
{
    public partial class MyPlanetMaterialProvider
    {

        public unsafe void GetMaterialForPositionDebug(ref Vector3 pos, out MyPlanetStorageProvider.SurfacePropertiesExtended props)
        {
            byte spawns = 0;

            MaterialSampleParams ps;
            GetPositionParams(ref pos, 1.0f, out ps, true);

            props.Position = pos;
            props.Gravity = ps.Gravity;

            props.Material = m_defaultMaterial.FirstOrDefault;

            props.Slope = ps.Normal.Z;
            props.HeightRatio = m_planetShape.AltitudeToRatio(ps.SampledHeight);
            props.Depth = ps.SurfaceDepth;
            props.Latitude = ps.Latitude;
            props.Longitude = ps.Longitude;
            props.Altitude = ps.DistanceToCenter - m_planetShape.Radius;

            props.Face = ps.Face;
            props.Texcoord = ps.Texcoord;


            props.BiomeValue = 0;
            props.MaterialValue = 0;
            props.OcclusionValue = 0;
            props.OreValue = 0;

            props.EffectiveRule = null;
            props.Biome = null;
            props.Ore = new PlanetOre();

            props.Origin = MyPlanetStorageProvider.SurfacePropertiesExtended.MaterialOrigin.Default;

            PlanetMaterial voxelMaterial = null;

            // Ore depositis from map come first.

            if (m_oreMap != null)
            {
                props.OreValue = m_oreMap.Faces[ps.Face].GetValue(ps.Texcoord.X, ps.Texcoord.Y);
                PlanetOre om;
                if (m_ores.TryGetValue(props.OreValue, out om))
                {
                    props.Ore = om;
                    if (om.Start <= -ps.SurfaceDepth && om.Start + om.Depth >= -ps.SurfaceDepth)
                    {
                        props.Material = om.Material;
                        props.Origin = MyPlanetStorageProvider.SurfacePropertiesExtended.MaterialOrigin.Ore;
                    }
                }
            }

            if (ps.DistanceToCenter < 0.01)
            {
                return;
            }

            Byte roundedMaterial = 0;

            if (m_biomePixelSize < ps.LodSize)
            {
                if (m_materialMap != null)
                    m_materialMap.Faces[ps.Face].GetValue((int)ps.Texcoord.X, (int)ps.Texcoord.Y, out roundedMaterial);

                if (m_occlusionMap != null)
                    m_occlusionMap.Faces[ps.Face].GetValue((int)ps.Texcoord.X, (int)ps.Texcoord.Y, out props.OcclusionValue);
            }
            else
            {
                if (m_biomeMap != null)
                    roundedMaterial = ComputeMapBlend(ps.Texcoord, ps.Face, ref m_materialBC,
                        m_materialMap.Faces[ps.Face]);

                if (m_occlusionMap != null)
                    props.OcclusionValue = ComputeMapBlend(ps.Texcoord, ps.Face, ref m_occlusionBC,
                        m_occlusionMap.Faces[ps.Face]);
            }

            m_materials.TryGetValue(roundedMaterial, out voxelMaterial);
            props.Origin = MyPlanetStorageProvider.SurfacePropertiesExtended.MaterialOrigin.Map;

            props.MaterialValue = roundedMaterial;

            if (voxelMaterial == null && m_biomes != null)
            {
                PlanetBiome b;

                m_biomes.TryGetValue(roundedMaterial, out b);

                props.Biome = b;

                // When the sample material is zero calculate the material using the definition rules;
                if (MyFakes.ENABLE_DEFINITION_ENVIRONMENTS && b != null && b.IsValid)
                {
                    foreach (var rule in b.Rules)
                    {
                        if (rule.Check(props.HeightRatio, ps.Latitude, ps.Longitude, ps.Normal.Z))
                        {
                            voxelMaterial = rule;
                            props.Origin = MyPlanetStorageProvider.SurfacePropertiesExtended.MaterialOrigin.Rule;
                            break;
                        }
                    }
                }

            }

            if (voxelMaterial == null)
            {
                voxelMaterial = m_defaultMaterial;
                props.Origin = MyPlanetStorageProvider.SurfacePropertiesExtended.MaterialOrigin.Default;
            }

            props.BiomeValue = spawns;

            // calc depth with what we already have
            float voxelDepth = ps.SurfaceDepth + .5f;

            // Check layers

            if (voxelMaterial.HasLayers)
            {
                var layers = voxelMaterial.Layers;

                for (int i = 0; i < layers.Length; i++)
                {
                    if (voxelDepth >= -layers[i].Depth)
                    {
                        props.Material = voxelMaterial.Layers[i].Material;
                        break;
                    }
                }
            }
            // Check single layered
            else
            {
                if (voxelDepth >= -voxelMaterial.Depth)
                {
                    props.Material = voxelMaterial.Material;
                }
            }

            props.EffectiveRule = voxelMaterial;
        }
    }
}
