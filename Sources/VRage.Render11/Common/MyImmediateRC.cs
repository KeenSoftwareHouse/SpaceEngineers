using VRage.Render11.RenderContext;
using VRageRender;

namespace VRage.Render11.Common
{
    class MyImmediateRC
    {
        internal static MyRenderContext RC { get { return MyRender11.RC; } }
    }
}
