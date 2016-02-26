using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.ModAPI;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_CompoundCubeBlock : MyObjectBuilder_CubeBlock
    {
        [ProtoMember]
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
