using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.ModAPI.Ingame;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    public struct MyUpgradeModuleInfo : IMyUpgradeInfo
    {
        [ProtoMember]
        public string UpgradeType { get; set; }
        [ProtoMember]
        public float Modifier { get; set; }
        [ProtoMember]
        public MyUpgradeModifierType ModifierType { get; set; }
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_UpgradeModuleDefinition : MyObjectBuilder_CubeBlockDefinition
    {
        [ProtoMember]
        public MyUpgradeModuleInfo[] Upgrades;
    }
}
