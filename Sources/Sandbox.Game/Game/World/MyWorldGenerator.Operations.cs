using System;
using VRageMath;
using VRage.Plugins;
using System.Diagnostics;
using VRage.ObjectBuilders;
using VRage;
using VRage.Library.Utils;
using Sandbox.Definitions;
using VRage.Utils;
using VRage.Game;
using VRage.Game.Common;
using VRage.Voxels;

namespace Sandbox.Game.World
{
    public abstract class MyWorldGeneratorOperationBase
    {

        public string FactionTag;

        public abstract void Apply();

        public virtual void Init(MyObjectBuilder_WorldGeneratorOperation builder)
        {
            this.FactionTag = builder.FactionTag;
        }

        public virtual MyObjectBuilder_WorldGeneratorOperation GetObjectBuilder()
        {
            MyObjectBuilder_WorldGeneratorOperation ob = Sandbox.Game.World.MyWorldGenerator.OperationFactory.CreateObjectBuilder(this);
            ob.FactionTag = FactionTag;

            return ob;
        }
    }

    public partial class MyWorldGenerator
    {
        #region Operation base and factory

        public class OperationTypeAttribute : MyFactoryTagAttribute
        {
            public OperationTypeAttribute(Type objectBuilderType)
                : base(objectBuilderType)
            {
            }
        }

        public static class OperationFactory
        {
            private static MyObjectFactory<OperationTypeAttribute, MyWorldGeneratorOperationBase> m_objectFactory;

            static OperationFactory()
            {
                m_objectFactory = new MyObjectFactory<OperationTypeAttribute, MyWorldGeneratorOperationBase>();
#if XB1 // XB1_ALLINONEASSEMBLY
                m_objectFactory.RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
                m_objectFactory.RegisterFromCreatedObjectAssembly();
                m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
                m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxAssembly); //TODO: Will be removed 
                m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
#endif // !XB1
            }

            public static MyWorldGeneratorOperationBase CreateInstance(MyObjectBuilder_WorldGeneratorOperation builder)
            {
                var instance = m_objectFactory.CreateInstance(builder.TypeId);
                instance.Init(builder);
                return instance;
            }

            public static MyObjectBuilder_WorldGeneratorOperation CreateObjectBuilder(MyWorldGeneratorOperationBase instance)
            {
                return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_WorldGeneratorOperation>(instance);
            }
        }

        #endregion

        [MyWorldGenerator.OperationType(typeof(MyObjectBuilder_WorldGeneratorOperation_AddShipPrefab))]
        public class OperationAddShipPrefab : MyWorldGeneratorOperationBase
        {
            public string PrefabFile;
            public bool UseFirstGridOrigin;
            public MyPositionAndOrientation Transform = MyPositionAndOrientation.Default;
            public float RandomRadius;

            public override void Apply()
            {
                MyFaction faction = null;
                if (FactionTag != null)
                {
                    faction = MySession.Static.Factions.TryGetOrCreateFactionByTag(FactionTag);
                }

                long factionId = faction != null ? faction.FactionId : 0;

                if (RandomRadius == 0f)
                    MyPrefabManager.Static.AddShipPrefab(PrefabFile, Transform.GetMatrix(), factionId, spawnAtOrigin: UseFirstGridOrigin);
                else
                    MyPrefabManager.Static.AddShipPrefabRandomPosition(PrefabFile, Transform.Position, RandomRadius, factionId);
            }

            public override void Init(MyObjectBuilder_WorldGeneratorOperation builder)
            {
                base.Init(builder);
                var ob = builder as MyObjectBuilder_WorldGeneratorOperation_AddShipPrefab;

                PrefabFile         = ob.PrefabFile;
                UseFirstGridOrigin = ob.UseFirstGridOrigin;
                Transform          = ob.Transform;
                RandomRadius       = ob.RandomRadius;
            }

