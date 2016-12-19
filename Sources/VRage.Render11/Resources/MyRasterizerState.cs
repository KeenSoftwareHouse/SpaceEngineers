using SharpDX.Direct3D11;
using VRage.Render11.Resources.Internal;
using VRageRender;

namespace VRage.Render11.Resources
{
    internal interface IRasterizerState : IMyPersistentResource<RasterizerStateDescription>
    {
    }

    internal interface IRasterizerStateInternal : IRasterizerState
    {
        RasterizerState Resource { get; }
    }

    namespace Internal
    {
        internal class MyRasterizerState : MyPersistentResource<RasterizerState, RasterizerStateDescription>, IRasterizerStateInternal
        {
            protected override RasterizerState CreateResource(ref RasterizerStateDescription description)
            {
                RasterizerState ret = new RasterizerState(MyRender11.Device, description);
                ret.DebugName = Name;
                return ret;
            }
        }
    }

    internal class MyRasterizerStateManager : MyPersistentResourceManager<MyRasterizerState, RasterizerStateDescription>
    {
        internal static IRasterizerState NocullRasterizerState;
        internal static IRasterizerState InvTriRasterizerState;
        internal static IRasterizerState WireframeRasterizerState;
        internal static IRasterizerState LinesRasterizerState;
        internal static IRasterizerState DecalRasterizerState;
        internal static IRasterizerState NocullDecalRasterizerState;
        internal static IRasterizerState CascadesRasterizerState;
        internal static IRasterizerState CascadesRasterizerStateOld;
        internal static IRasterizerState ShadowRasterizerState;
        internal static IRasterizerState NocullWireframeRasterizerState;
        internal static IRasterizerState ScissorTestRasterizerState;

        protected override int GetAllocResourceCount()
        {
            return 16;
        }

        public MyRasterizerStateManager()
        {
            RasterizerStateDescription desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Wireframe;
            desc.CullMode = CullMode.Back;
            MyRasterizerStateManager.WireframeRasterizerState = CreateResource("WireframeRasterizerState", ref desc);

            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.Front;
            MyRasterizerStateManager.InvTriRasterizerState = CreateResource("InvTriRasterizerState", ref desc);

            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            MyRasterizerStateManager.NocullRasterizerState = CreateResource("NocullRasterizerState", ref desc);

            desc.FillMode = FillMode.Wireframe;
            desc.CullMode = CullMode.None;
            MyRasterizerStateManager.NocullWireframeRasterizerState = CreateResource("NocullWireframeRasterizerState", ref desc);

            desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.Back;
            MyRasterizerStateManager.LinesRasterizerState = CreateResource("LinesRasterizerState", ref desc);

            desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.Back;
            desc.DepthBias = 25000;
            desc.DepthBiasClamp = 2;
            desc.SlopeScaledDepthBias = 0;
            MyRasterizerStateManager.DecalRasterizerState = CreateResource("DecalRasterizerState", ref desc);

            desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            desc.DepthBias = 25000;
            desc.DepthBiasClamp = 2;
            desc.SlopeScaledDepthBias = 0;
            MyRasterizerStateManager.NocullDecalRasterizerState = CreateResource("NocullDecalRasterizerState", ref desc);

            desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            desc.IsFrontCounterClockwise = true;
            desc.DepthBias = 0;
            desc.DepthBiasClamp = 0;
            desc.SlopeScaledDepthBias = 0;
            MyRasterizerStateManager.CascadesRasterizerState = CreateResource("CascadesRasterizerState", ref desc);

            desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            desc.IsFrontCounterClockwise = true;
            desc.DepthBias = 20;
            desc.DepthBiasClamp = 2;
            desc.SlopeScaledDepthBias = 4;
            MyRasterizerStateManager.CascadesRasterizerStateOld = CreateResource("CascadesRasterizerStateOld", ref desc);

            desc = new RasterizerStateDescription();
            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.None;
            desc.IsFrontCounterClockwise = true;
            desc.DepthBias = 2500;
            desc.DepthBiasClamp = 10000;
            desc.SlopeScaledDepthBias = 4;
            MyRasterizerStateManager.ShadowRasterizerState = CreateResource("ShadowRasterizerState", ref desc);

            desc.FillMode = FillMode.Solid;
            desc.CullMode = CullMode.Back;
            desc.IsFrontCounterClockwise = false;
            desc.IsScissorEnabled = true;
            MyRasterizerStateManager.ScissorTestRasterizerState = CreateResource("ScissorTestRasterizerState", ref desc);
        }
    }
}