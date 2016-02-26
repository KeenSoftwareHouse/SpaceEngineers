using System.ComponentModel;
using ProtoBuf;
using VRageMath;
using VRage.ObjectBuilders;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TransparentMaterial : MyObjectBuilder_Base
    {
        [ProtoMember]
        public string Name;

        [ProtoMember]
        public bool AlphaMistingEnable;

        [ProtoMember, DefaultValue(1)]
        public float AlphaMistingStart = 1;

        [ProtoMember, DefaultValue(4)]
        public float AlphaMistingEnd = 4;

        [ProtoMember, DefaultValue(1)]
        public float AlphaSaturation = 1;

        [ProtoMember]
        public bool CanBeAffectedByOtherLights;

        [ProtoMember]
        public float Emissivity;

        [ProtoMember]
        public bool IgnoreDepth;

        [ProtoMember, DefaultValue(true)]
        public bool NeedSort = true;

        [ProtoMember]
        public float SoftParticleDistanceScale;

        [ProtoMember]
        public string Texture;

        [ProtoMember]
        public bool UseAtlas;

        [ProtoMember]
        public Vector2 UVOffset = new Vector2(0, 0);

        [ProtoMember]
        public Vector2 UVSize = new Vector2(1, 1);

        [ProtoMember]
        public bool Reflection;
    }
}