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
using VRage.FileSystem;
using VRage.Voxels;
using Sandbox.Engine.Utils;

namespace Sandbox.Game.World
{
    public partial class MyWorldGenerator
    {

        public static void AddPlanetPrefab(string prefabName, string name)
        {
            if (MyFakes.ENABLE_PLANETS == false)
            {
                return ;
            }

            DictionaryValuesReader<MyDefinitionId, MyPlanetPrefabDefinition> planetDefinitions = MyDefinitionManager.Static.GetPlanetsPrefabsDefinitions();
            foreach (var planetPrebabDefinition in planetDefinitions)
            {
                if(planetPrebabDefinition.Id.SubtypeName == prefabName)
                {
                    var voxelMap = new MyPlanet();
                    var ob = planetPrebabDefinition.PlanetBuilder;
             
                    string storageName = MyFileSystem.ContentPath + "\\VoxelMaps\\" + ob.StorageName + MyVoxelConstants.FILE_EXTENSION;
                    voxelMap.EntityId = ob.EntityId;

                    MyPlanetInitArguments planetInitArguments;
                    planetInitArguments.StorageName = ob.StorageName;
                    planetInitArguments.Storage = MyStorageBase.LoadFromFile(storageName);
                    planetInitArguments.PositionMinCorner = ob.PositionAndOrientation.Value.Position;
                    planetInitArguments.AveragePlanetRadius = ob.Radius;
                    planetInitArguments.AtmosphereRadius = ob.AtmosphereRadius;
                    planetInitArguments.MaximumHillRadius = ob.MaximumHillRadius;
                    planetInitArguments.MinimumSurfaceRadius = ob.MinimumSurfaceRadius;
                    planetInitArguments.HasAtmosphere = ob.HasAtmosphere;
                    planetInitArguments.AtmosphereWavelengths = ob.AtmosphereWavelengths;
                    planetInitArguments.MaxOxygen = ob.MaximumOxygen;
                    planetInitArguments.GravityFalloff = ob.GravityFalloff;
                    planetInitArguments.MarkAreaEmpty = true;

                    voxelMap.Init(planetInitArguments);
                    MyEntities.Add(voxelMap);
                }
            }

        }

        public static MyPlanet AddPlanet(string storageName, Vector3D positionMinCorner, int seed, float size, long entityId = 0,bool isMoon = false)
        {        
            DictionaryValuesReader<MyDefinitionId, MyPlanetGeneratorDefinition> planetDefinitions = isMoon ? MyDefinitionManager.Static.GetMoonsGeneratorsDefinitions() :MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions();
            return CreatePlanet(storageName, ref positionMinCorner, seed, size, entityId, ref planetDefinitions);
        }

