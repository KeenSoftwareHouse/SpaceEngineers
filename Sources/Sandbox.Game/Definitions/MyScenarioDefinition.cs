using System;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Common.ObjectBuilders.VRageData;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using VRage;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{

    [MyDefinitionType(typeof(MyObjectBuilder_ScenarioDefinition))]
    public class MyScenarioDefinition : MyDefinitionBase
    {
        public BoundingBoxD WorldBoundaries;
        public MyWorldGeneratorStartingStateBase[] PossiblePlayerStarts;
        public MyWorldGeneratorOperationBase[] WorldGeneratorOperations;
        public bool  AsteroidClustersEnabled;
        public float AsteroidClustersOffset;
        public bool  CentralClusterEnabled;
        public MyStringId[] CreativeModeWeapons;
        public MyStringId[] SurvivalModeWeapons;
        public MyObjectBuilder_Toolbar CreativeDefaultToolbar;
        public MyObjectBuilder_Toolbar SurvivalDefaultToolbar;
        public MyStringId MainCharacterModel;

        public DateTime GameDate;

        public Vector3 SunDirection;

        public MyObjectBuilder_Toolbar DefaultToolbar
        {
            get { return MySession.Static.CreativeMode ? CreativeDefaultToolbar : SurvivalDefaultToolbar; }
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = builder as MyObjectBuilder_ScenarioDefinition;

            AsteroidClustersEnabled = ob.AsteroidClusters.Enabled;
            AsteroidClustersOffset  = ob.AsteroidClusters.Offset;
            CentralClusterEnabled   = ob.AsteroidClusters.CentralCluster;
            CreativeDefaultToolbar  = ob.CreativeDefaultToolbar;
            SurvivalDefaultToolbar  = ob.SurvivalDefaultToolbar;
            MainCharacterModel = MyStringId.GetOrCompute(ob.MainCharacterModel);

            GameDate = new DateTime(ob.GameDate);

            SunDirection = ob.SunDirection;
            
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
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_ScenarioDefinition;

            ob.AsteroidClusters.Enabled        = AsteroidClustersEnabled;
            ob.AsteroidClusters.Offset         = AsteroidClustersOffset;
            ob.AsteroidClusters.CentralCluster = CentralClusterEnabled;
            ob.CreativeDefaultToolbar          = CreativeDefaultToolbar;
            ob.SurvivalDefaultToolbar          = SurvivalDefaultToolbar;
            ob.MainCharacterModel = MainCharacterModel.ToString();
            ob.GameDate = GameDate.Ticks;

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

            return ob;
        }
    }

}
