﻿using ProtoBuf;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_InputScriptNode : MyObjectBuilder_EventScriptNode
    {
    }
}