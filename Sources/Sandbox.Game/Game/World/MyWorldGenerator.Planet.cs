using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using VRage.Collections;
using VRage.Library.Utils;
using VRageMath;
using VRage;
using VRage.FileSystem;
using Sandbox.Engine.Utils;
using VRage.Game;
using VRage.Utils;
using VRage.Voxels;
using VRageRender.Messages;

namespace Sandbox.Game.World
{
    public partial class MyWorldGenerator
    {

        public static void AddPlanetPrefab(string planetName,string definitionName,Vector3D position, bool addGPS)
        {
            DictionaryValuesReader<MyDefinitionId, MyPlanetPrefabDefinition> planetPrefabDefinitions = MyDefinitionManager.Static.GetPlanetsPrefabsDefinitions();
            foreach (var planetPrefabDefinition in planetPrefabDefinitions)
            {
                if (planetPrefabDefinition.Id.SubtypeName == planetName)
                {
                    MyPlanetGeneratorDefinition planetDefinition = MyDefinitionManager.Static.GetDefinition<MyPlanetGeneratorDefinition>(MyStringHash.GetOrCompute(definitionName));

                    var planet = new MyPlanet();
                    var ob = planetPrefabDefinition.PlanetBuilder;
             
                    string storageName = MyFileSystem.ContentPath + "\\VoxelMaps\\" + ob.StorageName + MyVoxelConstants.FILE_EXTENSION;
                    planet.EntityId = ob.EntityId;

                    MyPlanetInitArguments planetInitArguments;
                    planetInitArguments.StorageName = ob.StorageName;
                    planetInitArguments.Storage = MyStorageBase.LoadFromFile(storageName);
                    planetInitArguments.PositionMinCorner = position;
                    planetInitArguments.Radius = ob.Radius;
                    planetInitArguments.AtmosphereRadius = ob.AtmosphereRadius;
                    planetInitArguments.MaxRadius = ob.MaximumHillRadius;
                    planetInitArguments.MinRadius = ob.MinimumSurfaceRadius;
                    planetInitArguments.HasAtmosphere = planetDefinition.HasAtmosphere;
                    planetInitArguments.AtmosphereWavelengths = ob.AtmosphereWavelengths;
                    planetInitArguments.GravityFalloff = planetDefinition.GravityFalloffPower;
                    planetInitArguments.MarkAreaEmpty = true;
                    planetInitArguments.AtmosphereSettings = ob.AtmosphereSettings.HasValue ? ob.AtmosphereSettings.Value : MyAtmosphereSettings.Defaults();
                    planetInitArguments.SurfaceGravity = planetDefinition.SurfaceGravity;
                    planetInitArguments.AddGps = addGPS;
                    planetInitArguments.SpherizeWithDistance = true;
                    planetInitArguments.Generator = planetDefinition;
                    planetInitArguments.UserCreated = false;
                    planetInitArguments.InitializeComponents = true;

                    planet.Init(planetInitArguments);
                    MyEntities.Add(planet);
                    MyEntities.RaiseEntityCreated(planet);
                }
            }

        }

        public static MyPlanet AddPlanet(string storageName, string planetName, string definitionName, Vector3D positionMinCorner, int seed, float size, long entityId = 0, bool addGPS = false,bool userCreated = false)
        {
            MyPlanetGeneratorDefinition planetDefinition = MyDefinitionManager.Static.GetDefinition<MyPlanetGeneratorDefinition>(MyStringHash.GetOrCompute(definitionName));
            return CreatePlanet(storageName, planetName, ref positionMinCorner, seed, size, entityId, ref planetDefinition, addGPS, userCreated);
        }

        private static MyPlanet CreatePlanet(string storageName, string planetName, ref Vector3D positionMinCorner, int seed, float size, long entityId, ref MyPlanetGeneratorDefinition generatorDef, bool addGPS,bool userCreated = false)
        {
            if (MyFakes.ENABLE_PLANETS == false)
            {
                return null;
            }

            var random = MyRandom.Instance;
            using (MyRandom.Instance.PushSeed(seed))
            {

                MyPlanetStorageProvider provider = new MyPlanetStorageProvider();
                provider.Init(seed, generatorDef, size/2f);

                IMyStorage storage = new MyOctreeStorage(provider, provider.StorageSize);

                float minHillSize = provider.Radius * generatorDef.HillParams.Min;
                float maxHillSize = provider.Radius * generatorDef.HillParams.Max;

                float averagePlanetRadius = provider.Radius;

                float outerRadius = averagePlanetRadius + maxHillSize;
                float innerRadius = averagePlanetRadius + minHillSize;

                float atmosphereRadius = generatorDef.AtmosphereSettings.HasValue && generatorDef.AtmosphereSettings.Value.Scale > 1f ? 1 + generatorDef.AtmosphereSettings.Value.Scale : 1.75f;
                atmosphereRadius *= provider.Radius;

                float redAtmosphereShift = random.NextFloat(generatorDef.HostileAtmosphereColorShift.R.Min, generatorDef.HostileAtmosphereColorShift.R.Max);
                float greenAtmosphereShift = random.NextFloat(generatorDef.HostileAtmosphereColorShift.G.Min, generatorDef.HostileAtmosphereColorShift.G.Max);
                float blueAtmosphereShift = random.NextFloat(generatorDef.HostileAtmosphereColorShift.B.Min, generatorDef.HostileAtmosphereColorShift.B.Max);

                Vector3 atmosphereWavelengths = new Vector3(0.650f + redAtmosphereShift, 0.570f + greenAtmosphereShift, 0.475f + blueAtmosphereShift);

                atmosphereWavelengths.X = MathHelper.Clamp(atmosphereWavelengths.X, 0.1f, 1.0f);
                atmosphereWavelengths.Y = MathHelper.Clamp(atmosphereWavelengths.Y, 0.1f, 1.0f);
                atmosphereWavelengths.Z = MathHelper.Clamp(atmosphereWavelengths.Z, 0.1f, 1.0f);

                var planet = new MyPlanet();
                planet.EntityId = entityId;

                MyPlanetInitArguments planetInitArguments;
                planetInitArguments.StorageName = storageName;
                planetInitArguments.Storage = storage;
                planetInitArguments.PositionMinCorner = positionMinCorner;
                planetInitArguments.Radius = provider.Radius;
                planetInitArguments.AtmosphereRadius = atmosphereRadius;
                planetInitArguments.MaxRadius = outerRadius;
                planetInitArguments.MinRadius = innerRadius;
                planetInitArguments.HasAtmosphere = generatorDef.HasAtmosphere;
                planetInitArguments.AtmosphereWavelengths = atmosphereWavelengths;
                planetInitArguments.GravityFalloff = generatorDef.GravityFalloffPower;
                planetInitArguments.MarkAreaEmpty = true;
                planetInitArguments.AtmosphereSettings = generatorDef.AtmosphereSettings.HasValue ? generatorDef.AtmosphereSettings.Value : MyAtmosphereSettings.Defaults();
                planetInitArguments.SurfaceGravity = generatorDef.SurfaceGravity;
                planetInitArguments.AddGps = addGPS;
                planetInitArguments.SpherizeWithDistance = true;
                planetInitArguments.Generator = generatorDef;
                planetInitArguments.UserCreated = userCreated;
                planetInitArguments.InitializeComponents = true;

                planet.Init(planetInitArguments);

                MyEntities.Add(planet);
                MyEntities.RaiseEntityCreated(planet);

                planet.IsReadyForReplication = true;

                return planet;
            }
            return null;
        }
    }
}
