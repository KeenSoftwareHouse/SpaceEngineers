using ProtoBuf;
using System;
using VRageMath;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlCompositePanel : MyObjectBuilder_GuiControlPanel
    {
        [ProtoMember]
        public float InnerHeight;

    }
}
