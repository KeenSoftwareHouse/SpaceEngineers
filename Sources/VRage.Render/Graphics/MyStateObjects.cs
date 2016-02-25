using System.Diagnostics;
using SharpDX;
using SharpDX.Direct3D9;
using VRageRender.Graphics;

namespace VRageRender.Graphics
{
    static class MyStateObjects
    {
        public const int STENCIL_MASK_REFERENCE_STENCIL = 1;

        public static readonly BlendState SunGlareBlendState = new BlendState()
        {
            ColorWriteChannels = 0
        };


        public readonly static RasterizerState SolidRasterizerState = new RasterizerState()
        {
            FillMode = FillMode.Solid,
            CullMode = Cull.Counterclockwise
        };

        public readonly static RasterizerState WireframeClockwiseRasterizerState = new RasterizerState()
        {
            FillMode = FillMode.Wireframe,
            CullMode = Cull.Clockwise
        };

        public readonly static RasterizerState WireframeCounterClockwiseRasterizerState = new RasterizerState()
        {
            FillMode = FillMode.Wireframe,
            CullMode = Cull.Counterclockwise
        };
           
        public static readonly DepthStencilState DistantImpostorsDepthStencilState = new DepthStencilState()
        {
            DepthBufferEnable = true,
            DepthBufferFunction = Compare.Always,       //  Depth buffer testing is OFF because we don't wanna z-fighting
            DepthBufferWriteEnable = true                       //  Depth buffer writing is ON because we want to have depth-values in depth-buffer so sun-glare occlusion test will work
        };
           
        public static readonly DepthStencilState DecalsDepthStencilState = new DepthStencilState()
        {
            DepthBufferFunction = Compare.Equal,
            DepthBufferWriteEnable = false
        };

        //  Default GUI blend state 
        public static readonly BlendState GuiDefault_BlendState = BlendState.NonPremultiplied;//BlendState.AlphaBlend;

        //  Default GUI depth-stencil
        public static readonly DepthStencilState GuiDefault_DepthStencilState = DepthStencilState.None;

        //  Special blend state for GUI stencil mask rendering
        public static readonly BlendState StencilMask_Draw_BlendState = new BlendState()
        {
            ColorSourceBlend = Blend.Zero,
            AlphaSourceBlend = Blend.Zero,
            ColorDestinationBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };
            
        public static readonly DepthStencilState SetncilMask_DrawHud_DepthStencilState = new DepthStencilState()
        {
            //  Depth buffer is not required when working with stencil only
            DepthBufferEnable = false,
            DepthBufferWriteEnable = false,

            //  Every succesfully rendered pixel (not discarded or clipped) will also write 1 into stencil buffer
            ReferenceStencil = STENCIL_MASK_REFERENCE_STENCIL,
            StencilEnable = true,
            StencilFunction = Compare.Always,
            StencilPass = StencilOperation.Replace            
        };
                
        //  Special depth-stencil for GUI stencil mask rendering
        public static readonly DepthStencilState StencilMask_Draw_DepthStencilState = new DepthStencilState()
        {
            //  Depth buffer is not required when working with stencil only
            DepthBufferEnable = false,
            DepthBufferWriteEnable = false,

            //  Every succesfully rendered pixel (not discarded or clipped) will also write 1 into stencil buffer
            //ReferenceStencil = STENCIL_MASK_REFERENCE_STENCIL,
            StencilEnable = true,
            StencilFunction = Compare.Always,
            //StencilPass = StencilOperation.Replace
            StencilPass = StencilOperation.Increment
        };

        private static DepthStencilState CreateTestDepthStencilState(int level)
        {
            return new DepthStencilState()
            {
                //  Depth buffer is not required when working with stencil only
                DepthBufferEnable = false,
                DepthBufferWriteEnable = false,

                //  Write color only where stencil enables it (where stencil value equals level)
                //  But disable writing to stencil buffer
                StencilEnable = true,
                StencilFunction = Compare.Equal,
                ReferenceStencil = level,
                StencilPass = StencilOperation.Keep,
            };
        }

