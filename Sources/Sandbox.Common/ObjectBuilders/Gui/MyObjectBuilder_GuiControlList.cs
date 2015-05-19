using ProtoBuf;
using System;
using System.Collections.Generic;
using VRageMath;
using System.ComponentModel;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    public enum MyGuiControlListStyleEnum
    {
        Default,
        Simple,
    }
    
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlList : MyObjectBuilder_GuiControlParent
    {
        [ProtoMember]
        public MyGuiControlListStyleEnum VisualStyle;
    }
}
