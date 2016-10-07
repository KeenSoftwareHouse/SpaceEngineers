using System;
using System.Linq;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Localization;
using Sandbox.Game.World;
using VRage;
using VRage.Game;
using VRage.Game.Definitions;
using VRage.Library.Utils;
using VRage.Utils;
using VRageMath;

namespace Sandbox.Definitions
{

    [MyDefinitionType(typeof(MyObjectBuilder_ScenarioDefinition))]
    public class MyScenarioDefinition : MyDefinitionBase
    {
        public MyDefinitionId GameDefinition;

        public MyDefinitionId Environment;

        public BoundingBoxD? WorldBoundaries;
        public MyWorldGeneratorStartingStateBase[] PossiblePlayerStarts;
        public MyWorldGeneratorOperationBase[] WorldGeneratorOperations;
        public bool AsteroidClustersEnabled;
        public float AsteroidClustersOffset;
        public bool CentralClusterEnabled;
        public MyEnvironmentHostilityEnum DefaultEnvironment;
        public MyStringId[] CreativeModeWeapons;
        public MyStringId[] SurvivalModeWeapons;
        public StartingItem[] CreativeModeComponents;
        public StartingItem[] SurvivalModeComponents;
        public StartingPhysicalItem[] CreativeModePhysicalItems;
        public StartingPhysicalItem[] SurvivalModePhysicalItems;
        public StartingItem[] CreativeModeAmmoItems;
        public StartingItem[] SurvivalModeAmmoItems;

        public MyObjectBuilder_InventoryItem[] CreativeInventoryItems;
        public MyObjectBuilder_InventoryItem[] SurvivalInventoryItems;

        public MyObjectBuilder_Toolbar CreativeDefaultToolbar;
        public MyObjectBuilder_Toolbar SurvivalDefaultToolbar;
        public MyStringId MainCharacterModel;

        public struct StartingItem
        {
            public MyFixedPoint amount;

            public MyStringId itemName;
        }

        public struct StartingPhysicalItem
        {
            public MyFixedPoint amount;

            public MyStringId itemName;

            public MyStringId itemType;
        }

        public DateTime GameDate;

        public Vector3 SunDirection;

        public bool HasPlanets
        {
            get
            {
                return WorldGeneratorOperations != null && WorldGeneratorOperations.Any(s => s is MyWorldGenerator.OperationAddPlanetPrefab || s is MyWorldGenerator.OperationCreatePlanet);
            }
        }

        public MyObjectBuilder_Toolbar DefaultToolbar
        {
            get { return MySession.Static.CreativeMode ? CreativeDefaultToolbar : SurvivalDefaultToolbar; }
        }

