using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlParent : MyObjectBuilder_GuiControlBase
    {
        [ProtoMember]
        public MyObjectBuilder_GuiControls Controls;

    }
}
