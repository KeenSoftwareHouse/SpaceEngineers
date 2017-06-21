using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.ModAPI;
using VRage.Serialization;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CompoundCubeBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoMember]
        [DynamicItem(typeof(MyObjectBuilderDynamicSerializer), true)]
        [XmlArrayItem("MyObjectBuilder_CubeBlock", Type = typeof(MyAbstractXmlSerializer<MyObjectBuilder_CubeBlock>))]
        public MyObjectBuilder_CubeBlock[] Blocks;

        [ProtoMember]
        [Serialize(MyObjectFlags.Nullable)]
        public ushort[] BlockIds;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);

            foreach (var blockInCompound in Blocks)
                blockInCompound.Remap(remapHelper);
        }
    }
}
