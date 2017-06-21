using System.Diagnostics;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Utils;
using VRage.Game.Entity;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemCreateGrid))]
    public class MyToolbarItemCreateGrid : MyToolbarItemDefinition
    {
        private static MyStringHash CreateSmallShip, CreateLargeShip, CreateStation;

        static MyToolbarItemCreateGrid()
        {
            CreateSmallShip = MyStringHash.GetOrCompute("CreateSmallShip");
            CreateLargeShip = MyStringHash.GetOrCompute("CreateLargeShip");
            CreateStation = MyStringHash.GetOrCompute("CreateStation");
        }

        public override bool Init(MyObjectBuilder_ToolbarItem objBuilder)
        {
            base.Init(objBuilder);

            WantsToBeSelected = false;
            WantsToBeActivated = true; //by Gregory: changed to true because of 'Toolbar switching not working correctly' bug
            ActivateOnClick = true;
            return true;
        }

        void CreateGrid(MyCubeSize cubeSize, bool isStatic)
        {
            if (!MyEntities.MemoryLimitReachedReport && !MySandboxGame.IsPaused)
            {
                MySessionComponentVoxelHand.Static.Enabled = false;
                MyCubeBuilder.Static.StartStaticGridPlacement(cubeSize, isStatic);
                var character = MySession.Static.LocalCharacter;

                Debug.Assert(character != null);
                if (character != null)
                {
                    MyDefinitionId weaponDefinition = new MyDefinitionId(typeof(MyObjectBuilder_CubePlacer));
                    character.SwitchToWeapon(weaponDefinition);
                }
            }
        }

        public override bool Activate()
        {
            //if(Definition.Id.SubtypeId == CreateSmallShip)
            //    CreateGrid(MyCubeSize.Small, false);
            //else if(Definition.Id.SubtypeId == CreateLargeShip)
            //    CreateGrid(MyCubeSize.Large, false);
            //else 
            if (Definition.Id.SubtypeId == CreateStation)
                CreateGrid(MyCubeSize.Large, true);

            return false;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return (type == MyToolbarType.Character || type == MyToolbarType.Spectator);
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            return ChangeInfo.None;
        }
    }
}
