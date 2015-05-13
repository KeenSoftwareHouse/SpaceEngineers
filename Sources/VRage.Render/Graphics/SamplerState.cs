using SharpDX;
using SharpDX.Direct3D9;
using System.Collections.Generic;
using System;

namespace VRageRender.Graphics
{
    class SamplerState : MyRenderComponentBase, IDisposable
    {
        public override int GetID()
        {
            return (int)MyRenderComponentID.SamplerState;
        }

        // Summary:
        //     Contains default state for anisotropic filtering and texture coordinate clamping.
        public static readonly SamplerState AnisotropicClamp;
        //
        // Summary:
        //     Contains default state for anisotropic filtering and texture coordinate wrapping.
        public static readonly SamplerState AnisotropicWrap;
        //
        // Summary:
        //     Contains default state for linear filtering and texture coordinate clamping.
        public static readonly SamplerState LinearClamp;
        //
        // Summary:
        //     Contains default state for linear filtering and texture coordinate wrapping.
        public static readonly SamplerState LinearWrap;
        //
        // Summary:
        //     Contains default state for point filtering and texture coordinate clamping.
        public static readonly SamplerState PointClamp;
        //
        // Summary:
        //     Contains default state for point filtering and texture coordinate wrapping.
        public static readonly SamplerState PointWrap;

        static Device m_device;
        static List<SamplerState> m_instances = new List<SamplerState>(16);
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

        static SamplerState()
        {
            AnisotropicClamp = new SamplerState()
            {
                Filter = TextureFilter.Anisotropic,
                AddressU = TextureAddress.Clamp,
                AddressV = TextureAddress.Clamp,
                AddressW = TextureAddress.Clamp,
            };

            AnisotropicWrap = new SamplerState()
            {
                Filter = TextureFilter.Anisotropic,
                AddressU = TextureAddress.Wrap,
                AddressV = TextureAddress.Wrap,
                AddressW = TextureAddress.Wrap,
            };

            LinearClamp = new SamplerState()
            {
                Filter = TextureFilter.Linear,
                AddressU = TextureAddress.Clamp,
                AddressV = TextureAddress.Clamp,
                AddressW = TextureAddress.Clamp,
            };

            LinearWrap = new SamplerState()
            {
                Filter = TextureFilter.Linear,
                AddressU = TextureAddress.Wrap,
                AddressV = TextureAddress.Wrap,
                AddressW = TextureAddress.Wrap,
            };

            PointClamp = new SamplerState()
            {
                Filter = TextureFilter.Linear,
                AddressU = TextureAddress.Clamp,
                AddressV = TextureAddress.Clamp,
                AddressW = TextureAddress.Clamp,
            };

            PointWrap = new SamplerState()
            {
                Filter = TextureFilter.Linear,
                AddressU = TextureAddress.Wrap,
                AddressV = TextureAddress.Wrap,
                AddressW = TextureAddress.Wrap,
            };
        }

        // Summary:
        //     Initializes a new instance of the sampler state class.
        public SamplerState()
        {
            MaxAnisotropy = 1;
            AddressU = TextureAddress.Wrap;
            AddressV = TextureAddress.Wrap;
            AddressW = TextureAddress.Wrap;
            Filter = TextureFilter.None;
            MaxMipLevel = 0;
            MipMapLevelOfDetailBias = 0;
        }

        // Summary:
        //     Gets or sets the texture-address mode for the u-coordinate.
        public TextureAddress AddressU { get; set; }
        //
        // Summary:
        //     Gets or sets the texture-address mode for the v-coordinate.
        public TextureAddress AddressV { get; set; }
        //
        // Summary:
        //     Gets or sets the texture-address mode for the w-coordinate.
        public TextureAddress AddressW { get; set; }
        //
        // Summary:
        //     Gets or sets the type of filtering during sampling.
        public TextureFilter Filter { get; set; }
        //
        // Summary:
        //     Gets or sets the maximum anisotropy. The default value is 1.
        public int MaxAnisotropy { get; set; }
        //
        // Summary:
        //     Gets or sets the level of detail (LOD) index of the largest map to use.
        public int MaxMipLevel { get; set; }
        //
        // Summary:
        //     Gets or sets the mipmap LOD bias. The default value is 0.
        public float MipMapLevelOfDetailBias { get; set; }

        public void Apply()
        {
            if (m_stateBlock == null)
            {
                m_device.BeginStateBlock();
                m_device.SetSamplerState(0, SharpDX.Direct3D9.SamplerState.AddressU, AddressU);
                m_device.SetSamplerState(0, SharpDX.Direct3D9.SamplerState.AddressV, AddressV);
                m_device.SetSamplerState(0, SharpDX.Direct3D9.SamplerState.AddressW, AddressW);

                m_device.SetSamplerState(0, SharpDX.Direct3D9.SamplerState.MagFilter, Filter);
                m_device.SetSamplerState(0, SharpDX.Direct3D9.SamplerState.MinFilter, Filter);
                m_device.SetSamplerState(0, SharpDX.Direct3D9.SamplerState.MipFilter, Filter);

                m_device.SetSamplerState(0, SharpDX.Direct3D9.SamplerState.MaxAnisotropy, MaxAnisotropy);
                m_device.SetSamplerState(0, SharpDX.Direct3D9.SamplerState.MaxMipLevel, MaxMipLevel);
                m_device.SetSamplerState(0, SharpDX.Direct3D9.SamplerState.MipMapLodBias, MipMapLevelOfDetailBias);
                m_stateBlock = m_device.EndStateBlock();
                m_instances.Add(this);
            }

            m_stateBlock.Apply();
        }

    }
}
