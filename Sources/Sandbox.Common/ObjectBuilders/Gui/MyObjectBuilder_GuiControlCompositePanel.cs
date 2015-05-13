using ProtoBuf;
using System;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlCompositePanel : MyObjectBuilder_GuiControlPanel
    {
        [ProtoMember(1)]
        public float InnerHeight;

    }
}
