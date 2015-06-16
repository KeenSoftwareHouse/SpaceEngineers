using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Sandbox.Game.Screens.Helpers
{
    public abstract class MyToolbarItemDefinition : MyToolbarItem
    {
        public MyDefinitionBase Definition;

        public MyToolbarItemDefinition()
        {
            SetEnabled(true);
            WantsToBeActivated = true;
        }

        public override bool Equals(object obj)
        {
            if (object.ReferenceEquals(this, obj))
                return true;

            var otherObj = obj as MyToolbarItemDefinition;
            return otherObj != null && Definition != null && Definition.Id.Equals(otherObj.Definition.Id);
        }
        
        public sealed override int GetHashCode()
        {
            return Definition.Id.GetHashCode();
        }

        public override MyObjectBuilder_ToolbarItem GetObjectBuilder()
        {
            //This can happen when using mods
            //Initially, the mod is used and an action is added to the toolbar. Later, the mod is removed, but the item is still present in the toolbar
            if (Definition == null)
            {
                return null;
            }
            
            MyObjectBuilder_ToolbarItemDefinition output = (MyObjectBuilder_ToolbarItemDefinition)MyToolbarItemFactory.CreateObjectBuilder(this);
            output.DefinitionId = Definition.Id;
            return output;
        }

        public override bool Init(MyObjectBuilder_ToolbarItem data)
        {
            Debug.Assert(data is MyObjectBuilder_ToolbarItemDefinition, "Wrong definition put to toolbar");

            if(MyDefinitionManager.Static.TryGetDefinition(((MyObjectBuilder_ToolbarItemDefinition)data).DefinitionId, out Definition))
            {
                if (!Definition.Public && !Sandbox.Engine.Utils.MyFakes.ENABLE_NON_PUBLIC_BLOCKS)
                {
                    return false;
                }
                SetDisplayName(Definition.DisplayNameText);
                SetIcon(Definition.Icon);
                return true;
            }
            return false;
        }
    }
}