        private const int maxStencilLevels = 10;
        private static readonly DepthStencilState[] m_stencilMasks_TestBegin_DepthStencilState = new DepthStencilState[maxStencilLevels];
        public static DepthStencilState GetStencilMasks_TestBegin_DepthStencilState(int level)
        {
            Debug.Assert(level > 0);
            Debug.Assert(level < maxStencilLevels);

            if(m_stencilMasks_TestBegin_DepthStencilState[level-1] == null)
            {
                m_stencilMasks_TestBegin_DepthStencilState[level - 1] = CreateTestDepthStencilState(level);
            }
            return m_stencilMasks_TestBegin_DepthStencilState[level - 1];
        }

        //  Special depth-stencil for GUI stencil mask rendering
        public static readonly DepthStencilState StencilMask_TestHudBegin_DepthStencilState = new DepthStencilState()
        {
            //  Depth buffer is not required when working with stencil only
            DepthBufferEnable = false,
            DepthBufferWriteEnable = false,

            //  Write color only where stencil enables it (where stencil value equals 1)
            //  But disable writing to stencil buffer
            StencilEnable = true,
            StencilFunction = Compare.Equal,
            ReferenceStencil = STENCIL_MASK_REFERENCE_STENCIL,
            StencilPass = StencilOperation.Keep
        };

        // Depth stencil for rendering near objects and writing to stencil (cockpit, weapons)
        public static readonly DepthStencilState DepthStencil_WriteNearObject = new DepthStencilState()
        {
            //  Depth buffer is not required when working with stencil only
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,

            //  Every succesfully rendered pixel (not discarded or clipped) will also write 1 into stencil buffer
            ReferenceStencil = 1,
            StencilEnable = true,
            StencilFunction = Compare.Always,
            StencilPass = StencilOperation.Replace,
            StencilFail = StencilOperation.Replace,
            StencilWriteMask = 1
        };

        // Depth stencil for rendering far objects - all objects except cockpit and weapons (reads stencil and renders objects or not for this pixel)
        public static readonly DepthStencilState DepthStencil_TestFarObject = new DepthStencilState()
        {
            //  Depth buffer is not required when working with stencil only
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,

            //  Write color only where stencil enables it (where stencil value equals 1)
            //  But disable writing to stencil buffer
            StencilEnable = true,
            StencilFunction = Compare.NotEqual,
            ReferenceStencil = 1,
            StencilPass = StencilOperation.Keep,
            StencilFail = StencilOperation.Keep,
        };

        public static readonly DepthStencilState DepthStencil_WriteFarObject = new DepthStencilState()
        {
            //  Depth buffer is not required when working with stencil only
            DepthBufferEnable = true,
            DepthBufferWriteEnable = true,

            //  Write color only where stencil enables it (where stencil value equals 1)
            //  But disable writing to stencil buffer
            StencilEnable = true,
            StencilFunction = Compare.NotEqual,
            ReferenceStencil = 3,
            StencilPass = StencilOperation.Replace,
            StencilFail = StencilOperation.Keep,
            StencilWriteMask = 2,
            StencilMask = 1
        };

        public static readonly DepthStencilState DepthStencil_WriteNearAtmosphere = new DepthStencilState()
        {
            //  Depth buffer is not required when working with stencil only
            DepthBufferEnable = false,
            DepthBufferWriteEnable = true,

            //  Write color only where stencil enables it (where stencil value equals 1)
            //  But disable writing to stencil buffer
            StencilEnable = true,
            StencilFunction = Compare.Equal,
            ReferenceStencil = 4,
            StencilPass = StencilOperation.Replace,
            StencilFail = StencilOperation.Keep,
            StencilWriteMask = 4,
            StencilMask = 3
        };

