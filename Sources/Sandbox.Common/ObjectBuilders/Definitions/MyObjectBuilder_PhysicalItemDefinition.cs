using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRageMath;
using System.Xml.Serialization;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_PhysicalItemDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember]
        public Vector3 Size; // in meters

        [ProtoMember]
        public float Mass; // in Kg

        [ProtoMember]
        [ModdableContentFile("mwm")]
        public string Model = @"Models\Components\Sphere.mwm";

        [ProtoMember, DefaultValue(null)]
        public string IconSymbol = null;
        public bool ShouldSerializeIconSymbol() { return IconSymbol != null; }

        [ProtoMember, DefaultValue(null)]
        public float? Volume = null; // in liters

        [ProtoMember]
        public string PhysicalMaterial;
    }
}