            public override MyObjectBuilder_WorldGeneratorOperation GetObjectBuilder()
            {
                var ob = base.GetObjectBuilder() as MyObjectBuilder_WorldGeneratorOperation_AddShipPrefab;

                ob.PrefabFile   = PrefabFile;
                ob.Transform    = Transform;
                ob.RandomRadius = RandomRadius;

                return ob;
            }
        }

        [MyWorldGenerator.OperationType(typeof(MyObjectBuilder_WorldGeneratorOperation_AddAsteroidPrefab))]
        public class OperationAddAsteroidPrefab : MyWorldGeneratorOperationBase
        {
            public string Name;
            public string PrefabName;
            public Vector3 Position;

            public override void Apply()
            {
                MyWorldGenerator.AddAsteroidPrefab(PrefabName, Position, Name);
            }

            public override void Init(MyObjectBuilder_WorldGeneratorOperation builder)
            {
                base.Init(builder);
                var ob = builder as MyObjectBuilder_WorldGeneratorOperation_AddAsteroidPrefab;

                Name       = ob.Name;
                PrefabName = ob.PrefabFile;
                Position   = ob.Position;
            }

            public override MyObjectBuilder_WorldGeneratorOperation GetObjectBuilder()
            {
                var ob = base.GetObjectBuilder() as MyObjectBuilder_WorldGeneratorOperation_AddAsteroidPrefab;

                ob.Name       = Name;
                ob.PrefabFile = PrefabName;
                ob.Position   = Position;

                return ob;
            }
        }

        [MyWorldGenerator.OperationType(typeof(MyObjectBuilder_WorldGeneratorOperation_AddObjectsPrefab))]
        public class OperationAddObjectsPrefab : MyWorldGeneratorOperationBase
        {
            public string PrefabFile;

            public override void Apply()
            {
                MyWorldGenerator.AddObjectsPrefab(PrefabFile);
            }

            public override void Init(MyObjectBuilder_WorldGeneratorOperation builder)
            {
                base.Init(builder);
                var ob = builder as MyObjectBuilder_WorldGeneratorOperation_AddObjectsPrefab;
                PrefabFile = ob.PrefabFile;
            }

            public override MyObjectBuilder_WorldGeneratorOperation GetObjectBuilder()
            {
                var ob = base.GetObjectBuilder() as MyObjectBuilder_WorldGeneratorOperation_AddObjectsPrefab;
                ob.PrefabFile = PrefabFile;
                return ob;
            }
        }

        [MyWorldGenerator.OperationType(typeof(MyObjectBuilder_WorldGeneratorOperation_SetupBasePrefab))]
        public class OperationSetupBasePrefab : MyWorldGeneratorOperationBase
        {
            public string PrefabFile;
            public Vector3 Offset;
            public string AsteroidName;
            public string BeaconName;

            public override void Apply()
            {
                MyFaction faction = null;
                if (FactionTag != null)
                {
                    faction = MySession.Static.Factions.TryGetOrCreateFactionByTag(FactionTag);
                }

                long factionId = faction != null ? faction.FactionId : 0;
                MyWorldGenerator.SetupBase(PrefabFile, Offset, AsteroidName, BeaconName, factionId);
            }

            public override void Init(MyObjectBuilder_WorldGeneratorOperation builder)
            {
                base.Init(builder);
                var ob = builder as MyObjectBuilder_WorldGeneratorOperation_SetupBasePrefab;

                PrefabFile   = ob.PrefabFile;
                Offset       = ob.Offset;
                AsteroidName = ob.AsteroidName;
                BeaconName   = ob.BeaconName;
            }

            public override MyObjectBuilder_WorldGeneratorOperation GetObjectBuilder()
            {
                var ob = base.GetObjectBuilder() as MyObjectBuilder_WorldGeneratorOperation_SetupBasePrefab;

                ob.PrefabFile   = PrefabFile;
                ob.Offset       = Offset;
                ob.AsteroidName = AsteroidName;
                ob.BeaconName   = BeaconName;

                return ob;
            }
        }

