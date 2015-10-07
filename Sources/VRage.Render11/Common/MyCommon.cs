﻿using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using VRage.Voxels;
using VRageMath;
using VRageRender.Resources;

namespace VRageRender
{
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


        internal static ConstantsBufferId FrameConstants { get; set; }
        internal static ConstantsBufferId ProjectionConstants { get; set; }
        internal static ConstantsBufferId ObjectConstants { get; set; }
        internal static ConstantsBufferId FoliageConstants { get; set; }
        internal static ConstantsBufferId MaterialFoliageTableConstants { get; set; }
        internal static ConstantsBufferId OutlineConstants { get; set; }
        internal static ConstantsBufferId AlphamaskViewsConstants { get; set; }

        internal static UInt64 FrameCounter = 0;

        internal static MyRenderContext RC { get { return MyRenderContextPool.Immediate; } }

        internal unsafe static void Init()
        {
            FrameConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(MyFrameConstantsLayout));
            ProjectionConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix));
            ObjectConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix));
            FoliageConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix));
            MaterialFoliageTableConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Vector4) * 256);
            OutlineConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(OutlineConstantsLayout));
            AlphamaskViewsConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix) * 181);

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
                m_objectsConstantBuffers[size] = MyHwBuffers.CreateConstantsBuffer(size);

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
                m_materialsConstantBuffers[size] = MyHwBuffers.CreateConstantsBuffer(size);

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
             Vector2 _padding;

             internal float EnableVoxelAo;
             internal float VoxelAoMin;
             internal float VoxelAoMax;
             internal float VoxelAoOffset;

             internal Matrix BackgroundOrientation;
         }

        internal static void MoveToNextFrame()
        {
            FrameCounter++;
        }

        internal static void UpdateFrameConstants()
        {
            MyFrameConstantsLayout constants = new MyFrameConstantsLayout();
            constants.View = Matrix.Transpose(MyEnvironment.ViewAt0);
            constants.Projection = Matrix.Transpose(MyEnvironment.Projection);
            constants.ViewProjection = Matrix.Transpose(MyEnvironment.ViewProjectionAt0);
            constants.InvView = Matrix.Transpose(MyEnvironment.InvViewAt0);
            constants.InvProjection = Matrix.Transpose(MyEnvironment.InvProjection);
            constants.InvViewProjection = Matrix.Transpose(MyEnvironment.InvViewProjectionAt0);
            constants.ViewProjectionWorld = Matrix.Transpose(MyEnvironment.ViewProjection);
            constants.WorldOffset = new Vector4(MyEnvironment.CameraPosition, 0);
            
            constants.Resolution = MyRender11.ResolutionF;
            constants.TerrainTextureDistances = new Vector4(
                MyRender11.Settings.TerrainDetailD0,
                1.0f / (MyRender11.Settings.TerrainDetailD1 - MyRender11.Settings.TerrainDetailD0),
                MyRender11.Settings.TerrainDetailD2,
                1.0f / (MyRender11.Settings.TerrainDetailD3 - MyRender11.Settings.TerrainDetailD2));
            
            constants.TerrainDetailRange.X = 0;
            constants.TerrainDetailRange.Y = 0;
            constants.Time = (float) ( MyRender11.CurrentDrawTime.Seconds - Math.Truncate(MyRender11.CurrentDrawTime.Seconds / 1000.0) * 1000 );
            constants.TimeDelta = (float)(MyRender11.TimeDelta.Seconds);
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
            constants.FogDensity = MyEnvironment.FogSettings.FogDensity;
            constants.FogMult = MyEnvironment.FogSettings.FogMultiplier;
            constants.FogYOffset = MyRender11.Settings.FogYOffset;
            constants.FogColor = MyEnvironment.FogSettings.FogColor.PackedValue;
            constants.ForwardPassAmbient = MyRender11.Postprocess.ForwardPassAmbient;

            constants.LogLumThreshold = MyRender11.Postprocess.LogLumThreshold;
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

            constants.DirectionalLightColor = MyEnvironment.DirectionalLightIntensity;
            constants.DirectionalLightDir = MyEnvironment.DirectionalLightDir;
            constants.SkyboxBlend = 1 - 2 * (float)(Math.Abs(-MyEnvironment.DayTime + 0.5));
            constants.SkyboxBrightness = 1;
			constants.ShadowFadeout = MyRender11.Settings.ShadowFadeoutMultiplier;

            constants.DebugVoxelLod = MyRenderSettings.DebugClipmapLodColor ? 1.0f : 0.0f;
            constants.EnableVoxelAo = MyRenderSettings.EnableVoxelAo ? 1f : 0f;
            constants.VoxelAoMin = MyRenderSettings.VoxelAoMin;
            constants.VoxelAoMax = MyRenderSettings.VoxelAoMax;
            constants.VoxelAoOffset = MyRenderSettings.VoxelAoOffset;

            constants.BackgroundOrientation = Matrix.CreateFromQuaternion(MyEnvironment.BackgroundOrientation);

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 0, out constants.VoxelLodRange0.X, out constants.VoxelLodRange0.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 1, out constants.VoxelLodRange0.Z, out constants.VoxelLodRange0.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 2, out constants.VoxelLodRange1.X, out constants.VoxelLodRange1.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 3, out constants.VoxelLodRange1.Z, out constants.VoxelLodRange1.W);

            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 4, out constants.VoxelLodRange2.X, out constants.VoxelLodRange2.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 5, out constants.VoxelLodRange2.Z, out constants.VoxelLodRange2.W);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 6, out constants.VoxelLodRange3.X, out constants.VoxelLodRange3.Y);
            MyClipmap.ComputeLodViewBounds(MyClipmapScaleEnum.Normal, 7, out constants.VoxelLodRange3.Z, out constants.VoxelLodRange3.W);

            var mapping = MyMapping.MapDiscard(MyCommon.FrameConstants);
            mapping.stream.Write(constants);
            mapping.Unmap();
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


        internal static void UpdateAlphamaskViewsConstants()
        {
            System.Diagnostics.Debug.Assert(s_viewVectorData.Length == 181, "Only supported scheme of views for now");

            var viewVectors = new Matrix[s_viewVectorData.Length];
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
            for (int i = 0; i < 181; i++)
            {
                mapping.stream.Write(viewVectors[i]);
            }
            mapping.Unmap();
        }
    }
}
