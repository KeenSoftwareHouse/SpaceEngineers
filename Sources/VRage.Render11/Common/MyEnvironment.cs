using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    class MyEnvironmentMatrices
    {
        internal Vector3D CameraPosition;
        internal Matrix ViewAt0;
        internal Matrix InvViewAt0;
        internal Matrix ViewProjectionAt0;
        internal Matrix InvViewProjectionAt0;

        internal Matrix View;
        internal Matrix InvView;
        internal Matrix Projection;
        internal Matrix InvProjection;
        internal Matrix ViewProjection;
        internal Matrix InvViewProjection;

        internal MatrixD ViewD;
        //internal MatrixD InvViewD;
        internal MatrixD OriginalProjectionD;
        //internal MatrixD InvProjectionD;
        internal MatrixD ViewProjectionD;
        //internal MatrixD InvViewProjectionD;

        internal float NearClipping;
        internal float LargeDistanceFarClipping;
        internal float FarClipping;
        internal float FovY;

        internal BoundingFrustumD ViewFrustumD;
        internal BoundingFrustumD ViewFrustumClippedD;
    }

    class MyEnvironment: MyEnvironmentMatrices
    {
        Vector3 m_directionalLightIntensity;

        internal MyRenderFogSettings FogSettings;

        internal float DayTime;

        internal Vector3 DirectionalLightDir;
        internal Vector3 DirectionalLightIntensity;

        internal Vector3 SunColor;
        internal float   SunDistance;
        internal string  SunMaterial;
        internal float   SunSizeMultiplier;
        internal bool    SunBillboardEnabled;
        internal float[]   AdditionalSunIntensities;
        internal Vector3[] AdditionalSunColors;
        internal Vector2[] AdditionalSunDirections;
        internal float   PlanetFactor;

        internal string DaySkybox = null;

		internal string NightSkybox = null;
		internal string NightSkyboxPrefiltered = null;

        internal Quaternion BackgroundOrientation;
		internal Color BackgroundColor = Color.Black;
    }
}
