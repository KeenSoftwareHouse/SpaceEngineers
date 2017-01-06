using System.Collections.Generic;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRage.Generics;
using VRage.Render11.Common;
using VRage.Render11.Resources.Internal;
using VRageMath;
using VRageRender;
using Resource = SharpDX.Direct3D11.Resource;

namespace VRage.Render11.Resources
{
    internal interface ISrvArraySubresource : ISrvBindable
    {
        
    }

    internal interface IRtvArraySubresource : IRtvBindable
    {
        
    }

    internal interface IUavArraySubresource : IUavBindable
    {

    }

    internal interface IDepthArraySubresource : IDsvBindable
    {
    
    }

    internal interface IArrayTexture : ISrvBindable
    {
        int NumSlices { get; }
        int MipmapLevels { get; }
    }

    internal interface IRtvArrayTexture : IArrayTexture, IRtvBindable
    {
        ISrvBindable SubresourceSrv(int nSlice, int nMipmapLevel = 0);
        IRtvBindable SubresourceRtv(int nSlice, int nMipmapLevel = 0);
    }

    internal interface IUavArrayTexture : IArrayTexture
    {
        ISrvBindable SubresourceSrv(int nSlice, int nMipmapLevel = 0);
        IRtvBindable SubresourceRtv(int nSlice, int nMipmapLevel = 0);
        IUavBindable SubresourceUav(int nSlice, int nMipmapLevel = 0);
    }

    internal interface ILinkedArrayTexture : IArrayTexture
    {
    }

    internal interface IDepthArrayTexture : IArrayTexture
    {
        ISrvBindable SubresourceSrv(int nSlice, int nMipmapLevel = 0);
        IDsvBindable SubresourceDsv(int nSlice, int nMipmapLevel = 0);
    }

    namespace Internal
    {
        internal class MyArraySubresourceInternal
        {
            protected IResource m_owner;
            protected int m_slice;
            protected int m_mipmap;
            protected Format m_format;

            public string Name
            {
                get { return m_owner.Name; }
            }

            public Resource Resource
            {
                get { return m_owner.Resource; }
            }

            public Vector2I Size
            {
                get { return m_owner.Size; }
            }

            public Vector3I Size3
            {
                get { return m_owner.Size3; }
            }

            public void Init(MyArrayTextureResource owner, int slice, int mipmap, Format srvFormat)
            {
                m_owner = owner;

                m_slice = slice;
                m_mipmap = mipmap;
                m_format = srvFormat;
            }
        }

        class MyArraySubresourceSrv : MyArraySubresourceInternal, ISrvArraySubresource
        {
            ShaderResourceView m_srv;

            public ShaderResourceView Srv
            {
                get { return m_srv; }
            }

            public void OnDeviceInit()
            {
                ShaderResourceViewDescription desc = new ShaderResourceViewDescription();
                desc.Format = m_format;
                desc.Dimension = ShaderResourceViewDimension.Texture2DArray;
                desc.Texture2DArray.ArraySize = 1;
                desc.Texture2DArray.FirstArraySlice = m_slice;
                desc.Texture2DArray.MipLevels = 1;
                desc.Texture2DArray.MostDetailedMip = m_mipmap;
                m_srv = new ShaderResourceView(MyRender11.Device, m_owner.Resource, desc);
            }

            public void OnDeviceEnd()
            {
                if (m_srv != null)
                {
                    m_srv.Dispose();
                    m_srv = null;
                }
            }
        }

        internal class MyArraySubresourceRtv : MyArraySubresourceInternal, IRtvArraySubresource
        {
            RenderTargetView m_rtv;

            public RenderTargetView Rtv
            {
                get { return m_rtv; }
            }

            public void OnDeviceInit()
            {
                RenderTargetViewDescription desc = new RenderTargetViewDescription();
                desc.Format = m_format;
                desc.Dimension = RenderTargetViewDimension.Texture2DArray;
                desc.Texture2DArray.ArraySize = 1;
                desc.Texture2DArray.FirstArraySlice = m_slice;
                desc.Texture2DArray.MipSlice = m_mipmap;
                m_rtv = new RenderTargetView(MyRender11.Device, m_owner.Resource, desc);
            }