        protected override void Init(MyObjectBuilder_DefinitionBase builder)
        {
            base.Init(builder);
            var ob = (MyObjectBuilder_ScenarioDefinition) builder;

            GameDefinition = ob.GameDefinition;

            Environment = ob.EnvironmentDefinition;

            AsteroidClustersEnabled = ob.AsteroidClusters.Enabled;
            AsteroidClustersOffset  = ob.AsteroidClusters.Offset;
            CentralClusterEnabled   = ob.AsteroidClusters.CentralCluster;
            DefaultEnvironment      = ob.DefaultEnvironment;
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

            if (ob.CreativeModeComponents != null && ob.CreativeModeComponents.Length > 0)
            {
                CreativeModeComponents = new StartingItem[ob.CreativeModeComponents.Length];
                for (int i = 0; i < ob.CreativeModeComponents.Length; ++i)
                {
                    CreativeModeComponents[i].amount = (MyFixedPoint)ob.CreativeModeComponents[i].amount;
                    CreativeModeComponents[i].itemName = MyStringId.GetOrCompute(ob.CreativeModeComponents[i].itemName);
                }
            }

            if (ob.SurvivalModeComponents != null && ob.SurvivalModeComponents.Length > 0)
            {
                SurvivalModeComponents = new StartingItem[ob.SurvivalModeComponents.Length];
                for (int i = 0; i < ob.SurvivalModeComponents.Length; ++i)
                {
                    SurvivalModeComponents[i].amount = (MyFixedPoint)ob.SurvivalModeComponents[i].amount;
                    SurvivalModeComponents[i].itemName = MyStringId.GetOrCompute(ob.SurvivalModeComponents[i].itemName);
                }
            }

            if (ob.CreativeModePhysicalItems != null && ob.CreativeModePhysicalItems.Length > 0)
            {
                CreativeModePhysicalItems = new StartingPhysicalItem[ob.CreativeModePhysicalItems.Length];
                for (int i = 0; i < ob.CreativeModePhysicalItems.Length; ++i)
                {
                    CreativeModePhysicalItems[i].amount = (MyFixedPoint)ob.CreativeModePhysicalItems[i].amount;
                    CreativeModePhysicalItems[i].itemName = MyStringId.GetOrCompute(ob.CreativeModePhysicalItems[i].itemName);
                    CreativeModePhysicalItems[i].itemType = MyStringId.GetOrCompute(ob.CreativeModePhysicalItems[i].itemType);
                }
            }

            if (ob.SurvivalModePhysicalItems != null && ob.SurvivalModePhysicalItems.Length > 0)
            {
                SurvivalModePhysicalItems = new StartingPhysicalItem[ob.SurvivalModePhysicalItems.Length];
                for (int i = 0; i < ob.SurvivalModePhysicalItems.Length; ++i)
                {
                    SurvivalModePhysicalItems[i].amount = (MyFixedPoint)ob.SurvivalModePhysicalItems[i].amount;
                    SurvivalModePhysicalItems[i].itemName = MyStringId.GetOrCompute(ob.SurvivalModePhysicalItems[i].itemName);
                    SurvivalModePhysicalItems[i].itemType = MyStringId.GetOrCompute(ob.SurvivalModePhysicalItems[i].itemType);
                }
            }

            if (ob.CreativeModeAmmoItems != null && ob.CreativeModeAmmoItems.Length > 0)
            {
                CreativeModeAmmoItems = new StartingItem[ob.CreativeModeAmmoItems.Length];
                for (int i = 0; i < ob.CreativeModeAmmoItems.Length; ++i)
                {
                    CreativeModeAmmoItems[i].amount = (MyFixedPoint)ob.CreativeModeAmmoItems[i].amount;
                    CreativeModeAmmoItems[i].itemName = MyStringId.GetOrCompute(ob.CreativeModeAmmoItems[i].itemName);
                }
            }

            if (ob.SurvivalModeAmmoItems != null && ob.SurvivalModeAmmoItems.Length > 0)
            {
                SurvivalModeAmmoItems = new StartingItem[ob.SurvivalModeAmmoItems.Length];
                for (int i = 0; i < ob.SurvivalModeAmmoItems.Length; ++i)
                {
                    SurvivalModeAmmoItems[i].amount = (MyFixedPoint)ob.SurvivalModeAmmoItems[i].amount;
                    SurvivalModeAmmoItems[i].itemName = MyStringId.GetOrCompute(ob.SurvivalModeAmmoItems[i].itemName);
                }
            }

            CreativeInventoryItems = ob.CreativeInventoryItems;
            SurvivalInventoryItems = ob.SurvivalInventoryItems;

            WorldBoundaries = ob.WorldBoundaries;
        }

        public override MyObjectBuilder_DefinitionBase GetObjectBuilder()
        {
            var ob = base.GetObjectBuilder() as MyObjectBuilder_ScenarioDefinition;

            ob.AsteroidClusters.Enabled        = AsteroidClustersEnabled;
            ob.AsteroidClusters.Offset         = AsteroidClustersOffset;
            ob.AsteroidClusters.CentralCluster = CentralClusterEnabled;
            ob.DefaultEnvironment              = DefaultEnvironment;
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
