using SharpDX.Direct3D11;
using System;
using System.Collections.Generic;
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

        internal static UInt64 FrameCounter = 0;

        internal static MyRenderContext RC { get { return MyRenderContextPool.Immediate; } }

        internal unsafe static void Init()
        {
            FrameConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(MyFrameConstantsLayout));
            ProjectionConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix));
            ObjectConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix));
            FoliageConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Matrix));
            MaterialFoliageTableConstants = MyHwBuffers.CreateConstantsBuffer(sizeof(Vector4) * 256);
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
             internal uint Padding1;
             internal float Tonemapping_A;
             internal float Tonemapping_B;
             internal float Tonemapping_C;
             internal float Tonemapping_D;
             internal float Tonemapping_E;
             internal float Tonemapping_F;
             internal float LogLumThreshold;
             internal float padding2_;
            
             internal Vector4 VoxelLodRange0;
             internal Vector4 VoxelLodRange1;
             internal Vector4 VoxelLodRange2;
             internal Vector4 VoxelLodRange3;
        
        
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

            constants.Tau = MyRender11.Postprocess.EyeAdaptationTau;
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
    }
}
