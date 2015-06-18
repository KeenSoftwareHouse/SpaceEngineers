using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.Game.World.Generator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Collections;
using VRage.Library.Utils;
using VRageMath;
using VRage;

namespace Sandbox.Game.World
{
    public partial class MyWorldGenerator
    {
        public static MyPlanet AddPlanet(string storageName, Vector3D positionMinCorner, int seed, float size, long entityId = 0)
        {
            m_materialsByOreType.Clear();
            m_oreProbalities.Clear();
            m_spawningMaterials.Clear();
            m_organicMaterials.Clear();

            DictionaryValuesReader<MyDefinitionId, MyPlanetDefinition> planetDefinitions = MyDefinitionManager.Static.GetPlanetsDefinitions();

            foreach (var planetDefinition in planetDefinitions)
            {
                if (planetDefinition.Diameter.Min <= size && size <= planetDefinition.Diameter.Max)
                {

                    var random = MyRandom.Instance;
                    using (var stateToken = random.PushSeed(seed))
                    {
                        BuildOreProbabilities(planetDefinition);
                        FillMaterialCollections();

                        MyCsgShapePlanetShapeAttributes shapeAttributes = new MyCsgShapePlanetShapeAttributes();

                        shapeAttributes.Seed = seed;
                        shapeAttributes.Diameter = size;
                        shapeAttributes.Radius = size / 2.0f;
                        shapeAttributes.LayerDeviationSeed = random.Next();
                        shapeAttributes.LayerDeviationNoiseFrequency = random.NextFloat(100.0f, 500.0f);
                        shapeAttributes.NoiseFrequency = random.NextFloat(planetDefinition.StructureRatio.Min, planetDefinition.StructureRatio.Max);
                        shapeAttributes.NormalNoiseFrequency = random.NextFloat(planetDefinition.NormalNoiseValue.Min, planetDefinition.NormalNoiseValue.Max);
                        shapeAttributes.DeviationScale = random.NextFloat(planetDefinition.Deviation.Min, planetDefinition.Deviation.Max);

                        MyCsgShapePlanetHillAttributes hillAttributes = FillValues(planetDefinition.HillParams, random);
                        MyCsgShapePlanetHillAttributes canyonAttributes = FillValues(planetDefinition.CanyonParams, random);

                        float planetHalfDeviation = (shapeAttributes.Diameter * shapeAttributes.DeviationScale) / 2.0f;
                        float averagePlanetRadius = shapeAttributes.Diameter * (1 - shapeAttributes.DeviationScale * hillAttributes.SizeRatio) / 2.0f;

                        float hillHalfDeviation = planetHalfDeviation * hillAttributes.SizeRatio;
                        float canyonHalfDeviation = planetHalfDeviation * canyonAttributes.SizeRatio;

                        float outerRadius = averagePlanetRadius + hillHalfDeviation * 1.5f;
                        float innerRadius = averagePlanetRadius - canyonHalfDeviation * 2.5f;

                        float atmosphereRadius = MathHelper.Max(outerRadius, averagePlanetRadius * 1.06f);
                        float minPlanetRadius = MathHelper.Min(innerRadius, averagePlanetRadius - planetHalfDeviation * 2 * 2.5f);

                        bool isHostile = random.NextFloat(0, 1) < planetDefinition.HostilityProbability;
                        MyMaterialLayer[] materialLayers = CreateMaterialLayers(planetDefinition, isHostile, random, averagePlanetRadius, hillHalfDeviation, canyonHalfDeviation, ref outerRadius, ref innerRadius);

                        IMyStorage storage = new MyOctreeStorage(MyCompositeShapeProvider.CreatePlanetShape(0, ref shapeAttributes, ref hillAttributes, ref canyonAttributes, materialLayers), FindBestOctreeSize(size));

                        float redAtmosphereShift = isHostile ? random.NextFloat(-0.15f, -0.05f) : 0;
                        float greenAtmosphereShift = isHostile ? random.NextFloat(-0.15f, -0.05f) : 0;
                        float blueAtmosphereShift = isHostile ? random.NextFloat(-0.15f, -0.05f) : 0;

                        Vector3 atmosphereWavelengths = new Vector3(0.650f + redAtmosphereShift, 0.570f + greenAtmosphereShift, 0.475f + blueAtmosphereShift);
                        var voxelMap = new MyPlanet();
                        voxelMap.EntityId = entityId;
                        voxelMap.Init(storageName, storage, positionMinCorner, averagePlanetRadius, atmosphereRadius,
                            averagePlanetRadius + hillHalfDeviation, minPlanetRadius, planetDefinition.HasAtmosphere, atmosphereWavelengths, isHostile ? 0.0f : 1.0f);
                        MyEntities.Add(voxelMap);
                        return voxelMap;
                    }


                }
            }
            return null;
        }

