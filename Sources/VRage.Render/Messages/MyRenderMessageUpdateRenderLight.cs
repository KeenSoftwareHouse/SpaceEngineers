using System.Runtime.InteropServices;
using VRage.Utils;
using VRageMath;
using VRageRender.Lights;

namespace VRageRender.Messages
{
    public class MyRenderMessageUpdateRenderLight : MyRenderMessageBase
    {
        public UpdateRenderLightData Data;

        public override MyRenderMessageType MessageClass { get { return MyRenderMessageType.StateChangeOnce; } }
        public override MyRenderMessageEnum MessageType { get { return MyRenderMessageEnum.UpdateRenderLight; } }
    }

    public struct UpdateRenderLightData
    {
        public uint ID;
        public LightTypeEnum Type;
        public Vector3D Position;   // Same base position for all light types
        public int ParentID;

        // CHECK-ME: SpecularColor seems to be not used in Render11 but is set
        public Vector3 SpecularColor;
        public float PointPositionOffset;
        public bool UseInForwardRender;
        public float ReflectorConeMaxAngleCos;
        public float ShadowDistance;
        public bool CastShadows;

        public bool PointLightOn;
        public float PointLightIntensity;
        public MyLightLayout PointLight;

        public bool SpotLightOn;
        public float SpotLightIntensity;
        public MySpotLightLayout SpotLight;
        public string ReflectorTexture;

        public MyFlareDesc Glare;
    }

    public struct MyFlareDesc
    {
        public bool Enabled;
        public Vector3 Direction;
        public float Range;
        public Vector4 Color;
        public float Intensity;
        public MyStringId Material;
        public float MaxDistance;
        public float Size;
        public float QuerySize;
        public float QueryFreqMinMs;
        public float QueryFreqRndMs;
        public MyGlareTypeEnum Type;
        public int ParentGID;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MySpotLightLayout
    {
        // CHECK-ME: SpotLight Falloff seems to be not used in Render11 but is set
        public MyLightLayout Light;

        public Vector3 Up;
        public float ApertureCos;

        public Vector3 Direction;
        public float ShadowsRange;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MyLightLayout
    {
        public Vector3 Position;
        public float Range;

        public Vector3 Color;
        public float Falloff;

        public float GlossFactor;
        public float DiffuseFactor;
        public Vector2 _pad;
    }
}
