using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.Entity;

namespace Sandbox.Game.Screens.Helpers
{
    [MyToolbarItemDescriptor(typeof(MyObjectBuilder_ToolbarItemEmpty))]
    class MyToolbarItemEmpty : MyToolbarItem
    {
        public static MyToolbarItemEmpty Default = new MyToolbarItemEmpty();

        public MyToolbarItemEmpty()
        {
            SetEnabled(true);
            ActivateOnClick = false;
            WantsToBeSelected = true;
        }

        public override bool Activate()
        {
            return false;
        }

        public override bool Equals(object obj)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return -1;
        }

        public override bool Init(MyObjectBuilder_ToolbarItem data)
        {
            return true;
        }

        public override MyObjectBuilder_ToolbarItem GetObjectBuilder()
        {
            // Lets try what happens, may be can avoid saving by this
            return null;
            return (MyObjectBuilder_ToolbarItemEmpty)MyToolbarItemFactory.CreateObjectBuilder(this);
        }

        public override bool AllowedInToolbarType(MyToolbarType type)
        {
            return true;
        }

        public override ChangeInfo Update(MyEntity owner, long playerID = 0)
        {
            return ChangeInfo.None;
        }
    }
}
