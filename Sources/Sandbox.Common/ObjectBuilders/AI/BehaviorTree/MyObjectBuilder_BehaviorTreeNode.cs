using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sandbox.Common.ObjectBuilders.AI
{
    public enum MyBehaviorTreeState : sbyte
    { // keep order
        ERROR = -1,
        NOT_TICKED = 0,
        SUCCESS,
        FAILURE,
        RUNNING,
    }

    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_BehaviorTreeNode : MyObjectBuilder_Base
    {
    }
}