        class MyOreProbability
        {
            public float Probability;
            public float CummulativeProbability;
            public string OreName;
        }

        private static readonly Dictionary<string, List<MyVoxelMaterialDefinition>> m_materialsByOreType = new Dictionary<string, List<MyVoxelMaterialDefinition>>();
        private static readonly List<MyVoxelMaterialDefinition> m_spawningMaterials = new List<MyVoxelMaterialDefinition>();
        private static readonly List<MyVoxelMaterialDefinition> m_organicMaterials = new List<MyVoxelMaterialDefinition>();
        private static readonly List<MyOreProbability> m_oreProbalities = new List<MyOreProbability>();
        static float m_oreCummulativeProbability = 0.0f;

        private static MyCsgShapePlanetHillAttributes FillValues(MyStructureParams input, MyRandom random)
        {
            MyCsgShapePlanetHillAttributes outputValues = new MyCsgShapePlanetHillAttributes();

            outputValues.BlendTreshold = random.NextFloat(input.BlendSize.Min, input.BlendSize.Max);
            outputValues.Treshold = random.NextFloat(input.Treshold.Min, input.Treshold.Max);
            outputValues.Frequency = random.NextFloat(input.Frequency.Min, input.Frequency.Max);
            outputValues.SizeRatio = random.NextFloat(input.SizeRatio.Min, input.SizeRatio.Max);
            outputValues.NumNoises = random.Next((int)input.NumNoises.Min, (int)input.NumNoises.Max);

            return outputValues;
        }

        private static MyMaterialLayer CreatePoleLayer(MyRandom random, MyPoleParams poleParams, float startHeight, float outerRadius, ref int layerOffset)
        {
            if (m_materialsByOreType.ContainsKey("Ice") == false)
            {
                return null;
            }

            MyMaterialLayer poleLayer = null;
            float poleProbability = random.NextFloat(0, 1);

            if (poleParams != null && poleProbability < poleParams.Probability)
            {
                layerOffset++;

                poleLayer = new MyMaterialLayer();
                poleLayer.StartHeight = startHeight;
                poleLayer.EndHeight = outerRadius;
                poleLayer.MaterialDefinition = m_materialsByOreType["Ice"][random.Next() % m_materialsByOreType["Ice"].Count];
                poleLayer.HeightEndDeviation = 0;
                poleLayer.HeightStartDeviation = 0;
                poleLayer.StartAngle = random.NextFloat(poleParams.Angle.Min, poleParams.Angle.Max);
                poleLayer.EndAngle = 1.0f;
                poleLayer.AngleStartDeviation = random.NextFloat(poleParams.AngleDeviation.Min, poleParams.AngleDeviation.Max);
            }
            return poleLayer;
        }