        [MyWorldGenerator.OperationType(typeof(MyObjectBuilder_WorldGeneratorOperation_AddPlanetPrefab))]
        public class OperationAddPlanetPrefab : MyWorldGeneratorOperationBase
        {
            public string PrefabName;
            public string DefinitionName;
            public Vector3D Position;
            public bool AddGPS = false;

            public override void Apply()
            {
                MyWorldGenerator.AddPlanetPrefab(PrefabName, DefinitionName, Position, AddGPS);
            }

            public override void Init(MyObjectBuilder_WorldGeneratorOperation builder)
            {
                base.Init(builder);
                var ob = builder as MyObjectBuilder_WorldGeneratorOperation_AddPlanetPrefab;

                DefinitionName = ob.DefinitionName;
                PrefabName = ob.PrefabName;
                Position = ob.Position;
                AddGPS = ob.AddGPS;
            }

            public override MyObjectBuilder_WorldGeneratorOperation GetObjectBuilder()
            {
                var ob = base.GetObjectBuilder() as MyObjectBuilder_WorldGeneratorOperation_AddPlanetPrefab;

                ob.DefinitionName = DefinitionName;
                ob.PrefabName = PrefabName;
                ob.Position = Position;
                ob.AddGPS = AddGPS;
                return ob;
            }
        }

        [MyWorldGenerator.OperationType(typeof(MyObjectBuilder_WorldGeneratorOperation_CreatePlanet))]
        public class OperationCreatePlanet : MyWorldGeneratorOperationBase
        {
            public string DefinitionName;
            public bool AddGPS = false;
            public Vector3D PositionMinCorner;
            public Vector3D PositionCenter;
            public float Diameter;

            public override void Apply()
            {
                MyPlanetGeneratorDefinition planetDefinition = MyDefinitionManager.Static.GetDefinition<MyPlanetGeneratorDefinition>(MyStringHash.GetOrCompute(DefinitionName));

                if (planetDefinition == null)
                {
                    string message = String.Format("Definition for planet {0} could not be found. Skipping.", DefinitionName);
                    Debug.Fail(message);
                    MyLog.Default.WriteLine(message);
                    return;
                }

                Vector3D position = PositionMinCorner;
                if (PositionCenter.IsValid())
                {
                    position = PositionCenter;

                    var size = MyVoxelCoordSystems.FindBestOctreeSize(Diameter * (1 + planetDefinition.HillParams.Max));
                    position -= ((Vector3D)size) / 2;
                }

                int seed = MyRandom.Instance.Next();
                var storageNameBase = DefinitionName + "-" + seed + "d" + Diameter;
                MyWorldGenerator.CreatePlanet(storageNameBase, planetDefinition.FolderName, ref position, seed, Diameter, MyRandom.Instance.NextLong(), ref planetDefinition, AddGPS);
            }

            public override void Init(MyObjectBuilder_WorldGeneratorOperation builder)
            {
                base.Init(builder);
                var ob = builder as MyObjectBuilder_WorldGeneratorOperation_CreatePlanet;

                DefinitionName = ob.DefinitionName;
                DefinitionName = ob.DefinitionName;
                AddGPS = ob.AddGPS;
                Diameter = ob.Diameter;
                PositionMinCorner = ob.PositionMinCorner;
                PositionCenter = ob.PositionCenter;
            }

            public override MyObjectBuilder_WorldGeneratorOperation GetObjectBuilder()
            {
                var ob = base.GetObjectBuilder() as MyObjectBuilder_WorldGeneratorOperation_CreatePlanet;

                ob.DefinitionName = DefinitionName;
                ob.DefinitionName = DefinitionName;
                ob.AddGPS = AddGPS;
                ob.Diameter = Diameter;
                ob.PositionMinCorner = PositionMinCorner;
                ob.PositionCenter = PositionCenter;
                return ob;
            }
        }

    }
}
