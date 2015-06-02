using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VRageRender
{
    /// <summary>
    /// 1 at the end is naming convention from DX, saying this is newer version (for Dx11 render).
    /// </summary>
    public class MyRenderMessageSwitchRenderSettings : IMyRenderMessage
    {
        public MyRenderSettings1 Settings;

        MyRenderMessageType IMyRenderMessage.MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        MyRenderMessageEnum IMyRenderMessage.MessageType { get { return MyRenderMessageEnum.SwitchRenderSettings; } }
    }
}
