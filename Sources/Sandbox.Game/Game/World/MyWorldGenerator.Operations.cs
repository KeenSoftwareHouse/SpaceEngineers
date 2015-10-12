using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.Voxels;
using VRageMath;
using VRage.Plugins;
using Sandbox.Engine.Utils;
using System.Diagnostics;
using VRage.ObjectBuilders;
using VRage;
using System.Reflection;
using VRage.Library.Utils;
using Sandbox.Definitions;

namespace Sandbox.Game.World
{
    public abstract class MyWorldGeneratorOperationBase
    {
        public abstract void Apply();

        public virtual void Init(MyObjectBuilder_WorldGeneratorOperation builder)
        {

        }

        public virtual MyObjectBuilder_WorldGeneratorOperation GetObjectBuilder()
        {
            return Sandbox.Game.World.MyWorldGenerator.OperationFactory.CreateObjectBuilder(this);
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
                m_objectFactory.RegisterFromCreatedObjectAssembly();
                m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
                m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxAssembly); //TODO: Will be removed 
                m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
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
            public MyPositionAndOrientation Transform = MyPositionAndOrientation.Default;
            public float RandomRadius;

            public override void Apply()
            {
                if (RandomRadius == 0f)
                    MyPrefabManager.Static.AddShipPrefab(PrefabFile, Transform.GetMatrix());
                else
                    MyPrefabManager.Static.AddShipPrefabRandomPosition(PrefabFile, Transform.Position, RandomRadius);
            }

            public override void Init(MyObjectBuilder_WorldGeneratorOperation builder)
            {
                base.Init(builder);
                var ob = builder as MyObjectBuilder_WorldGeneratorOperation_AddShipPrefab;

                PrefabFile   = ob.PrefabFile;
                Transform    = ob.Transform;
                RandomRadius = ob.RandomRadius;

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
                MyWorldGenerator.SetupBase(PrefabFile, Offset, AsteroidName, BeaconName);
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


    }
}
