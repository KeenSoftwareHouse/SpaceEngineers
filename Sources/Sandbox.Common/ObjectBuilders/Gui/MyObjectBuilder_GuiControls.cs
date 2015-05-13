using ProtoBuf;
using System;
using VRageMath;
using System.Collections.Generic;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControls : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public List<MyObjectBuilder_GuiControlBase> Controls;
    }
}
