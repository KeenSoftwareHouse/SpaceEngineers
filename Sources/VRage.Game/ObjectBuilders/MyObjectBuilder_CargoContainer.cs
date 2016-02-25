using ProtoBuf;
using System.ComponentModel;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CargoContainer : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_Inventory Inventory = null;

        [ProtoMember, DefaultValue(null)]
        [Serialize(MyObjectFlags.Nullable)]
        public string ContainerType = null;
        public bool ShouldSerializeContainerType() { return ContainerType != null; }

        public MyObjectBuilder_CargoContainer()
        {

        }

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
