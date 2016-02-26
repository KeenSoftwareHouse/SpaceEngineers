using VRage.ObjectBuilders;
using ProtoBuf;
using VRage.Game;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_CameraBlock : MyObjectBuilder_FunctionalBlock
    {
        [ProtoMember]
        public bool IsActive;

        //By default set to maximum FOV value
        //Will get clamped to actual FOV in init
        [ProtoMember]
        public float Fov = 90;
    }
}
