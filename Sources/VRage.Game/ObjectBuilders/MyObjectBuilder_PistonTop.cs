using ProtoBuf;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PistonTop : MyObjectBuilder_CubeBlock
    {
        [ProtoMember]
        public long PistonBlockId;

        public override void Remap(IMyRemapHelper remapHelper)
        {
            base.Remap(remapHelper);
            if (PistonBlockId != 0) PistonBlockId = remapHelper.RemapEntityId(PistonBlockId);
        }
    }
}
