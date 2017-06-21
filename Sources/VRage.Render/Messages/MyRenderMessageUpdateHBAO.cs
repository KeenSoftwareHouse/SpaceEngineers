using VRage;

namespace VRageRender.Messages
{
    // the structure is directly copied to shader buffers - watch for padding!
    public struct MyHBAOData
    {
        public bool Enabled;

        // The AO radius in meters
        public float Radius;
        // To hide low-tessellation artifacts // 0.0~0.5
        public float Bias;
        // Scale factor for the small-scale AO, the greater the darker // 0.0~4.0
        public float SmallScaleAO;
        // Scale factor for the large-scale AO, the greater the darker // 0.0~4.0
        public float LargeScaleAO;
        // The final AO output is pow(AO, powerExponent) // 1.0~8.0
        public float PowerExponent;

        // To limit the occlusion scale in the foreground; Enabling this may have a small performance impact
        public bool ForegroundAOEnable;
        // View-space depth at which the AO footprint should get clamped
        public float ForegroundViewDepth;

        // To add larger-scale occlusion in the distance; Enabling this may have a small performance impact
        public bool BackgroundAOEnable;
        // Adapt BackgroundViewDepth when very low FOV is used?
        public bool AdaptToFOV;
        // View-space depth at which the AO footprint should stop falling off with depth
        public float BackgroundViewDepth;

        // To hide possible false-occlusion artifacts near screen borders:
        // true: may cause false occlusion near screen borders
        // false: may cause halos near screen borders
        public bool DepthClampToEdge;

        // To return white AO for ViewDepths > MaxViewDepth
        public bool DepthThresholdEnable;
        // Custom view-depth threshold   
        public float DepthThreshold;
        // The higher, the sharper are the AO-to-white transitions
        public float DepthThresholdSharpness;

        // Optional AO blur, to blur the AO before compositing it
        // To blur the AO with an edge-preserving blur
        public bool BlurEnable;
        // blur radius 4 or 2?
        public bool BlurRadius4;
        // The higher, the more the blur preserves edges // 0.0~16.0
        public float BlurSharpness;

        // Optional depth-dependent sharpness function
        // To make the blur sharper in the foreground
        public bool BlurSharpnessFunctionEnable;
        // Sharpness scale factor for ViewDepths <= ForegroundViewDepth
        public float BlurSharpnessFunctionForegroundScale;
        // Maximum view depth of the foreground depth range
        public float BlurSharpnessFunctionForegroundViewDepth;
        // Minimum view depth of the background depth range
        public float BlurSharpnessFunctionBackgroundViewDepth;

        [StructDefault]
        public static readonly MyHBAOData Default;

        static MyHBAOData()
        {
            Default = new MyHBAOData()
            {
                Enabled = true,

                Radius = 2.0f,
                Bias = 0.200000003f,
                SmallScaleAO = 1.0f,
                LargeScaleAO = 1.0f,
                PowerExponent = 5.0f,

                DepthClampToEdge = false,

                DepthThresholdEnable = false,
                ForegroundAOEnable = true,
                ForegroundViewDepth = 7.0f,

                BackgroundAOEnable = true,
                AdaptToFOV = true,
                BackgroundViewDepth = 200,

                DepthThreshold = 0,
                DepthThresholdSharpness = 100.0f,

                BlurEnable = true,
                BlurRadius4 = true,
                BlurSharpness = 1.0f,

                BlurSharpnessFunctionEnable = false,
                BlurSharpnessFunctionForegroundScale = 4.0f,
                BlurSharpnessFunctionForegroundViewDepth = 0,
                BlurSharpnessFunctionBackgroundViewDepth = 1.0f,
            };
        }
    }

    public class MyRenderMessageUpdateHBAO : MyRenderMessageBase
    {
        public MyHBAOData Settings;

        public override MyRenderMessageType MessageClass
        {
            get { return MyRenderMessageType.StateChangeOnce; }
        }

        public override MyRenderMessageEnum MessageType
        {
            get { return MyRenderMessageEnum.UpdateHBAO; }
        }
    }
}
