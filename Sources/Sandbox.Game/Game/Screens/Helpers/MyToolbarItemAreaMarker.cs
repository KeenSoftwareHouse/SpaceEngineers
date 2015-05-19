using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.AI;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Screens.Helpers
{
	[MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemAreaMarker))]
	public class MyToolbarItemAreaMarker : MyToolbarItemDefinition
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

			MyAIComponent.Static.AreaMarkerDefinition = Definition as MyAreaMarkerDefinition;
			var controlledObject = MySession.ControlledEntity as IMyControllableEntity;
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

		public override MyToolbarItem.ChangeInfo Update(Entities.MyEntity owner, long playerID = 0)
		{
			var markerDefinition = MyAIComponent.Static.AreaMarkerDefinition;
			WantsToBeSelected = markerDefinition != null && markerDefinition.Id.SubtypeId == (this.Definition as MyAreaMarkerDefinition).Id.SubtypeId;
			return ChangeInfo.None;
		}
	}
}
