using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    static class MyEnvironment
    {
        internal static Vector3D CameraPosition;
        internal static Matrix ViewAt0;
        internal static Matrix InvViewAt0;
        internal static Matrix ViewProjectionAt0;
        internal static Matrix InvViewProjectionAt0;

        internal static Matrix View;
        internal static Matrix InvView;
        internal static Matrix Projection;
        internal static Matrix InvProjection;
        internal static Matrix ViewProjection;
        internal static Matrix InvViewProjection;

        internal static MatrixD ViewD;
        internal static MatrixD InvViewD;
        internal static MatrixD ProjectionD;
        internal static MatrixD InvProjectionD;
        internal static MatrixD ViewProjectionD;
        internal static MatrixD InvViewProjectionD;

        internal static float NearClipping;
        internal static float LargeDistanceFarClipping;
        internal static float FarClipping;
        internal static float FovY;

        internal static BoundingFrustum ViewFrustum;
        internal static BoundingFrustumD ViewFrustumD;

        static Vector3 m_directionalLightIntensity;
        internal static bool DirectionalLightEnabled;

        internal static MyRenderFogSettings FogSettings;

        internal static float DayTime;

        internal static Vector3 DirectionalLightDir;
        internal static Vector3 DirectionalLightIntensity;

        internal static Vector3 SunColor;
        internal static float   SunDistance;
        internal static string  SunMaterial;
        internal static float   SunSizeMultiplier;
        internal static bool    SunBillboardEnabled;

        internal static string DaySkybox = "Textures/BackgroundCube/Final/BackgroundCube_skybox.dds";
        internal static string DaySkyboxPrefiltered = "Textures/BackgroundCube/Final/BackgroundCube_skybox_prefiltered.dds";

        internal static string NightSkybox = "Textures/BackgroundCube/Final/night/BackgroundCube_skybox.dds";
        internal static string NightSkyboxPrefiltered = "Textures/BackgroundCube/Final/night/BackgroundCube_skybox_prefiltered.dds";
    }
}
