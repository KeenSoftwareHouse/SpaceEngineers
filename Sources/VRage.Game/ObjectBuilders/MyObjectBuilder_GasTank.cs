using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
	[ProtoContract]
	[MyObjectBuilderDefinition]
	public class MyObjectBuilder_GasTank : MyObjectBuilder_FunctionalBlock
	{
		[ProtoMember]
		public bool IsStockpiling;
		[ProtoMember]
		public float FilledRatio;
		[ProtoMember]
		public MyObjectBuilder_Inventory Inventory;
		[ProtoMember]
		public bool AutoRefill;

		public override void SetupForProjector()
		{
			base.SetupForProjector();
			FilledRatio = 0f;

			if (Inventory != null)
			{
				Inventory.Clear();
			}
		}
	}
}
