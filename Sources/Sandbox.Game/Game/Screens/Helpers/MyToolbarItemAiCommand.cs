using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.Screens.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.AI;
using Sandbox.Definitions;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.World;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemAiCommand))]
    public class MyToolbarItemAiCommand : MyToolbarItemDefinition
    {
        public override bool Init(MyObjectBuilder_ToolbarItem data)
        {
            bool init = base.Init(data);
            ActivateOnClick = false;
            return init;
        }

        public override bool Activate()
        {
            if (Definition == null)
                return false;

            MyAIComponent.Static.CommandDefinition = Definition as MyAiCommandDefinition;
            var controlledObject = MySession.Static.ControlledEntity as IMyControllableEntity;
            if (controlledObject != null)
            {
                controlledObject.SwitchToWeapon(null);
            }

            return true;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return type == MyToolbarType.Character || type == MyToolbarType.Spectator;
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            var commandDefinition = MyAIComponent.Static.CommandDefinition;
            WantsToBeSelected = commandDefinition != null && commandDefinition.Id.SubtypeId == (this.Definition as MyAiCommandDefinition).Id.SubtypeId;
            return ChangeInfo.None;
        }
    }
}
