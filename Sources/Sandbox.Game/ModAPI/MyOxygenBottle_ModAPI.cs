using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Definitions;

namespace Sandbox.Game
{
    public partial struct MyPhysicalInventoryItem : Sandbox.ModAPI.IMyOxygenBottle
    {
        float ModAPI.Ingame.IMyOxygenBottle.Capacity
        {
            get
            {
                var content = Content as MyObjectBuilder_OxygenContainerObject;
                if (content != null)
                {
                    var physicalItem = MyDefinitionManager.Static.GetPhysicalItemDefinition(Content) as MyOxygenContainerDefinition;
                    return physicalItem != null ? physicalItem.Capacity : 0;
                }
                return 0;
            }
        }

        float ModAPI.Ingame.IMyOxygenBottle.OxygenLevel
        {
            get
            {
                var content = Content as MyObjectBuilder_OxygenContainerObject;
                if (content != null)
                {
                    return content.OxygenLevel;
                }
                return 0;
            }
        }

        /* set is not safe atm due sync ... not implementing
        float ModAPI.IMyOxygenBottle.OxygenLevel
        {
            get
            {
                var content = Content as MyObjectBuilder_OxygenContainerObject;
                if (content != null)
                {
                    return content.OxygenLevel;
                }
                return 0;
            }

            set
            {
                var content = Content as MyObjectBuilder_OxygenContainerObject;
                if (content != null && value != content.OxygenLevel)
                {
                    content.OxygenLevel = value;                    
                }
            }
        }
        */
    }
}
