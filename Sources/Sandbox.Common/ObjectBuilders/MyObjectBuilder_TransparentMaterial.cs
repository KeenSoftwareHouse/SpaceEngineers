using System.ComponentModel;
using ProtoBuf;
using VRageMath;

namespace Sandbox.Common.ObjectBuilders.VRageData
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TransparentMaterial : MyObjectBuilder_Base
    {
        [ProtoMember(1)]
        public string Name;

        [ProtoMember(2)]
        public bool AlphaMistingEnable;

        [ProtoMember(3), DefaultValue(1)]
        public float AlphaMistingStart = 1;

        [ProtoMember(4), DefaultValue(4)]
        public float AlphaMistingEnd = 4;

        [ProtoMember(5), DefaultValue(1)]
        public float AlphaSaturation = 1;

        [ProtoMember(6)]
        public bool CanBeAffectedByOtherLights;

        [ProtoMember(7)]
        public float Emissivity;

        [ProtoMember(8)]
        public bool IgnoreDepth;

        [ProtoMember(9), DefaultValue(true)]
        public bool NeedSort = true;

        [ProtoMember(10)]
        public float SoftParticleDistanceScale;

        [ProtoMember(11)]
        public string Texture;

        [ProtoMember(12)]
        public bool UseAtlas;

        [ProtoMember(13)]
        public Vector2 UVOffset = new Vector2(0, 0);

        [ProtoMember(14)]
        public Vector2 UVSize = new Vector2(1, 1);

        [ProtoMember(15)]
        public bool Reflection;
    }
}