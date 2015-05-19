using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ConveyorTurretBase : MyObjectBuilder_TurretBase
    {
        [ProtoMember, DefaultValue(true)]
        public bool UseConveyorSystem = true;
    }
}
