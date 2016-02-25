using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    public class MyRenderMessageUpdateRenderEntity : MyRenderMessageBase
    {
        public uint ID;
        public Color? DiffuseColor;
        public Vector3? ColorMaskHSV;
        public float Dithering;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderEntity; } }

        public override void Close()
        {
            DiffuseColor = null;
            ColorMaskHSV = null;

            base.Close();
        }
    }
}