            public void OnDeviceEnd()
            {
                if (m_rtv != null)
                {
                    m_rtv.Dispose();
                    m_rtv = null;
                }
            }
        }

        internal class MyArraySubresourceUav : MyArraySubresourceInternal, IUavArraySubresource
        {
            UnorderedAccessView m_uav;

            public UnorderedAccessView Uav
            {
                get { return m_uav; }
            }

            public void OnDeviceInit()
            {
                UnorderedAccessViewDescription desc = new UnorderedAccessViewDescription
                {
                    Format = m_format,
                    Dimension = UnorderedAccessViewDimension.Texture2DArray,
                    Texture2DArray =
                    {
                        ArraySize = 1,
                        FirstArraySlice = m_slice,
                        MipSlice = m_mipmap
                    }
                };
                m_uav = new UnorderedAccessView(MyRender11.Device, m_owner.Resource, desc);
            }

            public void OnDeviceEnd()
            {
                if (m_uav != null)
                {
                    m_uav.Dispose();
                    m_uav = null;
                }
            }
        }

        internal class MyArraySubresourceDepth : MyArraySubresourceInternal, IDsvBindable
        {
            DepthStencilView m_dsv;

            public DepthStencilView Dsv
            {
                get { return m_dsv; }
            }

            public void OnDeviceInit()
            {
                DepthStencilViewDescription desc = new DepthStencilViewDescription
                {
                    Format = m_format,
                    Flags = DepthStencilViewFlags.None,
                    Dimension = DepthStencilViewDimension.Texture2DArray,
                    Texture2DArray =
                    {
                        ArraySize = 1,
                        FirstArraySlice = m_slice,
                        MipSlice = m_mipmap
                    }
                };
                m_dsv = new DepthStencilView(MyRender11.Device, m_owner.Resource, desc);
                m_dsv.DebugName = m_owner.Name;
            }

            public void OnDeviceEnd()
            {
                if (m_dsv != null)
                {
                    m_dsv.Dispose();
                    m_dsv = null;
                }
            }
        }

        internal class MyArrayTextureResource : ISrvBindable
        {
            Resource m_resource;
            ShaderResourceView m_srv;

            protected string m_name;
            protected Texture2DDescription m_resourceDesc;
            protected ShaderResourceViewDescription m_srvDesc;

            MyArraySubresourceSrv[,] m_arraySubresourcesSrv;

            public Resource Resource
            {
                get { return m_resource; }
            }

            public string Name
            {
                get { return m_name; }
            }

            public Vector2I Size
            {
                get { return new Vector2I(m_resourceDesc.Width, m_resourceDesc.Height); }
            }

            public Vector3I Size3
            {
                get { return new Vector3I(m_resourceDesc.Width, m_resourceDesc.Height, m_resourceDesc.ArraySize); }
            }

            public int MipmapLevels
            {
                get { return m_resourceDesc.MipLevels; }
            }

            public int NumSlices
            {
                get { return m_resourceDesc.ArraySize; }
            }

            public ShaderResourceView Srv
            {
                get { return m_srv; }
            }

            public ISrvBindable SubresourceSrv(int id, int mipmapLevel = 0)
            {
                return m_arraySubresourcesSrv[id, mipmapLevel];
            }

            public void InitInternal(
                string name,
                Texture2DDescription resourceDesc,
                ShaderResourceViewDescription srvDesc)
            {
                m_name = name;
                m_resourceDesc = resourceDesc;
                m_srvDesc = srvDesc;
                m_arraySubresourcesSrv = new MyArraySubresourceSrv[NumSlices, MipmapLevels];
                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                    {
                        m_arraySubresourcesSrv[nSlice, nMipmap] = new MyArraySubresourceSrv();
                        m_arraySubresourcesSrv[nSlice, nMipmap].Init(this, nSlice, nMipmap, srvDesc.Format);
                    }
            }

