using ProtoBuf;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_SoundBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public float Range = 50;

        [ProtoMember]
        public float Volume = 1;

        [ProtoMember]
        [Nullable]
        public string CueName = null;

        [ProtoMember]
        public float LoopPeriod = 1f;

        [ProtoMember]
        public bool IsPlaying = false;
    }
}
