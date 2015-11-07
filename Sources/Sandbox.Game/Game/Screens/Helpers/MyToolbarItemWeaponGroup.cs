using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine.Utils;
using Sandbox.Game.Entities;
using Sandbox.Game.Gui;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemWeaponGroup))]
    class MyToolbarItemWeaponGroup : MyToolbarItemDefinition
    {
        private string m_groupName;
        public override bool Init(MyObjectBuilder_ToolbarItem data)
        {
            var item = data as MyObjectBuilder_ToolbarItemWeaponGroup;
            if(item==null)
            {
                return false;
            }
            Definition = new MyGuiBlockCategoryDefinition();
            Definition.Id = new MyDefinitionId(typeof(MyObjectBuilder_ToolbarItemWeaponGroup), item.GroupName);
            SetDisplayName(item.DisplayNameText);
            SetIcon(item.Icon);
            m_groupName = item.GroupName;
            return true;
        }

        public override bool Activate()
        {
            if (MyFakes.OCTOBER_RELEASE_DISABLE_WEAPONS_AND_TOOLS)
            {
                MyHud.Notifications.Add(MyNotificationSingletons.DisabledWeaponsAndTools);
                return false;
            }

            var controlledObject = MySession.ControlledEntity as IMyControllableEntity;
            if (controlledObject != null)
            {
                controlledObject.SwitchToWeapon(Definition.Id);
                WantsToBeActivated = false;
            }

            return true;

        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return true;
        }

        public override MyToolbarItem.ChangeInfo Update(Entities.MyEntity owner, long playerID = 0)
        {
            return ChangeInfo.None;
        }
    }
}