            protected void OnDeviceInitInternal()
            {
                m_resource = new Texture2D(MyRender11.Device, m_resourceDesc);
                m_resource.DebugName = m_name;

                m_srv = new ShaderResourceView(MyRender11.Device, m_resource, m_srvDesc);
                m_srv.DebugName = m_name;

                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                    {
                        m_arraySubresourcesSrv[nSlice, nMipmap].OnDeviceInit();
                    }
            }

            protected void OnDeviceEndInternal()
            {
                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                    {
                        m_arraySubresourcesSrv[nSlice, nMipmap].OnDeviceEnd();
                    }

                if (m_srv != null)
                {
                    m_srv.Dispose();
                    m_srv = null;
                }
                if (m_resource != null)
                {
                    m_resource.Dispose();
                    m_resource = null;
                }
            }
        }

        // todo: this class should be replaced by MyFileTexture in future
        internal class MyLinkedArrayTexture : ILinkedArrayTexture
        {
            MyObjectsPool<List<ISrvBindable>> m_objectsPoolLists = new MyObjectsPool<List<ISrvBindable>>(8);

            string m_debugName;

            ShaderResourceView m_srv;
            Resource m_resource;

            List<ISrvBindable> m_sourceTextures;

            public string Name
            {
                get { return m_debugName; }
            }

            public Vector3I Size3
            {
                get
                {
                    return new Vector3I(m_sourceTextures[0].Size.X, m_sourceTextures[0].Size.Y, m_sourceTextures.Count);
                }
            }

            public Vector2I Size
            {
                get { return new Vector2I(Size3.X, Size.Y); }
            }

            public int NumSlices
            {
                get { return m_sourceTextures.Count; }
            }

            public int MipmapLevels
            {
                get
                {
                    MyRenderProxy.Assert(false, "Not implemented");
                    return -1;
                }
            }

            public Resource Resource
            {
                get { return m_resource; }
            }

            public ShaderResourceView Srv
            {
                get { return m_srv; }
            }
            
            public void InitLinked(string debugName, ISrvBindable[] textures)
            {
                m_debugName = debugName;

                ISrvBindable tex0 = textures[0];
                MyRenderProxy.Assert(textures.Length != 0);
                foreach (var tex in textures)
                {
                    MyRenderProxy.Assert(tex.Size3 == tex0.Size3);
                    MyRenderProxy.Assert(tex.Size3.Z == 1);
                }

                MyRenderProxy.Assert(m_sourceTextures == null);
                m_objectsPoolLists.AllocateOrCreate(out m_sourceTextures);
                m_sourceTextures.Clear();
                m_sourceTextures.AddRange(textures);
            }

            public void Destroy()
            {
                m_sourceTextures.Clear();
                m_objectsPoolLists.Deallocate(m_sourceTextures);
                m_sourceTextures = null;
            }

            public void OnDeviceInit()
            {
                ISrvBindable tex0 = m_sourceTextures[0];

                Texture2DDescription texDesc = new Texture2DDescription
                {
                    ArraySize = m_sourceTextures.Count,
                    BindFlags = BindFlags.ShaderResource,
                    CpuAccessFlags = CpuAccessFlags.None,
                    Format = tex0.Srv.Description.Format,
                    Height = tex0.Size.X,
                    MipLevels = tex0.Srv.Description.Texture2D.MipLevels,
                    OptionFlags = ResourceOptionFlags.None,
                    SampleDescription =
                    {
                        Count = 1,
                        Quality = 0
                    },
                    Usage = ResourceUsage.Default,
                    Width = tex0.Size.Y
                };
                m_resource = new Texture2D(MyRender11.Device, texDesc);
                m_resource.DebugName = m_debugName;

                ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
                {
                    Format = tex0.Srv.Description.Format,
                    Dimension = ShaderResourceViewDimension.Texture2DArray,
                    Texture2DArray =
                    {
                        ArraySize = m_sourceTextures.Count,
                        FirstArraySlice = 0,
                        MipLevels = tex0.Srv.Description.Texture2D.MipLevels,
                        MostDetailedMip = 0
                    }
                };
                m_srv = new ShaderResourceView(MyRender11.Device, m_resource, srvDesc);
                m_srv.DebugName = m_debugName;

                int i = 0;
                foreach (ISrvBindable tex in m_sourceTextures)
                {
                    Texture2D tex2 = (Texture2D) tex.Resource;
                    if (tex2 == null)
                    {
                        MyRenderProxy.Assert(false, "Array texture is created using resource that is not texture2d");
                        i++;
                        continue;
                    }
                    Texture2DDescription texDesc2 = tex2.Description;
                    MyRenderProxy.Assert(MyArrayTextureManager.CheckArrayCompatible(texDesc, texDesc2),
                        "Incompatible texture is used to create array texture");

                    int mipmaps = tex.Srv.Description.Texture2D.MipLevels;
                    for (int m = 0; m < mipmaps; m++)
                    {
                        MyRender11.RC.CopySubresourceRegion(tex,
                            Resource.CalculateSubResourceIndex(m, 0, mipmaps), null, Resource,
                            Resource.CalculateSubResourceIndex(m, i, mipmaps));
                    }
                    i++;
                }
            }

