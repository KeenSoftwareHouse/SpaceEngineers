using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders.Definitions;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CrossbowItemDefinition : MyObjectBuilder_WeaponItemDefinition
    {
        [ProtoMember]
        public string ModelLoaded;

    }
}
