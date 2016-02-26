using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    /// <summary>
    /// 1 at the end is naming convention from DX, saying this is newer version (for Dx11 render).
    /// </summary>
    public class MyRenderMessageSwitchRenderSettings : MyRenderMessageBase
    {
        public MyRenderSettings1 Settings;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.SwitchRenderSettings; } }
    }
}
