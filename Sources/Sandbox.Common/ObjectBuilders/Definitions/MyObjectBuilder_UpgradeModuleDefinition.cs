using ProtoBuf;
using VRage.ObjectBuilders;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    public enum MyUpgradeModifierType
    {
        Multiplicative,
        Additive,
    }

    [ProtoContract]
    public struct MyUpgradeModuleInfo
    {
        [ProtoMember]
        public string UpgradeType;
        [ProtoMember]
        public float Modifier;
        [ProtoMember]
        public MyUpgradeModifierType ModifierType;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_UpgradeModuleDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public MyUpgradeModuleInfo[] Upgrades;
    }
}
