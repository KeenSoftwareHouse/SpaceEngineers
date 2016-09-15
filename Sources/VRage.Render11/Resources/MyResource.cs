using SharpDX.Direct3D11;
using Resource = SharpDX.Direct3D11.Resource;
using VRageMath;

namespace VRage.Render11.Resources
{
    internal interface IResource
    {
        string Name { get; }

        Resource Resource { get; }

        Vector3I Size3 { get; }

        Vector2I Size { get; }
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

    internal interface IDsvBindable : IResource
    {
        DepthStencilView Dsv { get; }
    }
}
