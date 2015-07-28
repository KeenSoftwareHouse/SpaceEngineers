using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using VRageMath;

namespace VRageRender
{
    class MyHWResource
    {
        internal SharpDX.Direct3D11.Resource m_resource;

        internal virtual void Release()
        {
            if (m_resource != null)
            {
                m_resource.Dispose();
                m_resource = null;
            }
        }

        internal void SetDebugName(string name)
        {
            m_resource.DebugName = name;
        }
    }

    class MyBindableResource : MyHWResource
    {
        internal static int AutoId = 0;
        internal static int GenerateId() { return AutoId++; }
        internal readonly int ResId;

        internal virtual Vector3I GetSize() { throw new NotImplementedException(); }

        internal MyBindableResource()
        {
            ResId = GenerateId();
        }

        internal virtual int GetID()
        {
            return ResId;
        }

        internal virtual SharpDX.Direct3D11.Resource GetHWResource()
        {
            return m_resource;
        }
    }

    class MyBackbuffer : MyBindableResource, IRenderTargetBindable
    {
        internal RenderTargetView m_RTV;

        RenderTargetView IRenderTargetBindable.RTV
        {
            get { return m_RTV; }
        }

        internal MyBackbuffer(SharpDX.Direct3D11.Resource swapChainBB)
        {
            m_resource = swapChainBB;
            m_RTV = new RenderTargetView(MyRender11.Device, swapChainBB);
        }

        internal override void Release()
        {
            if (m_RTV != null)
            {
                m_RTV.Dispose();
                m_RTV = null;
            }
            base.Release();
        }

        internal override Vector3I GetSize() { return new Vector3I(MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y, 1); }
    }

    class MyDepthView : MyBindableResource, IDepthStencilBindable, IShaderResourceBindable
    {
        MyDepthStencil m_owner;

        internal MyDepthView(MyDepthStencil from)
        {
            m_owner = from;
        }

        DepthStencilView IDepthStencilBindable.DSV
        {
            get { return m_owner.m_DSV_roDepth; }
        }

        ShaderResourceView IShaderResourceBindable.SRV
        {
            get { return m_owner.m_SRV_depth; }
        }
    }

    class MyStencilView : MyBindableResource, IDepthStencilBindable, IShaderResourceBindable
    {
        MyDepthStencil m_owner;

        internal MyStencilView(MyDepthStencil from)
        {
            m_owner = from;
        }

        DepthStencilView IDepthStencilBindable.DSV
        {
            get { return m_owner.m_DSV_roStencil; }
        }

        ShaderResourceView IShaderResourceBindable.SRV
        {
            get { return m_owner.m_SRV_stencil; }
        }
    }

    class MyDepthStencil : MyBindableResource
    {
        const bool Depth32F = true;

        internal ShaderResourceView m_SRV_depth;
        internal ShaderResourceView m_SRV_stencil;
        internal DepthStencilView m_DSV;
        internal DepthStencilView m_DSV_roDepth;
        internal DepthStencilView m_DSV_roStencil;
        internal DepthStencilView m_DSV_ro;

        internal Vector2I m_resolution;
        internal Vector2I m_samples;

        MyDepthView m_depthSubresource;
        MyStencilView m_stencilSubresource;

        internal MyBindableResource Depth { get { return m_depthSubresource; } }
        internal MyBindableResource Stencil { get { return m_stencilSubresource; } }

