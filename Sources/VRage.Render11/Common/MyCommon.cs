using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using SharpDX.Direct3D11;
using VRage.Library.Utils;
using VRage.Render11.Common;
using VRage.Render11.LightingStage;
using VRage.Render11.Resources;
using VRage.Voxels;
using VRageMath;
using VRageRender.Voxels;
using VRage.Render11.GeometryStage.Materials;

namespace VRageRender
{
    internal static class MyCommon
    {
        // constant buffers
        internal const int FRAME_SLOT = 0;
        internal const int PROJECTION_SLOT = 1;
        internal const int OBJECT_SLOT = 2;
        internal const int MATERIAL_SLOT = 3;
        internal const int FOLIAGE_SLOT = 4;
        internal const int ALPHAMASK_VIEWS_SLOT = 5;
        internal const int VOXELS_MATERIALS_LUT_SLOT = 6;

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

        private const float MAX_FRAMETIME = 66.0f;
        private const int DEFAULT_SEED = 0x4A6F7921;

        internal static IConstantBuffer FrameConstantsStereoLeftEye { get; set; }
        internal static IConstantBuffer FrameConstantsStereoRightEye { get; set; }

        internal static IConstantBuffer FrameConstants { get; set; }
        internal static IConstantBuffer ProjectionConstants { get; set; }
        internal static IConstantBuffer ObjectConstants { get; set; }
        internal static IConstantBuffer FoliageConstants { get; set; }
        internal static IConstantBuffer MaterialFoliageTableConstants { get; set; }
        internal static IConstantBuffer HighlightConstants { get; set; }
        internal static IConstantBuffer AlphamaskViewsConstants { get; set; }

        internal static MyVoxelMaterialsConstantBuffer VoxelMaterialsConstants { get; set; }

        internal static UInt64 FrameCounter = 0;

        static MyCommon()
        {
            m_timer = new Stopwatch();
            m_timer.Start();
        }
        internal static unsafe void Init()
        {
            FrameConstantsStereoLeftEye = MyManagers.Buffers.CreateConstantBuffer("FrameConstantsStereoLeftEye", sizeof(MyFrameConstantsLayout), usage: ResourceUsage.Dynamic);
            FrameConstantsStereoRightEye = MyManagers.Buffers.CreateConstantBuffer("FrameConstantsStereoRightEye", sizeof(MyFrameConstantsLayout), usage: ResourceUsage.Dynamic);

            FrameConstants = MyManagers.Buffers.CreateConstantBuffer("FrameConstants", sizeof(MyFrameConstantsLayout), usage: ResourceUsage.Dynamic);
            ProjectionConstants = MyManagers.Buffers.CreateConstantBuffer("ProjectionConstants", sizeof(Matrix), usage: ResourceUsage.Dynamic);
            ObjectConstants = MyManagers.Buffers.CreateConstantBuffer("ObjectConstants", sizeof(Matrix), usage: ResourceUsage.Dynamic);
            FoliageConstants = MyManagers.Buffers.CreateConstantBuffer("FoliageConstants", sizeof(Vector4), usage: ResourceUsage.Dynamic);
            MaterialFoliageTableConstants = MyManagers.Buffers.CreateConstantBuffer("MaterialFoliageTableConstants", sizeof(Vector4) * 256, usage: ResourceUsage.Dynamic);
            HighlightConstants = MyManagers.Buffers.CreateConstantBuffer("HighlightConstants", sizeof(HighlightConstantsLayout), usage: ResourceUsage.Dynamic);
            AlphamaskViewsConstants = MyManagers.Buffers.CreateConstantBuffer("AlphamaskViewsConstants", sizeof(Matrix) * 181, usage: ResourceUsage.Dynamic);
            VoxelMaterialsConstants = new MyVoxelMaterialsConstantBuffer();

            UpdateAlphamaskViewsConstants();
        }

        internal static ISrvBindable GetAmbientBrdfLut()
        {
            MyFileTextureManager texManager = MyManagers.FileTextures;
            return texManager.GetTexture("Textures/Miscellaneous/ambient_brdf.dds", MyFileTextureEnum.CUSTOM, true);
        }

        internal static Dictionary<int, IConstantBuffer> m_objectsConstantBuffers = new Dictionary<int, IConstantBuffer>();

        internal static IConstantBuffer GetObjectCB(int size)
        {
            // align size
            size = ((size + 15) / 16) * 16;
            if (!m_objectsConstantBuffers.ContainsKey(size))
            {
                m_objectsConstantBuffers[size] = MyManagers.Buffers.CreateConstantBuffer("CommonObjectCB" + size, size, usage: ResourceUsage.Dynamic);

            }
            return m_objectsConstantBuffers[size];
        }

