using ProtoBuf;
using System.Collections.Generic;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControls : MyObjectBuilder_Base
    {
        [ProtoMember]
        public List<MyObjectBuilder_GuiControlBase> Controls;
    }
}
