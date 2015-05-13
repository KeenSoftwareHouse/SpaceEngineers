using SharpDX.Direct3D9;
using System.Collections.Generic;
using System;
using SharpDX;

namespace VRageRender.Graphics
{
    /// <summary>
    /// BlendState is equivalent to <see cref="SharpDX.Direct3D11.BlendState"/>.
    /// </summary>
    /// <remarks>
    /// This class provides default stock blend states and easier constructors. It is also associating the <see cref="BlendFactor"/> and <see cref="MultiSampleMask"/> into the same object.
    /// </remarks>
    class BlendState : MyRenderComponentBase, IDisposable
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.BlendState;
        }

        public static BlendState Current { get; private set; }

        // Summary:
        //     A built-in state object with settings for additive blend, that is adding
        //     the destination data to the source data without using alpha.
        public static readonly BlendState Additive;
        //
        // Summary:
        //     A built-in state object with settings for alpha blend, that is blending the
        //     source and destination data using alpha.
        public static readonly BlendState AlphaBlend;
        //
        // Summary:
        //     A built-in state object with settings for blending with non-premultipled
        //     alpha, that is blending source and destination data using alpha while assuming
        //     the color data contains no alpha information.
        public static readonly BlendState NonPremultiplied;
        //
        // Summary:
        //     A built-in state object with settings for opaque blend, that is overwriting
        //     the source with the destination data.
        public static readonly BlendState Opaque;
        //
        // Summary:
        //     A built-in state object with settings for text panel emmisive text
        //     it will keep original aplha value and don't overwrite it
        public static readonly BlendState EmissiveTexture;

        static BlendState()
        {
            Additive = new BlendState()
            {
                ColorSourceBlend = Blend.SourceAlpha,
                AlphaSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.One,
                AlphaDestinationBlend = Blend.One
            };

            AlphaBlend = new BlendState()
            {
                ColorSourceBlend = Blend.One,
                AlphaSourceBlend = Blend.One,
                ColorDestinationBlend = Blend.InverseSourceAlpha,
                AlphaDestinationBlend = Blend.InverseSourceAlpha
            };

            NonPremultiplied = new BlendState()
            {
                ColorSourceBlend = Blend.SourceAlpha,
                AlphaSourceBlend = Blend.SourceAlpha,
                ColorDestinationBlend = Blend.InverseSourceAlpha,
                AlphaDestinationBlend = Blend.InverseSourceAlpha
            };

            Opaque = new BlendState()
            {
                ColorSourceBlend = Blend.One,
                AlphaSourceBlend = Blend.One,
                ColorDestinationBlend = Blend.Zero,
                AlphaDestinationBlend = Blend.Zero
            };

            EmissiveTexture = new BlendState()
            {
                ColorSourceBlend = Blend.SourceAlpha,
                AlphaSourceBlend = Blend.Zero,
                ColorDestinationBlend = Blend.InverseSourceAlpha,
                AlphaDestinationBlend = Blend.Zero,
                ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue,

            };
        }

        static Device m_device;
        static List<BlendState> m_instances = new List<BlendState>(16);
        StateBlock m_stateBlock;

        public override void LoadContent(Device device)
        {
            System.Diagnostics.Debug.Assert(m_device == null);
            m_device = device;
        }

        public override void UnloadContent()
        {
            m_device = null;

            foreach (var instance in m_instances)
            {
                instance.Dispose();
            }

            m_instances.Clear();
        }

        public void Dispose()
        {
            m_stateBlock.Dispose();
            m_stateBlock = null;
        }

        // Summary:
        //     Creates an instance of the BlendState class with default values, using additive
        //     color and alpha blending.
        public BlendState()
        {
            AlphaBlendFunction = BlendOperation.Add;
            AlphaSourceBlend = Blend.One;
            AlphaDestinationBlend = Blend.Zero;

            ColorBlendFunction = BlendOperation.Add;
            ColorSourceBlend = Blend.One;
            ColorDestinationBlend = Blend.Zero;

            BlendFactor = Color.White;
            MultiSampleMask = int.MaxValue;

            
            ColorWriteChannels = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue | ColorWriteEnable.Alpha;
            ColorWriteChannels1 = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue | ColorWriteEnable.Alpha;
            ColorWriteChannels2 = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue | ColorWriteEnable.Alpha;
            ColorWriteChannels3 = ColorWriteEnable.Red | ColorWriteEnable.Green | ColorWriteEnable.Blue | ColorWriteEnable.Alpha;
        }

        // Summary:
        //     Gets or sets the arithmetic operation when blending alpha values. The default
        //     is BlendFunction.Add.
        public BlendOperation AlphaBlendFunction { get; set; }
        //
        // Summary:
        //     Gets or sets the blend factor for the destination alpha, which is the percentage
        //     of the destination alpha included in the blended result. The default is Blend.One.
        public Blend AlphaDestinationBlend { get; set; }
        //
        // Summary:
        //     Gets or sets the alpha blend factor. The default is Blend.One.
        public Blend AlphaSourceBlend { get; set; }
        //
        // Summary:
        //     Gets or sets the four-component (RGBA) blend factor for alpha blending.
        public Color BlendFactor { get; set; }
        //
        // Summary:
        //     Gets or sets the arithmetic operation when blending color values. The default
        //     is BlendFunction.Add.
        public BlendOperation ColorBlendFunction { get; set; }
        //
        // Summary:
        //     Gets or sets the blend factor for the destination color. The default is Blend.One.
        public Blend ColorDestinationBlend { get; set; }
        //
        // Summary:
        //     Gets or sets the blend factor for the source color. The default is Blend.One.
        public Blend ColorSourceBlend { get; set; }
        //
        // Summary:
        //     Gets or sets which color channels (RGBA) are enabled for writing during color
        //     blending. The default value is ColorWriteChannels.None.
        public ColorWriteEnable ColorWriteChannels { get; set; }
        //
        // Summary:
        //     Gets or sets which color channels (RGBA) are enabled for writing during color
        //     blending. The default value is ColorWriteChannels.None.
        public ColorWriteEnable ColorWriteChannels1 { get; set; }
        //
        // Summary:
        //     Gets or sets which color channels (RGBA) are enabled for writing during color
        //     blending. The default value is ColorWriteChannels.None.
        public ColorWriteEnable ColorWriteChannels2 { get; set; }
        //
        // Summary:
        //     Gets or sets which color channels (RGBA) are enabled for writing during color
        //     blending. The default value is ColorWriteChannels.None.
        public ColorWriteEnable ColorWriteChannels3 { get; set; }
        //
        // Summary:
        //     Gets or sets a bitmask which defines which samples can be written during
        //     multisampling. The default is 0xffffffff.
        public int MultiSampleMask { get; set; }

        public void Apply()
        {
            if (m_stateBlock == null)
            {
                m_device.BeginStateBlock();

                m_device.SetRenderState(RenderState.AlphaBlendEnable, true);
                m_device.SetRenderState(RenderState.AlphaFunc, Compare.Always);

                m_device.SetRenderState(RenderState.BlendOperationAlpha, AlphaBlendFunction);
                m_device.SetRenderState(RenderState.DestinationBlendAlpha, AlphaDestinationBlend);
                m_device.SetRenderState(RenderState.SourceBlendAlpha, AlphaSourceBlend);
                m_device.SetRenderState(RenderState.BlendFactor, BlendFactor.ToRgba());
                m_device.SetRenderState(RenderState.BlendOperation, ColorBlendFunction);
                m_device.SetRenderState(RenderState.DestinationBlend, ColorDestinationBlend);
                m_device.SetRenderState(RenderState.SourceBlend, ColorSourceBlend);

                m_device.SetRenderState(RenderState.ColorWriteEnable, ColorWriteChannels);
                m_device.SetRenderState(RenderState.ColorWriteEnable1, ColorWriteChannels1);
                m_device.SetRenderState(RenderState.ColorWriteEnable2, ColorWriteChannels2);
                m_device.SetRenderState(RenderState.ColorWriteEnable3, ColorWriteChannels3);

                m_stateBlock = m_device.EndStateBlock();
                m_instances.Add(this);
            }
 
            m_stateBlock.Apply();

            Current = this;
        }
   }
}