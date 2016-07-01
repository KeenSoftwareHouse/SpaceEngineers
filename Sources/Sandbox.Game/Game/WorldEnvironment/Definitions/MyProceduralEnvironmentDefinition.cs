using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.WorldEnvironment.ObjectBuilders;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Library.Collections;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Game.WorldEnvironment.Definitions
{
    #region Mappings

    public struct MyBiomeMaterial
    {
        public readonly byte Biome;
        public readonly byte Material;

        public MyBiomeMaterial(byte biome, byte material)
        {
            Biome = biome;
            Material = material;
        }

        public override int GetHashCode()
        {
            return ((Biome << 8) | Material).GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("Biome[{0}]:{1}", Biome, MyDefinitionManager.Static.GetVoxelMaterialDefinition(Material).Id.SubtypeName);
        }

        private class MyComparer : IEqualityComparer<MyBiomeMaterial>
        {
            public unsafe bool Equals(MyBiomeMaterial x, MyBiomeMaterial y)
            {
                ushort* a = (ushort*)&x;
                ushort* b = (ushort*)&y;

                return *a == *b;
            }

            public unsafe int GetHashCode(MyBiomeMaterial obj)
            {
                return (*(ushort*)&obj).GetHashCode();
            }
        }

        public static IEqualityComparer<MyBiomeMaterial> Comparer = new MyComparer();
    }

    public class MyEnvironmentItemMapping
    {
        // Items that may be spawned.
        public MyDiscreteSampler<MyRuntimeEnvironmentItemInfo>[] Samplers;
        public int[] Keys;

        // Rule for this entry.
        public MyEnvironmentRule Rule;

        public MyEnvironmentItemMapping(MyRuntimeEnvironmentItemInfo[] map, MyEnvironmentRule rule, MyProceduralEnvironmentDefinition env)
        {
            Rule = rule;

            SortedDictionary<int, List<MyRuntimeEnvironmentItemInfo>> infos = new SortedDictionary<int, List<MyRuntimeEnvironmentItemInfo>>();

            foreach (var item in map)
            {
                var def = item.Type;

                List<MyRuntimeEnvironmentItemInfo> lodItems;
                // We store in the prev slot because of the binary search bias
                if (!infos.TryGetValue(def.LodFrom + 1, out lodItems))
                {
                    lodItems = new List<MyRuntimeEnvironmentItemInfo>();
                    infos[def.LodFrom + 1] = lodItems;
                }

                lodItems.Add(item);
            }

            Keys = infos.Keys.ToArray();
            List<MyRuntimeEnvironmentItemInfo>[] itemInfos = infos.Values.ToArray();

            Samplers = new MyDiscreteSampler<MyRuntimeEnvironmentItemInfo>[Keys.Length];

            int i = 0;
            for (int index = 0; index < Keys.Length; ++index)
            {
                Samplers[index] = PrepareSampler(itemInfos.Range(index, itemInfos.Length).SelectMany(x => x));
            }
        }

        public MyDiscreteSampler<MyRuntimeEnvironmentItemInfo> PrepareSampler(IEnumerable<MyRuntimeEnvironmentItemInfo> items)
        {
            float sum = 0;
            foreach (var item in items)
            {
                sum += item.Density;
            }

            if (sum < 1)
            {
                return new MyDiscreteSampler<MyRuntimeEnvironmentItemInfo>(items.Concat(new MyRuntimeEnvironmentItemInfo[] { null }), items.Select(x => x.Density).Concat(new[] { 1 - sum }));
            }
            else
                return new MyDiscreteSampler<MyRuntimeEnvironmentItemInfo>(items, items.Select(x => x.Density));
        }

        /**
         * Given a value between 0 and 1 this will return the id of a vegetation item in which
         * range the value falls.
         *
         * If the value of rate is uniformly distributed then the definitions will be distributed
         * according to their defined densities.
         */
        public MyRuntimeEnvironmentItemInfo GetItemRated(int lod, float rate)
        {
            int index = Keys.BinaryIntervalSearch(lod);
            if (index > Samplers.Length) return null;
            return Samplers[index].Sample(rate);
        }

        /**
         * Weather this mapping is valid.
         */
        public bool Valid
        {
            get
            {
                return Samplers != null;
            }
        }

        public bool ValidForLod(int lod)
        {
            int index = Keys.BinaryIntervalSearch(lod);
            if (index > Samplers.Length) return false;
            return true;
        }

        public MyDiscreteSampler<MyRuntimeEnvironmentItemInfo> Sampler(int lod)
        {
            int index = Keys.BinaryIntervalSearch(lod);
            if (index >= Samplers.Length) return null;
            return Samplers[index];
        }
    }

    #endregion

    public class MyItemTypeDefinition
    {
        public string Name;

        public int LodFrom;

        public struct Module
        {
            public Type Type;
            public MyDefinitionId Definition;
        }

        public Module StorageModule;

        public Module[] ProxyModules;

        public MyItemTypeDefinition(MyEnvironmentItemTypeDefinition def)
        {
            Name = def.Name;

            LodFrom = def.LodFrom == -1 ? MyEnvironmentSectorConstants.MaximumLod : def.LodFrom;

            if (def.Provider.HasValue)
            {
                var smod = MyDefinitionManager.Static.GetDefinition<MyProceduralEnvironmentModuleDefinition>(def.Provider.Value);
                if (smod == null)
                    MyLog.Default.Error("Could not find module definition for type {0}.", def.Provider.Value);
                else
                {
                    StorageModule.Type = smod.ModuleType;
                    StorageModule.Definition = def.Provider.Value;
                }
            }

            if (def.Proxies != null)
            {
                List<Module> proxies = new List<Module>();
                foreach (var proxy in def.Proxies)
                {
                    var pmod = MyDefinitionManager.Static.GetDefinition<MyEnvironmentModuleProxyDefinition>(proxy);
                    if (pmod == null)
                        MyLog.Default.Error("Could not find proxy module definition for type {0}.", proxy);
                    else
                        proxies.Add(new Module
                        {
                            Type = pmod.ModuleType,
                            Definition = proxy
                        });
                }

                proxies.Capacity = proxies.Count;

                ProxyModules = proxies.GetInternalArray();
            }

        }
    }

    public class MyRuntimeEnvironmentItemInfo
    {
        public MyItemTypeDefinition Type;

        public MyStringHash Subtype;

        public float Offset;

        public float Density;

        public short Index;

        public MyRuntimeEnvironmentItemInfo(MyProceduralEnvironmentDefinition def, MyEnvironmentItemInfo info, int id)
        {
            Index = (short)id;

            Type = def.ItemTypes[info.Type];

            Subtype = info.Subtype;
            Offset = info.Offset;
            Density = info.Density;
        }
    }

    public class MyEnvironmentRule
    {
        public SerializableRange Height = new SerializableRange(0, 1);

        public SymetricSerializableRange Latitude = new SymetricSerializableRange(-90, 90);

        public SerializableRange Longitude = new SerializableRange(-180, 180);

        public SerializableRange Slope = new SerializableRange(0, 90);

        public void ConvertRanges()
        {
            Latitude.ConvertToSine();
            Longitude.ConvertToCosineLongitude();
            Slope.ConvertToCosine();
        }

        /**
         * Check that a rule matches terrain properties.
         *
         * @param height Height ration to the height map.
         * @param latitude Latitude cosine
         * @param slope Surface dominant angle cosine.
         */
        public bool Check(float height, float latitude, float longitude, float slope)
        {
            return Height.ValueBetween(height) && Latitude.ValueBetween(latitude) && Longitude.ValueBetween(longitude)
                         && Slope.ValueBetween(slope);
        }
    }

    [MyDefinitionType(typeof(MyObjectBuilder_ProceduralWorldEnvironment), typeof(MyProceduralEnvironmentDefinitionPostprocessor))]
    public class MyProceduralEnvironmentDefinition : MyWorldEnvironmentDefinition
    {
        private static readonly int[] ArrayOfZero = { 0 };
        private MyObjectBuilder_ProceduralWorldEnvironment m_ob;

        #region Public

        public Dictionary<string, MyItemTypeDefinition> ItemTypes = new Dictionary<string, MyItemTypeDefinition>();

        public Dictionary<MyBiomeMaterial, List<MyEnvironmentItemMapping>> MaterialEnvironmentMappings;

        public MyProceduralScanningMethod ScanningMethod;

        #endregion

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);

            MyObjectBuilder_ProceduralWorldEnvironment ob = (MyObjectBuilder_ProceduralWorldEnvironment)builder;

            m_ob = ob;

            ScanningMethod = ob.ScanningMethod;
        }

        public void Prepare()
        {
            // Item types
            if (m_ob.ItemTypes != null)
                foreach (var item in m_ob.ItemTypes)
                {
                    try
                    {
                        var itemType = new MyItemTypeDefinition(item);
                        ItemTypes.Add(item.Name, itemType);
                    }
                    catch (ArgumentException)
                    {
                        MyLog.Default.Error("Duplicate environment item definition for item {0}.", item.Name);
                    }
                    catch (Exception e)
                    {
                        MyLog.Default.Error("Error preparing environment item definition for item {0}:\n {1}", item.Name, e.Message);
                    }
                }

            // Mappings
            MaterialEnvironmentMappings = new Dictionary<MyBiomeMaterial, List<MyEnvironmentItemMapping>>(MyBiomeMaterial.Comparer);

            List<MyRuntimeEnvironmentItemInfo> items = new List<MyRuntimeEnvironmentItemInfo>();

            var mappings = m_ob.EnvironmentMappings;

            if (mappings != null && mappings.Length > 0)
            {
                MaterialEnvironmentMappings = new Dictionary<MyBiomeMaterial, List<MyEnvironmentItemMapping>>(MyBiomeMaterial.Comparer);

                for (int i = 0; i < mappings.Length; i++)
                {
                    var map = mappings[i];

                    var rule = new MyEnvironmentRule
                    {
                        Height = map.Height,
                        Slope = map.Slope,
                        Latitude = map.Latitude,
                        Longitude = map.Longitude
                    };

                    // If the mapping does not assign a material it is ignored
                    if (map.Materials == null)
                    {
                        MyLog.Default.Warning("Mapping in definition {0} does not define any materials, it will not be applied.", Id);
                        continue;
                    }

                    // If not biomes we take default
                    if (map.Biomes == null) map.Biomes = ArrayOfZero;

                    // Check items if they are valid
                    bool anyAdded = false;
                    var ruleItems = new MyRuntimeEnvironmentItemInfo[map.Items.Length];
                    for (int j = 0; j < map.Items.Length; ++j)
                    {
                        if (!ItemTypes.ContainsKey(map.Items[j].Type))
                        {
                            MyLog.Default.Error("No definition for item type {0}", map.Items[j].Type);
                        }
                        else
                        {
                            ruleItems[j] = new MyRuntimeEnvironmentItemInfo(this, map.Items[j], items.Count);
                            items.Add(ruleItems[j]);
                            anyAdded = true;
                        }
                    }

                    // if no items were valid we skip this rule.
                    if (!anyAdded) continue;

                    var mapping = new MyEnvironmentItemMapping(ruleItems, rule, this);

                    foreach (var biome in map.Biomes)
                    {
                        foreach (var material in map.Materials)
                        {
                            MyBiomeMaterial bm = new MyBiomeMaterial((byte)biome, MyDefinitionManager.Static.GetVoxelMaterialDefinition(material).Index);

                            List<MyEnvironmentItemMapping> mappingList;
                            if (!MaterialEnvironmentMappings.TryGetValue(bm, out mappingList))
                            {
                                mappingList = new List<MyEnvironmentItemMapping>();
                                MaterialEnvironmentMappings[bm] = mappingList;
                            }

                            mappingList.Add(mapping);
                        }
                    }
                }
            }

            Items = items.GetInternalArray();

            m_ob = null;
        }

        public override Type SectorType
        {
            get
            {
                return typeof(MyEnvironmentSector);
            }
        }

        // Prepare an environment definition from legacy planet definitions.
        public static MyWorldEnvironmentDefinition FromLegacyPlanet(MyObjectBuilder_PlanetGeneratorDefinition pgdef, MyModContext context)
        {
            var envOb = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ProceduralWorldEnvironment>(pgdef.Id.SubtypeId); ;

            envOb.Id = new SerializableDefinitionId(envOb.TypeId, envOb.SubtypeName);

            // storage
            var staticModule = new SerializableDefinitionId(typeof(MyObjectBuilder_ProceduralEnvironmentModuleDefinition), "Static");

            var memoryModule = new SerializableDefinitionId(typeof(MyObjectBuilder_ProceduralEnvironmentModuleDefinition), "Memory");

            // proxies
            var breakable = new SerializableDefinitionId(typeof(MyObjectBuilder_EnvironmentModuleProxyDefinition), "Breakable");

            var voxelmap = new SerializableDefinitionId(typeof(MyObjectBuilder_EnvironmentModuleProxyDefinition), "VoxelMap");

            var botSpawner = new SerializableDefinitionId(typeof(MyObjectBuilder_EnvironmentModuleProxyDefinition), "BotSpawner");

            // TODO: Implement environmental particles.
            var environmentalParticleMarker = new SerializableDefinitionId(typeof(MyObjectBuilder_EnvironmentModuleProxyDefinition), "EnvironmentalParticles");

            envOb.ItemTypes = new[]
            {
                new MyEnvironmentItemTypeDefinition()
                {
                    LodFrom = -1,
                    Name = "Tree",
                    Provider = staticModule,
                    Proxies = new []{breakable}
                },
                new MyEnvironmentItemTypeDefinition()
                {
                    LodFrom = 0,
                    Name = "Bush",
                    Provider = staticModule,
                    Proxies = new []{breakable}
                },
                new MyEnvironmentItemTypeDefinition()
                {
                    LodFrom = 0,
                    Name = "VoxelMap",
                    Provider = memoryModule,
                    Proxies = new []{voxelmap}
                },
                new MyEnvironmentItemTypeDefinition()
                {
                    LodFrom = 0,
                    Name = "Bot",
                    Provider = null,
                    Proxies = new []{botSpawner}
                },
            };

            envOb.ScanningMethod = MyProceduralScanningMethod.Random;
            envOb.ItemsPerSqMeter = 0.0017;

            envOb.MaxSyncLod = 0;
            envOb.SectorSize = 384;

            List<MyProceduralEnvironmentMapping> mappings = new List<MyProceduralEnvironmentMapping>();

            List<MyEnvironmentItemInfo> items = new List<MyEnvironmentItemInfo>();

            var defaultRule = new MyPlanetSurfaceRule();

            if(pgdef.EnvironmentItems != null)
            foreach (var matmap in pgdef.EnvironmentItems)
            {
                var map = new MyProceduralEnvironmentMapping();
                map.Biomes = matmap.Biomes;
                map.Materials = matmap.Materials;

                var rule = matmap.Rule ?? defaultRule;

                map.Height = rule.Height;
                map.Latitude = rule.Latitude;
                map.Longitude = rule.Longitude;
                map.Slope = rule.Slope;

                items.Clear();
                foreach (var item in matmap.Items)
                {
                    var it = new MyEnvironmentItemInfo
                    {
                        Density = item.Density,
                        Subtype = MyStringHash.GetOrCompute(item.SubtypeId)
                    };

                    switch (item.TypeId)
                    {
                        case "MyObjectBuilder_DestroyableItems":
                            it.Type = "Bush";
                            break;

                        case "MyObjectBuilder_Trees":
                            it.Type = "Tree";
                            break;

                        case "MyObjectBuilder_VoxelMapStorageDefinition":
                            it.Type = "VoxelMap";

                            if (item.SubtypeId == null)
                            {
                                var subtype = MyStringHash.GetOrCompute(string.Format("G({0})M({1})", item.GroupId, item.ModifierId));

                                var vcolDef = MyDefinitionManager.Static.GetDefinition<MyVoxelMapCollectionDefinition>(subtype);

                                if (vcolDef == null)
                                {
                                    var vdefOb = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_VoxelMapCollectionDefinition>(subtype.ToString());

                                    vdefOb.Id = new SerializableDefinitionId(vdefOb.TypeId, vdefOb.SubtypeName);

                                    vdefOb.StorageDefs = new MyObjectBuilder_VoxelMapCollectionDefinition.VoxelMapStorage[1]
                                    {
                                        new MyObjectBuilder_VoxelMapCollectionDefinition.VoxelMapStorage()
                                        {
                                            Storage = item.GroupId
                                        }
                                    };

                                    vdefOb.Modifier = item.ModifierId;

                                    vcolDef = new MyVoxelMapCollectionDefinition();
                                    vcolDef.Init(vdefOb, context);

                                    MyDefinitionManager.Static.Definitions.AddDefinition(vcolDef);
                                }

                                it.Subtype = subtype;
                            }

                            break;
                        default:
                            MyLog.Default.Error("Planet Generator {0}: Invalid Item Type: {1}", pgdef.SubtypeName, item.SubtypeId);
                            continue;
                            break;
                    }

                    if (it.Subtype == null)
                    {
                        MyLog.Default.Error("Planet Generator {0}: Missing subtype for item of type {1}", pgdef.SubtypeName, it.Type);
                        continue;
                    }

                    items.Add(it);
                }

                map.Items = items.ToArray();

                mappings.Add(map);
            }

            mappings.Capacity = mappings.Count;

            envOb.EnvironmentMappings = mappings.GetInternalArray();

            var def = new MyProceduralEnvironmentDefinition();
            def.Context = context;
            def.Init(envOb);

            return def;
        }

        public void GetItemDefinition(ushort definitionIndex, out MyRuntimeEnvironmentItemInfo def)
        {
            Debug.Assert(definitionIndex < Items.Length);

            if (definitionIndex >= Items.Length)
                def = null;
            else def = Items[definitionIndex];
        }
    }

    public class MyProceduralEnvironmentDefinitionPostprocessor : MyDefinitionPostprocessor
    {
        public override void AfterLoaded(ref Bundle definitions)
        {
        }

        public override void AfterPostprocess(MyDefinitionSet set, Dictionary<MyStringHash, MyDefinitionBase> definitions)
        {
            foreach (var def in definitions)
            {
                ((MyProceduralEnvironmentDefinition)def.Value).Prepare();
            }
        }
    }
}
