using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    public enum MyDebrisType
    {
        Model,
        Voxel
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DebrisDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Model;

        [ProtoMember]
        public MyDebrisType Type;
    }
}
