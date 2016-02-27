using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_Collector : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public MyObjectBuilder_Inventory Inventory;

        [ProtoMember, DefaultValue(true)]
        public bool UseConveyorSystem = true;

        public override void SetupForProjector()
        {
            base.SetupForProjector();
            if (Inventory != null)
                Inventory.Clear();
            if (ComponentContainer != null)
            {
                var comp = ComponentContainer.Components.Find((s) => s.Component.TypeId == typeof(MyObjectBuilder_Inventory));
                if (comp != null)
                    (comp.Component as MyObjectBuilder_Inventory).Clear();
            }
        }
    }
}
