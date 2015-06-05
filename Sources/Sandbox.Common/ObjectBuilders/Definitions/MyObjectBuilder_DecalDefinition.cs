using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageRender;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DecalDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public MyDecalMaterialDesc Material;

        //[ProtoMember]
        //public string NormalMap;

        //[ProtoMember]
        //public string AlphaMask;
        
        //[ProtoMember]
        //public string 
    }
}
