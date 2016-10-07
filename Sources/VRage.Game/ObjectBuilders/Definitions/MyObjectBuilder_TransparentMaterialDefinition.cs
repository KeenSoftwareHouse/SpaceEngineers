using VRage.ObjectBuilders;
using ProtoBuf;
using VRageMath;
using VRage.Data;
using VRageRender;

namespace VRage.Game
{
    [ProtoContract]
    [MyObjectBuilderDefinition]
    public class MyObjectBuilder_TransparentMaterialDefinition : MyObjectBuilder_DefinitionBase
    {
        [ProtoMember, ModdableContentFile("dds")]
        public string Texture;

        [ProtoMember]
        public MyTransparentMaterialTextureType TextureType = MyTransparentMaterialTextureType.FileTexture;

        [ProtoMember]
        public bool CanBeAffectedByOtherLights;

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

        [ProtoMember]
        public bool AlphaCutout;

        [ProtoMember]
        public Vector2I TargetSize = new Vector2I(-1, -1);
    }
}
