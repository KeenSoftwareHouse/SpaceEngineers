using ProtoBuf;
using VRage.ObjectBuilders;
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
