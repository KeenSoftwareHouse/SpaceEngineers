using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_Crossbow : MyObjectBuilder_EntityBase
    {
        [ProtoMember]
        public MyObjectBuilder_GunBase GunBase;
     }
}
