using SharpDX.DXGI;
using VRageMath;

namespace VRageRender
{
    static class MyRender11Constants
    {
        public const Format DX11_BACKBUFFER_FORMAT = Format.R8G8B8A8_UNorm_SRgb;
        public const int BUFFER_COUNT = 2;

        public const int SHADER_MAX_BONES = 60;
        public const int MAX_POINT_LIGHTS = 256;
        public const int MAX_SPOTLIGHTS = 128;

        public const int CUBE_INSTANCE_BONES_NUM = 8;

        public static readonly Vector3 PRUNNING_EXTENSION = new Vector3(10, 10, 10);
    }
}
