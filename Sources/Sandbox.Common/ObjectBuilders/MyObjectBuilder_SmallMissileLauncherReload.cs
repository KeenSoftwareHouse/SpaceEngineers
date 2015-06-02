using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using VRage.ObjectBuilders;


namespace Sandbox.Common.ObjectBuilders
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_SmallMissileLauncherReload : MyObjectBuilder_SmallMissileLauncher
    {
        //new block type needs to have new object builder
        //e.g. two different blocks can't have same object builder class
    }
}
