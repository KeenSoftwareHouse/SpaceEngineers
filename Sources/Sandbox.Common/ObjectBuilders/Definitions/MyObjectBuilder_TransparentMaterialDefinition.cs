using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using VRageMath;
using VRage.Data;

namespace Sandbox.Common.ObjectBuilders.Definitions
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TransparentMaterialDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember, ModdableContentFile("dds")]
        public string Texture;

        [ProtoMember]
        public bool CanBeAffectedByLights;

        [ProtoMember]
        public bool AlphaMistingEnable;

        [ProtoMember]
        public bool IgnoreDepth;

        [ProtoMember]
        public bool NeedSort;

        [ProtoMember]
        public bool UseAtlas;

        [ProtoMember]
        public float AlphaMistingStart;

        [ProtoMember]
        public float AlphaMistingEnd;

        [ProtoMember]
        public float SoftParticleDistanceScale;

        [ProtoMember]
        public float Emissivity;

        [ProtoMember]
        public float AlphaSaturation;

        [ProtoMember]
        public bool Reflection;

        [ProtoMember]
        public Vector4 Color = Vector4.One;

        [ProtoMember]
        public float Reflectivity;
    }
}