            public void OnDeviceEnd()
            {
                if (m_srv != null)
                {
                    m_srv.Dispose();
                    m_srv = null;
                }

                if (m_resource != null)
                {
                    m_resource.Dispose();
                    m_resource = null;
                }
            }
        }

        internal class MyRtvArrayTexture : MyArrayTextureResource, IRtvArrayTexture
        {
            MyArraySubresourceRtv[,] m_arraySubresourcesRtv;

            public RenderTargetView Rtv { get; private set; }

            public void InitRtv(string debugName, Texture2DDescription resourceDesc,
                ShaderResourceViewDescription srvDesc, Format rtvFormat)
            {
                base.InitInternal(debugName, resourceDesc, srvDesc);

                m_arraySubresourcesRtv = new MyArraySubresourceRtv[NumSlices, MipmapLevels];
                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                    {
                        m_arraySubresourcesRtv[nSlice, nMipmap] = new MyArraySubresourceRtv();
                        m_arraySubresourcesRtv[nSlice, nMipmap].Init(this, nSlice, nMipmap, rtvFormat);
                    }
            }

            public IRtvBindable SubresourceRtv(int faceId, int mipmapLevel = 0)
            {
                return m_arraySubresourcesRtv[faceId, mipmapLevel];
            }

            public void OnDeviceInit()
            {
                base.OnDeviceInitInternal();

                Rtv = new RenderTargetView(MyRender11.Device, base.Resource);

                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                    {
                        m_arraySubresourcesRtv[nSlice, nMipmap].OnDeviceInit();
                    }
            }

            public void OnDeviceEnd()
            {
                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                    {
                        m_arraySubresourcesRtv[nSlice, nMipmap].OnDeviceEnd();
                    }

                base.OnDeviceEndInternal();
            }
        }

        internal class MyUavArrayTexture : MyArrayTextureResource, IUavArrayTexture
        {
            MyArraySubresourceRtv[,] m_arraySubresourcesRtv;
            MyArraySubresourceUav[,] m_arraySubresourcesUav;

            public void InitUav(string debugName, Texture2DDescription resourceDesc,
                ShaderResourceViewDescription srvDesc, Format rtvFormat, Format uavFormat)
            {
                base.InitInternal(debugName, resourceDesc, srvDesc);

                m_arraySubresourcesRtv = new MyArraySubresourceRtv[NumSlices, MipmapLevels];
                m_arraySubresourcesUav = new MyArraySubresourceUav[NumSlices, MipmapLevels];
                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                    {
                        m_arraySubresourcesRtv[nSlice, nMipmap] = new MyArraySubresourceRtv();
                        m_arraySubresourcesRtv[nSlice, nMipmap].Init(this, nSlice, nMipmap, rtvFormat);
                        m_arraySubresourcesUav[nSlice, nMipmap] = new MyArraySubresourceUav();
                        m_arraySubresourcesUav[nSlice, nMipmap].Init(this, nSlice, nMipmap, uavFormat);
                    }
            }

            public IRtvBindable SubresourceRtv(int faceId, int mipmapLevel = 0)
            {
                return m_arraySubresourcesRtv[faceId, mipmapLevel];
            }

            public IUavBindable SubresourceUav(int faceId, int mipmapLevel = 0)
            {
                return m_arraySubresourcesUav[faceId, mipmapLevel];
            }

