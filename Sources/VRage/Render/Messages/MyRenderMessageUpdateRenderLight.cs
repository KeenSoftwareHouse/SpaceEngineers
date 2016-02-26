using VRageMath;
using VRageRender.Lights;

namespace VRageRender
{
    public class MyRenderMessageUpdateRenderLight : MyRenderMessageBase
    {
        public uint ID;
        public LightTypeEnum Type;
        public Vector3D Position;
        public int ParentID;
        public float Offset;
        public Color Color;
        public Color SpecularColor;
        public float Falloff;
        public float Range;
        public float Intensity;
        public bool LightOn;
        public bool UseInForwardRender;
        public float ReflectorIntensity;
        public bool ReflectorOn;
        public Vector3 ReflectorDirection;
        public Vector3 ReflectorUp;
        public float ReflectorConeMaxAngleCos;
        public Color ReflectorColor;
        public float ReflectorRange;
        public float ReflectorFalloff;
        public string ReflectorTexture;
        public float ShadowDistance;
        public bool CastShadows;
        public bool GlareOn;
        public MyGlareTypeEnum GlareType;
        public float GlareSize;
        public float GlareQuerySize;
        public float GlareIntensity;
        public string GlareMaterial;
        public float GlareMaxDistance;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderLight; } }
    }
}
