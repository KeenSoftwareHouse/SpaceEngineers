using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRageMath;
using VRage.Plugins;
using Sandbox.Game.Entities;
using Sandbox.Definitions;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Entities.Character;
using System.Diagnostics;
using VRage.ObjectBuilders;
using VRage;
using System.Reflection;
using VRage.Game;
using VRage.Game.Common;
#if XB1 // XB1_ALLINONEASSEMBLY
using VRage.Utils;
#endif // XB1

namespace Sandbox.Game.World
{
    public abstract class MyWorldGeneratorStartingStateBase
    {
        public string FactionTag;

        public abstract Vector3D? GetStartingLocation();
        public abstract void SetupCharacter(MyWorldGenerator.Args generatorArgs);

        public virtual void Init(MyObjectBuilder_WorldGeneratorPlayerStartingState builder)
        {
            this.FactionTag = builder.FactionTag;
        }

        public virtual MyObjectBuilder_WorldGeneratorPlayerStartingState GetObjectBuilder()
        {
            MyObjectBuilder_WorldGeneratorPlayerStartingState builder = Sandbox.Game.World.MyWorldGenerator.StartingStateFactory.CreateObjectBuilder(this);
            builder.FactionTag = this.FactionTag;

            return builder;
        }

        //Fixes position to voxel map surface (assuming gravity Vector3.Down and single voxel map)
        protected Vector3D FixPositionToVoxel(Vector3D position)
        {
            //BoundingBox bb = new BoundingBox(position + Vector3.Down * maxVertDistance, position + Vector3.Up * maxVertDistance);
            MyVoxelMap map = null;
            foreach (var e in MyEntities.GetEntities())
            {
                map = e as MyVoxelMap;
                if (map != null)
                    break;
            }

            float maxVertDistance = 2048; //maximal distance character will be shifted verticaly
            if (map != null)
                position = map.GetPositionOnVoxel(position, maxVertDistance);
            return position;
        }

        /// <summary>
        /// Setups player faction accoring to Factions.sbc and Scenario.sbx settings. If faction is not created yet. It will be created 
        /// for the player with Faction.sbc settings. Faction have to accept humans.
        /// </summary>
        protected virtual void CreateAndSetPlayerFaction()
        {
            if (Sync.IsServer && this.FactionTag != null && MySession.Static.LocalHumanPlayer != null)
            {
                MyFaction playerFaction = MySession.Static.Factions.TryGetOrCreateFactionByTag(this.FactionTag);
                playerFaction.AcceptJoin(MySession.Static.LocalHumanPlayer.Identity.IdentityId);
            }
        }
    }

    public partial class MyWorldGenerator
    {
        #region StartingState base and factory

        public class StartingStateTypeAttribute : MyFactoryTagAttribute
        {
            public StartingStateTypeAttribute(Type objectBuilderType)
                : base(objectBuilderType)
            {
            }

        }

        public static class StartingStateFactory
        {
            private static MyObjectFactory<StartingStateTypeAttribute, MyWorldGeneratorStartingStateBase> m_objectFactory;

            static StartingStateFactory()
            {
                m_objectFactory = new MyObjectFactory<StartingStateTypeAttribute, MyWorldGeneratorStartingStateBase>();
#if XB1 // XB1_ALLINONEASSEMBLY
                m_objectFactory.RegisterFromAssembly(MyAssembly.AllInOneAssembly);
#else // !XB1
                m_objectFactory.RegisterFromCreatedObjectAssembly();
                m_objectFactory.RegisterFromAssembly(MyPlugins.GameAssembly);
                m_objectFactory.RegisterFromAssembly(MyPlugins.SandboxAssembly); //TODO: Will be removed 
                m_objectFactory.RegisterFromAssembly(MyPlugins.UserAssembly);
#endif // !XB1
            }

            public static MyWorldGeneratorStartingStateBase CreateInstance(MyObjectBuilder_WorldGeneratorPlayerStartingState builder)
            {
                var instance = m_objectFactory.CreateInstance(builder.TypeId);
                if (instance != null)
                    instance.Init(builder);
                return instance;
            }

            public static MyObjectBuilder_WorldGeneratorPlayerStartingState CreateObjectBuilder(MyWorldGeneratorStartingStateBase instance)
            {
                return m_objectFactory.CreateObjectBuilder<MyObjectBuilder_WorldGeneratorPlayerStartingState>(instance);
            }
        }

        #endregion

        [MyWorldGenerator.StartingStateType(typeof(MyObjectBuilder_WorldGeneratorPlayerStartingState_Transform))]
        public class MyTransformState : MyWorldGeneratorStartingStateBase
        {
            public MyPositionAndOrientation? Transform;
            public bool JetpackEnabled;
            public bool DampenersEnabled;

            public override void SetupCharacter(MyWorldGenerator.Args generatorArgs)
            {
                Debug.Assert(MySession.Static.LocalHumanPlayer != null, "Local controller does not exist!");
                if (MySession.Static.LocalHumanPlayer == null) return;

                var characterOb = Sandbox.Game.Entities.Character.MyCharacter.Random();

                if (Transform.HasValue && MyPerGameSettings.CharacterStartsOnVoxel)
                {
                    var transform = Transform.Value;
                    transform.Position = FixPositionToVoxel(transform.Position);
                    characterOb.PositionAndOrientation = transform;
                }
                else
                {
                    characterOb.PositionAndOrientation = Transform;
                }
                characterOb.JetpackEnabled = JetpackEnabled;
                characterOb.DampenersEnabled = DampenersEnabled;

                if (characterOb.Inventory == null)
                    characterOb.Inventory = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Inventory>();
                FillInventoryWithDefaults(characterOb.Inventory, generatorArgs.Scenario);

                var character = new MyCharacter();
                character.Name = "Player";
                character.Init(characterOb);
                MyEntities.RaiseEntityCreated(character);
                
                MyEntities.Add(character);

                character.IsReadyForReplication = true;

                this.CreateAndSetPlayerFaction();

                MySession.Static.LocalHumanPlayer.SpawnIntoCharacter(character);
            }

            public override void Init(MyObjectBuilder_WorldGeneratorPlayerStartingState builder)
            {
                base.Init(builder);
                var ob = builder as MyObjectBuilder_WorldGeneratorPlayerStartingState_Transform;

                Transform        = ob.Transform;
                JetpackEnabled   = ob.JetpackEnabled;
                DampenersEnabled = ob.DampenersEnabled;
            }

            public override MyObjectBuilder_WorldGeneratorPlayerStartingState GetObjectBuilder()
            {
                var ob = base.GetObjectBuilder() as MyObjectBuilder_WorldGeneratorPlayerStartingState_Transform;

                ob.Transform        = Transform;
                ob.JetpackEnabled   = JetpackEnabled;
                ob.DampenersEnabled = DampenersEnabled;

                return ob;
            }

            public override Vector3D? GetStartingLocation()
            {
                if (Transform.HasValue && MyPerGameSettings.CharacterStartsOnVoxel)
                    return FixPositionToVoxel(Transform.Value.Position);
                else
                    return null;
            }
        }
    }
}
