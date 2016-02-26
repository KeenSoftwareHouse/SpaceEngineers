using ProtoBuf;
using System;
using VRage.ObjectBuilders;

namespace VRage.Game.ObjectBuilders.ComponentSystem
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [Obsolete("This is here only for backwards compatibility, the component has renamed from Character to Basic!")]
    public class MyObjectBuilder_CraftingComponentCharacter : MyObjectBuilder_CraftingComponentBase
    {
    }
}


