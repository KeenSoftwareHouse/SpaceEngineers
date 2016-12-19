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

        [ProtoMember]
        public string Target = string.Empty;

        [ProtoMember]
        public string Source = string.Empty;

        [ProtoMember]
        public float MinSize = 1;

        [ProtoMember]
        public float MaxSize = 2;

        [ProtoMember]
        public float Depth = 0.2f;

        [ProtoMember]
        public float Rotation = float.PositiveInfinity;

        [ProtoMember]
        public bool Transparent = false;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_DecalGlobalsDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public int DecalQueueSize;
    }
}
