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
        [ProtoMember(1)]
        public Vector3 Size; // in meters

        [ProtoMember(2)]
        public float Mass; // in Kg

        [ProtoMember(3)]
        [ModdableContentFile("mwm")]
        public string Model = @"Models\Components\Sphere.mwm";

        [ProtoMember(4), DefaultValue(null)]
        public string IconSymbol = null;
        public bool ShouldSerializeIconSymbol() { return IconSymbol != null; }

        [ProtoMember(5), DefaultValue(null)]
        public float? Volume = null; // in liters

        [ProtoMember(6)]
        public string PhysicalMaterial;
    }
}
