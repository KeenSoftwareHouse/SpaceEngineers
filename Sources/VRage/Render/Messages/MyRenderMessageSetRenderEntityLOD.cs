using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    public class MyRenderMessageSetRenderEntityLOD : MyRenderMessageBase
    {
        public uint ID;
        public float Distance; //Multiplier of MyRenderConstants.LodTransitionDistanceBackgroundEnd
        public string Model;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SetRenderEntityLOD; } }
    }
}
