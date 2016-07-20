using System.Collections.Generic;
using ProtoBuf;
using System.Xml.Serialization;
using System.ComponentModel;
using VRage.ObjectBuilders;
using VRage.Serialization;


namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_GhostCharacter : MyObjectBuilder_EntityBase
    {
    }
}