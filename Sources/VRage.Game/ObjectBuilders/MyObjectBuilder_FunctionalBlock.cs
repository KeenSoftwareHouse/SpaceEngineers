using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_FunctionalBlock : MyObjectBuilder_TerminalBlock
    {
        [ProtoMember]
        public bool Enabled = true; // Can by overriden by subclasses in constructor, which is why there is no attribute DefaultValue.

        public override void SetupForProjector()
        {
            base.SetupForProjector();

            Enabled = false;
        }
    }
}