        private static MyPlanet CreatePlanet(string storageName, ref Vector3D positionMinCorner, int seed, float size, long entityId, ref DictionaryValuesReader<MyDefinitionId, MyPlanetGeneratorDefinition> planetDefinitions)
        {
            if (MyFakes.ENABLE_PLANETS == false)
            {
                return null;
            }

            m_materialsByOreType.Clear();
            m_oreProbalities.Clear();
            m_spawningMaterials.Clear();
            m_organicMaterials.Clear();

            foreach (var planetGeneratorDefinition in planetDefinitions)
            {
                var random = MyRandom.Instance;
                using (var stateToken = random.PushSeed(seed))
                {
                    BuildOreProbabilities(planetGeneratorDefinition);
                    FillMaterialCollections();

                    MyCsgShapePlanetShapeAttributes shapeAttributes = new MyCsgShapePlanetShapeAttributes();

                    shapeAttributes.Seed = seed;
                    shapeAttributes.Diameter = size;
                    shapeAttributes.Radius = size / 2.0f;
                    shapeAttributes.LayerDeviationSeed = random.Next();
                    shapeAttributes.LayerDeviationNoiseFrequency = random.NextFloat(10.0f, 500.0f);
                    shapeAttributes.NoiseFrequency = random.NextFloat(planetGeneratorDefinition.StructureRatio.Min, planetGeneratorDefinition.StructureRatio.Max);
                    shapeAttributes.DeviationScale = random.NextFloat(planetGeneratorDefinition.Deviation.Min, planetGeneratorDefinition.Deviation.Max);

                    MyCsgShapePlanetHillAttributes hillAttributes = FillValues(planetGeneratorDefinition.HillParams, random);
                    MyCsgShapePlanetHillAttributes canyonAttributes = FillValues(planetGeneratorDefinition.CanyonParams, random);

                    float planetHalfDeviation = (shapeAttributes.Diameter * shapeAttributes.DeviationScale) / 2.0f;
                    float averagePlanetRadius = shapeAttributes.Diameter * (1 - shapeAttributes.DeviationScale * hillAttributes.SizeRatio) / 2.0f;

                    float hillHalfDeviation = planetHalfDeviation * hillAttributes.SizeRatio;
                    float canyonHalfDeviation = planetHalfDeviation * canyonAttributes.SizeRatio;

                    float outerRadius = averagePlanetRadius + hillHalfDeviation * 1.5f;
                    float innerRadius = averagePlanetRadius - canyonHalfDeviation * 2.5f;

                    float atmosphereRadius = MathHelper.Max(outerRadius, averagePlanetRadius * 1.06f);
                    float minPlanetRadius = MathHelper.Min(innerRadius, averagePlanetRadius - planetHalfDeviation * 2 * 2.5f);

                    MyCsgShapePlanetMaterialAttributes materialAttributes = new MyCsgShapePlanetMaterialAttributes();
                    materialAttributes.OreStartDepth = innerRadius - random.NextFloat(planetGeneratorDefinition.MaterialsMinDepth.Min, planetGeneratorDefinition.MaterialsMinDepth.Max);
                    materialAttributes.OreEndDepth = innerRadius - random.NextFloat(planetGeneratorDefinition.MaterialsMaxDepth.Min, planetGeneratorDefinition.MaterialsMaxDepth.Max);
                    materialAttributes.OreEndDepth = MathHelper.Max(materialAttributes.OreEndDepth, 0);
                    materialAttributes.OreStartDepth = MathHelper.Max(materialAttributes.OreStartDepth, 0);

                    bool isHostile = random.NextFloat(0, 1) < planetGeneratorDefinition.HostilityProbability;
                    MyMaterialLayer[] materialLayers = CreateMaterialLayers(planetGeneratorDefinition, isHostile, random, averagePlanetRadius, hillHalfDeviation, canyonHalfDeviation, ref outerRadius, ref innerRadius);


                    materialAttributes.Layers = materialLayers;
                    materialAttributes.OreProbabilities = new MyOreProbability[m_oreProbalities.Count];

                    for (int i = 0; i < m_oreProbalities.Count; ++i)
                    {
                        materialAttributes.OreProbabilities[i] = m_oreProbalities[i];
                        materialAttributes.OreProbabilities[i].CummulativeProbability /= m_oreCummulativeProbability;
                    }

                    IMyStorage storage = new MyOctreeStorage(MyCompositeShapeProvider.CreatePlanetShape(0, ref shapeAttributes, ref hillAttributes, ref canyonAttributes, ref materialAttributes), FindBestOctreeSize(size));

                    float redAtmosphereShift = isHostile ? random.NextFloat(planetGeneratorDefinition.HostileAtmosphereColorShift.R.Min, planetGeneratorDefinition.HostileAtmosphereColorShift.R.Max) : 0;
                    float greenAtmosphereShift = isHostile ? random.NextFloat(planetGeneratorDefinition.HostileAtmosphereColorShift.G.Min, planetGeneratorDefinition.HostileAtmosphereColorShift.G.Max) : 0;
                    float blueAtmosphereShift = isHostile ? random.NextFloat(planetGeneratorDefinition.HostileAtmosphereColorShift.B.Min, planetGeneratorDefinition.HostileAtmosphereColorShift.B.Max) : 0;

                    Vector3 atmosphereWavelengths = new Vector3(0.650f + redAtmosphereShift, 0.570f + greenAtmosphereShift, 0.475f + blueAtmosphereShift);

                    atmosphereWavelengths.X = MathHelper.Clamp(atmosphereWavelengths.X, 0.1f, 1.0f);
                    atmosphereWavelengths.Y = MathHelper.Clamp(atmosphereWavelengths.Y, 0.1f, 1.0f);
                    atmosphereWavelengths.Z = MathHelper.Clamp(atmosphereWavelengths.Z, 0.1f, 1.0f);

                    float gravityFalloff = random.NextFloat(planetGeneratorDefinition.GravityFalloffPower.Min, planetGeneratorDefinition.GravityFalloffPower.Max);

                    var voxelMap = new MyPlanet();
                    voxelMap.EntityId = entityId;

                    MyPlanetInitArguments planetInitArguments;
                    planetInitArguments.StorageName = storageName;
                    planetInitArguments.Storage = storage;
                    planetInitArguments.PositionMinCorner = positionMinCorner;
                    planetInitArguments.AveragePlanetRadius = averagePlanetRadius;
                    planetInitArguments.AtmosphereRadius = atmosphereRadius;
                    planetInitArguments.MaximumHillRadius = averagePlanetRadius + hillHalfDeviation;
                    planetInitArguments.MinimumSurfaceRadius = minPlanetRadius;
                    planetInitArguments.HasAtmosphere = planetGeneratorDefinition.HasAtmosphere;
                    planetInitArguments.AtmosphereWavelengths = atmosphereWavelengths;
                    planetInitArguments.MaxOxygen = isHostile ? 0.0f : 1.0f;
                    planetInitArguments.GravityFalloff = gravityFalloff;
                    planetInitArguments.MarkAreaEmpty = false;

                    voxelMap.Init(planetInitArguments);

                    MyEntities.Add(voxelMap);

                    m_materialsByOreType.Clear();
                    m_oreProbalities.Clear();
                    m_spawningMaterials.Clear();
                    m_organicMaterials.Clear();

                    return voxelMap;
                }
            }
            return null;
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

        private static MyMaterialLayer[] CreateMaterialLayers(MyPlanetGeneratorDefinition planetDefinition, bool isHostile, MyRandom random, float averagePlanetRadius, float hillHalfDeviation, float canyonHalfDeviation, ref float outerRadius, ref float innerRadius)
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
            float floraMaterialSpawnProbability = random.NextFloat(planetDefinition.FloraMaterialSpawnProbability.Min, planetDefinition.FloraMaterialSpawnProbability.Max);
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

        private static void BuildOreProbabilities(MyPlanetGeneratorDefinition planetDefinition)
        {
            m_oreCummulativeProbability = 0.0f;
            if (planetDefinition.MetalsOreProbability != null)
            {
                foreach (var oreProbability in planetDefinition.MetalsOreProbability)
                {
                    MyOreProbability probability = new MyOreProbability();

                    m_oreCummulativeProbability += MyRandom.Instance.NextFloat(oreProbability.Min, oreProbability.Max);
                    probability.CummulativeProbability = m_oreCummulativeProbability;
                    probability.OreName = oreProbability.OreName;
                    m_oreProbalities.Add(probability);
                }
            }
        }
    }
}
