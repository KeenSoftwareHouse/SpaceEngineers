using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.ObjectBuilders;

namespace Sandbox.ModAPI.Interfaces
{
    public interface IMyInventoryItem
    {
        VRage.MyFixedPoint Amount
        {
            get;
            set;
        }

        MyObjectBuilder_Base Content
        {
            get;
            set;
        }

        uint ItemId
        {
            get;
            set;
        }
    }

    public static class MyInventoryItemExtension
    {
        public static MyDefinitionId GetDefinitionId(this IMyInventoryItem self)
        {
            var physicalObject = self.Content as MyObjectBuilder_PhysicalObject;
            if (physicalObject != null)
            {
                return physicalObject.GetObjectId();
            }
            else
            {
                return new MyDefinitionId(self.Content.TypeId, self.Content.SubtypeId);
            }
        }
    }
}
