using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using VRage.Library.Utils;
using VRage.Voxels;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
    public struct MyTextureDebugMultipliers
    {
        public float RgbMultiplier;
        public float MetalnessMultiplier;
        public float GlossMultiplier;
        public float AoMultiplier;

        public float EmissiveMultiplier;
        public float ColorMaskMultiplier;
        public float __padding1;
        public float __padding2;

        public static MyTextureDebugMultipliers Defaults = new MyTextureDebugMultipliers
        {
            RgbMultiplier = 1.0f,
            MetalnessMultiplier = 1.0f,
            GlossMultiplier = 1.0f,
            AoMultiplier = 1.0f,
            EmissiveMultiplier = 1.0f,
            ColorMaskMultiplier = 1.0f,
        };
    }

    static class MyCommon
    {
        // constant buffers
        internal const int FRAME_SLOT = 0;
        internal const int PROJECTION_SLOT = 1;
        internal const int OBJECT_SLOT = 2;
        internal const int MATERIAL_SLOT = 3;
        internal const int FOLIAGE_SLOT = 4;
        internal const int ALPHAMASK_VIEWS_SLOT = 5;

        // srvs
            // geometry
        internal const int BIG_TABLE_INDICES = 10;
        internal const int BIG_TABLE_VERTEX_POSITION = 11;
        internal const int BIG_TABLE_VERTEX = 12;
        internal const int INSTANCE_INDIRECTION = 13;
        internal const int INSTANCE_DATA = 14;
            // lighting
        internal const int SKYBOX_SLOT = 10;
        internal const int SKYBOX_IBL_SLOT = 11;
        internal const int SKYBOX2_SLOT = 17;
        internal const int SKYBOX2_IBL_SLOT = 18;
        internal const int AO_SLOT = 12;
        internal const int POINTLIGHT_SLOT = 13;
        internal const int TILE_LIGHT_INDICES_SLOT = 14;
        internal const int CASCADES_SM_SLOT = 15;
        internal const int AMBIENT_BRDF_LUT_SLOT = 16;
        internal const int SHADOW_SLOT = 19;
        internal const int MATERIAL_BUFFER_SLOT = 20;
        internal const int DITHER_8X8_SLOT = 28;

        // samplers
        internal const int SHADOW_SAMPLER_SLOT = 15;

        internal static ConstantsBufferId FrameConstantsStereoLeftEye { get; set; }
        internal static ConstantsBufferId FrameConstantsStereoRightEye { get; set; }
        
        internal static ConstantsBufferId FrameConstants { get; set; }
        internal static ConstantsBufferId ProjectionConstants { get; set; }
        internal static ConstantsBufferId ObjectConstants { get; set; }
        internal static ConstantsBufferId FoliageConstants { get; set; }
        internal static ConstantsBufferId MaterialFoliageTableConstants { get; set; }
        internal static ConstantsBufferId OutlineConstants { get; set; }
        internal static ConstantsBufferId AlphamaskViewsConstants { get; set; }

        internal static UInt64 FrameCounter = 0;

        static MyCommon()
        {
            m_timer = new Stopwatch();
            m_timer.Start();
        }
        internal unsafe static void Init()
        {
            FrameConstantsStereoLeftEye = MyHwBuffers.CreateConstantsBuffer(sizeof(MyFrameConstantsLayout), "FrameConstantsStereoLeftEye");
            FrameConstantsStereoRightEye = MyHwBuffers.CreateConstantsBuffer(sizeof(MyFrameConstantsLayout), "FrameConstantsStereoRightEye");

            FrameConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(MyFrameConstantsLayout), "FrameConstants");
            ProjectionConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix), "ProjectionConstants");
            ObjectConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix), "ObjectConstants");
            FoliageConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Vector4), "FoliageConstants");
            MaterialFoliageTableConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Vector4) * 256, "MaterialFoliageTableConstants");
            OutlineConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(OutlineConstantsLayout), "OutlineConstants");
            AlphamaskViewsConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix) * 181, "AlphamaskViewsConstants");

            UpdateAlphamaskViewsConstants();
        }

        internal static ShaderResourceView GetAmbientBrdfLut()
        {
            return MyTextures.GetView(MyTextures.GetTexture("Textures/Miscellaneous/ambient_brdf.dds", MyTextureEnum.CUSTOM, true));
        }

        internal static Dictionary<int, ConstantsBufferId> m_objectsConstantBuffers = new Dictionary<int, ConstantsBufferId>();

        internal static ConstantsBufferId GetObjectCB(int size)
        {
            // align size
            size = ((size + 15)/16)*16;
            if(!m_objectsConstantBuffers.ContainsKey(size))
            {
                m_objectsConstantBuffers[size] = MyHwBuffers.CreateConstantsBuffer(size, "CommonObjectCB" + size);

            }
            return m_objectsConstantBuffers[size];
        }

        internal static Dictionary<int, ConstantsBufferId> m_materialsConstantBuffers = new Dictionary<int, ConstantsBufferId>();

        internal static ConstantsBufferId GetMaterialCB(int size)
        {
            // align size
            size = ((size + 15) / 16) * 16;
            if (!m_materialsConstantBuffers.ContainsKey(size))
            {
                m_materialsConstantBuffers[size] = MyHwBuffers.CreateConstantsBuffer(size, "CommonMaterialCB" + size);

            }
            return m_materialsConstantBuffers[size];
        }

        struct MyFrameConstantsLayout
        {
            internal Matrix ViewProjection;
            internal Matrix View;
            internal Matrix Projection;
            internal Matrix InvView;
            internal Matrix InvProjection;
            internal Matrix InvViewProjection;
            internal Matrix ViewProjectionWorld;
            internal Vector4 WorldOffset;

            internal Vector3 EyeOffsetInWorld;
            internal float  _paddingX;

            internal Vector2I GBufferOffset;
            internal Vector2I ResolutionOfGBuffer;

            internal Vector2 Resolution;
            internal float Time;
            internal float TimeDelta;

            internal Vector4 TerrainTextureDistances;

            internal Vector2 TerrainDetailRange;
            internal uint TilesNum;
            internal uint TilesX;

            internal Vector4 FoliageClippingScaling;
            internal Vector3 WindVector;
            internal float Tau;

            internal float BacklightMult;
            internal float EnvMult;
            internal float Contrast;
            internal float Brightness;

            internal float MiddleGrey;
            internal float LuminanceExposure;
            internal float BloomExposure;
            internal float BloomMult;

            internal float MiddleGreyCurveSharpness;
            internal float MiddleGreyAt0;
            internal float BlueShiftRapidness;
            internal float BlueShiftScale;

            internal float FogDensity;
            internal float FogMult;
            internal float FogYOffset;
            internal uint FogColor;

            internal Vector3 DirectionalLightDir;
            internal float SkyboxBlend;

            internal Vector3 DirectionalLightColor;
            internal float ForwardPassAmbient;

            internal Vector3 AdditionalSunColor;
            internal float AdditionalSunIntensity;

            internal Vector4 SecondarySunDirection1;
            internal Vector4 SecondarySunDirection2;
            internal Vector4 SecondarySunDirection3;
            internal Vector4 SecondarySunDirection4;
            internal Vector4 SecondarySunDirection5;
            internal int AdditionalSunCount;
            internal Vector3 _Padding1;

            internal float Tonemapping_A;
            internal float Tonemapping_B;
            internal float Tonemapping_C;
            internal float Tonemapping_D;

            internal float Tonemapping_E;
            internal float Tonemapping_F;
            internal float LogLumThreshold;
            internal float DebugVoxelLod;
            
            internal Vector4 VoxelLodRange0;
            internal Vector4 VoxelLodRange1;
            internal Vector4 VoxelLodRange2;
            internal Vector4 VoxelLodRange3;

            internal Vector4 VoxelMassiveLodRange0;
            internal Vector4 VoxelMassiveLodRange1;
            internal Vector4 VoxelMassiveLodRange2;
            internal Vector4 VoxelMassiveLodRange3;
            internal Vector4 VoxelMassiveLodRange4;
            internal Vector4 VoxelMassiveLodRange5;
            internal Vector4 VoxelMassiveLodRange6;
            internal Vector4 VoxelMassiveLodRange7;

            internal float SkyboxBrightness;
			internal float ShadowFadeout;
            internal float FrameTimeDelta;
            internal float RandomSeed;

            internal float EnableVoxelAo;
            internal float VoxelAoMin;
            internal float VoxelAoMax;
            internal float VoxelAoOffset;

            internal Matrix BackgroundOrientation;

            internal MyTextureDebugMultipliers TextureDebugMultipliers;

            internal Vector3 CameraPositionDelta;
            internal float _Padding2;
        }

        internal static void MoveToNextFrame()
        {
            FrameCounter++;
        }

        static int m_lastGameplayFrame;
        static int m_lastFrameGameplayUpdate;
        static MyRandom m_random = new MyRandom();
        const float MAX_FRAMETIME = 66.0f;
        static float m_lastFrameDelta = 0;
        static float m_lastFrameTime = 0;
        static Stopwatch m_timer;
        static Vector3D m_lastCameraPosition;

        internal static float TimerMs { get { return (float)(m_timer.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0); } }
        internal static float LastFrameDelta() { return m_lastFrameDelta; }

        private static void UpdateFrameConstantsInternal(MyEnvironmentMatrices envMatrices, ref MyFrameConstantsLayout constants, MyStereoRegion typeofFC)
        {
            constants.View = Matrix.Transpose(envMatrices.ViewAt0);
            constants.Projection = Matrix.Transpose(envMatrices.Projection);
            constants.ViewProjection = Matrix.Transpose(envMatrices.ViewProjectionAt0);
            constants.InvView = Matrix.Transpose(envMatrices.InvViewAt0);
            constants.InvProjection = Matrix.Transpose(envMatrices.InvProjection);
            constants.InvViewProjection = Matrix.Transpose(envMatrices.InvViewProjectionAt0);
            constants.ViewProjectionWorld = Matrix.Transpose(envMatrices.ViewProjection);
            constants.WorldOffset = new Vector4(envMatrices.CameraPosition, 0);

            constants.Resolution = MyRender11.ResolutionF;
            if (typeofFC != MyStereoRegion.FULLSCREEN)
            {
                constants.Resolution.X /= 2;

                Vector3 eyeOffset = new Vector3(envMatrices.ViewAt0.M41, envMatrices.ViewAt0.M42, envMatrices.ViewAt0.M43);
                Vector3 eyeOffsetInWorld = Vector3.Transform(eyeOffset, Matrix.Transpose(MyRender11.Environment.ViewAt0));
                constants.EyeOffsetInWorld = eyeOffsetInWorld;
            }

            constants.GBufferOffset = new Vector2I(0, 0);
            if (typeofFC == MyStereoRegion.RIGHT)
                constants.GBufferOffset.X = MyRender11.ResolutionI.X / 2;

            constants.ResolutionOfGBuffer = MyRender11.ResolutionI;
        }

        internal static void UpdateFrameConstants()
        {
            MyFrameConstantsLayout constants = new MyFrameConstantsLayout();
            UpdateFrameConstantsInternal(MyRender11.Environment, ref constants, MyStereoRegion.FULLSCREEN);

            float skyboxBlend = 1 - 2 * (float)(Math.Abs(-MyRender11.Environment.DayTime + 0.5));

            constants.TerrainTextureDistances = new Vector4(
                MyRender11.Settings.TerrainDetailD0,
                1.0f / (MyRender11.Settings.TerrainDetailD1 - MyRender11.Settings.TerrainDetailD0),
                MyRender11.Settings.TerrainDetailD2,
                1.0f / (MyRender11.Settings.TerrainDetailD3 - MyRender11.Settings.TerrainDetailD2));
            
            constants.TerrainDetailRange.X = 0;
            constants.TerrainDetailRange.Y = 0;

            var currentGameplayFrame = MyRender11.Settings.GameplayFrame;
            constants.Time = (float) (currentGameplayFrame) / 60.0f;
            constants.TimeDelta = (float)(currentGameplayFrame - m_lastGameplayFrame) / 60.0f;

            if ((int)FrameCounter != m_lastFrameGameplayUpdate)
            {
                m_lastGameplayFrame = currentGameplayFrame;
                m_lastFrameGameplayUpdate = (int)FrameCounter;
            }

            float time = TimerMs;
            float delta = Math.Min(time - m_lastFrameTime, MAX_FRAMETIME);
            m_lastFrameTime = time;
            m_lastFrameDelta = delta / 1000.0f;
            constants.FrameTimeDelta = m_lastFrameDelta;
            constants.RandomSeed = m_random.NextFloat();

            constants.FoliageClippingScaling = new Vector4(
                //MyRender.Settings.GrassGeometryClippingDistance,
                MyRender11.RenderSettings.FoliageDetails.GrassDrawDistance(),
                MyRender11.Settings.GrassGeometryScalingNearDistance,
                MyRender11.Settings.GrassGeometryScalingFarDistance,
                MyRender11.Settings.GrassGeometryDistanceScalingFactor);
            constants.WindVector = new Vector3(
                (float)Math.Cos(MyRender11.Settings.WindAzimuth * Math.PI / 180.0),
                0, 
                (float)Math.Sin(MyRender11.Settings.WindAzimuth * Math.PI / 180.0)) * MyRender11.Settings.WindStrength;

            constants.Tau = MyRender11.Postprocess.EnableEyeAdaptation ? MyRender11.Postprocess.EyeAdaptationTau : 0;
            constants.BacklightMult = MyRender11.Settings.BacklightMult;
            constants.EnvMult = MyRender11.Settings.EnvMult;
            constants.Contrast = MyRender11.Postprocess.Contrast;
            constants.Brightness = MyRender11.Postprocess.Brightness;
            constants.MiddleGrey = MyRender11.Postprocess.MiddleGrey;
            constants.LuminanceExposure = MyRender11.Postprocess.LuminanceExposure;
            constants.BloomExposure = MyRender11.Postprocess.BloomExposure;
            constants.BloomMult = MyRender11.Postprocess.BloomMult;
            constants.MiddleGreyCurveSharpness = MyRender11.Postprocess.MiddleGreyCurveSharpness;
            constants.MiddleGreyAt0 = MyRender11.Postprocess.MiddleGreyAt0;
            constants.BlueShiftRapidness = MyRender11.Postprocess.BlueShiftRapidness;
            constants.BlueShiftScale = MyRender11.Postprocess.BlueShiftScale;
            constants.FogDensity = MyRender11.Environment.FogSettings.FogDensity;
            constants.FogMult = MyRender11.Environment.FogSettings.FogMultiplier;
            constants.FogYOffset = MyRender11.Settings.FogYOffset;
            constants.FogColor = MyRender11.Environment.FogSettings.FogColor.PackedValue;
            constants.ForwardPassAmbient = MyRender11.Postprocess.ForwardPassAmbient;

            constants.LogLumThreshold = MyRender11.Postprocess.LogLumThreshold + (MyRender11.Postprocess.LogLumThreshold + 2 - MyRender11.Postprocess.LogLumThreshold) * skyboxBlend;
            constants.Tonemapping_A = MyRender11.Postprocess.Tonemapping_A;
            constants.Tonemapping_B = MyRender11.Postprocess.Tonemapping_B;
            constants.Tonemapping_C = MyRender11.Postprocess.Tonemapping_C;
            constants.Tonemapping_D = MyRender11.Postprocess.Tonemapping_D;
            constants.Tonemapping_E = MyRender11.Postprocess.Tonemapping_E;
            constants.Tonemapping_F = MyRender11.Postprocess.Tonemapping_F;


            //if (true)
            //{
            //    constants.Tau = MyRender11.Settings.AdaptationTau;
            //    constants.BacklightMult = MyRender11.Settings.BacklightMult;
            //    constants.EnvMult = MyRender11.Settings.EnvMult;
            //    constants.Contrast = MyRender11.Settings.Contrast;
            //    constants.Brightness = MyRender11.Settings.Brightness;
            //    constants.MiddleGrey = MyRender11.Settings.MiddleGrey;
            //    constants.LuminanceExposure = MyRender11.Settings.LuminanceExposure;
            //    constants.BloomExposure = MyRender11.Settings.BloomExposure;
            //    constants.BloomMult = MyRender11.Settings.BloomMult;
            //    constants.MiddleGreyCurveSharpness = MyRender11.Settings.MiddleGreyCurveSharpness;
            //    constants.MiddleGreyAt0 = MyRender11.Settings.MiddleGreyAt0;
            //    constants.BlueShiftRapidness = MyRender11.Settings.BlueShiftRapidness;
            //    constants.BlueShiftScale = MyRender11.Settings.BlueShiftScale;
            //}

            constants.TilesNum = (uint)MyScreenDependants.TilesNum;
            constants.TilesX = (uint)MyScreenDependants.TilesX;

            constants.DirectionalLightColor = MyRender11.Environment.DirectionalLightIntensity;
            constants.DirectionalLightDir = MyRender11.Environment.DirectionalLightDir;

            int lightIndex = 0;
            if (MyRender11.Environment.AdditionalSunDirections != null && MyRender11.Environment.AdditionalSunDirections.Length > 0)
            {
                constants.AdditionalSunColor = MyRender11.Environment.AdditionalSunColors[0];
                constants.AdditionalSunIntensity = MyRender11.Environment.AdditionalSunIntensities[0];

                if (lightIndex < MyRender11.Environment.AdditionalSunDirections.Length)
                    constants.SecondarySunDirection1 = new Vector4(MathHelper.CalculateVectorOnSphere(MyRender11.Environment.DirectionalLightDir, MyRender11.Environment.AdditionalSunDirections[lightIndex][0], MyRender11.Environment.AdditionalSunDirections[lightIndex][1]), 0);
                ++lightIndex;
                if (lightIndex < MyRender11.Environment.AdditionalSunDirections.Length)
                    constants.SecondarySunDirection2 = new Vector4(MathHelper.CalculateVectorOnSphere(MyRender11.Environment.DirectionalLightDir, MyRender11.Environment.AdditionalSunDirections[lightIndex][0], MyRender11.Environment.AdditionalSunDirections[lightIndex][1]), 0);
                ++lightIndex;
                if (lightIndex < MyRender11.Environment.AdditionalSunDirections.Length)
                    constants.SecondarySunDirection3 = new Vector4(MathHelper.CalculateVectorOnSphere(MyRender11.Environment.DirectionalLightDir, MyRender11.Environment.AdditionalSunDirections[lightIndex][0], MyRender11.Environment.AdditionalSunDirections[lightIndex][1]), 0);
                ++lightIndex;
                if (lightIndex < MyRender11.Environment.AdditionalSunDirections.Length)
                    constants.SecondarySunDirection4 = new Vector4(MathHelper.CalculateVectorOnSphere(MyRender11.Environment.DirectionalLightDir, MyRender11.Environment.AdditionalSunDirections[lightIndex][0], MyRender11.Environment.AdditionalSunDirections[lightIndex][1]), 0);
                ++lightIndex;
                if (lightIndex < MyRender11.Environment.AdditionalSunDirections.Length)
                    constants.SecondarySunDirection5 = new Vector4(MathHelper.CalculateVectorOnSphere(MyRender11.Environment.DirectionalLightDir, MyRender11.Environment.AdditionalSunDirections[lightIndex][0], MyRender11.Environment.AdditionalSunDirections[lightIndex][1]), 0);
                ++lightIndex;
                constants.AdditionalSunCount = MyRender11.Environment.AdditionalSunDirections.Length;
            }
            else
                constants.AdditionalSunCount = 0;

            constants.SkyboxBlend = skyboxBlend;
            constants.SkyboxBrightness = MathHelper.Lerp(1.0f, 0.01f, MyRender11.Environment.PlanetFactor);
			constants.ShadowFadeout = MyRender11.Settings.ShadowFadeoutMultiplier;

            constants.DebugVoxelLod = MyRenderSettings.DebugClipmapLodColor ? 1.0f : 0.0f;
            constants.EnableVoxelAo = MyRenderSettings.EnableVoxelAo ? 1f : 0f;
            constants.VoxelAoMin = MyRenderSettings.VoxelAoMin;
            constants.VoxelAoMax = MyRenderSettings.VoxelAoMax;
            constants.VoxelAoOffset = MyRenderSettings.VoxelAoOffset;

            constants.BackgroundOrientation = Matrix.CreateFromQuaternion(MyRender11.Environment.BackgroundOrientation);

            constants.CameraPositionDelta = MyRender11.Environment.CameraPosition - m_lastCameraPosition;
            m_lastCameraPosition = MyRender11.Environment.CameraPosition;

            constants.TextureDebugMultipliers = new MyTextureDebugMultipliers
            {
                RgbMultiplier = MyRender11.Settings.RgbMultiplier,
                MetalnessMultiplier = MyRender11.Settings.MetalnessMultiplier,
                GlossMultiplier = MyRender11.Settings.GlossMultiplier,
                AoMultiplier = MyRender11.Settings.AoMultiplier,
                EmissiveMultiplier = MyRender11.Settings.EmissiveMultiplier,
                ColorMaskMultiplier = MyRender11.Settings.ColorMaskMultiplier,
            };

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 0, out constants.VoxelLodRange0.X, out constants.VoxelLodRange0.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 1, out constants.VoxelLodRange0.Z, out constants.VoxelLodRange0.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 2, out constants.VoxelLodRange1.X, out constants.VoxelLodRange1.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 3, out constants.VoxelLodRange1.Z, out constants.VoxelLodRange1.W);

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 4, out constants.VoxelLodRange2.X, out constants.VoxelLodRange2.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 5, out constants.VoxelLodRange2.Z, out constants.VoxelLodRange2.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 6, out constants.VoxelLodRange3.X, out constants.VoxelLodRange3.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 7, out constants.VoxelLodRange3.Z, out constants.VoxelLodRange3.W);

            //
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 0, out constants.VoxelMassiveLodRange0.X, out constants.VoxelMassiveLodRange0.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 1, out constants.VoxelMassiveLodRange0.Z, out constants.VoxelMassiveLodRange0.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 2, out constants.VoxelMassiveLodRange1.X, out constants.VoxelMassiveLodRange1.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 3, out constants.VoxelMassiveLodRange1.Z, out constants.VoxelMassiveLodRange1.W);

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 4, out constants.VoxelMassiveLodRange2.X, out constants.VoxelMassiveLodRange2.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 5, out constants.VoxelMassiveLodRange2.Z, out constants.VoxelMassiveLodRange2.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 6, out constants.VoxelMassiveLodRange3.X, out constants.VoxelMassiveLodRange3.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 7, out constants.VoxelMassiveLodRange3.Z, out constants.VoxelMassiveLodRange3.W);

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 8, out constants.VoxelMassiveLodRange4.X, out constants.VoxelMassiveLodRange4.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 9, out constants.VoxelMassiveLodRange4.Z, out constants.VoxelMassiveLodRange4.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 10, out constants.VoxelMassiveLodRange5.X, out constants.VoxelMassiveLodRange5.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 11, out constants.VoxelMassiveLodRange5.Z, out constants.VoxelMassiveLodRange5.W);

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 12, out constants.VoxelMassiveLodRange6.X, out constants.VoxelMassiveLodRange6.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 13, out constants.VoxelMassiveLodRange6.Z, out constants.VoxelMassiveLodRange6.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 14, out constants.VoxelMassiveLodRange7.X, out constants.VoxelMassiveLodRange7.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 15, out constants.VoxelMassiveLodRange7.Z, out constants.VoxelMassiveLodRange7.W);

            var mapping = MyMapping.MapDiscard(MyCommon.FrameConstants);
            mapping.WriteAndPosition(ref constants);
            mapping.Unmap();

            if (MyStereoRender.Enable)
            {
                UpdateFrameConstantsInternal(MyStereoRender.EnvMatricesLeftEye, ref constants, MyStereoRegion.LEFT);
                mapping = MyMapping.MapDiscard(MyCommon.FrameConstantsStereoLeftEye);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();

                UpdateFrameConstantsInternal(MyStereoRender.EnvMatricesRightEye, ref constants, MyStereoRegion.RIGHT);
                mapping = MyMapping.MapDiscard(MyCommon.FrameConstantsStereoRightEye);
                mapping.WriteAndPosition(ref constants);
                mapping.Unmap();
            }
        }


        static string[] s_viewVectorData = new string[]
        {
    "-0.707107,-0.707107,0.000000,0.000000,-0.000000,1.000000,0.707107,-0.707107,-0.000000", 
    "-0.613941,-0.789352,0.000000,0.000000,-0.000000,1.000000,0.789352,-0.613941,-0.000000",
    "-0.707107,-0.707107,0.000000,0.105993,-0.105993,0.850104,0.601114,-0.601114,-0.149896",
    "-0.789352,-0.613940,0.000000,0.000000,-0.000000,1.000000,0.613940,-0.789352,-0.000000",
    "-0.485643,-0.874157,0.000000,0.000000,-0.000000,1.000000,0.874157,-0.485643,-0.000000",
    "-0.600000,-0.800000,0.000000,0.119917,-0.089938,0.850104,0.680083,-0.510062,-0.149896",
    "-0.707107,-0.707107,0.000000,0.188689,-0.188689,0.733154,0.518418,-0.518418,-0.266846",
    "-0.800000,-0.600000,0.000000,0.089938,-0.119917,0.850104,0.510062,-0.680083,-0.149896",
    "-0.874157,-0.485643,0.000000,0.000000,-0.000000,1.000000,0.485643,-0.874157,-0.000000",
    "-0.316228,-0.948683,0.000000,0.000000,-0.000000,1.000000,0.948683,-0.316228,-0.000000",
    "-0.447214,-0.894427,0.000000,0.134071,-0.067036,0.850104,0.760356,-0.380178,-0.149896",
    "-0.581238,-0.813734,0.000000,0.217142,-0.155101,0.733154,0.596592,-0.426137,-0.266846",
    "-0.707107,-0.707107,0.000000,0.258819,-0.258819,0.633975,0.448288,-0.448288,-0.366025",
    "-0.813734,-0.581238,0.000000,0.155101,-0.217142,0.733154,0.426137,-0.596592,-0.266846",
    "-0.894427,-0.447214,0.000000,0.067036,-0.134071,0.850104,0.380178,-0.760356,-0.149896",
    "-0.948683,-0.316228,0.000000,0.000000,-0.000000,1.000000,0.316228,-0.948683,-0.000000",
    "-0.110431,-0.993884,0.000000,0.000000,-0.000000,1.000000,0.993884,-0.110431,-0.000000",
    "-0.242536,-0.970143,0.000000,0.145421,-0.036355,0.850104,0.824722,-0.206180,-0.149896",
    "-0.393919,-0.919145,0.000000,0.245270,-0.105116,0.733154,0.673875,-0.288803,-0.266846",
    "-0.554700,-0.832050,0.000000,0.304552,-0.203034,0.633975,0.527499,-0.351666,-0.366025",
    "-0.707107,-0.707107,0.000000,0.322621,-0.322621,0.543744,0.384485,-0.384485,-0.456256",
    "-0.832050,-0.554700,0.000000,0.203034,-0.304552,0.633975,0.351666,-0.527499,-0.366025",
    "-0.919145,-0.393919,0.000000,0.105116,-0.245270,0.733154,0.288803,-0.673875,-0.266846",
    "-0.970142,-0.242536,0.000000,0.036355,-0.145421,0.850104,0.206181,-0.824722,-0.149896",
    "-0.993884,-0.110432,0.000000,0.000000,-0.000000,1.000000,0.110432,-0.993884,-0.000000",
    "0.110431,-0.993884,0.000000,0.000000,0.000000,1.000000,0.993884,0.110431,-0.000000",
    "-0.000000,-1.000000,0.000000,0.149896,-0.000000,0.850104,0.850104,-0.000000,-0.149896",
    "-0.141422,-0.989949,0.000000,0.264164,-0.037738,0.733154,0.725785,-0.103684,-0.266846",
    "-0.316228,-0.948683,0.000000,0.347242,-0.115747,0.633975,0.601441,-0.200480,-0.366025",
    "-0.514496,-0.857493,0.000000,0.391236,-0.234742,0.543744,0.466257,-0.279754,-0.456256",
    "-0.707107,-0.707107,0.000000,0.384485,-0.384485,0.456256,0.322621,-0.322621,-0.543744",
    "-0.857493,-0.514496,0.000000,0.234742,-0.391236,0.543744,0.279754,-0.466257,-0.456256",
    "-0.948683,-0.316228,0.000000,0.115747,-0.347242,0.633975,0.200480,-0.601441,-0.366025",
    "-0.989950,-0.141421,0.000000,0.037738,-0.264164,0.733154,0.103684,-0.725785,-0.266846",
    "-1.000000,0.000000,0.000000,-0.000000,-0.149896,0.850104,-0.000000,-0.850104,-0.149896",
    "-0.993884,0.110431,0.000000,-0.000000,-0.000000,1.000000,-0.110431,-0.993884,-0.000000",
    "0.316228,-0.948683,0.000000,0.000000,0.000000,1.000000,0.948683,0.316228,-0.000000",
    "0.242536,-0.970143,0.000000,0.145421,0.036355,0.850104,0.824722,0.206180,-0.149896",
    "0.141422,-0.989949,0.000000,0.264164,0.037738,0.733154,0.725785,0.103684,-0.266846",
    "-0.000000,-1.000000,0.000000,0.366025,-0.000000,0.633975,0.633975,-0.000000,-0.366025",
    "-0.196116,-0.980581,0.000000,0.447395,-0.089479,0.543744,0.533185,-0.106637,-0.456256",
    "-0.447214,-0.894427,0.000000,0.486340,-0.243170,0.456256,0.408087,-0.204044,-0.543744",
    "-0.707107,-0.707107,0.000000,0.448288,-0.448288,0.366025,0.258819,-0.258819,-0.633975",
    "-0.894427,-0.447214,0.000000,0.243170,-0.486340,0.456256,0.204044,-0.408087,-0.543744",
    "-0.980581,-0.196116,0.000000,0.089479,-0.447395,0.543744,0.106637,-0.533185,-0.456256",
    "-1.000000,0.000000,0.000000,-0.000000,-0.366025,0.633975,-0.000000,-0.633975,-0.366025",
    "-0.989950,0.141421,0.000000,-0.037738,-0.264164,0.733154,-0.103684,-0.725785,-0.266846",
    "-0.970143,0.242536,0.000000,-0.036355,-0.145421,0.850104,-0.206180,-0.824722,-0.149896",
    "-0.948683,0.316228,0.000000,-0.000000,-0.000000,1.000000,-0.316228,-0.948683,-0.000000",
    "0.485643,-0.874157,0.000000,0.000000,0.000000,1.000000,0.874157,0.485643,-0.000000",
    "0.447214,-0.894427,0.000000,0.134071,0.067036,0.850104,0.760356,0.380178,-0.149896",
    "0.393919,-0.919145,0.000000,0.245270,0.105116,0.733154,0.673875,0.288803,-0.266846",
    "0.316228,-0.948683,0.000000,0.347242,0.115747,0.633975,0.601441,0.200480,-0.366025",
    "0.196116,-0.980581,0.000000,0.447395,0.089479,0.543744,0.533185,0.106637,-0.456256",
    "-0.000000,-1.000000,0.000000,0.543744,-0.000000,0.456256,0.456256,-0.000000,-0.543744",
    "-0.316228,-0.948683,0.000000,0.601441,-0.200480,0.366025,0.347242,-0.115747,-0.633975",
    "-0.707107,-0.707107,0.000000,0.518418,-0.518418,0.266846,0.188689,-0.188689,-0.733154",
    "-0.948683,-0.316228,0.000000,0.200480,-0.601441,0.366025,0.115747,-0.347242,-0.633975",
    "-1.000000,0.000000,0.000000,-0.000000,-0.543744,0.456256,-0.000000,-0.456256,-0.543744",
    "-0.980581,0.196116,0.000000,-0.089479,-0.447395,0.543744,-0.106637,-0.533185,-0.456256",
    "-0.948683,0.316228,0.000000,-0.115747,-0.347242,0.633975,-0.200480,-0.601441,-0.366025",
    "-0.919145,0.393919,0.000000,-0.105116,-0.245270,0.733154,-0.288803,-0.673875,-0.266846",
    "-0.894427,0.447214,0.000000,-0.067036,-0.134071,0.850104,-0.380178,-0.760356,-0.149896",
    "-0.874157,0.485643,0.000000,-0.000000,-0.000000,1.000000,-0.485643,-0.874157,-0.000000",
    "0.613941,-0.789352,0.000000,0.000000,0.000000,1.000000,0.789352,0.613941,-0.000000",
    "0.600000,-0.800000,0.000000,0.119917,0.089938,0.850104,0.680083,0.510062,-0.149896",
    "0.581238,-0.813734,0.000000,0.217142,0.155101,0.733154,0.596592,0.426137,-0.266846",
    "0.554700,-0.832050,0.000000,0.304552,0.203034,0.633975,0.527499,0.351666,-0.366025",
    "0.514496,-0.857493,0.000000,0.391236,0.234742,0.543744,0.466257,0.279754,-0.456256",
    "0.447214,-0.894427,0.000000,0.486340,0.243170,0.456256,0.408087,0.204044,-0.543744",
    "0.316228,-0.948683,0.000000,0.601441,0.200480,0.366025,0.347242,0.115747,-0.633975",
    "-0.000000,-1.000000,0.000000,0.733154,-0.000000,0.266846,0.266846,-0.000000,-0.733154",
    "-0.707107,-0.707107,0.000000,0.601114,-0.601114,0.149896,0.105993,-0.105993,-0.850104",
    "-1.000000,0.000000,0.000000,-0.000000,-0.733154,0.266846,-0.000000,-0.266846,-0.733154",
    "-0.948683,0.316228,0.000000,-0.200480,-0.601441,0.366025,-0.115747,-0.347242,-0.633975",
    "-0.894427,0.447214,0.000000,-0.243170,-0.486340,0.456256,-0.204044,-0.408087,-0.543744",
    "-0.857493,0.514496,0.000000,-0.234742,-0.391236,0.543744,-0.279754,-0.466257,-0.456256",
    "-0.832050,0.554700,0.000000,-0.203034,-0.304552,0.633975,-0.351666,-0.527499,-0.366025",
    "-0.813733,0.581238,0.000000,-0.155101,-0.217142,0.733154,-0.426137,-0.596592,-0.266846",
    "-0.800000,0.600000,0.000000,-0.089938,-0.119917,0.850104,-0.510062,-0.680083,-0.149896",
    "-0.789352,0.613941,0.000000,-0.000000,-0.000000,1.000000,-0.613941,-0.789352,-0.000000",
    "0.707107,-0.707107,0.000000,0.000000,0.000000,1.000000,0.707107,0.707107,-0.000000",
    "0.707107,-0.707107,0.000000,0.105993,0.105993,0.850104,0.601114,0.601114,-0.149896",
    "0.707107,-0.707107,0.000000,0.188689,0.188689,0.733154,0.518418,0.518418,-0.266846",
    "0.707107,-0.707107,0.000000,0.258819,0.258819,0.633975,0.448288,0.448288,-0.366025",
    "0.707107,-0.707107,0.000000,0.322621,0.322621,0.543744,0.384485,0.384485,-0.456256",
    "0.707107,-0.707107,0.000000,0.384485,0.384485,0.456256,0.322621,0.322621,-0.543744",
    "0.707107,-0.707107,0.000000,0.448288,0.448288,0.366025,0.258819,0.258819,-0.633975",
    "0.707107,-0.707107,0.000000,0.518418,0.518418,0.266846,0.188689,0.188689,-0.733154",
    "0.707107,-0.707107,0.000000,0.601114,0.601114,0.149896,0.105993,0.105993,-0.850104",
    "0.000000,1.000000,0.000000,-1.000000,0.000000,0.000000,0.000000,0.000000,-1.000000",
    "-0.707107,0.707107,0.000000,-0.601114,-0.601114,0.149896,-0.105993,-0.105993,-0.850104",
    "-0.707107,0.707107,0.000000,-0.518418,-0.518418,0.266846,-0.188689,-0.188689,-0.733154",
    "-0.707107,0.707107,0.000000,-0.448288,-0.448288,0.366025,-0.258819,-0.258819,-0.633975",
    "-0.707107,0.707107,0.000000,-0.384485,-0.384485,0.456256,-0.322621,-0.322621,-0.543744",
    "-0.707107,0.707107,0.000000,-0.322621,-0.322621,0.543744,-0.384485,-0.384485,-0.456256",
    "-0.707107,0.707107,0.000000,-0.258819,-0.258819,0.633975,-0.448288,-0.448288,-0.366025",
    "-0.707107,0.707107,0.000000,-0.188689,-0.188689,0.733154,-0.518418,-0.518418,-0.266846",
    "-0.707107,0.707107,0.000000,-0.105993,-0.105993,0.850104,-0.601114,-0.601114,-0.149896",
    "-0.707107,0.707107,0.000000,-0.000000,-0.000000,1.000000,-0.707107,-0.707107,-0.000000",
    "0.789352,-0.613941,0.000000,0.000000,0.000000,1.000000,0.613941,0.789352,-0.000000",
    "0.800000,-0.600000,0.000000,0.089938,0.119917,0.850104,0.510062,0.680083,-0.149896",
    "0.813734,-0.581238,0.000000,0.155101,0.217142,0.733154,0.426137,0.596592,-0.266846",
    "0.832050,-0.554700,0.000000,0.203034,0.304551,0.633975,0.351666,0.527499,-0.366025",
    "0.857493,-0.514496,0.000000,0.234742,0.391236,0.543744,0.279754,0.466257,-0.456256",
    "0.894427,-0.447214,0.000000,0.243170,0.486340,0.456256,0.204044,0.408087,-0.543744",
    "0.948683,-0.316228,0.000000,0.200480,0.601441,0.366025,0.115747,0.347242,-0.633975",
    "1.000000,0.000000,0.000000,0.000000,0.733154,0.266846,0.000000,0.266846,-0.733154",
    "0.707107,0.707107,0.000000,-0.601114,0.601114,0.149896,-0.105993,0.105993,-0.850104",
    "0.000000,1.000000,0.000000,-0.733154,0.000000,0.266846,-0.266846,0.000000,-0.733154",
    "-0.316228,0.948683,0.000000,-0.601441,-0.200480,0.366025,-0.347242,-0.115747,-0.633975",
    "-0.447214,0.894427,0.000000,-0.486340,-0.243170,0.456256,-0.408087,-0.204044,-0.543744",
    "-0.514496,0.857493,0.000000,-0.391236,-0.234742,0.543744,-0.466257,-0.279754,-0.456256",
    "-0.554700,0.832050,0.000000,-0.304552,-0.203034,0.633975,-0.527499,-0.351666,-0.366025",
    "-0.581238,0.813734,0.000000,-0.217142,-0.155101,0.733154,-0.596592,-0.426137,-0.266846",
    "-0.600000,0.800000,0.000000,-0.119917,-0.089938,0.850104,-0.680083,-0.510062,-0.149896",
    "-0.613941,0.789352,0.000000,-0.000000,-0.000000,1.000000,-0.789352,-0.613941,-0.000000",
    "0.874157,-0.485643,0.000000,0.000000,0.000000,1.000000,0.485643,0.874157,-0.000000",
    "0.894427,-0.447214,0.000000,0.067036,0.134071,0.850104,0.380178,0.760356,-0.149896",
    "0.919145,-0.393919,0.000000,0.105116,0.245270,0.733154,0.288803,0.673875,-0.266846",
    "0.948683,-0.316228,0.000000,0.115747,0.347242,0.633975,0.200480,0.601441,-0.366025",
    "0.980581,-0.196116,0.000000,0.089479,0.447395,0.543744,0.106637,0.533185,-0.456256",
    "1.000000,0.000000,0.000000,0.000000,0.543744,0.456256,0.000000,0.456256,-0.543744",
    "0.948683,0.316228,0.000000,-0.200480,0.601441,0.366025,-0.115747,0.347242,-0.633975",
    "0.707107,0.707107,0.000000,-0.518418,0.518418,0.266846,-0.188689,0.188689,-0.733154",
    "0.316228,0.948683,0.000000,-0.601441,0.200480,0.366025,-0.347242,0.115747,-0.633975",
    "0.000000,1.000000,0.000000,-0.543744,0.000000,0.456256,-0.456256,0.000000,-0.543744",
    "-0.196116,0.980581,0.000000,-0.447395,-0.089479,0.543744,-0.533185,-0.106637,-0.456256",
    "-0.316228,0.948683,0.000000,-0.347242,-0.115747,0.633975,-0.601441,-0.200480,-0.366025",
    "-0.393919,0.919145,0.000000,-0.245270,-0.105116,0.733154,-0.673875,-0.288803,-0.266846",
    "-0.447214,0.894427,0.000000,-0.134071,-0.067036,0.850104,-0.760356,-0.380178,-0.149896",
    "-0.485643,0.874157,0.000000,-0.000000,-0.000000,1.000000,-0.874157,-0.485643,-0.000000",
    "0.948683,-0.316228,0.000000,0.000000,0.000000,1.000000,0.316228,0.948683,-0.000000",
    "0.970142,-0.242536,0.000000,0.036355,0.145421,0.850104,0.206180,0.824722,-0.149896",
    "0.989950,-0.141421,0.000000,0.037738,0.264164,0.733154,0.103684,0.725785,-0.266846",
    "1.000000,0.000000,0.000000,0.000000,0.366025,0.633975,0.000000,0.633975,-0.366025",
    "0.980581,0.196116,0.000000,-0.089479,0.447395,0.543744,-0.106637,0.533185,-0.456256",
    "0.894427,0.447214,0.000000,-0.243170,0.486340,0.456256,-0.204044,0.408087,-0.543744",
    "0.707107,0.707107,0.000000,-0.448288,0.448288,0.366025,-0.258819,0.258819,-0.633975",
    "0.447214,0.894427,0.000000,-0.486340,0.243170,0.456256,-0.408087,0.204044,-0.543744",
    "0.196116,0.980581,0.000000,-0.447395,0.089479,0.543744,-0.533185,0.106637,-0.456256",
    "0.000000,1.000000,0.000000,-0.366025,0.000000,0.633975,-0.633975,0.000000,-0.366025",
    "-0.141421,0.989949,0.000000,-0.264164,-0.037738,0.733154,-0.725785,-0.103684,-0.266846",
    "-0.242536,0.970143,0.000000,-0.145421,-0.036355,0.850104,-0.824722,-0.206180,-0.149896",
    "-0.316228,0.948683,0.000000,-0.000000,-0.000000,1.000000,-0.948683,-0.316228,-0.000000",
    "0.993884,-0.110432,0.000000,0.000000,0.000000,1.000000,0.110432,0.993884,-0.000000",
    "1.000000,0.000000,0.000000,0.000000,0.149896,0.850104,0.000000,0.850104,-0.149896",
    "0.989949,0.141421,0.000000,-0.037738,0.264164,0.733154,-0.103684,0.725785,-0.266846",
    "0.948683,0.316228,0.000000,-0.115747,0.347242,0.633975,-0.200480,0.601441,-0.366025",
    "0.857493,0.514496,0.000000,-0.234742,0.391236,0.543744,-0.279754,0.466257,-0.456256",
    "0.707107,0.707107,0.000000,-0.384485,0.384485,0.456256,-0.322621,0.322621,-0.543744",
    "0.514496,0.857493,0.000000,-0.391236,0.234742,0.543744,-0.466257,0.279754,-0.456256",
    "0.316228,0.948683,0.000000,-0.347242,0.115747,0.633975,-0.601441,0.200480,-0.366025",
    "0.141421,0.989949,0.000000,-0.264164,0.037738,0.733154,-0.725785,0.103684,-0.266846",
    "0.000000,1.000000,0.000000,-0.149896,0.000000,0.850104,-0.850104,0.000000,-0.149896",
    "-0.110432,0.993884,0.000000,-0.000000,-0.000000,1.000000,-0.993884,-0.110432,-0.000000",
    "0.993884,0.110431,0.000000,-0.000000,0.000000,1.000000,-0.110431,0.993884,-0.000000",
    "0.970143,0.242536,0.000000,-0.036355,0.145421,0.850104,-0.206180,0.824722,-0.149896",
    "0.919145,0.393919,0.000000,-0.105116,0.245270,0.733154,-0.288803,0.673875,-0.266846",
    "0.832050,0.554700,0.000000,-0.203034,0.304552,0.633975,-0.351666,0.527499,-0.366025",
    "0.707107,0.707107,0.000000,-0.322621,0.322621,0.543744,-0.384485,0.384485,-0.456256",
    "0.554700,0.832050,0.000000,-0.304552,0.203034,0.633975,-0.527499,0.351666,-0.366025",
    "0.393919,0.919145,0.000000,-0.245270,0.105116,0.733154,-0.673875,0.288803,-0.266846",
    "0.242536,0.970143,0.000000,-0.145421,0.036355,0.850104,-0.824722,0.206180,-0.149896",
    "0.110432,0.993884,0.000000,-0.000000,0.000000,1.000000,-0.993884,0.110432,-0.000000",
    "0.948683,0.316228,0.000000,-0.000000,0.000000,1.000000,-0.316228,0.948683,-0.000000",
    "0.894427,0.447214,0.000000,-0.067036,0.134071,0.850104,-0.380178,0.760356,-0.149896",
    "0.813733,0.581238,0.000000,-0.155101,0.217142,0.733154,-0.426137,0.596592,-0.266846",
    "0.707107,0.707107,0.000000,-0.258819,0.258819,0.633975,-0.448288,0.448288,-0.366025",
    "0.581238,0.813733,0.000000,-0.217142,0.155101,0.733154,-0.596592,0.426137,-0.266846",
    "0.447214,0.894427,0.000000,-0.134071,0.067036,0.850104,-0.760356,0.380178,-0.149896",
    "0.316228,0.948683,0.000000,-0.000000,0.000000,1.000000,-0.948683,0.316228,-0.000000",
    "0.874157,0.485643,0.000000,-0.000000,0.000000,1.000000,-0.485643,0.874157,-0.000000",
    "0.800000,0.600000,0.000000,-0.089938,0.119917,0.850104,-0.510062,0.680083,-0.149896",
    "0.707107,0.707107,0.000000,-0.188689,0.188689,0.733154,-0.518418,0.518418,-0.266846",
    "0.600000,0.800000,0.000000,-0.119917,0.089938,0.850104,-0.680083,0.510062,-0.149896",
    "0.485643,0.874157,0.000000,-0.000000,0.000000,1.000000,-0.874157,0.485643,-0.000000",
    "0.789352,0.613941,0.000000,-0.000000,0.000000,1.000000,-0.613941,0.789352,-0.000000",
    "0.707107,0.707107,0.000000,-0.105993,0.105993,0.850104,-0.601114,0.601114,-0.149896",
    "0.613941,0.789352,0.000000,-0.000000,0.000000,1.000000,-0.789352,0.613941,-0.000000",
    "0.707107,0.707107,0.000000,-0.000000,0.000000,1.000000,-0.707107,0.707107,-0.000000",
        };


        internal unsafe static void UpdateAlphamaskViewsConstants()
        {
            System.Diagnostics.Debug.Assert(s_viewVectorData.Length == 181, "Only supported scheme of views for now");

            Matrix* viewVectors = stackalloc Matrix[s_viewVectorData.Length];
            for (int i = 0; i < s_viewVectorData.Length; i++)
            {
                Matrix mm = Matrix.Identity;
                string[] sp = s_viewVectorData[i].Split(',');
                mm.M11 = Convert.ToSingle(sp[0], CultureInfo.InvariantCulture);
                mm.M12 = Convert.ToSingle(sp[1], CultureInfo.InvariantCulture);
                mm.M13 = Convert.ToSingle(sp[2], CultureInfo.InvariantCulture);
                mm.M21 = Convert.ToSingle(sp[3], CultureInfo.InvariantCulture);
                mm.M22 = Convert.ToSingle(sp[4], CultureInfo.InvariantCulture);
                mm.M23 = Convert.ToSingle(sp[5], CultureInfo.InvariantCulture);
                mm.M31 = Convert.ToSingle(sp[6], CultureInfo.InvariantCulture);
                mm.M32 = Convert.ToSingle(sp[7], CultureInfo.InvariantCulture);
                mm.M33 = Convert.ToSingle(sp[8], CultureInfo.InvariantCulture);
                mm = Matrix.Normalize(mm);
                mm = mm * Matrix.CreateRotationX(MathHelper.PiOver2);
                mm.Up = -mm.Up;
                viewVectors[i] = mm;
            }

            var mapping = MyMapping.MapDiscard(MyCommon.AlphamaskViewsConstants);
            for (int vectorIndex = 0; vectorIndex < s_viewVectorData.Length; ++vectorIndex)
                mapping.WriteAndPosition(ref viewVectors[vectorIndex]);
            mapping.Unmap();
        }
    }
}