        internal static Dictionary<int, IConstantBuffer> m_materialsConstantBuffers = new Dictionary<int, IConstantBuffer>();

        internal static IConstantBuffer GetMaterialCB(int size)
        {
            // align size
            size = ((size + 15) / 16) * 16;
            if (!m_materialsConstantBuffers.ContainsKey(size))
            {
                m_materialsConstantBuffers[size] = MyManagers.Buffers.CreateConstantBuffer("CommonMaterialCB" + size, size, usage: ResourceUsage.Dynamic);

            }
            return m_materialsConstantBuffers[size];
        }

        #region Frame constants layout

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MyEnvironmentLayout
        {
            internal Matrix View;
            internal Matrix Projection;
            internal Matrix ViewProjection;
            internal Matrix InvView;
            internal Matrix InvProjection;
            internal Matrix InvViewProjection;
            internal Matrix ViewProjectionWorld;

            internal Matrix BackgroundOrientation;

            internal Vector4 WorldOffset;
            internal Vector3 EyeOffsetInWorld;
            internal float __pad0;

            internal Vector3 CameraPositionDelta;
            internal float __pad1;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MyScreenLayout
        {
            internal Vector2I GBufferOffset;
            internal Vector2I ResolutionOfGBuffer;

            internal Vector2 Resolution;
            internal uint TilesNum;
            internal uint TilesX;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MyFoliageLayout
        {
            internal Vector4 ClippingScaling;
            internal Vector3 WindVector;
            private float __pad;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MyFogLayout
        {
            internal float Density;
            internal float Mult;
            internal uint Color;             // sRGB
            private float __pad;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MyVoxelLayout
        {
            internal Vector4 LodRange0;
            internal Vector4 LodRange1;
            internal Vector4 LodRange2;
            internal Vector4 LodRange3;

            internal Vector4 MassiveLodRange0;
            internal Vector4 MassiveLodRange1;
            internal Vector4 MassiveLodRange2;
            internal Vector4 MassiveLodRange3;
            internal Vector4 MassiveLodRange4;
            internal Vector4 MassiveLodRange5;
            internal Vector4 MassiveLodRange6;
            internal Vector4 MassiveLodRange7;

            internal float DebugVoxelLod;
            private Vector3 __pad;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MyTextureDebugMultipliers
        {
            public float AlbedoMultiplier;
            public float MetalnessMultiplier;
            public float GlossMultiplier;
            public float AoMultiplier;

            public float EmissiveMultiplier;
            public float ColorMaskMultiplier;
            public float AlbedoShift;
            public float MetalnessShift;

            public float GlossShift;
            public float AoShift;
            public float EmissiveShift;
            public float ColorMaskShift;

            internal static MyTextureDebugMultipliers Defaults = new MyTextureDebugMultipliers
            {
                AlbedoMultiplier = 1.0f,
                MetalnessMultiplier = 1.0f,
                GlossMultiplier = 1.0f,
                AoMultiplier = 1.0f,
                EmissiveMultiplier = 1.0f,
                ColorMaskMultiplier = 1.0f,
            };
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct MyFrameConstantsLayout
        {
            internal MyEnvironmentLayout Environment;

            internal MyScreenLayout Screen;

            internal MyFoliageLayout Foliage;

            internal MyEnvironmentLightData EnvironmentLight;

            internal MyPostprocessSettings.Layout Postprocess;

            internal MyFogLayout Fog;

            internal MyVoxelLayout Voxel;

            internal MyTextureDebugMultipliers TextureDebugMultipliers;


            internal float FrameTime;
            internal float FrameTimeDelta;
            internal float RandomSeed;
            private float __pad;
        }

        #endregion

        internal static void MoveToNextFrame()
        {
            FrameCounter++;
        }

        private static readonly MyRandom m_random = new MyRandom();
        private static uint m_fixedTimeStep;
        private static float m_lastFrameDelta = 0;
        private static float m_frameTime = 0;
        private static float m_lastFrameTimer = 0;
        private static readonly Stopwatch m_timer;
        private static Vector3D m_lastCameraPosition;

        internal static float TimerMs { get { return (float)(m_timer.ElapsedTicks / (double)Stopwatch.Frequency * 1000.0); } }
        internal static float LastFrameDelta() { return m_lastFrameDelta; }

        public static void SetFrameTimeStep(uint timestep)
        {
            if (timestep == 0 && m_fixedTimeStep > 0)
            {
                // Arbitrarily restore last timer value
                m_lastFrameTimer = TimerMs - m_fixedTimeStep;
            }

            m_fixedTimeStep = timestep;
        }

        public static void SetRandomSeed(int? value)
        {
            int seed = value ?? DEFAULT_SEED;

            m_random.SetSeed(seed);
            MyManagers.GeneratedTextures.InitializeRandomTexture(seed);
            MyHBAO.InitializeConstantBuffer(seed);
        }

        private static void UpdateFrameConstantsInternal(MyEnvironmentMatrices envMatrices, ref MyFrameConstantsLayout constants, MyStereoRegion typeofFC)
        {
            constants.Environment.View = Matrix.Transpose(envMatrices.ViewAt0);
            constants.Environment.Projection = Matrix.Transpose(envMatrices.Projection);
            constants.Environment.ViewProjection = Matrix.Transpose(envMatrices.ViewProjectionAt0);
            constants.Environment.InvView = Matrix.Transpose(envMatrices.InvViewAt0);
            constants.Environment.InvProjection = Matrix.Transpose(envMatrices.InvProjection);
            constants.Environment.InvViewProjection = Matrix.Transpose(envMatrices.InvViewProjectionAt0);
            constants.Environment.ViewProjectionWorld = Matrix.Transpose(envMatrices.ViewProjection);
            constants.Environment.WorldOffset = new Vector4(envMatrices.CameraPosition, 0);

            constants.Screen.Resolution = MyRender11.ResolutionF;
            if (typeofFC != MyStereoRegion.FULLSCREEN)
            {
                constants.Screen.Resolution.X /= 2;

                Vector3 eyeOffset = new Vector3(envMatrices.ViewAt0.M41, envMatrices.ViewAt0.M42, envMatrices.ViewAt0.M43);
                Vector3 eyeOffsetInWorld = Vector3.Transform(eyeOffset, Matrix.Transpose(MyRender11.Environment.Matrices.ViewAt0));
                constants.Environment.EyeOffsetInWorld = eyeOffsetInWorld;
            }

            constants.Screen.GBufferOffset = new Vector2I(0, 0);
            if (typeofFC == MyStereoRegion.RIGHT)
                constants.Screen.GBufferOffset.X = MyRender11.ResolutionI.X / 2;

            constants.Screen.ResolutionOfGBuffer = MyRender11.ResolutionI;
        }

        internal static void UpdateFrameConstants()
        {
            // environment
            MyFrameConstantsLayout constants = new MyFrameConstantsLayout();
            UpdateFrameConstantsInternal(MyRender11.Environment.Matrices, ref constants, MyStereoRegion.FULLSCREEN);
            constants.Environment.CameraPositionDelta = MyRender11.Environment.Matrices.CameraPosition - m_lastCameraPosition;
            m_lastCameraPosition = MyRender11.Environment.Matrices.CameraPosition;

            // skybox
            constants.Environment.BackgroundOrientation = Matrix.CreateFromQuaternion(MyRender11.Environment.Data.SkyboxOrientation);

            // screen
            constants.Screen.TilesNum = (uint)MyLightsRendering.GetTilesNum();
            constants.Screen.TilesX = (uint)MyLightsRendering.GetTilesX();

            // foliage
            constants.Foliage.ClippingScaling = new Vector4(
                //MyRender.Settings.GrassGeometryClippingDistance,
                MyRender11.Settings.User.FoliageDetails.GrassDrawDistance(),
                MyRender11.Settings.GrassGeometryScalingNearDistance,
                MyRender11.Settings.GrassGeometryScalingFarDistance,
                MyRender11.Settings.GrassGeometryDistanceScalingFactor);
            constants.Foliage.WindVector = new Vector3(
                (float)Math.Cos(MyRender11.Settings.WindAzimuth * Math.PI / 180.0),
                0,
                (float)Math.Sin(MyRender11.Settings.WindAzimuth * Math.PI / 180.0)) * MyRender11.Settings.WindStrength;

            // postprocess
            constants.Postprocess = MyRender11.Postprocess.GetProcessedData();

            // lighting data
            constants.EnvironmentLight = MyRender11.Environment.Data.EnvironmentLight;
            if (!MyRender11.DebugOverrides.Sun)
                constants.EnvironmentLight.SunColorRaw = new Vector3(0, 0, 0);

            // fog
            constants.Fog.Density = MyRender11.Environment.Fog.FogDensity;
            constants.Fog.Mult = MyRender11.Environment.Fog.FogMultiplier;
            constants.Fog.Color = MyRender11.Environment.Fog.FogColor.PackedValue;

            // voxels
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 0, out constants.Voxel.LodRange0.X, out constants.Voxel.LodRange0.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 1, out constants.Voxel.LodRange0.Z, out constants.Voxel.LodRange0.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 2, out constants.Voxel.LodRange1.X, out constants.Voxel.LodRange1.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 3, out constants.Voxel.LodRange1.Z, out constants.Voxel.LodRange1.W);

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 4, out constants.Voxel.LodRange2.X, out constants.Voxel.LodRange2.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 5, out constants.Voxel.LodRange2.Z, out constants.Voxel.LodRange2.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 6, out constants.Voxel.LodRange3.X, out constants.Voxel.LodRange3.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 7, out constants.Voxel.LodRange3.Z, out constants.Voxel.LodRange3.W);

            //
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 0, out constants.Voxel.MassiveLodRange0.X, out constants.Voxel.MassiveLodRange0.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 1, out constants.Voxel.MassiveLodRange0.Z, out constants.Voxel.MassiveLodRange0.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 2, out constants.Voxel.MassiveLodRange1.X, out constants.Voxel.MassiveLodRange1.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 3, out constants.Voxel.MassiveLodRange1.Z, out constants.Voxel.MassiveLodRange1.W);

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 4, out constants.Voxel.MassiveLodRange2.X, out constants.Voxel.MassiveLodRange2.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 5, out constants.Voxel.MassiveLodRange2.Z, out constants.Voxel.MassiveLodRange2.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 6, out constants.Voxel.MassiveLodRange3.X, out constants.Voxel.MassiveLodRange3.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 7, out constants.Voxel.MassiveLodRange3.Z, out constants.Voxel.MassiveLodRange3.W);

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 8, out constants.Voxel.MassiveLodRange4.X, out constants.Voxel.MassiveLodRange4.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 9, out constants.Voxel.MassiveLodRange4.Z, out constants.Voxel.MassiveLodRange4.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 10, out constants.Voxel.MassiveLodRange5.X, out constants.Voxel.MassiveLodRange5.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 11, out constants.Voxel.MassiveLodRange5.Z, out constants.Voxel.MassiveLodRange5.W);

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 12, out constants.Voxel.MassiveLodRange6.X, out constants.Voxel.MassiveLodRange6.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 13, out constants.Voxel.MassiveLodRange6.Z, out constants.Voxel.MassiveLodRange6.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 14, out constants.Voxel.MassiveLodRange7.X, out constants.Voxel.MassiveLodRange7.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Massive, 15, out constants.Voxel.MassiveLodRange7.Z, out constants.Voxel.MassiveLodRange7.W);

            constants.Voxel.DebugVoxelLod = MyRender11.Settings.DebugClipmapLodColor ? 1.0f : 0.0f;

            // debug multipliers
            constants.TextureDebugMultipliers = new MyTextureDebugMultipliers
            {
                AlbedoMultiplier = MyRender11.Settings.AlbedoMultiplier,
                AlbedoShift = MyRender11.Settings.AlbedoShift,
                MetalnessMultiplier = MyRender11.Settings.MetalnessMultiplier,
                MetalnessShift = MyRender11.Settings.MetalnessShift,
                GlossMultiplier = MyRender11.Settings.GlossMultiplier,
                GlossShift = MyRender11.Settings.GlossShift,
                AoMultiplier = MyRender11.Settings.AoMultiplier,
                AoShift = MyRender11.Settings.AoShift,
                EmissiveMultiplier = MyRender11.Settings.EmissiveMultiplier,
                EmissiveShift = MyRender11.Settings.EmissiveShift,
                ColorMaskMultiplier = MyRender11.Settings.ColorMaskMultiplier,
                ColorMaskShift = MyRender11.Settings.ColorMaskShift,
            };

            // misc
            if (m_fixedTimeStep > 0)
            {
                m_frameTime = m_frameTime + m_fixedTimeStep;
                m_lastFrameDelta = m_fixedTimeStep;
            }
            else
            {
                float timer = TimerMs;
                float delta = Math.Min(timer - m_lastFrameTimer, MAX_FRAMETIME) / 1000.0f;
                m_frameTime += delta;
                m_lastFrameDelta = delta;
                m_lastFrameTimer = timer;
            }

            constants.FrameTimeDelta = m_lastFrameDelta;
            constants.FrameTime = m_frameTime;

            constants.RandomSeed = m_random.NextFloat();

            // send constants to device
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


        private static string[] s_viewVectorData = new string[]
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