            public void OnDeviceInit()
            {
                base.OnDeviceInitInternal();

                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                    {
                        m_arraySubresourcesRtv[nSlice, nMipmap].OnDeviceInit();
                        m_arraySubresourcesUav[nSlice, nMipmap].OnDeviceInit();
                    }
            }

            public void OnDeviceEnd()
            {
                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                    {
                        m_arraySubresourcesRtv[nSlice, nMipmap].OnDeviceEnd();
                        m_arraySubresourcesUav[nSlice, nMipmap].OnDeviceEnd();
                    }

                base.OnDeviceEndInternal();
            }
        }

        internal class MyDepthArrayTexture : MyArrayTextureResource, IDepthArrayTexture
        {
            MyArraySubresourceDepth[,] m_arraySubresourcesDsv;

            public IDsvBindable SubresourceDsv(int nSlice, int mipmapLevel = 0)
            {
                return m_arraySubresourcesDsv[nSlice, mipmapLevel];
            }

            public void InitDepth(string debugName, Texture2DDescription resourceDesc,
                ShaderResourceViewDescription srvDesc, Format dsvFormat)
            {
                base.InitInternal(debugName, resourceDesc, srvDesc);

                m_arraySubresourcesDsv = new MyArraySubresourceDepth[NumSlices, MipmapLevels];
                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                    {
                        m_arraySubresourcesDsv[nSlice, nMipmap] = new MyArraySubresourceDepth();
                        m_arraySubresourcesDsv[nSlice, nMipmap].Init(this, nSlice, nMipmap, dsvFormat);
                    }
            }

            public virtual void OnDeviceInit()
            {
                base.OnDeviceInitInternal();

                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                        m_arraySubresourcesDsv[nSlice, nMipmap].OnDeviceInit();
            }

            public virtual void OnDeviceEnd()
            {
                for (int nSlice = 0; nSlice < NumSlices; nSlice++)
                    for (int nMipmap = 0; nMipmap < MipmapLevels; nMipmap++)
                        m_arraySubresourcesDsv[nSlice, nMipmap].OnDeviceEnd();

                base.OnDeviceEndInternal();
            }
        }
    }

    internal class MyArrayTextureManager : IManager, IManagerDevice
    {
        MyObjectsPool<MyLinkedArrayTexture> m_linkedArrays = new MyObjectsPool<MyLinkedArrayTexture>(16);
        MyObjectsPool<MyRtvArrayTexture> m_rtvArrays = new MyObjectsPool<MyRtvArrayTexture>(16);
        MyObjectsPool<MyUavArrayTexture> m_uavArrays = new MyObjectsPool<MyUavArrayTexture>(16);
        MyObjectsPool<MyDepthArrayTexture> m_depthArrays = new MyObjectsPool<MyDepthArrayTexture>(16);

        bool m_isDeviceInit = false;

        internal static bool CheckArrayCompatible(Texture2DDescription desc1, Texture2DDescription desc2)
        {
            return desc1.Format == desc2.Format &&
                   desc1.Width == desc2.Width &&
                   desc1.Height == desc2.Height &&
                   desc1.Format == desc2.Format &&
                   desc1.MipLevels == desc2.MipLevels &&
                   desc1.SampleDescription.Count == desc2.SampleDescription.Count &&
                   desc1.SampleDescription.Quality == desc2.SampleDescription.Quality;
        }