        public static readonly DepthStencilState DepthStencil_RenderNearPlanetSurfaceInAtmosphere = new DepthStencilState()
        {
            //  Depth buffer is not required when working with stencil only
            DepthBufferEnable = false,
            DepthBufferWriteEnable = false,

            //  Write color only where stencil enables it (where stencil value equals 1)
            //  But disable writing to stencil buffer
            StencilEnable = true,
            StencilFunction = Compare.Equal,
            ReferenceStencil = 6,
            StencilPass = StencilOperation.Keep,
            StencilFail = StencilOperation.Keep,
            StencilWriteMask = 4,
            StencilMask = 2
        };
        public static readonly DepthStencilState DepthStencil_TestFarObject_DepthReadOnly = new DepthStencilState()
        {
            //  Depth buffer is not required when working with stencil only
            DepthBufferEnable = true,
            DepthBufferWriteEnable = false,

            //  Write color only where stencil enables it (where stencil value equals 1)
            //  But disable writing to stencil buffer
            StencilEnable = true,
            StencilFunction = Compare.NotEqual,
            ReferenceStencil = 1,
            StencilPass = StencilOperation.Keep,
            StencilFail = StencilOperation.Keep,
        };

        public static readonly DepthStencilState DepthStencil_StencilReadOnly = new DepthStencilState()
        {
            //  Depth buffer is not required when working with stencil only
            DepthBufferEnable = false,
            DepthBufferWriteEnable = false,

            //  Write color only where stencil enables it (where stencil value equals 1)
            //  But disable writing to stencil buffer
            StencilEnable = true,
            StencilFunction = Compare.NotEqual,
            ReferenceStencil = 1,
            StencilPass = StencilOperation.Keep,
            StencilFail = StencilOperation.Keep,
        };

        //Used for dynamic decals
        public static RasterizerState BiasedRasterizer_Decals = new RasterizerState
        {
            CullMode = Cull.Counterclockwise,
            //Be careful about setting too high values, decals then are visible through objects
            DepthBias = -0.0001f,  //There is missile decal popping with DepthBias = -0.00001f,
        };

        //Used for static decals
        public static RasterizerState BiasedRasterizer_StaticDecals = new RasterizerState
        {
            CullMode = Cull.Counterclockwise,
            //Be careful about setting too high values, decals then are visible through objects
            DepthBias = -0.00001f,  
        };

        //  Special depth-stencil incrementing whenever is something written
        public static readonly DepthStencilState StencilMask_AlwaysIncrement_DepthStencilState = new DepthStencilState()
        {
            StencilEnable = true,
            StencilFunction = Compare.Always,
            StencilPass = StencilOperation.Increment,

            DepthBufferEnable = false,
        };

        public static readonly RasterizerState BiasedRasterizerCullNone_DebugDraw = new RasterizerState()
        {
            CullMode = Cull.None,
            DepthBias = -0.0000005f,
        };

        public static readonly RasterizerState BiasedRasterizerCounterclockwise_DebugDraw = new RasterizerState()
        {
            CullMode = Cull.Counterclockwise,
            DepthBias = -0.0000005f,
        };


        // Blend state for disabled color channels in occlusion queries
        public static readonly BlendState DisabledColorChannels_BlendState = new BlendState
        {
            AlphaBlendFunction = BlendState.Opaque.AlphaBlendFunction,
            AlphaDestinationBlend = BlendState.Opaque.AlphaDestinationBlend,
            AlphaSourceBlend = BlendState.Opaque.AlphaSourceBlend,
            ColorBlendFunction = BlendState.Opaque.ColorBlendFunction,
            ColorDestinationBlend = BlendState.Opaque.ColorDestinationBlend,
            ColorSourceBlend = BlendState.Opaque.ColorSourceBlend,

            ColorWriteChannels = 0,
            ColorWriteChannels1 = 0,
            ColorWriteChannels2 = 0,
            ColorWriteChannels3 = 0,
        };

        // Blend state for disabled color channels in occlusion queries
        public static readonly BlendState AlphaChannels_BlendState = new BlendState
        {
            AlphaBlendFunction = BlendState.Opaque.AlphaBlendFunction,
            AlphaDestinationBlend = BlendState.Opaque.AlphaDestinationBlend,
            AlphaSourceBlend = BlendState.Opaque.AlphaSourceBlend,
            ColorBlendFunction = BlendState.Opaque.ColorBlendFunction,
            ColorDestinationBlend = BlendState.Opaque.ColorDestinationBlend,
            ColorSourceBlend = BlendState.Opaque.ColorSourceBlend,

            ColorWriteChannels = ColorWriteEnable.Alpha,
            ColorWriteChannels1 = ColorWriteEnable.Alpha,
            ColorWriteChannels2 = ColorWriteEnable.Alpha,
            ColorWriteChannels3 = ColorWriteEnable.Alpha,
        };

