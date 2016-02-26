using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
	public class MyRenderMessageUpdateCloudLayerFogFlag : MyRenderMessageBase
	{
		public bool ShouldDrawFog;

	    public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
	    public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateCloudLayerFogFlag; } }
	}
}
