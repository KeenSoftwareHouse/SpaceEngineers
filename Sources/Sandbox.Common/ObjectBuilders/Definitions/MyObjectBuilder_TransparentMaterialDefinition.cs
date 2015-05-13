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
        [ProtoMember(1), ModdableContentFile("dds")]
        public string Texture;

        [ProtoMember(2)]
        public bool CanBeAffectedByLights;

        [ProtoMember(3)]
        public bool AlphaMistingEnable;

        [ProtoMember(4)]
        public bool IgnoreDepth;

        [ProtoMember(5)]
        public bool NeedSort;

        [ProtoMember(6)]
        public bool UseAtlas;

        [ProtoMember(7)]
        public float AlphaMistingStart;

        [ProtoMember(8)]
        public float AlphaMistingEnd;

        [ProtoMember(9)]
        public float SoftParticleDistanceScale;

        [ProtoMember(10)]
        public float Emissivity;

        [ProtoMember(11)]
        public float AlphaSaturation;

        [ProtoMember(12)]
        public bool Reflection;

        [ProtoMember(13)]
        public Vector4 Color = Vector4.One;

        [ProtoMember(14)]
        public float Reflectivity;
    }
}
