using ProtoBuf;
using System.ComponentModel;
using VRage.Data;
using VRage.ObjectBuilders;
using VRageMath;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_ModelComponentDefinition : MyObjectBuilder_ComponentDefinitionBase
    {
        [ProtoMember]
        public Vector3 Size; // in meters

        [ProtoMember]
        public float Mass; // in kg

        [ProtoMember, DefaultValue(null)]
        public float? Volume = null; // in dm3 (liters)

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Model = @"Models\Components\Sphere.mwm";
    }
}