        internal IUavArrayTexture CreateUavCube(string debugName, int size, Format format, int mipmapLevels = 1)
        {
            MyRenderProxy.Assert(size > 0);
            MyRenderProxy.Assert(mipmapLevels > 0);

            MyUavArrayTexture tex;
            m_uavArrays.AllocateOrCreate(out tex);

            Texture2DDescription desc = new Texture2DDescription
            {
                ArraySize = 6,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = format,
                Height = size,
                MipLevels = mipmapLevels,
                OptionFlags = ResourceOptionFlags.TextureCube,
                SampleDescription =
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = ResourceUsage.Default,
                Width = size
            };

            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
            {
                Format = format,
                Dimension = ShaderResourceViewDimension.TextureCube,
                TextureCube =
                {
                    MipLevels = mipmapLevels,
                    MostDetailedMip = 0
                }
            };

            tex.InitUav(debugName, desc, srvDesc, format, format);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        internal IDepthArrayTexture CreateDepthCube(string debugName, int size, Format resourceFormat, Format srvFormat, Format dsvFormat, int mipmapLevels = 1)
        {
            MyRenderProxy.Assert(size > 0);
            MyRenderProxy.Assert(mipmapLevels > 0);

            MyDepthArrayTexture tex;
            m_depthArrays.AllocateOrCreate(out tex);

            Texture2DDescription desc = new Texture2DDescription
            {
                ArraySize = 6,
                BindFlags = BindFlags.ShaderResource | BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = resourceFormat,
                Height = size,
                MipLevels = mipmapLevels,
                OptionFlags = ResourceOptionFlags.TextureCube,
                SampleDescription =
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = ResourceUsage.Default,
                Width = size
            };

            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
            {
                Format = srvFormat,
                Dimension = ShaderResourceViewDimension.TextureCube,
                TextureCube =
                {
                    MipLevels = mipmapLevels,
                    MostDetailedMip = 0
                }
            };

            tex.InitDepth(debugName, desc, srvDesc, dsvFormat);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        internal ILinkedArrayTexture CreateLinkedArray(string debugName, ISrvBindable[] srvs)
        {
            MyLinkedArrayTexture tex;
            m_linkedArrays.AllocateOrCreate(out tex);
            tex.InitLinked(debugName, srvs);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        internal IRtvArrayTexture CreateRtvArray(string debugName, int width, int height, int arraySize,
            Format format, int mipmapLevels = 1)
        {
            MyRenderProxy.Assert(width > 0);
            MyRenderProxy.Assert(height > 0);
            MyRenderProxy.Assert(arraySize > 0);
            MyRenderProxy.Assert(mipmapLevels > 0);

            MyRtvArrayTexture tex;
            m_rtvArrays.AllocateOrCreate(out tex);

            Texture2DDescription desc = new Texture2DDescription
            {
                ArraySize = arraySize,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = format,
                Height = height,
                MipLevels = mipmapLevels,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription =
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = ResourceUsage.Default,
                Width = width
            };

            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
            {
                Format = format,
                Dimension = ShaderResourceViewDimension.Texture2DArray,
                Texture2DArray =
                {
                    ArraySize = arraySize,
                    FirstArraySlice = 0,
                    MipLevels = mipmapLevels,
                    MostDetailedMip = 0
                }
            };


            tex.InitRtv(debugName, desc, srvDesc, format);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        internal IUavArrayTexture CreateUavArray(string debugName, int width, int height, int arraySize,
            Format format, int mipmapLevels = 1)
        {
            MyRenderProxy.Assert(width > 0);
            MyRenderProxy.Assert(height > 0);
            MyRenderProxy.Assert(arraySize > 0);
            MyRenderProxy.Assert(mipmapLevels > 0); 
            
            MyUavArrayTexture tex;
            m_uavArrays.AllocateOrCreate(out tex);

            Texture2DDescription desc = new Texture2DDescription
            {
                ArraySize = arraySize,
                BindFlags = BindFlags.ShaderResource | BindFlags.RenderTarget | BindFlags.UnorderedAccess,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = format,
                Height = height,
                MipLevels = mipmapLevels,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription =
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = ResourceUsage.Default,
                Width = width
            };

            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
            {
                Format = format,
                Dimension = ShaderResourceViewDimension.Texture2DArray,
                Texture2DArray =
                {
                    ArraySize = arraySize,
                    FirstArraySlice = 0,
                    MipLevels = mipmapLevels,
                    MostDetailedMip = 0
                }
            };


            tex.InitUav(debugName, desc, srvDesc, format, format);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        internal IDepthArrayTexture CreateDepthArray(string debugName, int width, int height, int arraySize,
            Format resourceFormat, Format srvFormat, Format dsvFormat, int mipmapLevels = 1)
        {
            MyRenderProxy.Assert(width > 0);
            MyRenderProxy.Assert(height > 0);
            MyRenderProxy.Assert(arraySize > 0);
            MyRenderProxy.Assert(mipmapLevels > 0);

            MyDepthArrayTexture tex;
            m_depthArrays.AllocateOrCreate(out tex);

            Texture2DDescription desc = new Texture2DDescription
            {
                ArraySize = arraySize,
                BindFlags = BindFlags.ShaderResource | BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                Format = resourceFormat,
                Height = height,
                MipLevels = mipmapLevels,
                OptionFlags = ResourceOptionFlags.None,
                SampleDescription =
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = ResourceUsage.Default,
                Width = width
            };

            ShaderResourceViewDescription srvDesc = new ShaderResourceViewDescription
            {
                Format = srvFormat,
                Dimension = ShaderResourceViewDimension.Texture2DArray,
                Texture2DArray =
                {
                    ArraySize = arraySize,
                    FirstArraySlice = 0,
                    MipLevels = mipmapLevels,
                    MostDetailedMip = 0
                }
            };

            tex.InitDepth(debugName, desc, srvDesc, dsvFormat);

            if (m_isDeviceInit)
                tex.OnDeviceInit();

            return tex;
        }

        internal void DisposeTex(ref ILinkedArrayTexture texture)
        {
            if (texture == null)
                return;

            MyLinkedArrayTexture castedTexture = (MyLinkedArrayTexture) texture;

            if (m_isDeviceInit)
                castedTexture.OnDeviceEnd();

            m_linkedArrays.Deallocate(castedTexture);
            texture = null;
        }

        internal void DisposeTex(ref IRtvArrayTexture texture)
        {
            if (texture == null)
                return;

            MyRtvArrayTexture castedTexture = (MyRtvArrayTexture)texture;

            if (m_isDeviceInit)
                castedTexture.OnDeviceEnd();

            m_rtvArrays.Deallocate(castedTexture);
            texture = null;
        }

        internal void DisposeTex(ref IUavArrayTexture texture)
        {
            if (texture == null)
                return;

            MyUavArrayTexture castedTexture = (MyUavArrayTexture)texture;

            if (m_isDeviceInit)
                castedTexture.OnDeviceEnd();

            m_uavArrays.Deallocate(castedTexture);
            texture = null;
        }

        internal void DisposeTex(ref IDepthArrayTexture texture)
        {
            if (texture == null)
                return;

            MyDepthArrayTexture castedTexture = (MyDepthArrayTexture)texture;

            if (m_isDeviceInit)
                castedTexture.OnDeviceEnd();

            m_depthArrays.Deallocate(castedTexture);
            texture = null;
        }
        
        public void OnDeviceInit()
        {
            m_isDeviceInit = true;
            foreach (var tex in m_rtvArrays.Active)
                tex.OnDeviceInit();
            foreach (var tex in m_uavArrays.Active)
                tex.OnDeviceInit();
            foreach (var tex in m_depthArrays.Active)
                tex.OnDeviceInit();
        }

        public void OnDeviceReset()
        {
            foreach (var tex in m_rtvArrays.Active)
            {
                tex.OnDeviceEnd();
                tex.OnDeviceInit();
            }
            foreach (var tex in m_uavArrays.Active)
            {
                tex.OnDeviceEnd();
                tex.OnDeviceInit();
            }
            foreach (var tex in m_depthArrays.Active)
            {
                tex.OnDeviceEnd();
                tex.OnDeviceInit();
            }
        }

        public void OnDeviceEnd()
        {
            m_isDeviceInit = false;
            foreach (var tex in m_rtvArrays.Active)
                tex.OnDeviceEnd();
            foreach (var tex in m_uavArrays.Active)
                tex.OnDeviceEnd();
            foreach (var tex in m_depthArrays.Active)
                tex.OnDeviceEnd();
        }

        public int GetArrayTexturesCount()
        {
            int count = 0;
            count += m_linkedArrays.ActiveCount;
            count += m_rtvArrays.ActiveCount;
            count += m_uavArrays.ActiveCount;
            count += m_depthArrays.ActiveCount;
            return count;
        }
    }
}
