using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.WorldEnvironment;
using Sandbox.Game.WorldEnvironment.Definitions;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Library.Utils;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace Sandbox.Definitions
{

    public class MyMaterialEnvironmentItem
    {
        public MyDefinitionId Definition;
        public string GroupId;
        public int GroupIndex;
        public string ModifierId;
        public int ModifierIndex;
        public float Frequency;

        private bool m_detail;
        public bool IsDetail
        {
            set { m_detail = value; }
            get { return m_detail || IsBot || IsVoxel; }
        }
        public bool IsBot;
        public bool IsVoxel;
        public bool IsEnvironemntItem;

        public Vector3 BaseColor;
        public Vector2 ColorSpread;

        public float Offset;

        public float MaxRoll;
    }

    public class MyPlanetEnvironmentMapping
    {
        // Items that may be spawned.
        public MyMaterialEnvironmentItem[] Items;

        // Rule for this entry.
        public MyPlanetSurfaceRule Rule;

        // Cumulative frequency distribution.
        private float[] CumulativeIntervals;

        // Total frequency.
        public float TotalFrequency;

        public MyPlanetEnvironmentMapping(PlanetEnvironmentItemMapping map)
        {
            Rule = map.Rule;
            Items = new MyMaterialEnvironmentItem[map.Items.Length];
            if (Items.Length <= 0)
            {
                CumulativeIntervals = null;
                TotalFrequency = 0;
                return;
            }

            TotalFrequency = 0;
            for (int i = 0; i < map.Items.Length; i++)
            {
                var item = map.Items[i];

                MyObjectBuilderType type;
                if (item.TypeId != null && MyObjectBuilderType.TryParse(item.TypeId, out type))
                {
                    if (!typeof(MyObjectBuilder_BotDefinition).IsAssignableFrom((Type)type) && !typeof(MyObjectBuilder_VoxelMapStorageDefinition).IsAssignableFrom((Type)type) && !typeof(MyObjectBuilder_EnvironmentItems).IsAssignableFrom((Type)type))
                    {
                        MyLog.Default.WriteLine(String.Format("Object builder type {0} is not supported for environment items.", item.TypeId));
                        Items[i].Frequency = 0; // This should disable this item
                    }
                    else
                    {
                        Items[i] = new MyMaterialEnvironmentItem()
                        {
                            Definition = new MyDefinitionId(type, item.SubtypeId),
                            Frequency = map.Items[i].Density,
                            IsDetail = map.Items[i].IsDetail,
                            IsBot = typeof(MyObjectBuilder_BotDefinition).IsAssignableFrom((Type)type),
                            IsVoxel = typeof(MyObjectBuilder_VoxelMapStorageDefinition).IsAssignableFrom((Type)type),
                            IsEnvironemntItem = typeof(MyObjectBuilder_EnvironmentItems).IsAssignableFrom((Type)type),
                            BaseColor = map.Items[i].BaseColor,
                            ColorSpread = map.Items[i].ColorSpread,
                            MaxRoll = (float)Math.Cos(MathHelper.ToDegrees(map.Items[i].MaxRoll)),
                            Offset = map.Items[i].Offset,
                            GroupId = map.Items[i].GroupId,
                            GroupIndex = map.Items[i].GroupIndex,
                            ModifierId = map.Items[i].ModifierId,
                            ModifierIndex = map.Items[i].ModifierIndex
                        };
                    }
                }
                else
                {
                    MyLog.Default.WriteLine(String.Format("Object builder type {0} does not exist.", item.TypeId));
                    Items[i].Frequency = 0; // This should disable this item
                }
            }

            ComputeDistribution();
        }

        public void ComputeDistribution()
        {
            if (!Valid)
            {
                TotalFrequency = 0;
                CumulativeIntervals = null;
                return;
            }

            TotalFrequency = 0;

            for (int i = 0; i < Items.Length; ++i)
            {
                TotalFrequency += Items[i].Frequency;
            }

            CumulativeIntervals = new float[Items.Length - 1];

            float prev = 0;
            for (int i = 0; i < CumulativeIntervals.Length; i++)
            {
                CumulativeIntervals[i] = prev + Items[i].Frequency / TotalFrequency;
                prev = CumulativeIntervals[i];
            }
        }

        /**
         * Given a value between 0 and 1 this will return the id of a vegetation item in which's
         * range the value falls.
         * 
         * If the value of rate is uniformly distributed then the definitions will be distributed
         * according to their defined densities.
         */
        public int GetItemRated(float rate)
        {
            return CumulativeIntervals.BinaryIntervalSearch(rate);
        }

        /**
         * Weather this mapping is valid.
         */
        public bool Valid
        {
            get
            {
                return Items != null && Items.Length > 0;
            }
        }
    }

    public struct MyPlanetEnvironmentalSoundRule
    {
        public SymetricSerializableRange Latitude;
        public SerializableRange Height;
        public SerializableRange SunAngleFromZenith;

        public MyStringHash EnvironmentSound;

        public bool Check(float angleFromEquator, float height, float sunAngleFromZenith)
        {
            return Latitude.ValueBetween(angleFromEquator) && Height.ValueBetween(height) && SunAngleFromZenith.ValueBetween(sunAngleFromZenith);
        }
    }

    [MyDefinitionType(typeof(MyObjectBuilder_PlanetGeneratorDefinition), typeof(Postprocessor))]
    public class MyPlanetGeneratorDefinition : MyDefinitionBase
    {
        public MyDefinitionId? EnvironmentId;

        public MyWorldEnvironmentDefinition EnvironmentDefinition;

        private MyObjectBuilder_PlanetGeneratorDefinition m_pgob;

        public bool HasAtmosphere = false;

        public List<MyCloudLayerSettings> CloudLayers;

        public MyPlanetMaps PlanetMaps;

        public SerializableRange HillParams = new SerializableRange();

        public SerializableRange MaterialsMaxDepth = new SerializableRange();

        public SerializableRange MaterialsMinDepth = new SerializableRange();

        public MyPlanetOreMapping[] OreMappings = new MyPlanetOreMapping[0];

        public float GravityFalloffPower = 7.0f;

        public MyAtmosphereColorShift HostileAtmosphereColorShift = new MyAtmosphereColorShift();

        public MyPlanetMaterialDefinition[] SurfaceMaterialTable = new MyPlanetMaterialDefinition[0];

        public MyPlanetDistortionDefinition[] DistortionTable = new MyPlanetDistortionDefinition[0];

        public MyPlanetMaterialDefinition DefaultSurfaceMaterial;

        public MyPlanetMaterialDefinition DefaultSubSurfaceMaterial;

        public MyPlanetEnvironmentalSoundRule[] SoundRules;

        public List<MyMusicCategory> MusicCategories = null;

        // May need some acceleration on the rules.
        public MyPlanetMaterialGroup[] MaterialGroups = new MyPlanetMaterialGroup[0];

        public Dictionary<int, Dictionary<string, List<MyPlanetEnvironmentMapping>>> MaterialEnvironmentMappings = new Dictionary<int, Dictionary<string, List<MyPlanetEnvironmentMapping>>>();

        public float SurfaceGravity = 1.0f;

        public float AtmosphereHeight;

        public float SectorDensity = 0.0017f;

        public MyPlanetAtmosphere Atmosphere = new MyPlanetAtmosphere();

        public MyAtmosphereSettings? AtmosphereSettings;

        public MyPlanetMaterialBlendSettings MaterialBlending = new MyPlanetMaterialBlendSettings()
        {
            Texture = "Data/PlanetDataFiles/Extra/material_blend_grass",
            CellSize = 64
        };

        public string FolderName;

        public MyPlanetSurfaceDetail Detail = null;
        // Description of animal spawning (only during the day if NightAnimalSpawnInfo is defined)
        public MyPlanetAnimalSpawnInfo AnimalSpawnInfo = null;
        // If defined, it is used in night instead of AnimalSpawnInfo
        public MyPlanetAnimalSpawnInfo NightAnimalSpawnInfo = null;

        public Type EnvironmentSectorType;

        private void InheritFrom(string generator)
        {
            MyPlanetGeneratorDefinition parent = MyDefinitionManager.Static.GetDefinition<MyPlanetGeneratorDefinition>(MyStringHash.GetOrCompute(generator));

            if (parent == null)
            {
                MyDefinitionManager.Static.LoadingSet.m_planetGeneratorDefinitions.TryGetValue(new MyDefinitionId(typeof(MyObjectBuilder_PlanetGeneratorDefinition), generator), out parent);
            }

            if (parent == null)
            {
                MyLog.Default.WriteLine(String.Format("Could not find planet generator definition for '{0}'.", generator));
                return;
            }

            PlanetMaps = parent.PlanetMaps;
            HasAtmosphere = parent.HasAtmosphere;
            Atmosphere = parent.Atmosphere;
            CloudLayers = parent.CloudLayers;
            SoundRules = parent.SoundRules;
            MusicCategories = parent.MusicCategories;
            HillParams = parent.HillParams;
            MaterialsMaxDepth = parent.MaterialsMaxDepth;
            MaterialsMinDepth = parent.MaterialsMinDepth;
            GravityFalloffPower = parent.GravityFalloffPower;
            HostileAtmosphereColorShift = parent.HostileAtmosphereColorShift;
            SurfaceMaterialTable = parent.SurfaceMaterialTable;
            DistortionTable = parent.DistortionTable;
            DefaultSurfaceMaterial = parent.DefaultSurfaceMaterial;
            DefaultSubSurfaceMaterial = parent.DefaultSubSurfaceMaterial;
            MaterialGroups = parent.MaterialGroups;
            MaterialEnvironmentMappings = parent.MaterialEnvironmentMappings;
            SurfaceGravity = parent.SurfaceGravity;
            AtmosphereSettings = parent.AtmosphereSettings;
            FolderName = parent.FolderName;
            MaterialBlending = parent.MaterialBlending;
            OreMappings = parent.OreMappings;
            AnimalSpawnInfo = parent.AnimalSpawnInfo;
            NightAnimalSpawnInfo = parent.NightAnimalSpawnInfo;
            Detail = parent.Detail;
            SectorDensity = parent.SectorDensity;
            EnvironmentSectorType = parent.EnvironmentSectorType;
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_PlanetGeneratorDefinition;

            if (ob.InheritFrom != null && ob.InheritFrom.Length > 0)
            {
                InheritFrom(ob.InheritFrom);
            }

            if (ob.Environment.HasValue)
            {
                EnvironmentId = ob.Environment.Value;
            }
            else
            {
                m_pgob = ob;
            }

            if (ob.PlanetMaps.HasValue)
                PlanetMaps = ob.PlanetMaps.Value;

            if (ob.HasAtmosphere.HasValue) HasAtmosphere = ob.HasAtmosphere.Value;

            if (ob.CloudLayers != null)
                CloudLayers = ob.CloudLayers;

            if (ob.SoundRules != null)
            {
                SoundRules = new MyPlanetEnvironmentalSoundRule[ob.SoundRules.Length];

                for (int ruleIndex = 0; ruleIndex < ob.SoundRules.Length; ++ruleIndex)
                {
                    MyPlanetEnvironmentalSoundRule sr;

                    sr = new MyPlanetEnvironmentalSoundRule()
                    {
                        Latitude = ob.SoundRules[ruleIndex].Latitude,
                        Height = ob.SoundRules[ruleIndex].Height,
                        SunAngleFromZenith = ob.SoundRules[ruleIndex].SunAngleFromZenith,
                        EnvironmentSound = MyStringHash.GetOrCompute(ob.SoundRules[ruleIndex].EnvironmentSound)
                    };

                    sr.Latitude.ConvertToSine();
                    sr.SunAngleFromZenith.ConvertToCosine();
                    SoundRules[ruleIndex] = sr;
                }
            }
            if (ob.MusicCategories != null)
                MusicCategories = ob.MusicCategories;

            if (ob.HillParams.HasValue)
                HillParams = ob.HillParams.Value;

            if (ob.Atmosphere != null)
                Atmosphere = ob.Atmosphere;

            if (ob.GravityFalloffPower.HasValue) GravityFalloffPower = ob.GravityFalloffPower.Value;

            if (ob.HostileAtmosphereColorShift != null)
                HostileAtmosphereColorShift = ob.HostileAtmosphereColorShift;

            if (ob.MaterialsMaxDepth.HasValue)
                MaterialsMaxDepth = ob.MaterialsMaxDepth.Value;
            if (ob.MaterialsMinDepth.HasValue)
                MaterialsMinDepth = ob.MaterialsMinDepth.Value;

            if (ob.CustomMaterialTable != null && ob.CustomMaterialTable.Length > 0)
            {
                SurfaceMaterialTable = new MyPlanetMaterialDefinition[ob.CustomMaterialTable.Length];
                for (int i = 0; i < SurfaceMaterialTable.Length; i++)
                {
                    SurfaceMaterialTable[i] = ob.CustomMaterialTable[i].Clone() as MyPlanetMaterialDefinition;
                    if (SurfaceMaterialTable[i].Material == null && !SurfaceMaterialTable[i].HasLayers)
                    {
                        MyLog.Default.WriteLine("Custom material does not contain any material ids.");
                    }
                    else if (SurfaceMaterialTable[i].HasLayers)
                    {
                        // Make the depth cumulative so we don't have to calculate it later.
                        // If we want we can even binary search it.
                        float depth = SurfaceMaterialTable[i].Layers[0].Depth;
                        for (int j = 1; j < SurfaceMaterialTable[i].Layers.Length; j++)
                        {
                            SurfaceMaterialTable[i].Layers[j].Depth += depth;
                            depth = SurfaceMaterialTable[i].Layers[j].Depth;
                        }
                    }
                }
            }

            if (ob.DistortionTable != null && ob.DistortionTable.Length > 0)
            {
                DistortionTable = ob.DistortionTable;
            }

            if (ob.DefaultSurfaceMaterial != null)
                DefaultSurfaceMaterial = ob.DefaultSurfaceMaterial;

            if (ob.DefaultSubSurfaceMaterial != null)
                DefaultSubSurfaceMaterial = ob.DefaultSubSurfaceMaterial;

            if (ob.SurfaceGravity.HasValue) SurfaceGravity = ob.SurfaceGravity.Value;

            if (ob.AtmosphereSettings != null)
                AtmosphereSettings = ob.AtmosphereSettings;

            // Folder name is not inherited to avoid weirdness.
            FolderName = ob.FolderName != null ? ob.FolderName : ob.Id.SubtypeName;

            if (ob.ComplexMaterials != null && ob.ComplexMaterials.Length > 0)
            {
                MaterialGroups = new MyPlanetMaterialGroup[ob.ComplexMaterials.Length];

                for (int k = 0; k < ob.ComplexMaterials.Length; k++)
                {
                    MaterialGroups[k] = ob.ComplexMaterials[k].Clone() as MyPlanetMaterialGroup;

                    var group = MaterialGroups[k];
                    var matRules = group.MaterialRules;
                    List<int> badMaterials = new List<int>();

                    for (int i = 0; i < matRules.Length; i++)
                    {
                        if (matRules[i].Material == null && (matRules[i].Layers == null || matRules[i].Layers.Length == 0))
                        {
                            MyLog.Default.WriteLine("Material rule does not contain any material ids.");
                            badMaterials.Add(i);
                            continue;
                        }
                        else if (matRules[i].Layers != null && matRules[i].Layers.Length != 0)
                        {
                            // Make the depth cumulative so we don't have to calculate it later.
                            // If we want we can even binary search it.
                            float depth = matRules[i].Layers[0].Depth;
                            for (int j = 1; j < matRules[i].Layers.Length; j++)
                            {
                                matRules[i].Layers[j].Depth += depth;
                                depth = matRules[i].Layers[j].Depth;
                            }
                        }

                        // We use the cosine later so we precompute it here.
                        matRules[i].Slope.ConvertToCosine();
                        matRules[i].Latitude.ConvertToSine();
                        matRules[i].Longitude.ConvertToCosineLongitude();
                    }

                    if (badMaterials.Count > 0)
                    {
                        matRules = matRules.RemoveIndices(badMaterials);
                    }

                    group.MaterialRules = matRules;
                }
            }

            /*if (ob.EnvironmentItems != null && ob.EnvironmentItems.Length > 0)
            {
                MaterialEnvironmentMappings = new Dictionary<int, Dictionary<string, List<MyPlanetEnvironmentMapping>>>();

                for (int i = 0; i < ob.EnvironmentItems.Length; i++)
                {
                    PlanetEnvironmentItemMapping map = ob.EnvironmentItems[i];
                    if (map.Rule != null)
                        map.Rule = map.Rule.Clone() as MyPlanetSurfaceRule;
                    else
                        map.Rule = new MyPlanetSurfaceRule();
                    map.Rule.Slope.ConvertToCosine();
                    map.Rule.Latitude.ConvertToSine();
                    map.Rule.Longitude.ConvertToCosineLongitude();

                    // If the mapping does not assign a material it is ignored
                    if (map.Materials == null) break;

                    if (map.Biomes == null) map.Biomes = m_arrayOfZero;

                    foreach (var biome in map.Biomes)
                    {
                        Dictionary<string, List<MyPlanetEnvironmentMapping>> matmap;

                        if (MaterialEnvironmentMappings.ContainsKey(biome))
                        {
                            matmap = MaterialEnvironmentMappings[biome];
                        }
                        else
                        {
                            matmap = new Dictionary<string, List<MyPlanetEnvironmentMapping>>();
                            MaterialEnvironmentMappings.Add(biome, matmap);
                        }

                        foreach (var material in map.Materials)
                        {
                            if (!matmap.ContainsKey(material))
                                matmap.Add(material, new List<MyPlanetEnvironmentMapping>());

                            matmap[material].Add(new MyPlanetEnvironmentMapping(map));
                        }
                    }
                }
            }*/

            if (ob.OreMappings != null)
            {
                OreMappings = ob.OreMappings;
            }

            if (ob.MaterialBlending.HasValue)
            {
                MaterialBlending = ob.MaterialBlending.Value;
            }

            if (ob.SurfaceDetail != null)
            {
                Detail = ob.SurfaceDetail;
            }

            if (ob.AnimalSpawnInfo != null)
            {
                AnimalSpawnInfo = ob.AnimalSpawnInfo;
            }

            if (ob.NightAnimalSpawnInfo != null)
            {
                NightAnimalSpawnInfo = ob.NightAnimalSpawnInfo;
            }

            if (ob.SectorDensity.HasValue)
            {
                SectorDensity = ob.SectorDensity.Value;
            }
        }

        public override string ToString()
        {
            string rtnString = base.ToString();
#if !XB1
            foreach (var prop in typeof(MyPlanetGeneratorDefinition).GetFields())
            {
                if (prop.IsPublic)
                {
                    var value = prop.GetValue(this);
                    rtnString = rtnString + "\n   " + prop.Name + " = " + (value ?? "<null>");
                }
            }
            foreach (var prop in typeof(MyPlanetGeneratorDefinition).GetProperties())
            {
                var value = prop.GetValue(this, null);
                rtnString = rtnString + "\n   " + prop.Name + " = " + (value ?? "<null>");
            }
#endif
            return rtnString;
        }

        internal class Postprocessor : MyDefinitionPostprocessor
        {
            public override int Priority
            {
                get { return 1000; }
            }

            // Called after definitions for the current mod context are loaded.
            public override void AfterLoaded(ref Bundle definitions)
            {
            }

            // Called after all definitions are merged to one set
            // Anything that may refere to other definitions from game or that may be modified by mods should be postprocessed here.
            public override void AfterPostprocess(MyDefinitionSet set, Dictionary<MyStringHash, MyDefinitionBase> definitions)
            {
                List<int> toRemove = new List<int>();

                foreach (var def in definitions.Values)
                {
                    var pgdef = (MyPlanetGeneratorDefinition)def;

                    // Legacy planet with automatically converted definition
                    if (!pgdef.EnvironmentId.HasValue)
                    {
                        pgdef.EnvironmentDefinition = MyProceduralEnvironmentDefinition.FromLegacyPlanet(pgdef.m_pgob, def.Context);
                        set.AddOrRelaceDefinition(pgdef.EnvironmentDefinition);

                        pgdef.m_pgob = null;
                    }
                    else
                    {
                        pgdef.EnvironmentDefinition = MyDefinitionManager.Static.GetDefinition<MyWorldEnvironmentDefinition>(pgdef.EnvironmentId.Value);
                    }

                    if (pgdef.EnvironmentDefinition == null)
                        continue;

                    pgdef.EnvironmentSectorType = pgdef.EnvironmentDefinition.SectorType;
                    Debug.Assert(typeof(MyEnvironmentSector).IsAssignableFrom(pgdef.EnvironmentSectorType));

                    foreach (var bmap in pgdef.MaterialEnvironmentMappings.Values)
                    {
                        foreach (var mmap in bmap.Values)
                        {
                            foreach (var env in mmap)
                            {
                                for (int i = 0; i < env.Items.Length; ++i)
                                {
                                    if (env.Items[i].IsEnvironemntItem)
                                    {
                                        MyEnvironmentItemsDefinition eidef;
                                        if (!MyDefinitionManager.Static.TryGetDefinition(env.Items[i].Definition, out eidef))
                                        {
                                            MyLog.Default.WriteLine(string.Format("Could not find environment item definition for {0}.", env.Items[i].Definition));
                                            toRemove.Add(i);
                                        }
                                    }
                                }

                                if (toRemove.Count > 0)
                                {
                                    env.Items = env.Items.RemoveIndices(toRemove);
                                    env.ComputeDistribution();
                                    toRemove.Clear();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
