using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using VRage.Library.Utils;
using VRage.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{

    [MyDefinitionType(typeof(MyObjectBuilder_ScenarioDefinition))]
    public class MyScenarioDefinition : MyDefinitionBase
    {
        public class MyBattleSettings
        {
            public BoundingBoxD[] AttackerSlots;
            public BoundingBoxD DefenderSlot;
            public long DefenderEntityId;
        }

        public BoundingBoxD WorldBoundaries;
        public MyWorldGeneratorStartingStateBase[] PossiblePlayerStarts;
        public MyWorldGeneratorOperationBase[] WorldGeneratorOperations;
        public bool  AsteroidClustersEnabled;
        public float AsteroidClustersOffset;
        public bool  CentralClusterEnabled;
        public MyStringId[] CreativeModeWeapons;
        public MyStringId[] SurvivalModeWeapons;
        public MyObjectBuilder_Toolbar DefaultToolbar;
        public MyBattleSettings Battle;
        public MyStringId MainCharacterModel;

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_ScenarioDefinition;

            AsteroidClustersEnabled = ob.AsteroidClusters.Enabled;
            AsteroidClustersOffset  = ob.AsteroidClusters.Offset;
            CentralClusterEnabled   = ob.AsteroidClusters.CentralCluster;
            DefaultToolbar = ob.DefaultToolbar;
            MainCharacterModel = MyStringId.GetOrCompute(ob.MainCharacterModel);

            if (ob.PossibleStartingStates != null && ob.PossibleStartingStates.Length > 0)
            {
                PossiblePlayerStarts = new MyWorldGeneratorStartingStateBase[ob.PossibleStartingStates.Length];
                for (int i = 0; i < ob.PossibleStartingStates.Length; ++i)
                {
                    PossiblePlayerStarts[i] = MyWorldGenerator.StartingStateFactory.CreateInstance(ob.PossibleStartingStates[i]);
                }
            }

            if (ob.WorldGeneratorOperations != null && ob.WorldGeneratorOperations.Length > 0)
            {
                WorldGeneratorOperations = new MyWorldGeneratorOperationBase[ob.WorldGeneratorOperations.Length];
                for (int i = 0; i < ob.WorldGeneratorOperations.Length; ++i)
                {
                    WorldGeneratorOperations[i] = MyWorldGenerator.OperationFactory.CreateInstance(ob.WorldGeneratorOperations[i]);
                }
            }

            if (ob.CreativeModeWeapons != null && ob.CreativeModeWeapons.Length > 0)
            {
                CreativeModeWeapons = new MyStringId[ob.CreativeModeWeapons.Length];
                for (int i = 0; i < ob.CreativeModeWeapons.Length; ++i)
                {
                    CreativeModeWeapons[i] = MyStringId.GetOrCompute(ob.CreativeModeWeapons[i]);
                }
            }

            if (ob.SurvivalModeWeapons != null && ob.SurvivalModeWeapons.Length > 0)
            {
                SurvivalModeWeapons = new MyStringId[ob.SurvivalModeWeapons.Length];
                for (int i = 0; i < ob.SurvivalModeWeapons.Length; ++i)
                {
                    SurvivalModeWeapons[i] = MyStringId.GetOrCompute(ob.SurvivalModeWeapons[i]);
                }
            }

            WorldBoundaries.Min = ob.WorldBoundaries.Min;
            WorldBoundaries.Max = ob.WorldBoundaries.Max;

            if (MyFakes.ENABLE_BATTLE_SYSTEM && ob.Battle != null)
            {
                Battle = new MyBattleSettings();

                Battle.DefenderSlot = ob.Battle.DefenderSlot;
                Battle.DefenderEntityId = ob.Battle.DefenderEntityId;

                if (ob.Battle.AttackerSlots != null && ob.Battle.AttackerSlots.Length > 0)
                {
                    Battle.AttackerSlots = new BoundingBoxD[ob.Battle.AttackerSlots.Length];
                    for (int i = 0; i < ob.Battle.AttackerSlots.Length; ++i)
                    {
                        Battle.AttackerSlots[i] = ob.Battle.AttackerSlots[i];
                    }
                }
            }
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_ScenarioDefinition;

            ob.AsteroidClusters.Enabled        = AsteroidClustersEnabled;
            ob.AsteroidClusters.Offset         = AsteroidClustersOffset;
            ob.AsteroidClusters.CentralCluster = CentralClusterEnabled;
            ob.DefaultToolbar = DefaultToolbar;
            ob.MainCharacterModel = MainCharacterModel.ToString();

            if (PossiblePlayerStarts != null && PossiblePlayerStarts.Length > 0)
            {
                ob.PossibleStartingStates = new MyObjectBuilder_WorldGeneratorPlayerStartingState[PossiblePlayerStarts.Length];
                for (int i = 0; i < PossiblePlayerStarts.Length; ++i)
                {
                    ob.PossibleStartingStates[i] = PossiblePlayerStarts[i].GetObjectBuilder();
                }
            }

            if (WorldGeneratorOperations != null && WorldGeneratorOperations.Length > 0)
            {
                ob.WorldGeneratorOperations = new MyObjectBuilder_WorldGeneratorOperation[WorldGeneratorOperations.Length];
                for (int i = 0; i < WorldGeneratorOperations.Length; ++i)
                {
                    ob.WorldGeneratorOperations[i] = WorldGeneratorOperations[i].GetObjectBuilder();
                }
            }

            if (CreativeModeWeapons != null && CreativeModeWeapons.Length > 0)
            {
                ob.CreativeModeWeapons = new string[CreativeModeWeapons.Length];
                for (int i = 0; i < CreativeModeWeapons.Length; ++i)
                {
                    ob.CreativeModeWeapons[i] = CreativeModeWeapons[i].ToString();
                }
            }

            if (SurvivalModeWeapons != null && SurvivalModeWeapons.Length > 0)
            {
                ob.SurvivalModeWeapons = new string[SurvivalModeWeapons.Length];
                for (int i = 0; i < SurvivalModeWeapons.Length; ++i)
                {
                    ob.SurvivalModeWeapons[i] = SurvivalModeWeapons[i].ToString();
                }
            }

            if (MyFakes.ENABLE_BATTLE_SYSTEM && Battle != null)
            {
                ob.Battle = new Sandbox.Common.ObjectBuilders.Definitions.MyObjectBuilder_ScenarioDefinition.MyOBBattleSettings();

                if (Battle.AttackerSlots != null && Battle.AttackerSlots.Length > 0)
                {
                    ob.Battle.AttackerSlots = new SerializableBoundingBoxD[Battle.AttackerSlots.Length];
                    for (int i = 0; i < Battle.AttackerSlots.Length; ++i)
                    {
                        ob.Battle.AttackerSlots[i] = Battle.AttackerSlots[i];
                    }
                }
            }

            return ob;
        }
    }

}
