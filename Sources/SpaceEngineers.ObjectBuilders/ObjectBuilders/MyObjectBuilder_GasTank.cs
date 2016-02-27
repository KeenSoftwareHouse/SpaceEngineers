using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_GasTank : MyObjectBuilder_FunctionalBlock
	{
		[ProtoMember]
		public bool IsStockpiling;
		
        [ProtoMember]
		public float FilledRatio;
		
        [ProtoMember]
        [Nullable]
		public MyObjectBuilder_Inventory Inventory;
		
        [ProtoMember]
		public bool AutoRefill;

		public override void SetupForProjector()
		{
			base.SetupForProjector();
			FilledRatio = 0f;

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
