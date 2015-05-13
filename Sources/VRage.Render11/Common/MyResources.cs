using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    static class MyResources
    {
        internal static MyConstantBuffer FrameConstants { get; set; }
        internal static MyConstantBuffer ProjectionConstants { get; set; }
        internal static MyConstantBuffer ObjectConstants { get; set; }
        internal static MyConstantBuffer BonesConstants { get; set; }
        internal static MyConstantBuffer LocalTransformConstants { get; set; }

        internal static MyConstantBuffer FoliageConstants { get; set; }

        internal unsafe static void Init()
        {
            FrameConstants = MyRender.WrapResource(new MyConstantBuffer(sizeof(MyFrameConstantsLayout)), "frame constants");
            ProjectionConstants = MyRender.WrapResource(new MyConstantBuffer(sizeof(Matrix)), "projection constants");
            ObjectConstants = MyRender.WrapResource(new MyConstantBuffer(sizeof(Matrix)), "object constants");
            BonesConstants = MyRender.WrapResource(new MyConstantBuffer(sizeof(Matrix) * MyRenderConstants.SHADER_MAX_BONES), "bones constants");
            LocalTransformConstants = MyRender.WrapResource(new MyConstantBuffer(32), "local transform constants");

            // temporary? merge with frame
            FoliageConstants = MyRender.WrapResource(new MyConstantBuffer(16), "folaige constants");
        }
    }

    static class MyEnvironment
    {
        internal static Vector3 CameraPosition;
        internal static Matrix CameraView;
        internal static Matrix Projection;
        internal static float NearClipping;
        internal static float FarClipping;
        internal static float FovY;
    }
}
