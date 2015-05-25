using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Interfaces;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_OxygenContainerObject : MyObjectBuilder_PhysicalObject, IMyOxygenBottle
    {
        /// <summary>
        /// This is not synced automatically
        /// Call SyncOxygenContainerLevel on inventory to sync it
        /// </summary>
        [ProtoMember]
        public float OxygenLevel = 0f;

        public override bool CanStack(MyObjectBuilder_PhysicalObject a)
        {
            return false;
        }

        float Sandbox.ModAPI.Interfaces.IMyOxygenBottle.OxygenLevel 
        {
            get {
                return OxygenLevel;
            }
        }
    }
}
