﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Weapons;
using System.Diagnostics;
using Sandbox.Game.Entities.Character;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemAnimation))]
    class MyToolbarItemAnimation : MyToolbarItemDefinition
    {
        public override bool Init(MyObjectBuilder_ToolbarItem objBuilder)
        {
            base.Init(objBuilder);

            ActivateOnClick = true;
            WantsToBeActivated = false;
            return true;
        }

        public override bool Activate()
        {
            if (Definition == null)
                return false;

            var animationDefinition = (MyAnimationDefinition)Definition;

            var controlledObject = MySession.ControlledEntity is MyCockpit ? ((MyCockpit)MySession.ControlledEntity).Pilot : MySession.LocalCharacter;

            if (controlledObject != null)
            {
                controlledObject.AddCommand(new MyAnimationCommand()
                {
                    AnimationSubtypeName = animationDefinition.Id.SubtypeName,
                    BlendTime = 0.2f,
                    PlaybackCommand = MyPlaybackCommand.Play,
                    FrameOption = animationDefinition.Loop ? MyFrameOption.Loop : MyFrameOption.PlayOnce,
                    TimeScale = 1
                },
                true);
            }

            return true;
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return (type == MyToolbarType.Character || type == MyToolbarType.Ship || type == MyToolbarType.Seat);
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            return ChangeInfo.None;
        }
    }
}