        internal MyDepthStencil(int width, int height,
            int sampleCount, int sampleQuality)
        {
            m_resolution = new Vector2I(width, height);
            m_samples = new Vector2I(sampleCount, sampleQuality);

            m_depthSubresource = new MyDepthView(this);
            m_stencilSubresource = new MyStencilView(this);

            Texture2DDescription desc = new Texture2DDescription();
            desc.Width = width;
            desc.Height = height;
            desc.Format = Depth32F ? Format.R32G8X24_Typeless : Format.R24G8_Typeless;
            desc.ArraySize = 1;
            desc.MipLevels = 1;
            desc.BindFlags = BindFlags.DepthStencil | BindFlags.ShaderResource;
            desc.Usage = ResourceUsage.Default;
            desc.CpuAccessFlags = 0;
            desc.SampleDescription.Count = sampleCount;
            desc.SampleDescription.Quality = sampleQuality;
            desc.OptionFlags = 0;
            m_resource = new Texture2D(MyRender11.Device, desc);

            DepthStencilViewDescription dsvDesc = new DepthStencilViewDescription();
            dsvDesc.Format = Depth32F ? Format.D32_Float_S8X24_UInt : Format.D24_UNorm_S8_UInt;
            if (sampleCount == 1)
            {
                dsvDesc.Dimension = DepthStencilViewDimension.Texture2D;
                dsvDesc.Flags = DepthStencilViewFlags.None;
                dsvDesc.Texture2D.MipSlice = 0;
            }
            else
            {
                dsvDesc.Dimension = DepthStencilViewDimension.Texture2DMultisampled;
                dsvDesc.Flags = DepthStencilViewFlags.None;
            }
            m_DSV = new DepthStencilView(MyRender11.Device, m_resource, dsvDesc);
            if (sampleCount == 1)
            {
                dsvDesc.Dimension = DepthStencilViewDimension.Texture2D;
                dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyDepth;
                dsvDesc.Texture2D.MipSlice = 0;
            }
            else
            {
                dsvDesc.Dimension = DepthStencilViewDimension.Texture2DMultisampled;
                dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyDepth;
            }
            m_DSV_roDepth = new DepthStencilView(MyRender11.Device, m_resource, dsvDesc);
            if (sampleCount == 1)
            {
                dsvDesc.Dimension = DepthStencilViewDimension.Texture2D;
                dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyStencil;
                dsvDesc.Texture2D.MipSlice = 0;
            }
            else
            {
                dsvDesc.Dimension = DepthStencilViewDimension.Texture2DMultisampled;
                dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyStencil;
            }
            m_DSV_roStencil = new DepthStencilView(MyRender11.Device, m_resource, dsvDesc);
            dsvDesc.Flags = DepthStencilViewFlags.ReadOnlyStencil | DepthStencilViewFlags.ReadOnlyDepth;
            if (sampleCount == 1)
            {
                dsvDesc.Dimension = DepthStencilViewDimension.Texture2D;
                dsvDesc.Texture2D.MipSlice = 0;
            }
            else
            {
                dsvDesc.Dimension = DepthStencilViewDimension.Texture2DMultisampled;
            }
            m_DSV_ro = new DepthStencilView(MyRender11.Device, m_resource, dsvDesc);

            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription();
            srvDesc.Format = Depth32F ? Format.R32_Float_X8X24_Typeless : Format.R24_UNorm_X8_Typeless;
            if (sampleCount == 1)
            {
                srvDesc.Dimension = ShaderResourceViewDimension.Texture2D;
                srvDesc.Texture2D.MipLevels = -1;
                srvDesc.Texture2D.MostDetailedMip = 0;
            }
            else
            {
                srvDesc.Dimension = ShaderResourceViewDimension.Texture2DMultisampled;
            }
            m_SRV_depth = new ShaderResourceView(MyRender11.Device, m_resource, srvDesc);

            srvDesc.Format = Depth32F ? Format.X32_Typeless_G8X24_UInt : Format.X24_Typeless_G8_UInt;
            if (sampleCount == 1)
            {
                srvDesc.Dimension = ShaderResourceViewDimension.Texture2D;
                srvDesc.Texture2D.MipLevels = -1;
                srvDesc.Texture2D.MostDetailedMip = 0;
            }
            else
            {
                srvDesc.Dimension = ShaderResourceViewDimension.Texture2DMultisampled;
            }
            m_SRV_stencil = new ShaderResourceView(MyRender11.Device, m_resource, srvDesc);
        }

        internal override void Release()
        {
            if (m_SRV_depth != null)
            {
                m_SRV_depth.Dispose();
                m_SRV_depth = null;
            }
            if (m_SRV_stencil != null)
            {
                m_SRV_stencil.Dispose();
                m_SRV_stencil = null;
            }
            if (m_DSV != null)
            {
                m_DSV.Dispose();
                m_DSV = null;
            }
            if (m_DSV_roDepth != null)
            {
                m_DSV_roDepth.Dispose();
                m_DSV_roDepth = null;
            }
            if (m_DSV_roStencil != null)
            {
                m_DSV_roStencil.Dispose();
                m_DSV_roStencil = null;
            }
            if (m_DSV_ro != null)
            {
                m_DSV_ro.Dispose();
                m_DSV_ro = null;
            }

            base.Release();
        }
    }

    class MyUnorderedAccessTexture : MyBindableResource, IUnorderedAccessBindable, IShaderResourceBindable, IRenderTargetBindable
    {
        internal ShaderResourceView m_SRV;
        internal UnorderedAccessView m_UAV;
        internal RenderTargetView m_RTV;

        internal Vector2I m_resolution;

        ShaderResourceView IShaderResourceBindable.SRV
        {
            get { return m_SRV; }
        }

        UnorderedAccessView IUnorderedAccessBindable.UAV
        {
            get { return m_UAV; }
        }

        RenderTargetView IRenderTargetBindable.RTV
        {
            get { return m_RTV; }
        }

        internal override void Release()
        {
            if (m_SRV != null)
            {
                m_SRV.Dispose();
                m_SRV = null;
            }
            if (m_UAV != null)
            {
                m_UAV.Dispose();
                m_UAV = null;
            }
            if (m_RTV != null)
            {
                m_RTV.Dispose();
                m_RTV = null;
            }

            base.Release();
        }

