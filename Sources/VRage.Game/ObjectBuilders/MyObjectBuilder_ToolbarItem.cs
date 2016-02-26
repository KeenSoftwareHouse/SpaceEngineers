using ProtoBuf;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public abstract class MyObjectBuilder_ToolbarItem : MyObjectBuilder_Base
    {
        public virtual void Remap(IMyRemapHelper remapHelper)
        {
        }
    }
}
