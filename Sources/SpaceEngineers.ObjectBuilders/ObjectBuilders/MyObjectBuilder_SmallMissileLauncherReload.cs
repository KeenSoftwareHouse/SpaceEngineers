using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using VRage.ObjectBuilders;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    [System.Xml.Serialization.XmlSerializerAssembly("SpaceEngineers.ObjectBuilders.XmlSerializers")]
    public class MyObjectBuilder_SmallMissileLauncherReload : MyObjectBuilder_SmallMissileLauncher
    {
        //new block type needs to have new object builder
        //e.g. two different blocks can't have same object builder class
    }
}
