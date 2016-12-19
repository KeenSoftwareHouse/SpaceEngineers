using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Entity;
using VRageMath;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemBot))]
    public class MyToolbarItemBot : MyToolbarItemDefinition
    {
        public override bool Init(MyObjectBuilder_ToolbarItem data)
        {
            base.Init(data);
            ActivateOnClick = false;
            return true;
        }

        public override bool Activate()
        {
            if (!MyFakes.ENABLE_BARBARIANS || !MyPerGameSettings.EnableAi)
                return false;

            if (Definition == null)
                return false;

            MyAIComponent.Static.BotToSpawn = Definition as MyAgentDefinition;
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

        public override MyToolbarItem.ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            var botDefinition = MyAIComponent.Static.BotToSpawn;
            WantsToBeSelected = botDefinition != null && botDefinition.Id.SubtypeId == (this.Definition as MyAgentDefinition).Id.SubtypeId;
            return ChangeInfo.None;
        }
    }
}
