using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;

namespace Sandbox.Common.ObjectBuilders.Gui
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GuiControlTabPage : MyObjectBuilder_GuiControlParent
    {
        [ProtoMember(1)]
        public int PageKey;

        [ProtoMember(2)]
        public string TextEnum;
        
        [ProtoMember(3)]
        public float TextScale;
    }
}
