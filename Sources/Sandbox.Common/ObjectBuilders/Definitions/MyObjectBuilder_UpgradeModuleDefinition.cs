using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        [ProtoMember(1)]
        public string UpgradeType;
        [ProtoMember(2)]
        public float Modifier;
        [ProtoMember(3)]
        public MyUpgradeModifierType ModifierType;
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_UpgradeModuleDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember(1)]
        public MyUpgradeModuleInfo[] Upgrades;
    }
}
