using ProtoBuf;
using VRage.ObjectBuilders;
using VRageRender;

namespace VRage.Game
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
