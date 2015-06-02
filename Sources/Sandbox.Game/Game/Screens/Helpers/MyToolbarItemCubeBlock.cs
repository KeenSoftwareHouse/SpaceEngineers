using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Graphics.GUI;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Gui;
using Sandbox.Game.Weapons;
using Sandbox.Game.World;
using VRageMath;
using Sandbox.Common.ObjectBuilders.Definitions;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemCubeBlock))]
    public class MyToolbarItemCubeBlock : MyToolbarItemDefinition
    {
        public static readonly string VariantsAvailableSubicon = @"Textures\GUI\Icons\VariantsAvailable.dds";

        public override bool Activate()
        {
            var character = MySession.LocalCharacter;

			MyDefinitionId weaponDefinition = new MyDefinitionId(typeof(MyObjectBuilder_CubePlacer));
            if (character != null)
            {
                if (!(character.CurrentWeapon != null && character.CurrentWeapon.DefinitionId == weaponDefinition))
                {
                    character.SwitchToWeapon(weaponDefinition);
                }
                
                MyCubeBuilder.Static.ActivateBlockCreation(((MyCubeBlockDefinition)Definition).Id);
            }

            if (MyCubeBuilder.DeveloperSpectatorIsBuilding)
            {
                MyCubeBuilder.Static.ActivateBlockCreation(((MyCubeBlockDefinition)Definition).Id);
                if (!MyCubeBuilder.Static.IsActivated)
                {
                    MyCubeBuilder.Static.Activate();
                }
            }
            return true;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return (type == MyToolbarType.Character || type == MyToolbarType.Spectator);
        }

        public override bool Init(MyObjectBuilder_ToolbarItem data)
        {
            bool result = base.Init(data);
            ActivateOnClick = false;
            var cubeDef = Definition as MyCubeBlockDefinition;
            if (result && cubeDef != null && cubeDef.BlockStages != null && cubeDef.BlockStages.Length > 0)
            {
                SetSubIcon(VariantsAvailableSubicon);
            }
            return result;
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            var blockDefinition = MyCubeBuilder.Static.IsActivated ? MyCubeBuilder.Static.ToolbarBlockDefinition : null;
            WantsToBeSelected = (MyCubeBuilder.Static.BlockCreationIsActivated || MyCubeBuilder.Static.MultiBlockCreationIsActivated) && blockDefinition != null && blockDefinition.BlockPairName == (this.Definition as MyCubeBlockDefinition).BlockPairName;
            return ChangeInfo.None;
        }
    }
}
