using ProtoBuf;
using VRage.Serialization;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ScriptManager : MyObjectBuilder_SessionComponent
    {
        [ProtoMember]
        public SerializableDictionary<string, object> variables = new SerializableDictionary<string,object>();
    }
}

