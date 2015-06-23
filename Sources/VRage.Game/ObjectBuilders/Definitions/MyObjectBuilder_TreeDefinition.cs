using System;
using VRage.ObjectBuilders;
using ProtoBuf;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Common.ObjectBuilders.Definitions;


namespace VRage.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TreeDefinition : MyObjectBuilder_EnvironmentItemDefinition
    {
        // Distance [m] from tree origin to first log with branches
        [ProtoMember]
        public float BranchesStartHeight = 0.0f;
    }
}