        internal override Vector3I GetSize()
        {
            return new Vector3I(m_resolution.X, m_resolution.Y, 0);
        }

        internal MyUnorderedAccessTexture(int width, int height, Format format)
        {
            m_resolution = new Vector2I(width, height);

            Texture2DDescription desc = new Texture2DDescription();
            desc.Width = width;
            desc.Height = height;
            desc.Format = format;
            desc.ArraySize = 1;
            desc.MipLevels = 1;
            desc.BindFlags = BindFlags.UnorderedAccess | BindFlags.ShaderResource | BindFlags.RenderTarget;
            desc.Usage = ResourceUsage.Default;
            desc.CpuAccessFlags = 0;
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;
            desc.OptionFlags = 0;

            m_resource = new Texture2D(MyRender11.Device, desc);
            m_UAV = new UnorderedAccessView(MyRender11.Device, m_resource);
            m_SRV = new ShaderResourceView(MyRender11.Device, m_resource);
            m_RTV = new RenderTargetView(MyRender11.Device, m_resource);
        }
    }

    enum MyViewEnum
    {
        UavView,
        SrvView,
        RtvView
    }

    struct MyViewKey
    {
        internal MyViewEnum View;
        internal Format Fmt;
    }

    class MySrvView : MyBindableResource, IShaderResourceBindable
    {
        MyBindableResource m_owner;
        ShaderResourceView m_srv;

        internal MySrvView(MyBindableResource from, Format fmt)
        {
            m_owner = from;

            var desc = new ShaderResourceViewDescription
            {
                Format = fmt,
                Dimension = ShaderResourceViewDimension.Texture2D,
                Texture2D = new ShaderResourceViewDescription.Texture2DResource { MipLevels = 1, MostDetailedMip = 0 }
            };

            m_srv = new ShaderResourceView(MyRender11.Device, m_owner.m_resource, desc);
        }

        internal override int GetID()
        {
            return m_owner.GetID();
        }

        internal override Vector3I GetSize()
        {
            return m_owner.GetSize();
        }

        internal override SharpDX.Direct3D11.Resource GetHWResource()
        {
            return m_owner.GetHWResource();
        }

        ShaderResourceView IShaderResourceBindable.SRV
        {
            get { return m_srv; }
        }

        internal override void Release()
        {
            if(m_srv != null)
            {
                m_srv.Dispose();
            }

            m_owner = null;

            base.Release();
        }
    }

    class MyUavView : MyBindableResource, IUnorderedAccessBindable
    {
        MyBindableResource m_owner;
        UnorderedAccessView m_uav;

        internal MyUavView(MyBindableResource from, Format fmt)
        {
            m_owner = from;

            var desc = new UnorderedAccessViewDescription
            {
                Format = fmt,
                Dimension = UnorderedAccessViewDimension.Texture2D,
                Texture2D = new UnorderedAccessViewDescription.Texture2DResource { MipSlice = 0 }
            };

            m_uav = new UnorderedAccessView(MyRender11.Device, m_owner.m_resource, desc);
        }

        internal override Vector3I GetSize()
        {
            return m_owner.GetSize();
        }

        internal override void Release()
        {
            if (m_uav != null)
            {
                m_uav.Dispose();
            }

            m_owner = null;

            base.Release();
        }

        internal override int GetID()
        {
            return m_owner.GetID();
        }
    
        public UnorderedAccessView UAV
        {
	        get { return m_uav; }
        }
    }

    class MyRtvView : MyBindableResource, IRenderTargetBindable
    {
        MyBindableResource m_owner;
        RenderTargetView m_rtv;

        internal MyRtvView(MyBindableResource from, Format fmt)
        {
            m_owner = from;

            var desc = new RenderTargetViewDescription
            {
                Format = fmt,
                Dimension = RenderTargetViewDimension.Texture2D,
                Texture2D = new RenderTargetViewDescription.Texture2DResource { MipSlice = 0 }
            };

            m_rtv = new RenderTargetView(MyRender11.Device, m_owner.m_resource, desc);
        }

        internal override Vector3I GetSize()
        {
            return m_owner.GetSize();
        }

        internal override void Release()
        {
            if (m_rtv != null)
            {
                m_rtv.Dispose();
            }

            m_owner = null;

            base.Release();
        }

        internal override int GetID()
        {
            return m_owner.GetID();
        }

        public RenderTargetView RTV
        {
	        get { return m_rtv; }
        }
}

    class MyCustomTexture : MyBindableResource
    {
        internal Vector2I m_resolution;

        internal Dictionary<MyViewKey, MyBindableResource> m_views = new Dictionary<MyViewKey, MyBindableResource>();