        public static readonly BlendState SSAO_BlendState = new BlendState
        {
            AlphaBlendFunction = BlendOperation.Add,
            ColorBlendFunction = BlendOperation.Add,

            AlphaSourceBlend = Blend.Zero,
            AlphaDestinationBlend = Blend.One,

            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,

            ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue,
            ColorWriteChannels1 = 0,
            ColorWriteChannels2 = 0
        };

        public static readonly BlendState Dynamic_Decals_BlendState = new BlendState()
        {
            //Alpha is maxed because of emissivity. If underlaying decal has emissivity 1.0 and overlaying 0.0, we 
            //want their max value
            AlphaBlendFunction = BlendOperation.Maximum,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,

            //Diffuse and normal colors are alpha blended. Emissive component is added (maxed)
            ColorBlendFunction = BlendOperation.Add,
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,

            ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue, //normals
            ColorWriteChannels1 = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue, //diffuse
            ColorWriteChannels2 = ColorWriteEnable.Alpha, //depth
        }; 

        public static readonly BlendState AlphaBlend_NoAlphaWrite_BlendState = new BlendState
        {
            AlphaBlendFunction = BlendState.AlphaBlend.AlphaBlendFunction,
            AlphaDestinationBlend = BlendState.AlphaBlend.AlphaDestinationBlend,
            AlphaSourceBlend = BlendState.AlphaBlend.AlphaSourceBlend,

            BlendFactor = BlendState.AlphaBlend.BlendFactor,
            ColorBlendFunction = BlendState.AlphaBlend.ColorBlendFunction,
            ColorDestinationBlend = BlendState.AlphaBlend.ColorDestinationBlend,
            ColorSourceBlend = BlendState.AlphaBlend.ColorSourceBlend,

            ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue,
        };

        public static readonly BlendState Additive_NoAlphaWrite_BlendState = new BlendState
        {
            AlphaBlendFunction = BlendState.Additive.AlphaBlendFunction,
            AlphaDestinationBlend = BlendState.Additive.AlphaDestinationBlend,
            AlphaSourceBlend = BlendState.Additive.AlphaSourceBlend,

            BlendFactor = BlendState.Additive.BlendFactor,
            ColorBlendFunction = BlendState.Additive.ColorBlendFunction,
            ColorDestinationBlend = BlendState.Additive.ColorDestinationBlend,
            ColorSourceBlend = BlendState.Additive.ColorSourceBlend,

            ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue,
        };

        public static readonly BlendState NonPremultiplied_NoAlphaWrite_BlendState = new BlendState
        {
            AlphaBlendFunction = BlendState.NonPremultiplied.AlphaBlendFunction,
            AlphaDestinationBlend = BlendState.NonPremultiplied.AlphaDestinationBlend,
            AlphaSourceBlend = BlendState.NonPremultiplied.AlphaSourceBlend,

            BlendFactor = BlendState.NonPremultiplied.BlendFactor,
            ColorBlendFunction = BlendState.NonPremultiplied.ColorBlendFunction,
            ColorDestinationBlend = BlendState.NonPremultiplied.ColorDestinationBlend,
            ColorSourceBlend = BlendState.NonPremultiplied.ColorSourceBlend,

            ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue,
        };


