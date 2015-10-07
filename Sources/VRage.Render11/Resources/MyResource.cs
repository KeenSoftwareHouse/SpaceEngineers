using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRageMath;
using Buffer = SharpDX.Direct3D11.Buffer;
using Resource = SharpDX.Direct3D11.Resource;
using Vector2 = VRageMath.Vector2;

namespace VRageRender.Resources
{
    abstract class MyResource
    {
        protected Resource m_resource;

        internal Resource Resource { get { return m_resource; } }
        internal Buffer Buffer { get { return (Buffer) m_resource; } }

        internal virtual void Dispose()
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

    class MyTextureArray : MyResource
    {
        internal ShaderResourceView ShaderView { get; set; }
        internal Vector2 Size { get; private set; }
        internal int ArrayLen { get; private set; }

        internal MyTextureArray()
        {
        }

        internal MyTextureArray(TexId[] mergeList)
        {
            var srcDesc = MyTextures.GetView(mergeList[0]).Description;
            Size = MyTextures.GetSize(mergeList[0]);
            ArrayLen = mergeList.Length;

            Texture2DDescription desc = new Texture2DDescription();
            desc.ArraySize = ArrayLen;
            desc.BindFlags = BindFlags.ShaderResource;
            desc.CpuAccessFlags = CpuAccessFlags.None;
            desc.Format = srcDesc.Format;
            desc.Height = (int)Size.Y;
            desc.Width = (int)Size.X;
            desc.MipLevels = 0;
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;
            desc.Usage = ResourceUsage.Default;
            m_resource = new Texture2D(MyRender11.Device, desc);

            // foreach mip
            var mipmaps = (int)Math.Log(Size.X, 2) + 1;

            for (int a = 0; a < ArrayLen; a++)
            {
                for (int m = 0; m < mipmaps; m++)
                {

                    if (((Texture2D)MyTextures.Textures.Data[mergeList[a].Index].Resource).Description.Format != ((Texture2D)Resource).Description.Format)
                    {
                        MyRender11.Log.WriteLine(String.Format("Inconsistent format in textures array {0}", MyTextures.Textures.Data[mergeList[a].Index].Name));
                    }

                    MyRender11.Context.CopySubresourceRegion(MyTextures.Textures.Data[mergeList[a].Index].Resource, Resource.CalculateSubResourceIndex(m, 0, mipmaps), null, Resource,
                        Resource.CalculateSubResourceIndex(m, a, mipmaps));
                }
            }

            ShaderView = new ShaderResourceView(MyRender11.Device, Resource);
        }

        internal override void Dispose()
        {
            if (ShaderView != null)
            {
                ShaderView.Dispose();
                ShaderView = null;
            }

            base.Dispose();
        }

        internal static MyTextureArray FromStringArray(string[] mergeList, MyTextureEnum type)
        {
            if (mergeList == null)
            {
                return null;
            }

            TexId[] ids = new TexId[mergeList.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = MyTextures.GetTexture(mergeList[i], type, true);
            }

            return new MyTextureArray(ids);
        }
    }
}

namespace VRageRender
{
    struct ConstantsBufferId
    {
        internal int Index;