        private static MyMaterialLayer[] CreateMaterialLayers(MyPlanetDefinition planetDefinition, bool isHostile, MyRandom random, float averagePlanetRadius, float hillHalfDeviation, float canyonHalfDeviation, ref float outerRadius, ref float innerRadius)
        {
            int numLayers = random.Next((int)planetDefinition.NumLayers.Min, (int)planetDefinition.NumLayers.Max);

            float startHeight = averagePlanetRadius - canyonHalfDeviation;
            outerRadius = averagePlanetRadius + hillHalfDeviation;
            innerRadius = averagePlanetRadius - canyonHalfDeviation;

            int layerOffset = 0;

            MyMaterialLayer southPoleLayer = CreatePoleLayer(random, planetDefinition.SouthPole, startHeight, outerRadius, ref layerOffset);
            MyMaterialLayer northPoleLayer = CreatePoleLayer(random, planetDefinition.NorthPole, startHeight, outerRadius, ref layerOffset);


            MyMaterialLayer[] materialLayers = new MyMaterialLayer[numLayers + layerOffset];

            float endAngle = 1;
            float startAngle = -1;
            int currentLayer = 0;

            if (southPoleLayer != null)
            {
                materialLayers[currentLayer] = southPoleLayer;
                endAngle = southPoleLayer.StartAngle;
                currentLayer++;
            }

            if (northPoleLayer != null)
            {
                materialLayers[currentLayer] = northPoleLayer;
                northPoleLayer.EndAngle = northPoleLayer.StartAngle;
                northPoleLayer.StartAngle = -1.0f;
                northPoleLayer.AngleEndDeviation = northPoleLayer.AngleStartDeviation;
                northPoleLayer.AngleStartDeviation = 0.0f;
                startAngle = northPoleLayer.EndAngle;
            }

            float step = (outerRadius - innerRadius) / materialLayers.Length;


            float organicHeightEnd = random.NextFloat(planetDefinition.OrganicHeightEnd.Min, planetDefinition.OrganicHeightEnd.Max);
            float metalsHeightEnd = random.NextFloat(planetDefinition.MetalsHeightEndHostile.Min, planetDefinition.MetalsHeightEndHostile.Max);
            float floraMaterialSpawnProbability = random.NextFloat(planetDefinition.FloraMaterialSpawnProbability.Min, planetDefinition.FloraMaterialSpawnProbability.Max);
            float metalsSpawnProbability = random.NextFloat(planetDefinition.MetalsSpawnProbability.Min, planetDefinition.MetalsSpawnProbability.Max);
            float metalsSpawnValue = random.NextFloat(0, 1);

            for (int i = layerOffset; i < materialLayers.Length; ++i)
            {
                float layerHeight = random.NextFloat(0, step);
                materialLayers[i] = new MyMaterialLayer();
                materialLayers[i].StartHeight = startHeight;
                materialLayers[i].EndHeight = startHeight + layerHeight;
                materialLayers[i].StartAngle = startAngle;
                materialLayers[i].EndAngle = endAngle;
                materialLayers[i].HeightStartDeviation = random.NextFloat(0, 100.0f / (float)(i + 1));
                materialLayers[i].AngleStartDeviation = 0;
                materialLayers[i].HeightEndDeviation = random.NextFloat(0, 100.0f / (float)(i + 1));
                materialLayers[i].AngleEndDeviation = 0;

                MyVoxelMaterialDefinition materialDefinition = null;

                if (m_materialsByOreType.ContainsKey("Stone") == true)
                {
                    materialDefinition = m_materialsByOreType["Stone"][random.Next() % m_materialsByOreType["Stone"].Count];
                }

                if (planetDefinition.HasAtmosphere && isHostile == false)
                {
                    if ((outerRadius - startHeight) > ((outerRadius - innerRadius) * (1 - organicHeightEnd)))
                    {
                        float value = random.NextFloat(0, 1);
                        if (value > floraMaterialSpawnProbability)
                        {
                            materialDefinition = m_organicMaterials[random.Next() % m_organicMaterials.Count];
                        }
                        else
                        {
                            materialDefinition = m_spawningMaterials[random.Next() % m_spawningMaterials.Count];
                        }
                    }
                }
                else
                {
                    if (metalsSpawnValue < metalsSpawnProbability)
                    {
                        if ((outerRadius - startHeight) > ((outerRadius - innerRadius) * (1 - metalsHeightEnd)))
                        {
                            MyOreProbability probablity = GetOre(random.NextFloat(0, 1));
                            if (probablity != null)
                            {
                                materialLayers[i].EndHeight = materialLayers[i].StartHeight - 1;
                                materialLayers[i].HeightStartDeviation *= probablity.Probability;
                                materialLayers[i].HeightEndDeviation *= probablity.Probability;

                                materialDefinition = m_materialsByOreType[probablity.OreName][random.Next() % m_materialsByOreType[probablity.OreName].Count];
                            }
                        }
                    }
                }

                materialLayers[i].MaterialDefinition = materialDefinition;
                startHeight += layerHeight;
            }
            return materialLayers;
        }

        private static void FillMaterialCollections()
        {
            foreach (var material in MyDefinitionManager.Static.GetVoxelMaterialDefinitions())
            {
                if (material.MinedOre == "Organic")
                {
                    if (material.SpawnsFlora)
                    {
                        m_spawningMaterials.Add(material);
                    }
                    else
                    {
                        m_organicMaterials.Add(material);
                    }
                }
                else
                {
                    List<MyVoxelMaterialDefinition> materialDefinitions;
                    if (false == m_materialsByOreType.TryGetValue(material.MinedOre, out materialDefinitions))
                    {
                        materialDefinitions = new List<MyVoxelMaterialDefinition>();
                    }
                    materialDefinitions.Add(material);
                    m_materialsByOreType[material.MinedOre] = materialDefinitions;
                }
            }
        }

        private static void BuildOreProbabilities(MyPlanetDefinition planetDefinition)
        {
            m_oreCummulativeProbability = 0.0f;
            if (planetDefinition.MetalsOreProbability != null)
            {
                foreach (var oreProbability in planetDefinition.MetalsOreProbability)
                {
                    MyOreProbability probability = new MyOreProbability();
                    probability.Probability = MyRandom.Instance.NextFloat(oreProbability.Min, oreProbability.Max);
                    m_oreCummulativeProbability += probability.Probability;
                    probability.CummulativeProbability = m_oreCummulativeProbability;
                    probability.OreName = oreProbability.OreName;
                    m_oreProbalities.Add(probability);
                }
            }
        }

        private static MyOreProbability GetOre(float probability)
        {
            foreach (var oreProbability in m_oreProbalities)
            {
                if (oreProbability.CummulativeProbability / m_oreCummulativeProbability >= probability)
                {
                    return oreProbability;
                }
            }

            return null;
        }
    }
}