        /// <summary>
        /// Blend state for static decals. It has these features:
        /// - Alpha blends diffuse color and preserve specular intensity of underlying material
        /// - Overwrites normals and preserve specular power
        /// - Preserves depth, but overwrites emissive
        /// </summary>
        public static readonly BlendState Static_Decals_BlendState = new BlendState()
        {
            //Alpha is minimum because of emissivity. If underlaying decal has emissivity 1.0 and overlaying 0.0, we 
            //want their min value (because it is inverted)
            AlphaBlendFunction = BlendOperation.Maximum ,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,

            //Diffuse and normal colors are alpha blended. Emissive component is added (maxed)
            ColorBlendFunction = BlendOperation.Add,
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,

            ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue, //normals
            ColorWriteChannels1 = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue, //diffuse
            ColorWriteChannels2 = ColorWriteEnable.Alpha, //depth
        };

        /// <summary>
        /// Blend state for static decals. It has these features:
        /// - Alpha blends diffuse color and preserve specular intensity of underlying material
        /// - Overwrites normals and preserve specular power
        /// - Preserves depth, but overwrites emissive
        /// </summary>
        public static readonly BlendState Holo_BlendState = new BlendState()
        {
            //Alpha is maxed because of emissivity. If underlaying decal has emissivity 1.0 and overlaying 0.0, we 
            //want their min value (because it is inverted)
            AlphaBlendFunction = BlendOperation.Maximum,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,

            //Diffuse and normal colors are alpha blended. Emissive component is added (maxed)
            ColorBlendFunction = BlendOperation.Add,
            ColorSourceBlend = Blend.SourceAlpha,
            ColorDestinationBlend = Blend.InverseSourceAlpha,

            ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue, //normals
            ColorWriteChannels1 = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue, //diffuse
            ColorWriteChannels2 = ColorWriteEnable.Alpha, //depth
        };

        /// <summary>
        /// Blends in emissive light using additive blend (additive blending to light acc target)
        /// Overwrites alpha (emissivity) with values from source (diffuse rt)
        /// </summary>
        public static readonly BlendState AddEmissiveLight_BlendState = new BlendState()
        {
            ColorSourceBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendOperation.Add,
            AlphaBlendFunction = BlendOperation.Add,
        };

        /// <summary>
        /// Blends in emissive light using additive blend (additive blending to light acc target)
        /// Overwrites leaves alpha as it is - in order to be able to generate images with transparent background.
        /// Used for rendering preview images for editor.
        /// </summary>
        public static readonly BlendState AddEmissiveLight_NoAlphaWrite_BlendState = new BlendState()
        {
            ColorSourceBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaDestinationBlend = Blend.Zero,
            ColorBlendFunction = BlendOperation.Add,
            AlphaBlendFunction = BlendOperation.Add,
            ColorWriteChannels = ColorWriteEnable.Green | ColorWriteEnable.Red | ColorWriteEnable.Blue
        };
                   
        /// <summary>
        /// </summary>
        public static readonly BlendState Light_Combination_BlendState = new BlendState()
        {
            //Alpha is maxed because of emissivity. If underlaying decal has emissivity 1.0 and overlaying 0.0, we 
            //want their min value (because it is inverted)
            AlphaBlendFunction = BlendOperation.Maximum,
            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,

            //Diffuse and normal colors are alpha blended. Emissive component is added (maxed)
            ColorBlendFunction = BlendOperation.Maximum,
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,

            ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue, //normals
            ColorWriteChannels1 = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue, //diffuse
            ColorWriteChannels2 = ColorWriteEnable.Alpha, //depth
        };

        public static readonly BlendState Sun_Combination_BlendState = new BlendState()
        {
            ColorSourceBlend = Blend.One,
            AlphaSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            AlphaDestinationBlend = Blend.One
        };


        public static readonly BlendState VolumetricFogBlend = new BlendState()
        {
            AlphaBlendFunction = BlendState.AlphaBlend.AlphaBlendFunction,
            AlphaDestinationBlend = BlendState.AlphaBlend.AlphaDestinationBlend,
            AlphaSourceBlend = BlendState.AlphaBlend.AlphaSourceBlend,

            BlendFactor = BlendState.AlphaBlend.BlendFactor,
            ColorBlendFunction = BlendState.AlphaBlend.ColorBlendFunction,
            ColorDestinationBlend = Blend.InverseSourceAlpha,
            ColorSourceBlend = Blend.SourceAlpha,

            ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue,       
        };
          
    }
}
