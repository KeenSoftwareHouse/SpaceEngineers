using System;
using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_OxygenContainerObject : MyObjectBuilder_GasContainerObject	// Use GasContainer instead
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
    }
}
