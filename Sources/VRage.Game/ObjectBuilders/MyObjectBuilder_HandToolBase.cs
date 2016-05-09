using ProtoBuf;
using VRage.Game.ObjectBuilders;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_HandToolBase : MyObjectBuilder_EntityBase, IMyObjectBuilder_GunObject<MyObjectBuilder_ToolBase>
    {
        [Serialize(MyObjectFlags.Nullable)]
        public MyObjectBuilder_ToolBase DeviceBase = null;
        public bool ShouldSerializeDeviceBase() { return DeviceBase != null; }

        MyObjectBuilder_DeviceBase IMyObjectBuilder_GunObject<MyObjectBuilder_ToolBase>.DeviceBase
        {
            get
            {
                return DeviceBase;
            }
            set
            {
                DeviceBase = value as MyObjectBuilder_ToolBase;
            }
        }
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_HandTool : MyObjectBuilder_HandToolBase
    {
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GoodAIControlHandTool : MyObjectBuilder_HandToolBase
    {
    }
}