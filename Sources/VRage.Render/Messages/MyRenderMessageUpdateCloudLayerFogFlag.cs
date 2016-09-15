namespace VRageRender.Messages
{
	public class MyRenderMessageUpdateCloudLayerFogFlag : MyRenderMessageBase
	{
		public bool ShouldDrawFog;

	    public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
	    public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateCloudLayerFogFlag; } }
	}
}
