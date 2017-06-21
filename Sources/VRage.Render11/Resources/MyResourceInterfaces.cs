using System;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using VRageMath;
using Buffer = SharpDX.Direct3D11.Buffer;
using Resource = SharpDX.Direct3D11.Resource;

namespace VRage.Render11.Resources
{
    internal interface IResource
    {
        string Name { get; }

        Resource Resource { get; }

        Vector3I Size3 { get; }

        Vector2I Size { get; }
    }

    internal interface ITexture : ISrvBindable
    {
        Format Format { get; }
        int MipmapCount { get; }
    }

    internal interface IBuffer : IResource
    {
        /// <summary>
        /// It's the same as <see cref="Description"/>.SizeInBytes.
        /// </summary>
        int ByteSize { get; }
        int ElementCount { get; }

        BufferDescription Description { get; }
        Buffer Buffer { get; }

        // this method should not be called outside of the MyBufferManager
        void DisposeInternal();
    }


    internal interface ISrvBindable : IResource
    {
        ShaderResourceView Srv { get; }
    }

    internal interface IRtvBindable : IResource
    {
        RenderTargetView Rtv { get; }
    }

    internal interface IUavBindable : IResource
    {
        UnorderedAccessView Uav { get; }
    }

    internal interface ISrvUavBindable : ISrvBindable, IUavBindable
    { }

    internal interface IDsvBindable : IResource
    {
        DepthStencilView Dsv { get; }
    }
}