        public static bool operator ==(ConstantsBufferId x, ConstantsBufferId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(ConstantsBufferId x, ConstantsBufferId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly ConstantsBufferId NULL = new ConstantsBufferId { Index = -1 };

        //
        public static implicit operator Buffer(ConstantsBufferId id)
        {
            return MyHwBuffers.GetConstantsBuffer(id);
        }
    }

    struct VertexBufferId
    {
        internal int Index;

        public static bool operator ==(VertexBufferId x, VertexBufferId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(VertexBufferId x, VertexBufferId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly VertexBufferId NULL = new VertexBufferId { Index = -1 };



        //
        internal Buffer Buffer { get { return MyHwBuffers.GetVertexBuffer(this); } }
        internal int Stride { get { return MyHwBuffers.GetVertexBufferStride(this); } }
        internal int Capacity { get { return MyHwBuffers.GetVertexBufferCapacity(this); } }
        internal int ByteSize { get { return MyHwBuffers.GetBufferDesc(this).SizeInBytes; } }
    }

    struct IndexBufferId
    {
        internal int Index;

        public static bool operator ==(IndexBufferId x, IndexBufferId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(IndexBufferId x, IndexBufferId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly IndexBufferId NULL = new IndexBufferId { Index = -1 };


        internal Buffer Buffer { get { return MyHwBuffers.GetIndexBuffer(this); } }
        internal Format Format { get { return MyHwBuffers.GetIndexBufferFormat(this); } }
        internal int Capacity { get { return MyHwBuffers.GetBufferDesc(this).SizeInBytes / FormatHelper.SizeOfInBytes(Format); } }
        internal int ByteSize { get { return MyHwBuffers.GetBufferDesc(this).SizeInBytes; } }
    }

    struct StructuredBufferId
    {
        internal int Index;

        public static bool operator ==(StructuredBufferId x, StructuredBufferId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(StructuredBufferId x, StructuredBufferId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly StructuredBufferId NULL = new StructuredBufferId { Index = -1 };


        internal Buffer Buffer { get { return MyHwBuffers.GetBuffer(this); } }
        internal int Capacity { get { return MyHwBuffers.GetBufferDesc(this).SizeInBytes / MyHwBuffers.GetBufferDesc(this).StructureByteStride; } }
        internal int ByteSize { get { return MyHwBuffers.GetBufferDesc(this).SizeInBytes; } }
        internal ShaderResourceView Srv { get { return MyHwBuffers.GetView(this); } }
    }


    struct MyHwBufferDesc
    {
        internal BufferDescription Description;
        internal string DebugName;
    }

    struct MyConstantBufferData
    {
        internal Buffer Buffer;
    }

    struct MyVertexBufferData
    {
        internal Buffer Buffer;
        internal int Stride;
    }

    struct MyIndexBufferData
    {
        internal Buffer Buffer;
        internal Format Format;
    }

    struct MyStructuredBufferData
    {
        internal Buffer Buffer;
        internal ShaderResourceView Srv;
    }

    static class MyHwBuffers
    {
        internal static void Init()
        {
            //MyCallbacks.RegisterDeviceEndListener(new OnDeviceEndDelegate(OnDeviceEnd));
            //MyCallbacks.RegisterDeviceResetListener(new OnDeviceResetDelegate(OnDeviceReset));
        }

        #region Constants buffer

        static HashSet<ConstantsBufferId> CbIndices = new HashSet<ConstantsBufferId>();
        static MyFreelist<MyHwBufferDesc> CBuffers = new MyFreelist<MyHwBufferDesc>(128);
        static MyConstantBufferData[] CBuffersData = new MyConstantBufferData[128];

        internal static ConstantsBufferId CreateConstantsBuffer(int size)
        {
            Debug.Assert(size == ((size + 15) / 16) * 16, "CB size not padded");

            BufferDescription desc = new BufferDescription();
            desc.BindFlags = BindFlags.ConstantBuffer;
            desc.CpuAccessFlags = CpuAccessFlags.Write;
            desc.SizeInBytes = size;
            desc.Usage = ResourceUsage.Dynamic;

            return CreateConstantsBuffer(desc);
        }

        internal static ConstantsBufferId CreateConstantsBuffer(BufferDescription description, string debugName = null)
        {
            var id = new ConstantsBufferId { Index = CBuffers.Allocate() };
            MyArrayHelpers.Reserve(ref CBuffersData, id.Index + 1);
            CBuffers.Data[id.Index] = new MyHwBufferDesc { Description = description, DebugName = debugName };

            CbIndices.Add(id);
            InitConstantsBuffer(id);

            return id;
        }

        internal static void InitConstantsBuffer(ConstantsBufferId id)
        {
            CBuffersData[id.Index].Buffer = new Buffer(MyRender11.Device, CBuffers.Data[id.Index].Description);
            if (CBuffers.Data[id.Index].DebugName != null)
            {
                CBuffersData[id.Index].Buffer.DebugName = CBuffers.Data[id.Index].DebugName;
            }
        }

        internal static Buffer GetConstantsBuffer(ConstantsBufferId id)
        {
            return CBuffersData[id.Index].Buffer;
        }

        #endregion

        #region Vertex buffer

        static HashSet<VertexBufferId> VbIndices = new HashSet<VertexBufferId>();
        static MyFreelist<MyHwBufferDesc> VBuffers = new MyFreelist<MyHwBufferDesc>(128);
        static MyVertexBufferData[] VBuffersData = new MyVertexBufferData[128];

        internal static VertexBufferId CreateVertexBuffer(int elements, int stride, IntPtr? data = null, string debugName = null)
        {
            return CreateVertexBuffer(elements, stride, BindFlags.VertexBuffer, ResourceUsage.Default, data, debugName);
        }

        internal static VertexBufferId CreateVertexBuffer(int elements, int stride, BindFlags bind, ResourceUsage usage, IntPtr? data = null, string debugName = null)
        {
            bind |= BindFlags.VertexBuffer;

            BufferDescription desc = new BufferDescription();
            desc.BindFlags = bind;
            desc.SizeInBytes = stride * elements;
            desc.Usage = usage;
            desc.CpuAccessFlags = usage == ResourceUsage.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None;

            return CreateVertexBuffer(desc, stride, data, debugName);
        }

        internal static VertexBufferId CreateVertexBuffer(BufferDescription description, int stride, IntPtr ? data = null, string debugName = null)
        {
            var id = new VertexBufferId { Index = VBuffers.Allocate() };
            
            MyArrayHelpers.Reserve(ref VBuffersData, id.Index + 1);
            VBuffers.Data[id.Index] = new MyHwBufferDesc { Description = description, DebugName = debugName };
            VBuffersData[id.Index] = new MyVertexBufferData { Stride = stride };

            VbIndices.Add(id);

            if(!data.HasValue)
            {
                InitVertexBuffer(id);
            }
            else
            {
                InitVertexBuffer(id, data.Value);
            }

            return id;
        }

        internal static void Destroy(VertexBufferId id)
        {
            Debug.Assert(VbIndices.Contains(id));
            VbIndices.Remove(id);
            if(VBuffersData[id.Index].Buffer != null)
            {
                VBuffersData[id.Index].Buffer.Dispose();
                VBuffersData[id.Index].Buffer = null;
            }
            VBuffers.Free(id.Index);
        }

        internal static void ResizeVertexBuffer(VertexBufferId id, int size)
        {
            VBuffersData[id.Index].Buffer.Dispose();
            VBuffers.Data[id.Index].Description.SizeInBytes = VBuffersData[id.Index].Stride * size;
            InitVertexBuffer(id);
        }

        internal static void InitVertexBuffer(VertexBufferId id)
        {
            VBuffersData[id.Index].Buffer = new Buffer(MyRender11.Device, VBuffers.Data[id.Index].Description);
            if (VBuffers.Data[id.Index].DebugName != null)
            {
                VBuffersData[id.Index].Buffer.DebugName = VBuffers.Data[id.Index].DebugName;
            }
        }

        internal static void InitVertexBuffer(VertexBufferId id, IntPtr data)
        {
            VBuffersData[id.Index].Buffer = new Buffer(MyRender11.Device, data, VBuffers.Data[id.Index].Description);
            if (VBuffers.Data[id.Index].DebugName != null)
            {
                VBuffersData[id.Index].Buffer.DebugName = VBuffers.Data[id.Index].DebugName;
            }
        }

        internal static Buffer GetVertexBuffer(VertexBufferId id)
        {
            return VBuffersData[id.Index].Buffer;
        }

        internal static int GetVertexBufferStride(VertexBufferId id)
        {
            return VBuffersData[id.Index].Stride;
        }

        internal static int GetVertexBufferCapacity(VertexBufferId id)
        {
            return VBuffers.Data[id.Index].Description.SizeInBytes / VBuffersData[id.Index].Stride;
        }

        internal static BufferDescription GetBufferDesc(VertexBufferId id)
        {
            return VBuffers.Data[id.Index].Description;
        }

        #endregion

        #region Index buffer

        static HashSet<IndexBufferId> IbIndices = new HashSet<IndexBufferId>();
        static MyFreelist<MyHwBufferDesc> IBuffers = new MyFreelist<MyHwBufferDesc>(128);
        static MyIndexBufferData[] IBuffersData = new MyIndexBufferData[128];

        internal static IndexBufferId CreateIndexBuffer(int elements, Format format, IntPtr? data = null, string debugName = null)
        {
            return CreateIndexBuffer(elements, format, BindFlags.IndexBuffer, ResourceUsage.Default, data, debugName);
        }

        internal static IndexBufferId CreateIndexBuffer(int elements, Format format, BindFlags bind, ResourceUsage usage, IntPtr? data = null, string debugName = null)
        {
            bind |= BindFlags.IndexBuffer;

            Debug.Assert(format == Format.R32_UInt || format == Format.R16_UInt);

            BufferDescription desc = new BufferDescription();
            desc.BindFlags = bind;
            desc.SizeInBytes = elements * (format == Format.R32_UInt ? 4 : 2);
            desc.Usage = usage;
            desc.CpuAccessFlags = usage == ResourceUsage.Dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None;

            return CreateIndexBuffer(desc, format, data, debugName);
        }

        internal static IndexBufferId CreateIndexBuffer(BufferDescription description, Format format, IntPtr ? data = null, string debugName = null)
        {
            var id = new IndexBufferId { Index = IBuffers.Allocate() };
            MyArrayHelpers.Reserve(ref IBuffersData, id.Index + 1);
            IBuffers.Data[id.Index] = new MyHwBufferDesc { Description = description, DebugName = debugName };
            IBuffersData[id.Index] = new MyIndexBufferData { Format = format };

            IbIndices.Add(id);

            if (!data.HasValue)
            {
                InitIndexBuffer(id);
            }
            else
            {
                InitIndexBuffer(id, data.Value);
            }

            return id;
        }

        internal static void InitIndexBuffer(IndexBufferId id)
        {
            IBuffersData[id.Index].Buffer = new Buffer(MyRender11.Device, IBuffers.Data[id.Index].Description);
            if (IBuffers.Data[id.Index].DebugName != null)
            {
                IBuffersData[id.Index].Buffer.DebugName = IBuffers.Data[id.Index].DebugName;
            }
        }

        internal static void InitIndexBuffer(IndexBufferId id, IntPtr data)
        {
            IBuffersData[id.Index].Buffer = new Buffer(MyRender11.Device, data, IBuffers.Data[id.Index].Description);
            if (IBuffers.Data[id.Index].DebugName != null)
            {
                IBuffersData[id.Index].Buffer.DebugName = IBuffers.Data[id.Index].DebugName;
            }
        }

        internal static void Destroy(IndexBufferId id)
        {
            IbIndices.Remove(id);
            if (IBuffersData[id.Index].Buffer != null)
            {
                IBuffersData[id.Index].Buffer.Dispose();
                IBuffersData[id.Index].Buffer = null;
            }
            IBuffers.Free(id.Index);
        }

        internal static Buffer GetIndexBuffer(IndexBufferId id)
        {
            return IBuffersData[id.Index].Buffer;
        }

        internal static Format GetIndexBufferFormat(IndexBufferId id)
        {
            return IBuffersData[id.Index].Format;
        }

        internal static BufferDescription GetBufferDesc(IndexBufferId id)
        {
            return IBuffers.Data[id.Index].Description;
        }

        #endregion

        #region Structured buffer

        static HashSet<StructuredBufferId> SbIndices = new HashSet<StructuredBufferId>();
        static MyFreelist<MyHwBufferDesc> SBuffers = new MyFreelist<MyHwBufferDesc>(128);
        static MyStructuredBufferData[] SBuffersData = new MyStructuredBufferData[128];

        internal static StructuredBufferId CreateStructuredBuffer(int elements, int stride, bool dynamic, IntPtr? data = null, string debugName = null)
        {
            return CreateStructuredBuffer(new BufferDescription { 
                BindFlags = BindFlags.ShaderResource, 
                OptionFlags = ResourceOptionFlags.BufferStructured,
                CpuAccessFlags = dynamic ? CpuAccessFlags.Write : CpuAccessFlags.None,
                SizeInBytes = elements * stride,
                StructureByteStride = stride,
                Usage = dynamic ? ResourceUsage.Dynamic : ResourceUsage.Default
            }, data, debugName);
        }

        internal static StructuredBufferId CreateStructuredBuffer(BufferDescription description, IntPtr? data = null, string debugName = null)
        {
            var id = new StructuredBufferId { Index = SBuffers.Allocate() };
            MyArrayHelpers.Reserve(ref SBuffersData, id.Index + 1);
            SBuffers.Data[id.Index] = new MyHwBufferDesc { Description = description, DebugName = debugName };
            SBuffersData[id.Index] = new MyStructuredBufferData { };

            SbIndices.Add(id);

            if (!data.HasValue)
            {
                InitStructuredBuffer(id);
            }
            else
            {
                InitStructuredBuffer(id, data.Value);
            }

            return id;
        }

        internal static void InitStructuredBuffer(StructuredBufferId id)
        {
            SBuffersData[id.Index].Buffer = new Buffer(MyRender11.Device, SBuffers.Data[id.Index].Description);
            SBuffersData[id.Index].Srv = new ShaderResourceView(MyRender11.Device, SBuffersData[id.Index].Buffer);
            if (SBuffers.Data[id.Index].DebugName != null)
            {
                SBuffersData[id.Index].Buffer.DebugName = SBuffers.Data[id.Index].DebugName;
                SBuffersData[id.Index].Srv.DebugName = SBuffers.Data[id.Index].DebugName;
            }
        }

        internal static void InitStructuredBuffer(StructuredBufferId id, IntPtr data)
        {
            SBuffersData[id.Index].Buffer = new Buffer(MyRender11.Device, data, SBuffers.Data[id.Index].Description);
            SBuffersData[id.Index].Srv = new ShaderResourceView(MyRender11.Device, SBuffersData[id.Index].Buffer);
            if (SBuffers.Data[id.Index].DebugName != null)
            {
                SBuffersData[id.Index].Buffer.DebugName = SBuffers.Data[id.Index].DebugName;
                SBuffersData[id.Index].Srv.DebugName = SBuffers.Data[id.Index].DebugName;
            }
        }

        internal static void Destroy(StructuredBufferId id)
        {
            SbIndices.Remove(id);
            if (SBuffersData[id.Index].Buffer != null)
            {
                SBuffersData[id.Index].Buffer.Dispose();
                SBuffersData[id.Index].Buffer = null;
            }
            if (SBuffersData[id.Index].Srv != null)
            {
                SBuffersData[id.Index].Srv.Dispose();
                SBuffersData[id.Index].Srv = null;
            }
            SBuffers.Free(id.Index);
        }

        internal static Buffer GetBuffer(StructuredBufferId id)
        {
            return SBuffersData[id.Index].Buffer;
        }

        internal static ShaderResourceView GetView(StructuredBufferId id)
        {
            return id != StructuredBufferId.NULL ? SBuffersData[id.Index].Srv : null;
        }

        internal static BufferDescription GetBufferDesc(StructuredBufferId id)
        {
            return SBuffers.Data[id.Index].Description;
        }

        #endregion

        internal static void OnDeviceEnd()
        {
            // drop all resources
            foreach (var id in CbIndices)
            {
                if (CBuffersData[id.Index].Buffer != null)
                {
                    CBuffersData[id.Index].Buffer.Dispose();
                    CBuffersData[id.Index].Buffer = null;
                }
            }
            foreach (var id in VbIndices)
            {
                if (VBuffersData[id.Index].Buffer != null)
                { 
                    VBuffersData[id.Index].Buffer.Dispose();
                    VBuffersData[id.Index].Buffer = null;
                }
            }
            foreach (var id in IbIndices)
            {
                if (IBuffersData[id.Index].Buffer != null)
                {
                    IBuffersData[id.Index].Buffer.Dispose();
                    IBuffersData[id.Index].Buffer = null;
                }
            }
            foreach (var id in SbIndices)
            {
                if (SBuffersData[id.Index].Buffer != null)
                {
                    SBuffersData[id.Index].Buffer.Dispose();
                    SBuffersData[id.Index].Buffer = null;
                    SBuffersData[id.Index].Srv.Dispose();
                    SBuffersData[id.Index].Srv = null;
                }
            }
        }

        internal static void OnDeviceReset()
        {
            OnDeviceEnd();

            // recreate all resources
            foreach (var id in CbIndices)
            {
                InitConstantsBuffer(id);
            }
            foreach (var id in VbIndices)
            {
                if(GetBufferDesc(id).Usage != ResourceUsage.Immutable)
                {
                    InitVertexBuffer(id);
                }
            }
            foreach (var id in IbIndices)
            {
                if (GetBufferDesc(id).Usage != ResourceUsage.Immutable)
                {
                    InitIndexBuffer(id);
                }
            }
            foreach (var id in SbIndices)
            {
                if (GetBufferDesc(id).Usage != ResourceUsage.Immutable)
                {
                    InitStructuredBuffer(id);
                }
            }
        }
    }


    struct RasterizerId
    {
        internal int Index;

        public static bool operator ==(RasterizerId x, RasterizerId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(RasterizerId x, RasterizerId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly RasterizerId NULL = new RasterizerId { Index = -1 };


        // 
        public static implicit operator RasterizerState(RasterizerId id)
        {
            return MyPipelineStates.GetRasterizer(id);
        }
    }

    struct SamplerId
    {
        internal int Index;

        public static bool operator ==(SamplerId x, SamplerId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(SamplerId x, SamplerId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly SamplerId NULL = new SamplerId { Index = -1 };



        // 
        public static implicit operator SamplerState(SamplerId id)
        {
            return MyPipelineStates.GetSampler(id);
        }
    }

    struct BlendId
    {
        internal int Index;

        public static bool operator ==(BlendId x, BlendId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(BlendId x, BlendId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly BlendId NULL = new BlendId { Index = -1 };


        // 
        public static implicit operator BlendState(BlendId id)
        {
            return MyPipelineStates.GetBlend(id);
        }
    }

    struct DepthStencilId
    {
        internal int Index;

        public static bool operator ==(DepthStencilId x, DepthStencilId y)
        {
            return x.Index == y.Index;
        }

        public static bool operator !=(DepthStencilId x, DepthStencilId y)
        {
            return x.Index != y.Index;
        }

        internal static readonly DepthStencilId NULL = new DepthStencilId { Index = -1 };


        // 
        public static implicit operator DepthStencilState(DepthStencilId id)
        {
            return MyPipelineStates.GetDepthStencil(id);
        }
    }

    static class MyPipelineStates
    {
        static HashSet<BlendId> BlendIndices = new HashSet<BlendId>();
        static MyFreelist<BlendStateDescription> BlendStates = new MyFreelist<BlendStateDescription>(128);
        static BlendState[] BlendObjects = new BlendState[128];

        static HashSet<SamplerId> SamplerIndices = new HashSet<SamplerId>();
        static MyFreelist<SamplerStateDescription> SamplerStates = new MyFreelist<SamplerStateDescription>(128);
        static SamplerState[] SamplerObjects = new SamplerState[128];

        static HashSet<RasterizerId> RasterizerIndices = new HashSet<RasterizerId>();
        static MyFreelist<RasterizerStateDescription> RasterizerStates = new MyFreelist<RasterizerStateDescription>(128);
        static RasterizerState[] RasterizerObjects = new RasterizerState[128];

        static HashSet<DepthStencilId> DepthStencilIndices = new HashSet<DepthStencilId>();
        static MyFreelist<DepthStencilStateDescription> DepthStencilStates = new MyFreelist<DepthStencilStateDescription>(128);
        static DepthStencilState[] DepthStencilObjects = new DepthStencilState[128];

        #region Blend states
        internal static BlendId CreateBlendState(BlendStateDescription description)
        {
            var id = new BlendId { Index = BlendStates.Allocate() };
            MyArrayHelpers.Reserve(ref BlendObjects, id.Index + 1);

            BlendStates.Data[id.Index] = description.Clone();

            InitBlendState(id);
            BlendIndices.Add(id);

            return id;
        }

        internal static void InitBlendState(BlendId id)
        {
            BlendObjects[id.Index] = new BlendState(MyRender11.Device, BlendStates.Data[id.Index]);
        }

        internal static BlendState GetBlend(BlendId id)
        {
            return BlendObjects[id.Index];
        }
        #endregion

        #region Sampler states

        internal static SamplerId CreateSamplerState(SamplerStateDescription description)
        {
            var id = new SamplerId { Index = SamplerStates.Allocate() };
            MyArrayHelpers.Reserve(ref SamplerObjects, id.Index + 1);

            SamplerStates.Data[id.Index] = description;

            InitSamplerState(id);
            SamplerIndices.Add(id);

            return id;
        }

        internal static void ChangeSamplerState(SamplerId id, SamplerStateDescription description)
        {
            SamplerStates.Data[id.Index] = description;
            if(SamplerObjects[id.Index] != null)
            {
                SamplerObjects[id.Index].Dispose();
            }
            SamplerObjects[id.Index] = new SamplerState(MyRender11.Device, description);
        }

        internal static void InitSamplerState(SamplerId id)
        {
            SamplerObjects[id.Index] = new SamplerState(MyRender11.Device, SamplerStates.Data[id.Index]);
        }

        internal static SamplerState GetSampler(SamplerId id)
        {
            return SamplerObjects[id.Index];
        }

        #endregion

        #region Rasterizer states

        internal static RasterizerId CreateRasterizerState(RasterizerStateDescription description)
        {
            var id = new RasterizerId { Index = RasterizerStates.Allocate() };
            MyArrayHelpers.Reserve(ref RasterizerObjects, id.Index + 1);

            RasterizerStates.Data[id.Index] = description;

            InitRasterizerState(id);
            RasterizerIndices.Add(id);

            return id;
        }

        internal static void InitRasterizerState(RasterizerId id)
        {
            if (RasterizerObjects[id.Index] != null)
            {
                RasterizerObjects[id.Index].Dispose();
                RasterizerObjects[id.Index] = null;
            }
            RasterizerObjects[id.Index] = new RasterizerState(MyRender11.Device, RasterizerStates.Data[id.Index]);
        }

        internal static void Modify(RasterizerId id, RasterizerStateDescription desc)
        {
            RasterizerStates.Data[id.Index] = desc;

            InitRasterizerState(id);
        }

        internal static RasterizerState GetRasterizer(RasterizerId id)
        {
            return RasterizerObjects[id.Index];
        }

        #endregion

        #region Depth stencil states

        internal static DepthStencilId CreateDepthStencil(DepthStencilStateDescription description)
        {
            var id = new DepthStencilId { Index = DepthStencilStates.Allocate() };
            MyArrayHelpers.Reserve(ref DepthStencilObjects, id.Index + 1);

            DepthStencilStates.Data[id.Index] = description;

            InitDepthStencilState(id);
            DepthStencilIndices.Add(id);

            return id;
        }

        internal static void InitDepthStencilState(DepthStencilId id)
        {
            DepthStencilObjects[id.Index] = new DepthStencilState(MyRender11.Device, DepthStencilStates.Data[id.Index]);
        }

        internal static DepthStencilState GetDepthStencil(DepthStencilId id)
        {
            return DepthStencilObjects[id.Index];
        }

        #endregion

        internal static void Init()
        {
            //MyCallbacks.RegisterDeviceEndListener(new OnDeviceEndDelegate(OnDeviceEnd));
            //MyCallbacks.RegisterDeviceResetListener(new OnDeviceResetDelegate(OnDeviceReset));
        }

        internal static void OnDeviceEnd()
        {
            foreach(var id in BlendIndices)
            {
                if (BlendObjects[id.Index] != null)
                { 
                    BlendObjects[id.Index].Dispose();
                    BlendObjects[id.Index] = null;
                }
            }

            foreach (var id in DepthStencilIndices)
            {
                if (DepthStencilObjects[id.Index] != null)
                {
                    DepthStencilObjects[id.Index].Dispose();
                    DepthStencilObjects[id.Index] = null;
                }
            }

            foreach (var id in RasterizerIndices)
            {
                if (RasterizerObjects[id.Index] != null)
                {
                    RasterizerObjects[id.Index].Dispose();
                    RasterizerObjects[id.Index] = null;
                }
            }

            foreach (var id in SamplerIndices)
            {
                if (SamplerObjects[id.Index] != null)
                {
                    SamplerObjects[id.Index].Dispose();
                    SamplerObjects[id.Index] = null;
                }
            }
        }

        internal static void OnDeviceReset()
        {
            OnDeviceEnd();

            foreach (var id in BlendIndices)
            {
                InitBlendState(id);
            }

            foreach (var id in DepthStencilIndices)
            {
                InitDepthStencilState(id);
            }

            foreach (var id in RasterizerIndices)
            {
                InitRasterizerState(id);
            }

            foreach (var id in SamplerIndices)
            {
                InitSamplerState(id);
            }
        }
    }
}