        internal MyCustomTexture(int width, int height, BindFlags bindflags,
            Format format)
        {
            m_resolution = new Vector2I(width, height);

            Texture2DDescription desc = new Texture2DDescription();
            desc.Width = width;
            desc.Height = height;
            desc.Format = format;
            desc.ArraySize = 1;
            desc.MipLevels = 1;
            desc.BindFlags = bindflags;
            desc.Usage = ResourceUsage.Default;
            desc.CpuAccessFlags = 0;
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;
            desc.OptionFlags = 0;
            m_resource = new Texture2D(MyRender11.Device, desc);   
        }

        internal void AddView(MyViewKey view)
        {
            Debug.Assert(!m_views.ContainsKey(view));


            if(view.View == MyViewEnum.SrvView)
            {
                m_views[view] = new MySrvView(this, view.Fmt);
            }
            else if(view.View == MyViewEnum.UavView)
            {
                m_views[view] = new MyUavView(this, view.Fmt);
            }
            else if(view.View == MyViewEnum.RtvView)
            {
                m_views[view] = new MyRtvView(this, view.Fmt);
            }
            
        }

        internal override Vector3I GetSize() { return new Vector3I(MyRender11.ResolutionI.X, MyRender11.ResolutionI.Y, 1); }

        internal MyBindableResource GetView(MyViewKey view)
        {
            return m_views.Get(view);
        }

        internal override void Release()
        {
            foreach(var view in m_views)
            {
                view.Value.Release();
            }
            m_views.Clear();

            base.Release();
        }
    }

    class MyRWStructuredBuffer : MyBindableResource, IUnorderedAccessBindable, IShaderResourceBindable
    {
        internal ShaderResourceView m_SRV;
        internal UnorderedAccessView m_UAV;

        internal Vector2I m_resolution;

        ShaderResourceView IShaderResourceBindable.SRV
        {
            get { return m_SRV; }
        }

        UnorderedAccessView IUnorderedAccessBindable.UAV
        {
            get { return m_UAV; }
        }

        internal override void Release()
        {
            if (m_SRV != null)
            {
                m_SRV.Dispose();
                m_SRV = null;
            }
            if (m_UAV != null)
            {
                m_UAV.Dispose();
                m_UAV = null;
            }

            base.Release();
        }

        internal override Vector3I GetSize()
        {
            return new Vector3I(m_resolution.X, m_resolution.Y, 0);
        }

        internal MyRWStructuredBuffer(int elements, int stride)
        {
            m_resolution = new Vector2I(elements, 1);

            var bufferDesc = new BufferDescription(elements * stride, ResourceUsage.Default, BindFlags.ShaderResource | BindFlags.UnorderedAccess, 
                CpuAccessFlags.None, ResourceOptionFlags.BufferStructured, stride);

            m_resource = new SharpDX.Direct3D11.Buffer(MyRender11.Device, bufferDesc);
            m_UAV = new UnorderedAccessView(MyRender11.Device, m_resource);
            m_SRV = new ShaderResourceView(MyRender11.Device, m_resource);
        }
    }

    class MyRenderTarget : MyBindableResource, IRenderTargetBindable, IShaderResourceBindable
    {
        internal ShaderResourceView m_SRV;
        internal RenderTargetView m_RTV;

        internal Vector2I m_resolution;
        internal Vector2I m_samples;

        internal override Vector3I GetSize()
        {
            return new Vector3I(m_resolution.X, m_resolution.Y, 1);
        }

        ShaderResourceView IShaderResourceBindable.SRV
        {
            get { return m_SRV; }
        }

        RenderTargetView IRenderTargetBindable.RTV
        {
            get { return m_RTV; }
        }

        internal override void Release()
        {
            if (m_SRV != null)
            {
                m_SRV.Dispose();
                m_SRV = null;
            }
            if (m_RTV != null)
            {
                m_RTV.Dispose();
                m_RTV = null;
            }

            base.Release();
        }

        internal MyRenderTarget(int width, int height, Format format,
            int sampleCount, int sampleQuality)
        {
            m_resolution = new Vector2I(width, height);
            m_samples = new Vector2I(sampleCount, sampleQuality);

            Texture2DDescription desc = new Texture2DDescription();
            desc.Width = width;
            desc.Height = height;
            desc.Format = format;
            desc.ArraySize = 1;
            desc.MipLevels = 1;
            desc.BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource;
            desc.Usage = ResourceUsage.Default;
            desc.CpuAccessFlags = 0;
            desc.SampleDescription.Count = sampleCount;
            desc.SampleDescription.Quality = sampleQuality;
            desc.OptionFlags = 0;

            m_resource = new Texture2D(MyRender11.Device, desc);
            m_RTV = new RenderTargetView(MyRender11.Device, m_resource);
            m_SRV = new ShaderResourceView(MyRender11.Device, m_resource);
        }
    }
}
